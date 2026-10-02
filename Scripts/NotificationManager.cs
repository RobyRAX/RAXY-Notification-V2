using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RAXY.Notification
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [SerializeField, FormerlySerializedAs("catalog")]
        List<NotificationDefinition> notificationDefinitions = new();

        [TitleGroup("Runtime")]
        [ShowInInspector]
        [HideReferenceObjectPicker]
        readonly List<NotificationLane> lanes = new();

        readonly Dictionary<string, NotificationLane> lanesById = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Multiple NotificationService instances are active. Keeping the first one.", this);
                return;
            }

            Instance = this;
            BuildLanes();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnValidate()
        {
            var seen = new HashSet<string>();
            for (var i = 0; i < notificationDefinitions.Count; i++)
            {
                var definition = notificationDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                {
                    Debug.LogWarning("A notification entry has an empty Id.", this);
                    continue;
                }

                if (!seen.Add(definition.Id))
                    Debug.LogWarning($"Multiple notification entries share Id '{definition.Id}'.", this);
            }
        }

        public static void Show(NotificationRequest request)
        {
            if (Instance == null)
            {
                Debug.LogError("NotificationService.Show was called, but no NotificationService is in the scene.");
                return;
            }

            Instance.Enqueue(request);
        }

        void BuildLanes()
        {
            lanes.Clear();
            lanesById.Clear();

            for (var i = 0; i < notificationDefinitions.Count; i++)
            {
                var definition = notificationDefinitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                    continue;

                if (lanesById.ContainsKey(definition.Id))
                {
                    Debug.LogError($"Multiple notification entries share Id '{definition.Id}'. That entry was skipped.", this);
                    continue;
                }

                var lane = new NotificationLane(definition);
                lanes.Add(lane);
                lanesById.Add(definition.Id, lane);

                if (definition.Behaviour == NotificationBehaviour.Fullscreen && definition.FullscreenView != null)
                    definition.FullscreenView.Hide();
            }
        }

        void Enqueue(NotificationRequest request)
        {
            if (request == null)
            {
                Debug.LogError("NotificationService.Show received a null request.", this);
                return;
            }

            if (!lanesById.TryGetValue(request.DefinitionId ?? string.Empty, out var lane))
            {
                Debug.LogError($"No notification entry with Id '{request.DefinitionId}' is in the NotificationService catalog.", this);
                return;
            }

            lane.Queue.Enqueue(request);
            Pump(lane);
        }

        void Pump(NotificationLane lane)
        {
            if (lane.Pumping || lane.Spacing != null)
                return;

            if (!CanPresent(lane))
                return;

            lane.Pumping = true;
            try
            {
                do
                {
                    if (!CanPresent(lane))
                        return;

                    var wait = SecondsUntilNextPresent(lane);
                    if (wait > 0f)
                    {
                        lane.Spacing = StartCoroutine(PresentAfterDelay(lane, wait));
                        return;
                    }

                    Present(lane, lane.Queue.Dequeue());
                    lane.LastPresentedTime = Time.time;
                }
                while (ShowsAsList(lane.Definition));
            }
            finally
            {
                lane.Pumping = false;
            }
        }

        bool CanPresent(NotificationLane lane)
        {
            if (lane.Queue.Count == 0)
                return false;

            if (ShowsAsList(lane.Definition))
                return true;

            return lane.Live.Count == 0;
        }

        float SecondsUntilNextPresent(NotificationLane lane)
        {
            if (lane.LastPresentedTime < 0f)
                return 0f;

            var minimum = Mathf.Max(0f, lane.Definition.Interval);
            var elapsed = Time.time - lane.LastPresentedTime;
            return Mathf.Max(0f, minimum - elapsed);
        }

        IEnumerator PresentAfterDelay(NotificationLane lane, float delay)
        {
            yield return new WaitForSeconds(delay);
            lane.Spacing = null;
            Pump(lane);
        }

        void Present(NotificationLane lane, NotificationRequest request)
        {
            var definition = lane.Definition;
            if (definition.Behaviour == NotificationBehaviour.Fullscreen)
            {
                PresentExisting(lane, request);
                return;
            }

            PresentSpawned(lane, request);
        }

        void PresentExisting(NotificationLane lane, NotificationRequest request)
        {
            var definition = lane.Definition;
            var view = definition.FullscreenView;
            if (view == null)
            {
                Debug.LogError($"Notification '{definition.Id}' has no fullscreen view.", this);
                return;
            }

            if (!TryBind(lane, view, request, null))
                return;
        }

        void PresentSpawned(NotificationLane lane, NotificationRequest request)
        {
            var definition = lane.Definition;
            if (definition.SpawnPrefab == null)
            {
                Debug.LogError($"Notification '{definition.Id}' has no spawn prefab.", this);
                return;
            }

            if (definition.Container == null)
            {
                Debug.LogError($"Notification '{definition.Id}' has no container.", this);
                return;
            }

            var spawned = Instantiate(definition.SpawnPrefab, definition.Container, false);
            var view = spawned.GetComponent<NotificationView>();
            if (view == null)
                view = spawned.GetComponentInChildren<NotificationView>(true);

            if (view == null)
            {
                Debug.LogError($"Spawn prefab '{definition.SpawnPrefab.name}' for '{definition.Id}' has no NotificationView.", this);
                Destroy(spawned);
                return;
            }

            if (!TryBind(lane, view, request, spawned))
                return;

            StartTimer(lane, view, Mathf.Max(0f, definition.Lifetime));
        }

        bool TryBind(NotificationLane lane, NotificationView view, NotificationRequest request, GameObject spawned)
        {
            var live = new LiveNotification
            {
                View = view,
                Instance = spawned
            };
            lane.Live.Add(live);

            try
            {
                view.BindRequest(request, () => Complete(lane, view));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, view);
                lane.Live.Remove(live);
                if (spawned != null)
                    Destroy(spawned);
                else
                    view.Hide();

                return false;
            }

            if (!lane.Live.Contains(live))
                return false;

            view.Show();
            return true;
        }

        void Complete(NotificationLane lane, NotificationView view)
        {
            var live = FindLive(lane, view);
            if (live == null || live.Exiting)
                return;

            live.Exiting = true;
            if (live.Routine != null)
            {
                StopCoroutine(live.Routine);
                live.Routine = null;
            }

            if (view == null)
            {
                Finish(lane, live);
                return;
            }

            view.PlayExit(() => Finish(lane, live));
        }

        void Finish(NotificationLane lane, LiveNotification live)
        {
            if (!lane.Live.Contains(live))
                return;

            lane.Live.Remove(live);
            if (live.View != null)
                live.View.Hide();

            if (live.Instance != null)
                Destroy(live.Instance);

            if (!lane.Pumping)
                Pump(lane);
        }

        void StartTimer(NotificationLane lane, NotificationView view, float lifetime)
        {
            var live = FindLive(lane, view);
            if (live == null)
                return;

            live.Remaining = lifetime;
            live.Routine = StartCoroutine(RunTimer(lane, live));
        }

        IEnumerator RunTimer(NotificationLane lane, LiveNotification live)
        {
            while (live.Remaining > 0f)
            {
                live.Remaining -= Time.deltaTime;
                yield return null;
            }

            live.Routine = null;
            if (live.View != null)
                Complete(lane, live.View);
        }

        static bool ShowsAsList(NotificationDefinition definition)
        {
            return definition.Behaviour == NotificationBehaviour.NonFullscreen
                && definition.Flow == NotificationFlow.List;
        }

        static LiveNotification FindLive(NotificationLane lane, NotificationView view)
        {
            for (var i = 0; i < lane.Live.Count; i++)
            {
                if (lane.Live[i].View == view)
                    return lane.Live[i];
            }

            return null;
        }

        class NotificationLane
        {
            public NotificationLane(NotificationDefinition definition)
            {
                Definition = definition;
            }

            public NotificationDefinition Definition { get; }

            [ShowInInspector]
            string NotificationId => Definition.Id;

            [ShowInInspector]
            public float LastPresentedTime { get; set; } = -1f;

            [ShowInInspector]
            public bool Pumping { get; set; }

            [ShowInInspector]
            public Queue<NotificationRequest> Queue { get; } = new();

            [ShowInInspector]
            public List<LiveNotification> Live { get; } = new();

            public Coroutine Spacing { get; set; }
        }

        class LiveNotification
        {
            public NotificationView View;
            public GameObject Instance;
            public Coroutine Routine;
            public float Remaining;
            public bool Exiting;
        }
    }
}
