using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RAXY.Notification.Editor
{
    public static class NotificationRequestGenerator
    {
        public static bool TryGenerate(IReadOnlyList<NotificationEntry> entries, out string message)
        {
            if (entries == null || entries.Count == 0)
            {
                message = "Add at least one Notification Id.";
                return false;
            }

            var settings = NotificationEditorSettings.instance;
            var folder = NormalizeFolder(settings.GeneratedFolder);
            if (folder == null)
            {
                message = "Script folder must be a path under Assets, without '..'.";
                return false;
            }

            var namespaceName = settings.GeneratedNamespace;
            if (!IsNamespace(namespaceName))
            {
                message = $"Namespace '{namespaceName}' is not a valid C# namespace.";
                return false;
            }

            var ids = new List<string>();
            var plans = new List<GeneratedEntry>();
            var lockedEntries = new List<NotificationEntry>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            var lockedCount = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var id = entry == null ? string.Empty : (entry.id ?? string.Empty).Trim();
                if (!IsIdentifier(id))
                {
                    message = $"Entry {i + 1} needs an Id that is a valid C# identifier.";
                    return false;
                }

                if (!seenIds.Add(id))
                {
                    message = $"Id '{id}' is duplicated.";
                    return false;
                }

                ids.Add(id);
                if (entry.locked)
                {
                    lockedCount++;
                    lockedEntries.Add(entry);
                    continue;
                }

                if (!TryBuildFields(entry, i, out var fields, out message))
                    return false;

                plans.Add(new GeneratedEntry(entry, id, id + "Request", id + "View", entry.behaviour, fields));
            }

            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var absoluteFolder = Path.GetFullPath(Path.Combine(projectRoot, folder));
            Directory.CreateDirectory(absoluteFolder);

            if (!NotificationScriptPaths.ResolveNotificationIdPath(
                    settings.NotificationIdScriptPath,
                    namespaceName,
                    folder,
                    out var idPath,
                    out message))
                return false;

            WriteFile(idPath, BuildIdSource(namespaceName, ids));
            settings.NotificationIdScriptPath = idPath;

            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddExpectedInFolder(expected, folder, idPath);

            for (var i = 0; i < lockedEntries.Count; i++)
                AddLockedEntryExpected(lockedEntries[i], folder, expected);

            var replacedOutsideFolder = 0;
            var writtenInFolder = NotificationScriptPaths.IsUnderFolder(idPath, folder) ? 1 : 0;
            if (!NotificationScriptPaths.IsUnderFolder(idPath, folder))
                replacedOutsideFolder++;

            for (var i = 0; i < plans.Count; i++)
            {
                var plan = plans[i];
                if (!NotificationScriptPaths.ResolveRequestPath(plan.SourceEntry, plan.Id, folder, out var requestPath, out message))
                    return false;

                if (!NotificationScriptPaths.ResolveViewPath(plan.SourceEntry, plan.Id, folder, out var viewPath, out message))
                    return false;

                plan.SourceEntry.requestScriptPath = requestPath;
                plan.SourceEntry.viewScriptPath = viewPath;

                WriteFile(requestPath, BuildRequestSource(namespaceName, plan));
                WriteFile(viewPath, BuildViewSource(namespaceName, plan));

                if (NotificationScriptPaths.IsUnderFolder(requestPath, folder))
                {
                    writtenInFolder++;
                    AddExpectedInFolder(expected, folder, requestPath);
                }
                else
                    replacedOutsideFolder++;

                if (NotificationScriptPaths.IsUnderFolder(viewPath, folder))
                {
                    writtenInFolder++;
                    AddExpectedInFolder(expected, folder, viewPath);
                }
                else
                    replacedOutsideFolder++;
            }

            var removed = DeleteOrphans(absoluteFolder, expected);
            settings.SaveSettings();
            AssetDatabase.Refresh();

            var builder = new StringBuilder();
            builder.Append($"Updated NotificationId and generated {plans.Count} request class");
            builder.Append(plans.Count == 1 ? "" : "es");
            builder.Append($", and {plans.Count} view class");
            builder.Append(plans.Count == 1 ? "" : "es");
            builder.Append('.');
            if (replacedOutsideFolder > 0)
            {
                builder.Append(" Replaced ");
                builder.Append(replacedOutsideFolder);
                builder.Append(replacedOutsideFolder == 1 ? " existing script" : " existing scripts");
                builder.Append(" outside the Script Folder.");
            }

            if (writtenInFolder > 0)
            {
                builder.Append(" Wrote ");
                builder.Append(writtenInFolder);
                builder.Append(writtenInFolder == 1 ? " script" : " scripts");
                builder.Append(" in ");
                builder.Append(folder);
                builder.Append('.');
            }

            if (lockedCount > 0)
            {
                builder.Append(" Skipped ");
                builder.Append(lockedCount);
                builder.Append(lockedCount == 1 ? " locked entry." : " locked entries.");
            }

            if (removed.Count > 0)
            {
                builder.Append(" Removed outdated files:");
                for (var i = 0; i < removed.Count; i++)
                    builder.Append(" ").Append(removed[i]);
            }

            message = builder.ToString();
            return true;
        }

        static void AddExpectedInFolder(HashSet<string> expected, string folder, string assetPath)
        {
            if (!NotificationScriptPaths.IsUnderFolder(assetPath, folder))
                return;

            expected.Add(NotificationScriptPaths.NormalizeAssetPath(assetPath));
        }

        static void AddLockedEntryExpected(NotificationEntry entry, string folder, HashSet<string> expected)
        {
            if (entry == null)
                return;

            AddExpectedInFolder(expected, folder, entry.requestScriptPath);
            AddExpectedInFolder(expected, folder, entry.viewScriptPath);
        }

        static void WriteFile(string assetPath, string source)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var absolutePath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            File.WriteAllText(absolutePath, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath);
        }

        static string BuildIdSource(string namespaceName, List<string> ids)
        {
            var builder = new StringBuilder();
            AppendHeader(builder);
            builder.Append("namespace ").Append(namespaceName).Append("\n{\n");
            builder.Append("    public static class NotificationId\n    {\n");
            for (var i = 0; i < ids.Count; i++)
            {
                builder.Append("        public const string ");
                builder.Append(ids[i]);
                builder.Append(" = \"");
                builder.Append(ids[i]);
                builder.Append("\";\n");
            }

            builder.Append("    }\n}\n");
            return builder.ToString();
        }

        static string BuildRequestSource(string namespaceName, GeneratedEntry entry)
        {
            var builder = new StringBuilder();
            AppendHeader(builder);
            if (namespaceName != "RAXY.Notification")
                builder.Append("using RAXY.Notification;\n\n");

            builder.Append("namespace ").Append(namespaceName).Append("\n{\n");
            builder.Append("    public sealed class ").Append(entry.RequestClass).Append(" : NotificationRequest\n");
            builder.Append("    {\n");

            for (var i = 0; i < entry.Fields.Count; i++)
            {
                builder.Append("        public ");
                builder.Append(entry.Fields[i].TypeName);
                builder.Append(' ');
                builder.Append(entry.Fields[i].PropertyName);
                builder.Append(" { get; }\n");
            }

            if (entry.Fields.Count > 0)
                builder.Append('\n');

            if (entry.Fields.Count > 0)
            {
                builder.Append("        public ").Append(entry.RequestClass).Append("()\n");
                builder.Append("            : base(NotificationId.").Append(entry.Id).Append(")\n");
                builder.Append("        {\n");
                builder.Append("        }\n\n");
            }

            builder.Append("        public ").Append(entry.RequestClass).Append('(');
            if (entry.Fields.Count == 0)
            {
                builder.Append(")\n");
            }
            else
            {
                builder.Append('\n');
                for (var i = 0; i < entry.Fields.Count; i++)
                {
                    builder.Append("            ");
                    builder.Append(entry.Fields[i].TypeName);
                    builder.Append(' ');
                    builder.Append(entry.Fields[i].ParameterName);
                    if (i < entry.Fields.Count - 1)
                        builder.Append(',');
                    builder.Append('\n');
                }

                builder.Append("        )\n");
            }

            builder.Append("            : base(NotificationId.").Append(entry.Id).Append(")\n");
            builder.Append("        {\n");
            for (var i = 0; i < entry.Fields.Count; i++)
            {
                builder.Append("            ");
                builder.Append(entry.Fields[i].PropertyName);
                builder.Append(" = ");
                builder.Append(entry.Fields[i].ParameterName);
                builder.Append(";\n");
            }

            builder.Append("        }\n");
            builder.Append("    }\n}\n");
            return builder.ToString();
        }

        static string BuildViewSource(string namespaceName, GeneratedEntry entry)
        {
            var builder = new StringBuilder();
            AppendHeader(builder);
            var needsText = false;
            var needsImage = false;
            for (var i = 0; i < entry.Fields.Count; i++)
            {
                if (entry.Fields[i].Kind == NotificationFieldKind.Text)
                    needsText = true;
                else if (entry.Fields[i].Kind == NotificationFieldKind.Image)
                    needsImage = true;
            }

            builder.Append("using UnityEngine;\n");
            if (needsImage)
                builder.Append("using UnityEngine.UI;\n");
            if (needsText)
                builder.Append("using TMPro;\n");
            if (namespaceName != "RAXY.Notification")
                builder.Append("using RAXY.Notification;\n");
            builder.Append('\n');

            var baseName = entry.Behaviour == NotificationBehaviour.Fullscreen
                ? "FullscreenNotificationView"
                : "NotificationView";

            builder.Append("namespace ").Append(namespaceName).Append("\n{\n");
            builder.Append("    public class ").Append(entry.ViewClass).Append(" : ");
            builder.Append(baseName).Append('<').Append(entry.RequestClass).Append(">\n");
            builder.Append("    {\n");
            for (var i = 0; i < entry.Fields.Count; i++)
            {
                var field = entry.Fields[i];
                var viewField = ViewFieldName(field);
                builder.Append("        [SerializeField]\n");
                builder.Append("        ");
                builder.Append(field.Kind == NotificationFieldKind.Text ? "TextMeshProUGUI" : "Image");
                builder.Append(' ');
                builder.Append(viewField);
                builder.Append(";\n\n");
            }

            builder.Append("        protected override void Bind(").Append(entry.RequestClass).Append(" request)\n");
            builder.Append("        {\n");
            for (var i = 0; i < entry.Fields.Count; i++)
            {
                var field = entry.Fields[i];
                var viewField = ViewFieldName(field);
                var member = field.Kind == NotificationFieldKind.Text ? "text" : "sprite";
                builder.Append("            if (").Append(viewField).Append(" != null)\n");
                builder.Append("                ").Append(viewField).Append('.').Append(member);
                builder.Append(" = request.").Append(field.PropertyName).Append(";\n");
            }

            builder.Append("        }\n");
            builder.Append("    }\n}\n");
            return builder.ToString();
        }

        static string ViewFieldName(EmittedField field)
        {
            var suffix = field.Kind == NotificationFieldKind.Text ? "Tmp" : "Img";
            return field.PropertyName + suffix;
        }

        static bool TryBuildFields(NotificationEntry entry, int entryIndex, out List<EmittedField> fields, out string message)
        {
            fields = new List<EmittedField>();
            message = null;
            var source = entry == null || entry.fields == null
                ? new List<NotificationFieldDefinition>()
                : entry.fields;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < source.Count; i++)
            {
                var field = source[i];
                var name = field == null ? string.Empty : (field.fieldName ?? string.Empty).Trim();
                if (!IsIdentifier(name))
                {
                    message = $"Entry {entryIndex + 1}, field {i + 1} needs a valid C# name.";
                    return false;
                }

                if (!seen.Add(name))
                {
                    message = $"Entry {entryIndex + 1} duplicates field '{name}'.";
                    return false;
                }

                if (!TryResolveBinding(field, out var requestType, out message))
                    return false;

                fields.Add(new EmittedField(name, ToParameterName(name), requestType, field.kind));
            }

            return true;
        }

        static bool TryResolveBinding(NotificationFieldDefinition field, out string requestType, out string message)
        {
            switch (field.kind)
            {
                case NotificationFieldKind.Text:
                    requestType = "string";
                    message = null;
                    return true;
                case NotificationFieldKind.Image:
                    requestType = "UnityEngine.Sprite";
                    message = null;
                    return true;
                default:
                    requestType = null;
                    message = $"Field '{field.fieldName}' has an unsupported component.";
                    return false;
            }
        }

        static string ToParameterName(string propertyName)
        {
            var parameterName = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            if (parameterName == propertyName)
                parameterName += "Value";
            return parameterName;
        }

        static void AppendHeader(StringBuilder builder)
        {
            builder.Append("// -----------------------------------------------------------------------------\n");
            builder.Append("// <auto-generated>\n");
            builder.Append("//     This file was generated by the Notification Framework.\n");
            builder.Append("// </auto-generated>\n");
            builder.Append("// -----------------------------------------------------------------------------\n\n");
        }

        static List<string> DeleteOrphans(string absoluteFolder, HashSet<string> expectedAssetPaths)
        {
            var removed = new List<string>();
            if (!Directory.Exists(absoluteFolder))
                return removed;

            var files = Directory.GetFiles(absoluteFolder, "*.cs", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < files.Length; i++)
            {
                var assetPath = ToAssetPath(files[i]);
                if (assetPath == null || expectedAssetPaths.Contains(assetPath))
                    continue;

                var contents = File.ReadAllText(files[i]);
                if (!contents.Contains(NotificationScriptPaths.GeneratedMarker))
                    continue;

                if (AssetDatabase.DeleteAsset(assetPath))
                    removed.Add(assetPath);
            }

            return removed;
        }

        static string ToAssetPath(string absolutePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var full = Path.GetFullPath(absolutePath);
            if (!full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            var relative = full.Substring(projectRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relative.Replace('\\', '/');
        }

        static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            var normalized = folder.Replace('\\', '/').Trim().TrimEnd('/');
            if (normalized.Contains(".."))
                return null;

            if (normalized != "Assets" && !normalized.StartsWith("Assets/"))
                return null;

            if (normalized == "Assets")
                return null;

            return normalized;
        }

        static bool IsNamespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Trim().Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!IsIdentifier(parts[i]))
                    return false;
            }

            return parts.Length > 0;
        }

        readonly struct EmittedField
        {
            public EmittedField(string propertyName, string parameterName, string typeName, NotificationFieldKind kind)
            {
                PropertyName = propertyName;
                ParameterName = parameterName;
                TypeName = typeName;
                Kind = kind;
            }

            public string PropertyName { get; }
            public string ParameterName { get; }
            public string TypeName { get; }
            public NotificationFieldKind Kind { get; }
        }

        readonly struct GeneratedEntry
        {
            public GeneratedEntry(
                NotificationEntry sourceEntry,
                string id,
                string requestClass,
                string viewClass,
                NotificationBehaviour behaviour,
                List<EmittedField> fields)
            {
                SourceEntry = sourceEntry;
                Id = id;
                RequestClass = requestClass;
                ViewClass = viewClass;
                Behaviour = behaviour;
                Fields = fields;
            }

            public NotificationEntry SourceEntry { get; }
            public string Id { get; }
            public string RequestClass { get; }
            public string ViewClass { get; }
            public NotificationBehaviour Behaviour { get; }
            public List<EmittedField> Fields { get; }
        }

        static bool IsIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            if (!char.IsLetter(value[0]) && value[0] != '_')
                return false;

            for (var i = 1; i < value.Length; i++)
            {
                if (!char.IsLetterOrDigit(value[i]) && value[i] != '_')
                    return false;
            }

            return true;
        }
    }
}
