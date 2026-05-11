using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if !UNITY_EDITOR
namespace PigBrain.LegacyCore.Utility
{
    public static partial class AssetUtility
    {
        public static IEnumerable<T> FindAssetsInProject<T>(string path = null) where T : Object 
        { yield break; }
    }
}
#endif


#if UNITY_EDITOR
namespace PigBrain.LegacyCore.Utility
{
    using static UnityEditor.AssetDatabase;
    public static partial class AssetUtility
    {
        public static IEnumerable<T> FindAssetsInProject<T>(string path = null) where T : Object
        {
            System.Type TType = typeof(T);

            bool ValidPath(string guid) =>
                string.IsNullOrEmpty(path) || GUIDToAssetPath(guid).Contains(path, System.StringComparison.OrdinalIgnoreCase);

            if (TType == typeof(GameObject))
                return FindAssets("t:Prefab")
                    .Where(guid => ValidPath(guid))
                    .Select(guid => LoadAssetAtPath<GameObject>(GUIDToAssetPath(guid)) as T);

            else if (TType.IsSubclassOf(typeof(Component)))
                return FindAssets("t:Prefab")
                    .Where(guid => ValidPath(guid))
                    .SelectMany(guid => LoadAssetAtPath<GameObject>(GUIDToAssetPath(guid)).GetComponents<T>());

            else if (TType.IsSubclassOf(typeof(ScriptableObject)))
                return FindAssets($"t:{TType.Name}")
                    .Where(guid => ValidPath(guid))
                    .Select(guid => LoadAssetAtPath<T>(GUIDToAssetPath(guid)));

            else throw new System.Exception("Not supported!");
        }
    }
}
#endif
