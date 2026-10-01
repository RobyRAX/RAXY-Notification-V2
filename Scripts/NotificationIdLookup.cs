using System;
using System.Collections.Generic;
using System.Reflection;

namespace RAXY.Notification
{
    public static class NotificationIdLookup
    {
        public static string[] All()
        {
            var type = FindGeneratedType();
            if (type == null)
                return Array.Empty<string>();

            var values = new List<string>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (!field.IsLiteral || field.IsInitOnly || field.FieldType != typeof(string))
                    continue;

                if (field.GetRawConstantValue() is string value && !string.IsNullOrEmpty(value))
                    values.Add(value);
            }

            return values.ToArray();
        }

        static Type FindGeneratedType()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType("RAXY.Notification.NotificationId");
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
