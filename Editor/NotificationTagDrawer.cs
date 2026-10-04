using UnityEditor;
using UnityEngine;

namespace RAXY.Notification.Editor
{
    [CustomPropertyDrawer(typeof(NotificationTagAttribute))]
    public class NotificationTagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var tags = NotificationTagLookup.All();
            if (tags.Length == 0)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var fieldRect = EditorGUI.PrefixLabel(position, label);
            var buttonWidth = 18f;
            var gap = 2f;
            var textRect = new Rect(fieldRect.x, fieldRect.y, Mathf.Max(0f, fieldRect.width - buttonWidth - gap), fieldRect.height);
            var buttonRect = new Rect(textRect.xMax + gap, fieldRect.y, buttonWidth, fieldRect.height);

            var serializedObject = property.serializedObject;
            var propertyPath = property.propertyPath;

            EditorGUI.BeginProperty(position, label, property);
            property.stringValue = EditorGUI.TextField(textRect, property.stringValue ?? string.Empty);

            if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, EditorStyles.popup))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("(None)"), string.IsNullOrEmpty(property.stringValue), () =>
                    SetTag(serializedObject, propertyPath, string.Empty));

                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    menu.AddItem(new GUIContent(tag), property.stringValue == tag, () =>
                        SetTag(serializedObject, propertyPath, tag));
                }

                menu.DropDown(buttonRect);
            }

            EditorGUI.EndProperty();
        }

        static void SetTag(SerializedObject serializedObject, string propertyPath, string value)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
                return;

            property.stringValue = value ?? string.Empty;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
