using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RAXY.Notification
{
    public enum NotificationBehaviour
    {
        NonFullscreen,
        Fullscreen
    }

    public enum NotificationFlow
    {
        Wait,
        List
    }

    [Serializable]
    public class NotificationDefinition
    {
        [NotificationId]
        [SerializeField]
        string id;

        [SerializeField]
        NotificationBehaviour behaviour;

        [ShowIf(nameof(IsNonFullscreen))]
        [SerializeField]
        NotificationFlow flow = NotificationFlow.Wait;

        [ShowIf(nameof(IsNonFullscreen))]
        [SerializeField, FormerlySerializedAs("prefab")]
        GameObject spawnPrefab;

        [ShowIf(nameof(IsNonFullscreen))]
        [SerializeField]
        RectTransform container;

        [ShowIf(nameof(IsNonFullscreen))]
        [SerializeField, FormerlySerializedAs("duration")]
        float lifetime = 3f;

        [ShowIf(nameof(IsNonFullscreen))]
        [SerializeField, FormerlySerializedAs("minimumDelay")]
        float interval;

        [ShowIf(nameof(IsFullscreen))]
        [SerializeField, FormerlySerializedAs("instance")]
        FullscreenNotificationView fullscreenView;

        public string Id => id;
        public NotificationBehaviour Behaviour => behaviour;
        public NotificationFlow Flow => flow;
        public GameObject SpawnPrefab => spawnPrefab;
        public RectTransform Container => container;
        public float Lifetime => lifetime;
        public float Interval => interval;
        public FullscreenNotificationView FullscreenView => fullscreenView;


        bool IsNonFullscreen => behaviour == NotificationBehaviour.NonFullscreen;
        bool IsFullscreen => behaviour == NotificationBehaviour.Fullscreen;

    }

    public sealed class NotificationIdAttribute : PropertyAttribute
    {
    }

    public sealed class NotificationTagAttribute : PropertyAttribute
    {
    }
}
