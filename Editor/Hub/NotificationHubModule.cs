using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using RAXY.Utility.Editor.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RAXY.Notification.Editor
{
    public sealed class NotificationHubModule : IRaxyHubModule
    {
        const string ManagerPrefabName = "Notification Manager.prefab";

        readonly List<GameObject> _prefabs = new();
        Vector2 _scroll;
        int _selectedPrefabIndex;

        public string Id => "notification";
        public string DisplayName => "Notification";
        public int Order => 110;

        public void OnEnable()
        {
            RefreshPrefabs();
        }

        public void OnDisable()
        {
        }

        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
                DrawManagerSection();
                EditorGUILayout.Space(10f);
                DrawEntriesSection();
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawManagerSection()
        {
            RaxyHubGui.BeginCard();
            EditorGUILayout.LabelField("Notification Manager", EditorStyles.boldLabel);

            if (_prefabs.Count == 0)
            {
                RaxyHubGui.DrawStatusBanner(
                    false,
                    "No Notification Manager prefab",
                    "Generate one in " + NotificationEditorSettings.instance.GeneratedFolder + ".");

                if (RaxyHubGui.PrimaryButton("Generate"))
                    GenerateManagerPrefab();

                RaxyHubGui.EndCard();
                return;
            }

            var selected = SelectedPrefab();
            if (_prefabs.Count == 1)
            {
                RaxyHubGui.DrawStatusBanner(true, "Prefab ready", AssetDatabase.GetAssetPath(selected));
                RememberPrefab(selected);
            }
            else
            {
                RaxyHubGui.DrawStatusBanner(
                    true,
                    $"{_prefabs.Count} Notification Manager prefabs found",
                    "Select which Notification Manager prefab belongs to this project.");

                var labels = new string[_prefabs.Count];
                for (var i = 0; i < _prefabs.Count; i++)
                    labels[i] = $"{_prefabs[i].name}  ({AssetDatabase.GetAssetPath(_prefabs[i])})";

                var newIndex = EditorGUILayout.Popup("Active Prefab", _selectedPrefabIndex, labels);
                if (newIndex != _selectedPrefabIndex && newIndex >= 0 && newIndex < _prefabs.Count)
                {
                    _selectedPrefabIndex = newIndex;
                    RememberPrefab(_prefabs[newIndex]);
                    selected = _prefabs[newIndex];
                }
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Prefab", selected, typeof(GameObject), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (RaxyHubGui.SecondaryButton("Ping", 80f) && selected != null)
                {
                    EditorGUIUtility.PingObject(selected);
                    Selection.activeObject = selected;
                }

                if (RaxyHubGui.SecondaryButton("Refresh", 80f))
                    RefreshPrefabs();

                if (RaxyHubGui.PrimaryButton("Assign Entries", 130f) && selected != null)
                    AssignEntries(selected);
            }

            RaxyHubGui.EndCard();
        }

        void DrawEntriesSection()
        {
            var settings = NotificationEditorSettings.instance;

            RaxyHubGui.BeginCard();
            EditorGUILayout.LabelField("Notification Entries", EditorStyles.boldLabel);
            RaxyHubGui.DrawHint(
                "Each entry is an Id, whether it is fullscreen, and the view bindings that receive its data.");

            EditorGUI.BeginChangeCheck();
            settings.GeneratedFolder = EditorGUILayout.TextField("Script Folder", settings.GeneratedFolder);
            settings.GeneratedNamespace = EditorGUILayout.TextField("Namespace", settings.GeneratedNamespace);
            if (EditorGUI.EndChangeCheck())
                settings.SaveSettings();

            EditorGUILayout.Space(6f);

            var entries = settings.Entries;
            var deleteIndex = -1;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] == null)
                    entries[i] = new NotificationEntry();

                RaxyHubGui.BeginCard();
                var title = string.IsNullOrWhiteSpace(entries[i].id) ? $"Entry {i + 1}" : entries[i].id;
                RaxyHubGui.DrawTitleRow(title, entries[i].behaviour.ToString(), entries[i].locked ? "Locked" : null);

                using (new EditorGUI.DisabledScope(entries[i].locked))
                {
                    entries[i].id = EditorGUILayout.TextField("Id", entries[i].id);
                    entries[i].behaviour = (NotificationBehaviour)EditorGUILayout.EnumPopup("Behaviour", entries[i].behaviour);
                    if (entries[i].behaviour == NotificationBehaviour.NonFullscreen)
                    {
                        entries[i].flow = (NotificationFlow)EditorGUILayout.EnumPopup("Flow", entries[i].flow);
                        entries[i].lifetime = EditorGUILayout.FloatField("Lifetime", entries[i].lifetime);
                        entries[i].interval = EditorGUILayout.FloatField("Interval", entries[i].interval);
                    }

                    DrawBindings(entries[i]);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.75f, 0.28f, 0.28f);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                        deleteIndex = i;
                    GUI.backgroundColor = previousColor;

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Toggle Lock", GUILayout.Width(110f)))
                        entries[i].locked = !entries[i].locked;
                }

                RaxyHubGui.EndCard();
            }

            if (deleteIndex >= 0)
            {
                entries.RemoveAt(deleteIndex);
                GUI.FocusControl(null);
            }

            if (GUI.changed)
                settings.SaveSettings();

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (RaxyHubGui.SecondaryButton("Add Entry", 100f))
                {
                    entries.Add(new NotificationEntry());
                    settings.SaveSettings();
                }

                if (RaxyHubGui.SecondaryButton("Read from Project", 150f))
                    ReadFromProject();

                GUILayout.FlexibleSpace();
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.35f, 0.72f, 0.38f);
                if (GUILayout.Button("Generate C#", GUILayout.Width(140f), GUILayout.Height(22f)))
                    GenerateClasses();
                GUI.backgroundColor = previousColor;
            }

            RaxyHubGui.EndCard();
        }

        static void DrawBindings(NotificationEntry entry)
        {
            if (entry.fields == null)
                entry.fields = new List<NotificationFieldDefinition>();

            EditorGUILayout.LabelField("Bindings");
            var deleteIndex = -1;
            for (var i = 0; i < entry.fields.Count; i++)
            {
                if (entry.fields[i] == null)
                    entry.fields[i] = new NotificationFieldDefinition();

                var field = entry.fields[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    field.fieldName = EditorGUILayout.TextField(field.fieldName);
                    field.kind = (NotificationFieldKind)EditorGUILayout.EnumPopup(field.kind);

                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                        deleteIndex = i;
                }
            }

            if (deleteIndex >= 0)
                entry.fields.RemoveAt(deleteIndex);

            if (GUILayout.Button("Add Binding"))
                entry.fields.Add(new NotificationFieldDefinition());
        }

        static void ReadFromProject()
        {
            var settings = NotificationEditorSettings.instance;
            var knownIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < settings.Entries.Count; i++)
            {
                var existing = settings.Entries[i];
                if (existing != null && !string.IsNullOrWhiteSpace(existing.id))
                    knownIds.Add(existing.id.Trim());
            }

            var constants = ReadNotificationIds();
            var views = IndexTypes(TypeCache.GetTypesDerivedFrom<NotificationView>(), "View", constants);
            var requests = IndexTypes(TypeCache.GetTypesDerivedFrom<NotificationRequest>(), "Request", constants);
            var ids = new HashSet<string>(views.Keys, StringComparer.Ordinal);
            foreach (var id in requests.Keys)
                ids.Add(id);

            var added = 0;
            foreach (var id in ids)
            {
                if (knownIds.Contains(id))
                    continue;

                views.TryGetValue(id, out var viewType);
                var entry = new NotificationEntry
                {
                    id = id,
                    behaviour = viewType != null && typeof(FullscreenNotificationView).IsAssignableFrom(viewType)
                        ? NotificationBehaviour.Fullscreen
                        : NotificationBehaviour.NonFullscreen,
                    locked = !IsGenerated(viewType ?? requests[id])
                };
                if (viewType != null)
                    entry.fields = ReadBindings(viewType);

                settings.Entries.Add(entry);
                added++;
            }

            settings.SaveSettings();
            Debug.Log($"[RAXY Hub / Notification] Read {added} entr{(added == 1 ? "y" : "ies")} from the project.");
        }

        static Dictionary<string, Type> IndexTypes(TypeCache.TypeCollection types, string suffix, Dictionary<string, string> constants)
        {
            var result = new Dictionary<string, Type>(StringComparer.Ordinal);
            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                if (type.IsAbstract || type.IsGenericTypeDefinition || !type.Name.EndsWith(suffix, StringComparison.Ordinal))
                    continue;

                var stripped = type.Name.Substring(0, type.Name.Length - suffix.Length);
                if (string.IsNullOrEmpty(stripped))
                    continue;

                var id = constants.TryGetValue(stripped, out var value) ? value : stripped;
                if (!result.ContainsKey(id))
                    result.Add(id, type);
            }

            return result;
        }

        static Dictionary<string, string> ReadNotificationIds()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType("RAXY.Notification.NotificationId");
                if (type == null)
                    continue;

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                for (var f = 0; f < fields.Length; f++)
                {
                    var field = fields[f];
                    if (!field.IsLiteral || field.FieldType != typeof(string))
                        continue;

                    if (field.GetRawConstantValue() is string value && !result.ContainsKey(field.Name))
                        result.Add(field.Name, value);
                }
            }

            return result;
        }

        static List<NotificationFieldDefinition> ReadBindings(Type viewType)
        {
            var bindings = new List<NotificationFieldDefinition>();
            var type = viewType;
            while (type != null && type != typeof(NotificationView) && type != typeof(FullscreenNotificationView))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.Name.EndsWith("Tmp", StringComparison.Ordinal) && field.FieldType.Name == "TextMeshProUGUI")
                    {
                        bindings.Add(new NotificationFieldDefinition
                        {
                            fieldName = field.Name.Substring(0, field.Name.Length - 3),
                            kind = NotificationFieldKind.Text
                        });
                    }
                    else if (field.Name.EndsWith("Img", StringComparison.Ordinal) && field.FieldType == typeof(Image))
                    {
                        bindings.Add(new NotificationFieldDefinition
                        {
                            fieldName = field.Name.Substring(0, field.Name.Length - 3),
                            kind = NotificationFieldKind.Image
                        });
                    }
                }

                type = type.BaseType;
            }

            return bindings;
        }

        static bool IsGenerated(Type type)
        {
            if (type == null)
                return false;

            var guids = AssetDatabase.FindAssets(type.Name + " t:MonoScript");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null || script.GetClass() != type)
                    continue;

                var contents = File.ReadAllText(path);
                return contents.Contains("generated by the Notification Framework");
            }

            return false;
        }

        static void GenerateClasses()
        {
            var settings = NotificationEditorSettings.instance;
            settings.SaveSettings();
            if (NotificationRequestGenerator.TryGenerate(settings.Entries, out var message))
                Debug.Log($"[RAXY Hub / Notification] {message}");
            else
                Debug.LogError($"[RAXY Hub / Notification] Generate failed: {message}");
        }

        void AssignEntries(GameObject prefab)
        {
            var manager = prefab.GetComponent<NotificationManager>();
            if (manager == null)
                return;

            var wanted = new List<NotificationEntry>();
            var wantedIds = new HashSet<string>(StringComparer.Ordinal);
            var source = NotificationEditorSettings.instance.Entries;
            for (var i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                var id = entry == null ? string.Empty : (entry.id ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(id) || !wantedIds.Add(id))
                    continue;

                wanted.Add(entry);
            }

            var serializedManager = new SerializedObject(manager);
            serializedManager.Update();
            var catalog = serializedManager.FindProperty("notificationDefinitions");
            if (catalog == null)
            {
                Debug.LogError("[RAXY Hub / Notification] Notification Manager has no notificationDefinitions list.", prefab);
                return;
            }
            var existing = new Dictionary<string, CatalogSnapshot>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.arraySize; i++)
            {
                var element = catalog.GetArrayElementAtIndex(i);
                var id = element.FindPropertyRelative("id").stringValue ?? string.Empty;
                if (string.IsNullOrEmpty(id) || existing.ContainsKey(id))
                    continue;

                existing.Add(id, CatalogSnapshot.Read(element));
            }

            var kept = 0;
            var added = 0;
            catalog.ClearArray();
            for (var i = 0; i < wanted.Count; i++)
            {
                var id = wanted[i].id.Trim();
                var index = catalog.arraySize;
                catalog.arraySize++;
                var element = catalog.GetArrayElementAtIndex(index);
                if (existing.TryGetValue(id, out var snapshot))
                {
                    snapshot.Write(element);
                    element.FindPropertyRelative("lifetime").floatValue = wanted[i].lifetime;
                    element.FindPropertyRelative("interval").floatValue = wanted[i].interval;
                    kept++;
                }
                else
                {
                    CatalogSnapshot.WriteNew(
                        element,
                        id,
                        wanted[i].behaviour,
                        wanted[i].flow,
                        wanted[i].lifetime,
                        wanted[i].interval);
                    added++;
                }
            }

            var removed = 0;
            foreach (var id in existing.Keys)
            {
                if (!wantedIds.Contains(id))
                    removed++;
            }

            serializedManager.ApplyModifiedProperties();
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log($"[RAXY Hub / Notification] Assigned entries to '{prefab.name}'. Kept {kept}, added {added}, removed {removed}.");
        }

        readonly struct CatalogSnapshot
        {
            readonly string _id;
            readonly int _behaviour;
            readonly int _flow;
            readonly UnityEngine.Object _spawnPrefab;
            readonly UnityEngine.Object _container;
            readonly float _lifetime;
            readonly float _interval;
            readonly UnityEngine.Object _fullscreenView;

            CatalogSnapshot(
                string id,
                int behaviour,
                int flow,
                UnityEngine.Object spawnPrefab,
                UnityEngine.Object container,
                float lifetime,
                float interval,
                UnityEngine.Object fullscreenView)
            {
                _id = id;
                _behaviour = behaviour;
                _flow = flow;
                _spawnPrefab = spawnPrefab;
                _container = container;
                _lifetime = lifetime;
                _interval = interval;
                _fullscreenView = fullscreenView;
            }

            public static CatalogSnapshot Read(SerializedProperty element)
            {
                return new CatalogSnapshot(
                    element.FindPropertyRelative("id").stringValue,
                    element.FindPropertyRelative("behaviour").enumValueIndex,
                    element.FindPropertyRelative("flow").enumValueIndex,
                    element.FindPropertyRelative("spawnPrefab").objectReferenceValue,
                    element.FindPropertyRelative("container").objectReferenceValue,
                    element.FindPropertyRelative("lifetime").floatValue,
                    element.FindPropertyRelative("interval").floatValue,
                    element.FindPropertyRelative("fullscreenView").objectReferenceValue);
            }

            public void Write(SerializedProperty element)
            {
                element.FindPropertyRelative("id").stringValue = _id;
                element.FindPropertyRelative("behaviour").enumValueIndex = _behaviour;
                element.FindPropertyRelative("flow").enumValueIndex = _flow;
                element.FindPropertyRelative("spawnPrefab").objectReferenceValue = _spawnPrefab;
                element.FindPropertyRelative("container").objectReferenceValue = _container;
                element.FindPropertyRelative("lifetime").floatValue = _lifetime;
                element.FindPropertyRelative("interval").floatValue = _interval;
                element.FindPropertyRelative("fullscreenView").objectReferenceValue = _fullscreenView;
            }

            public static void WriteNew(
                SerializedProperty element,
                string id,
                NotificationBehaviour behaviour,
                NotificationFlow flow,
                float lifetime,
                float interval)
            {
                element.FindPropertyRelative("id").stringValue = id;
                element.FindPropertyRelative("behaviour").enumValueIndex = (int)behaviour;
                element.FindPropertyRelative("flow").enumValueIndex = (int)flow;
                element.FindPropertyRelative("spawnPrefab").objectReferenceValue = null;
                element.FindPropertyRelative("container").objectReferenceValue = null;
                element.FindPropertyRelative("lifetime").floatValue = lifetime;
                element.FindPropertyRelative("interval").floatValue = interval;
                element.FindPropertyRelative("fullscreenView").objectReferenceValue = null;
            }
        }

        void GenerateManagerPrefab()
        {
            var folder = NotificationEditorSettings.instance.GeneratedFolder.Replace('\\', '/').Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(folder) || folder.Contains("..") ||
                (folder != "Assets" && !folder.StartsWith("Assets/")))
            {
                Debug.LogError("[RAXY Hub / Notification] Script folder must be a path under Assets.");
                return;
            }

            var prefabPath = folder + "/" + ManagerPrefabName;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.LogWarning($"[RAXY Hub / Notification] Prefab already exists at '{prefabPath}'.");
                RefreshPrefabs();
                return;
            }

            EnsureFolder(folder);

            var managerObject = new GameObject("Notification Manager");
            managerObject.AddComponent<NotificationManager>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(managerObject, prefabPath);
            UnityEngine.Object.DestroyImmediate(managerObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshPrefabs();

            if (prefab != null)
            {
                RememberPrefab(prefab);
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
                Debug.Log($"[RAXY Hub / Notification] Generated Notification Manager at '{prefabPath}'.");
            }
        }

        void RefreshPrefabs()
        {
            _prefabs.Clear();
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<NotificationManager>() != null)
                    _prefabs.Add(prefab);
            }

            _prefabs.Sort((a, b) => string.Compare(
                AssetDatabase.GetAssetPath(a),
                AssetDatabase.GetAssetPath(b),
                StringComparison.OrdinalIgnoreCase));

            _selectedPrefabIndex = 0;
            var activeGuid = NotificationEditorSettings.instance.ActivePrefabGuid;
            if (!string.IsNullOrEmpty(activeGuid))
            {
                for (var i = 0; i < _prefabs.Count; i++)
                {
                    if (AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_prefabs[i])) == activeGuid)
                    {
                        _selectedPrefabIndex = i;
                        break;
                    }
                }
            }

            if (_prefabs.Count == 1)
                RememberPrefab(_prefabs[0]);
        }

        GameObject SelectedPrefab()
        {
            if (_prefabs.Count == 0)
                return null;

            _selectedPrefabIndex = Mathf.Clamp(_selectedPrefabIndex, 0, _prefabs.Count - 1);
            return _prefabs[_selectedPrefabIndex];
        }

        static void RememberPrefab(GameObject prefab)
        {
            if (prefab == null)
                return;

            var settings = NotificationEditorSettings.instance;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            if (settings.ActivePrefabGuid == guid)
                return;

            settings.ActivePrefabGuid = guid;
            settings.SaveSettings();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
