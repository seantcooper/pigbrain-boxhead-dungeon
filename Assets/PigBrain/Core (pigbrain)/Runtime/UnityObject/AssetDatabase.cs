#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

namespace pigbrain.core.UnityObject
{
    public static class AssetDatabaseX
    {
        public static Dictionary<string, T> LoadAssetsAtPath<T>(string path)
            where T : UnityEngine.Object
        {
            var result = new Dictionary<string, T>();

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    if (asset) result[assetPath] = asset;
                }
            }
            else
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset) result[path] = asset;
            }

            return result;
        }

        public static void RemoveSubAssets(this UnityEngine.Object mainAsset, params Type[] remove)
        {
            string path = AssetDatabase.GetAssetPath(mainAsset);
            if (string.IsNullOrEmpty(path)) return;

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets.Where(a => a != mainAsset && remove.Any(rt => rt.IsAssignableFrom(a.GetType()))))
            {
                AssetDatabase.RemoveObjectFromAsset(a);
                DestroyImmediate(a, true);
            }
            EditorUtility.SetDirty(mainAsset);
        }
    }
}
#endif
