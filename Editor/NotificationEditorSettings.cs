using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RAXY.Notification.Editor
{
    [Serializable]
    public class NotificationEntry
    {
        [FormerlySerializedAs("notificationId")]
        public string id;

        public NotificationBehaviour behaviour;

        public NotificationFlow flow = NotificationFlow.Wait;

        public float lifetime = 3f;

        [FormerlySerializedAs("minimumDelay")]
        public float interval;

        public bool locked;

        public List<NotificationFieldDefinition> fields = new();
    }

    [FilePath("ProjectSettings/RaxyNotificationEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NotificationEditorSettings : ScriptableSingleton<NotificationEditorSettings>
    {
        public const string DefaultGeneratedFolder = "Assets/RAXY Generated";
        public const string DefaultGeneratedNamespace = "RAXY.Notification";

        [SerializeField]
        string generatedFolder = DefaultGeneratedFolder;

        [SerializeField]
        string generatedNamespace = DefaultGeneratedNamespace;

        [SerializeField]
        string activePrefabGuid = string.Empty;

        [SerializeField]
        List<NotificationEntry> entries = new();

        public string GeneratedFolder
        {
            get => string.IsNullOrWhiteSpace(generatedFolder)
                ? DefaultGeneratedFolder
                : generatedFolder;
            set => generatedFolder = value;
        }

        public string GeneratedNamespace
        {
            get => string.IsNullOrWhiteSpace(generatedNamespace)
                ? DefaultGeneratedNamespace
                : generatedNamespace;
            set => generatedNamespace = value ?? string.Empty;
        }

        public string ActivePrefabGuid
        {
            get => activePrefabGuid ?? string.Empty;
            set => activePrefabGuid = value ?? string.Empty;
        }

        public List<NotificationEntry> Entries
        {
            get
            {
                if (entries == null)
                    entries = new List<NotificationEntry>();
                return entries;
            }
        }

        public void SaveSettings() => Save(true);
    }
}
