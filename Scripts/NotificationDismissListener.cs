using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace RAXY.Notification
{
    public sealed class NotificationDismissListener : MonoBehaviour
    {
        [NotificationId]
        [SerializeField]
        string definitionId;

        [Tooltip("Optional. When set, only dismissals with this tag invoke the event. Leave empty to match any tag.")]
        [ValueDropdown(nameof(NotificationTagOptions), AppendNextDrawer = true)]
        [FormerlySerializedAs("tag")]
        [SerializeField]
        string notificationTag;

        [SerializeField]
        UnityEvent onDismissed = new();

        void Awake()
        {
            NotificationEvents.Dismissed += HandleDismissed;
        }

        void OnDestroy()
        {
            NotificationEvents.Dismissed -= HandleDismissed;
        }

        void HandleDismissed(NotificationDismissedInfo info)
        {
            if (!MatchesFilter(info))
                return;

            onDismissed.Invoke();
        }

        bool MatchesFilter(NotificationDismissedInfo info)
        {
            if (!string.IsNullOrEmpty(definitionId) && info.DefinitionId != definitionId)
                return false;

            if (string.IsNullOrEmpty(notificationTag))
                return true;

            return info.Tag == notificationTag;
        }

        static IList<string> NotificationTagOptions()
        {
            return NotificationTagLookup.All();
        }
    }
}
