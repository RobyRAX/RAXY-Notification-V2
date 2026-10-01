using System;
using System.Collections.Generic;
using UnityEngine;

namespace RAXY.Notification.Editor
{
    public enum NotificationFieldKind
    {
        Text,
        Image
    }

    [Serializable]
    public class NotificationFieldDefinition
    {
        public string fieldName;
        public NotificationFieldKind kind;
    }

    [Serializable]
    public class NotificationRequestSchema : ScriptableObject
    {
        public string requestName;
        public string definitionId;
        public string outputFolder = "Assets/RAXY Generated";
        public List<NotificationFieldDefinition> fields = new();
    }
}
