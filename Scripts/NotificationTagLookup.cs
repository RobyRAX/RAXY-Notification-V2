using System;
using System.Collections.Generic;
using System.Reflection;

namespace RAXY.Notification
{
    public static class NotificationTagLookup
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
            Type fallback = null;
            for (var i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                if (types == null)
                    continue;

                for (var t = 0; t < types.Length; t++)
                {
                    var type = types[t];
                    if (type == null || !string.Equals(type.Name, "NotificationTag", StringComparison.Ordinal))
                        continue;

                    if (string.Equals(type.FullName, "RAXY.Notification.NotificationTag", StringComparison.Ordinal))
                        return type;

                    fallback ??= type;
                }
            }

            return fallback;
        }
    }
}
