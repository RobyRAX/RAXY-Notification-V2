namespace RAXY.Notification
{
    public abstract class NotificationRequest
    {
        public string DefinitionId { get; }

        protected NotificationRequest(string definitionId)
        {
            DefinitionId = definitionId;
        }
    }
}
