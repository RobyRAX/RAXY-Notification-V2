using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RAXY.Notification.Editor
{
    [CustomPropertyDrawer(typeof(NotificationIdAttribute))]
    public class NotificationIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var ids = NotificationIdLookup.All();
            if (ids.Length == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var options = new List<string> { string.Empty };
            options.AddRange(ids);
            var current = property.stringValue ?? string.Empty;
            if (!string.IsNullOrEmpty(current) && !options.Contains(current))
                options.Add(current);

            var labels = new string[options.Count];
            for (var i = 0; i < options.Count; i++)
                labels[i] = string.IsNullOrEmpty(options[i]) ? "(None)" : options[i];

            var index = Mathf.Max(0, options.IndexOf(current));
            var next = EditorGUI.Popup(position, label.text, index, labels);
            if (next >= 0 && next < options.Count)
                property.stringValue = options[next];
        }
    }
}
