namespace RAXY.Notification
{
    public readonly struct NotificationDismissedInfo
    {
        public NotificationDismissedInfo(
            string definitionId,
            NotificationBehaviour behaviour,
            NotificationRequest request)
        {
            DefinitionId = definitionId;
            Behaviour = behaviour;
            Request = request;
            Tag = request?.Tag;
        }

        public string DefinitionId { get; }

        public NotificationBehaviour Behaviour { get; }

        public NotificationRequest Request { get; }

        public string Tag { get; }
    }
}
