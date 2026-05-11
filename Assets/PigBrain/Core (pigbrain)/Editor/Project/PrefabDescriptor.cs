using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace pigbrain.core.Project
{
    // Weapon Icon($Base)
    // ├── Outline
    // ├── Background
    // └── Content
    //     ├── Icon
    //     ├── Text
    //     ├── Ammo
    //     └── Key

    public static class DescriptorBuilder
    {
        #region Menu Entry
        [MenuItem("Assets/Create/PigBrain/Prefab Descriptor", true)]
        static bool Validate() => Selection.gameObjects.Length > 0;

        [MenuItem("Assets/Create/PigBrain/Prefab Descriptor")]
        static void Create()
        {
            var selected = Selection.objects;
            List<string> guids = new();

            for (int i = 0; i < selected.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(selected[i]);
                if (!string.IsNullOrEmpty(path))
                    guids.Add(AssetDatabase.AssetPathToGUID(path));
            }

            EditorPrefs.SetString("PrefabDescriptor.Selection", string.Join("|", guids));

            foreach (GameObject prefab in Selection.gameObjects)
                BuildPrefabDescClass(prefab);
        }
        #endregion

        static List<(string method, string path)> CollectTMPSetters(Transform root)
        {
            List<(string, string)> list = new();

            void Walk(Transform t, string prefix)
            {
                foreach (Transform child in t)
                {
                    string clean = CleanName(child.name);
                    string field = char.ToLower(clean[0]) + clean.Substring(1);
                    string p = string.IsNullOrEmpty(prefix) ? field : prefix + "." + field;

                    var comps = child.GetComponents<Component>();
                    for (int i = 0; i < comps.Length; i++)
                    {
                        var c = comps[i];
                        if (c is TMP_Text || c is TextMeshProUGUI)
                        {
                            string method = "Set" + clean;
                            string path = p + ".text";
                            list.Add((method, path));
                        }
                    }

                    Walk(child, p);
                }
            }

            Walk(root, "");
            return list;
        }

        #region Build Prefab
        static void BuildPrefabDescClass(GameObject prefab)
        {
            string typeName = CleanName(prefab.name);

            string assetPathCheck = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(assetPathCheck))
            {
                Debug.LogError($"PrefabDescriptor: '{prefab.name}' is not a prefab asset. Select prefab from Project window.");
                return;
            }

            HashSet<string> usings = new() { "System", "UnityEngine" };

            var setters = CollectTMPSetters(prefab.transform);
            List<string> classLines = BuildClass(prefab.transform, typeName, 1, usings, setters);
            List<string> lines = new();

            lines.AddRange(usings.OrderBy(x => x).Distinct().Select(ns => $"using {ns};").Append(string.Empty));

            for (int i = 0; i < NamspaceTemplate.Count; i++)
            {
                string line = NamspaceTemplate[i]
                    .Replace(TypeTag, typeName)
                    .Replace(ContentTag, string.Join("\n", classLines));
                lines.Add(line);
            }

            string full = string.Join("\n", lines);
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string dir = Path.GetDirectoryName(assetPath);
            string fileName = prefab.name + ".cs";
            string fullPath = Path.Combine(dir, fileName);

            File.WriteAllText(fullPath, full);
            AssetDatabase.Refresh();

            string fullTypeName = GetFullClassName(typeName);
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            string existing = EditorPrefs.GetString(PendingPrefabPathKey, "");
            string entry = prefabPath + "|" + fullTypeName;

            if (string.IsNullOrEmpty(existing)) EditorPrefs.SetString(PendingPrefabPathKey, entry);
            else EditorPrefs.SetString(PendingPrefabPathKey, existing + "\n" + entry);

            EditorPrefs.SetBool(PendingAttachKey, true);

            Debug.Log($"PrefabDescriptor: Scheduled attach after compile: {fullTypeName}");
            Debug.Log($"Generated: {fullPath}");
        }
        #endregion

        #region Build Class
        static List<string> BuildClass(Transform t, string typeName, int depth, HashSet<string> usings, List<(string method, string path)> setters)
        {
            string indent = new(' ', depth * 4);
            List<string> lines = new();
            List<string> content = new();

            Type transformType = t.GetType();
            if (!string.IsNullOrEmpty(transformType.Namespace))
                usings.Add(transformType.Namespace);

            // skip transform on root MonoBehaviour (already has transform)
            if (depth > 1)
            {
                string transformTypeName = transformType.Name;
                string transformFieldName = TypeNames.TryGetValue(transformType, out string name) ? name : "transform";
                content.Add($"public {transformTypeName} {transformFieldName};");
            }

            Component[] components = t.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Type type = components[i].GetType();
                if (!string.IsNullOrEmpty(type.Namespace))
                    usings.Add(type.Namespace);
                if (TypeExcludes.Contains(type)) continue;
                if (components[i] is Transform) continue;
                string fieldName = TypeNames.TryGetValue(type, out string nameOverride)
                    ? nameOverride
                    : char.ToLower(type.Name[0]) + type.Name.Substring(1);
                content.Add($"public {type.Name} {fieldName};");
            }

            foreach (Transform child in t)
            {
                string clean = CleanName(child.name);
                string childType = clean + ClassPostfixTag;
                string fieldName = char.ToLower(clean[0]) + clean.Substring(1);
                content.Add($"public {childType} {fieldName};");
            }

            // build inner content: fields + nested classes
            List<string> inner = new();

            // fields
            for (int i = 0; i < content.Count; i++)
                inner.Add(indent + "    " + content[i]);

            // nested classes
            foreach (Transform child in t)
            {
                inner.Add(string.Empty);
                var childLines = BuildClass(child, CleanName(child.name), depth + 1, usings, setters);
                for (int i = 0; i < childLines.Count; i++)
                    inner.Add(childLines[i]);
            }


            // apply template
            for (int i = 0; i < ClassTemplate.Count; i++)
            {
                string raw = ClassTemplate[i].Replace(TypeTag, typeName);

                if (raw.StartsWith("public class ") && depth == 1) raw = MainClassName;

                if (raw == ContentTag) lines.AddRange(inner);
                else lines.Add(indent + raw);
            }

            return lines;
        }
        #endregion

        #region Clean Name
        static string CleanName(string name)
        {
            var chars = name.Where(char.IsLetterOrDigit).ToArray();
            string clean = new string(chars);
            if (string.IsNullOrEmpty(clean)) return "Node";
            return char.ToUpper(clean[0]) + clean.Substring(1);
        }
        #endregion

        #region Structure
        const string StandardName = "PrefabView";
        const string ContentTag = "-content-";
        const string TypeTag = "{type}";
        const string ClassPostfixTag = "View";
        const string NamespacePrefixTag = "";
        const string PendingAttachKey = "PrefabDescriptor.PendingAttach";
        const string PendingPrefabPathKey = "PrefabDescriptor.PendingPrefabPath";
        static readonly string MainClassName = $"public class {StandardName} : MonoBehaviour";

        static string GetFullClassName(string typeName) =>
            $"pigbrain.Generated.{NamespacePrefixTag}{typeName}.{StandardName}";

        static readonly List<string> NamspaceTemplate = new()
    {
        $"namespace pigbrain.Generated.{NamespacePrefixTag}{TypeTag}",
        "{",
        ContentTag,
        "}"
    };

        static readonly List<string> ClassTemplate = new()
    {
        "[Serializable]",
        $"public class {TypeTag}{ClassPostfixTag}",
        "{",
        ContentTag,
        "}"
    };

        static readonly Dictionary<Type, string> TypeNames = new()
    {
        { typeof(Transform), "transform" },
        { typeof(RectTransform), "transform" },
        { typeof(TMP_Text), "text" },
        { typeof(TextMeshProUGUI), "text" },
        { typeof(TextMeshPro), "text" },
    };

        static readonly List<Type> TypeExcludes = new()
    {
        typeof(CanvasRenderer),
        typeof(Rigidbody), // as example
    };
        #endregion
    }

    #region Post Compile
    [InitializeOnLoad]
    public static class PrefabDescriptorPostCompile
    {
        static PrefabDescriptorPostCompile()
        {
            EditorApplication.delayCall += TryAttachPending;
        }

        static void TryAttachPending()
        {
            if (!EditorPrefs.GetBool("PrefabDescriptor.PendingAttach", false)) return;
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += TryAttachPending;
                return;
            }

            string data = EditorPrefs.GetString("PrefabDescriptor.PendingPrefabPath", string.Empty);
            if (string.IsNullOrEmpty(data))
            {
                ClearPending();
                return;
            }

            var entries = data.Split('\n');
            List<string> remaining = new();
            List<UnityEngine.Object> reselection = new();

            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e)) continue;

                var parts = e.Split('|');
                if (parts.Length != 2) continue;

                string prefabPath = parts[0];
                string fullTypeName = parts[1];

                Type generatedType = null;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    generatedType = assemblies[i].GetType(fullTypeName);
                    if (generatedType != null) break;
                }

                if (generatedType == null)
                {
                    remaining.Add(e);
                    continue;
                }

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                var comp = root.GetComponent(generatedType);
                if (comp == null) comp = root.AddComponent(generatedType);

                BindRecursive(comp, root.transform);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);

                Debug.Log($"Bound PrefabView: {Path.GetFileNameWithoutExtension(prefabPath)}");
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (asset != null) reselection.Add(asset);
            }

            if (remaining.Count > 0)
            {
                EditorPrefs.SetString("PrefabDescriptor.PendingPrefabPath", string.Join("\n", remaining));
                EditorApplication.delayCall += TryAttachPending;
            }
            else
            {
                ClearPending();
            }

            // restore original selection (preferred)
            string sel = EditorPrefs.GetString("PrefabDescriptor.Selection", string.Empty);
            if (!string.IsNullOrEmpty(sel))
            {
                var guids = sel.Split('|');
                List<UnityEngine.Object> restored = new();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (obj != null) restored.Add(obj);
                    }
                }

                if (restored.Count > 0)
                {
                    var objs = restored.ToArray();
                    EditorApplication.delayCall += () => Selection.objects = objs;
                }

                EditorPrefs.DeleteKey("PrefabDescriptor.Selection");
            }
            else if (reselection.Count > 0)
            {
                var objs = reselection.ToArray();
                EditorApplication.delayCall += () => Selection.objects = objs;
            }
        }

        static void ClearPending()
        {
            EditorPrefs.DeleteKey("PrefabDescriptor.PendingAttach");
            EditorPrefs.DeleteKey("PrefabDescriptor.PendingPrefabPath");
        }

        static void BindRecursive(object target, Transform root)
        {
            if (target == null || root == null) return;

            var fields = target.GetType().GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                var type = f.FieldType;

                // Component binding
                if (typeof(Component).IsAssignableFrom(type))
                {
                    var comp = root.GetComponent(type);
                    if (comp != null) f.SetValue(target, comp);
                    continue;
                }

                // Nested class binding
                if (type.IsClass && type != typeof(string))
                {
                    var child = FindChild(root, f.Name);
                    if (!child) continue;

                    var instance = Activator.CreateInstance(type);
                    f.SetValue(target, instance);

                    BindRecursive(instance, child);
                }
            }
        }

        static Transform FindChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (Normalize(c.name) == Normalize(name)) return c;
            }
            return null;
        }

        static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var chars = s.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToLower();
        }
    }
    #endregion
}