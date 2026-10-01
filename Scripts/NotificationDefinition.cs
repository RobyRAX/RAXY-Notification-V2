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

        [SerializeField]
        NotificationFlow flow = NotificationFlow.Wait;

        [SerializeField, FormerlySerializedAs("prefab")]
        GameObject spawnPrefab;

        [SerializeField]
        RectTransform container;

        [SerializeField, FormerlySerializedAs("duration")]
        float lifetime = 3f;

        [SerializeField, FormerlySerializedAs("minimumDelay")]
        float interval;

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

    }

    public sealed class NotificationIdAttribute : PropertyAttribute
    {
    }
}
