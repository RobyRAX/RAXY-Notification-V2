using Sirenix.OdinInspector;

namespace RAXY.Notification
{
    public abstract class NotificationRequest
    {
        [ShowInInspector]
        public string DefinitionId { get; }

        protected NotificationRequest(string definitionId)
        {
            DefinitionId = definitionId;
        }
    }
}
