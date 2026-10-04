using Sirenix.OdinInspector;

namespace RAXY.Notification
{
    public abstract class NotificationRequest
    {
        [ShowInInspector]
        public string DefinitionId { get; }

        [ShowInInspector]
        public string Tag { get; protected set; }

        protected NotificationRequest(string definitionId)
        {
            DefinitionId = definitionId;
        }
    }
}
