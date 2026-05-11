using System;
using System.IO;
using System.Linq;
using pigbrain.core.Utility;
using UnityEngine;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace pigbrain.core.Populator
{
    public static class Populate
    {
        public class ControlAttribute : Attribute { }

        public class TypesAttribute : PropertyAttribute
        {
            public readonly Type[] types;
            public TypesAttribute(params Type[] types) => this.types = types;
        }

#if !UNITY_EDITOR
        public static void Parse(UnityEngine.Object target) {}
#else
        public static void Parse(UnityEngine.Object target)
        {
            var (contextObject, basePath) = GetContext(target);
            if (contextObject == null || string.IsNullOrEmpty(basePath)) return;

            var dir = Path.GetDirectoryName(basePath);
            var prefabName = Path.GetFileNameWithoutExtension(basePath);
            var dataFolder = Path.Combine(dir, prefabName);

            var searchFolders = AssetDatabase.IsValidFolder(dataFolder)
                ? new[] { dataFolder }
                : new[] { dir };

            var fields = contextObject.GetType().GetFields(ReflectionUtility.DefaultBindings)
                .Where(f => f.GetCustomAttribute<Populate.TypesAttribute>() != null);

            foreach (var f in fields)
            {
                var attr = f.GetCustomAttribute<Populate.TypesAttribute>();

                var types = (attr.types != null && attr.types.Length > 0) ? attr.types : new[] { f.FieldType };
                foreach (var type in types)
                {
                    UnityEngine.Object asset = null;

                    if (typeof(GameObject).IsAssignableFrom(type) || typeof(Component).IsAssignableFrom(type))
                    {
                        var guids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

                        asset = guids
                            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                            .Where(a => a && (type == typeof(GameObject) || a.GetComponent(type) != null))
                            .OrderBy(a => a.name)
                            .FirstOrDefault();
                    }

                    else if (typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        var guids = AssetDatabase.FindAssets($"t:{type.Name}", searchFolders);

                        asset = guids
                            .Select(g => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g), type))
                            .Where(a => a != null)
                            .OrderBy(a => a.name)
                            .FirstOrDefault();
                    }

                    if (asset != null)
                    {
                        f.SetValue(contextObject, asset);
                        break;
                    }
                }
            }

            EditorUtility.SetDirty(contextObject);
        }

        static (UnityEngine.Object contextObject, string basePath) GetContext(UnityEngine.Object target)
        {
            if (target is Component component)
                return (component, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(component.gameObject));

            if (target is ScriptableObject so)
                return (so, AssetDatabase.GetAssetPath(so));

            throw new Exception($"'{target}' must be a Component or ScriptableObject");
        }
#endif
    }
}