// using System;
#pragma warning disable CS0618
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class LifetimeScope : System.IDisposable
    {
        readonly UnityEngine.Object target;
        public LifetimeScope(UnityEngine.Object target) =>
            this.target = target;
        void System.IDisposable.Dispose() => UnityEngine.Object.Destroy(target);
    }

    public static class Instantiation
    {
        public static int GetUniqueID(this UnityEngine.Object o) => o.GetInstanceID();
        public static string GetDisplayName(this UnityEngine.Object o)
        {
            var m = System.Text.RegularExpressions.Regex.Match(o.name, @"\((.*?)\)");
            return ReflectionUtility.GetDisplayName(m.Success ? m.Groups[1].Value : o.name);
        }

        #region Instantiate
        public static T Instantiate<T>(this T original, System.Action<T> initialize) where T : Object
        {
            if (!CheckOriginal(original)) return null;
            var instance = SetName(Object.Instantiate(original), original.name);
            initialize?.Invoke(instance);
            return instance;
        }

        public static T Instantiate<T>(this T original, Vector3 position, Quaternion rotation) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original, position, rotation), original.name);

        public static T Instantiate<T>(this T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original, position, rotation, parent), original.name);

        public static T Instantiate<T>(this T original, Vector3 position) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original, position, Quaternion.identity), original.name);

        public static T Instantiate<T>(this T original, Vector3 position, Transform parent) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original, position, Quaternion.identity, parent), original.name);

        public static T Instantiate<T>(this T original, Transform parent) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original, parent), original.name);

        public static T Instantiate<T>(this T original) where T : Object =>
            !CheckOriginal(original) ? null
            : SetName(Object.Instantiate(original), original.name);

        static bool CheckOriginal<T>(this T original) where T : Object
        {
            if (original) return true;
            Debug.LogWarning("prefab is null or destroyed!");
            return false;
        }

        static T SetName<T>(T instance, string name) where T : Object
        {
            instance.name = name;
            return instance;
        }

        public static void ResetLocal(this Transform target)
        {
            target.SetLocalPositionAndRotation(default, Quaternion.identity);
            target.localScale = (float3)1;
        }
        #endregion

        public static T PrefabInstantiate<T>(this T target, Transform parent) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            var r = (T)UnityEditor.PrefabUtility.InstantiatePrefab(target, parent);
            r.name = target.name;
            return r;
#else
            return target.Instantiate(parent);
#endif
        }

        public static T PrefabInstantiate<T>(this T target) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return (T)UnityEditor.PrefabUtility.InstantiatePrefab(target);
#else
            return target.Instantiate();
#endif
        }

        #region Destroy
        public static void DestroyObject(this Component component)
        { if (component) component.gameObject.DestroyObject(); }
        public static void DestroyObject(this GameObject gameObject)
        { if (gameObject) Object.Destroy(gameObject); }

#if UNITY_EDITOR
        public static void DestroyImmediate(this Object target, float duration)
        {
            double end = UnityEditor.EditorApplication.timeSinceStartup + duration;
            void Cleanup()
            {
                if (!target)
                {
                    UnityEditor.EditorApplication.update -= Cleanup;
                    return;
                }
                if (UnityEditor.EditorApplication.timeSinceStartup >= end)
                {
                    UnityEditor.EditorApplication.update -= Cleanup;
                    Object.DestroyImmediate(target);
                }
            }
            UnityEditor.EditorApplication.update += Cleanup;
        }
#endif

        public static void DestroyImmediate(this UnityEngine.Object target)
        { if (target) DestroyImmediate(target); }

        public static void DestroyChildrenImmediate(this Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(target.GetChild(i).gameObject);
        }
        public static void DestroyChildren(this Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
                Object.Destroy(target.GetChild(i).gameObject);
        }
        #endregion

        public static void Hide(this GameObject gameObject) => gameObject.transform.Hide();
        public static void Hide(this Component component)
        {
            foreach (var r in component.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        #region Transform Path
        public static string GetPath(this Transform transform)
        {
            if (!transform) return null;

            // 1) Scene hierarchy
            if (transform.gameObject.scene.IsValid() && transform.gameObject.scene.isLoaded)
                return transform.GetHierarchyPath();

#if UNITY_EDITOR
            // 2 & 3) Project / Prefab (asset or instance)
            var go = transform.gameObject;
            var assetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (!string.IsNullOrEmpty(assetPath))
            {
                // If inside a prefab, append local path within prefab
                var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(go) ?? go;
                var local = GetLocalPath(transform, root.transform);
                return string.IsNullOrEmpty(local) ? assetPath : assetPath + "/" + local;
            }
#endif

            return transform.GetHierarchyPath(); // fallback
        }

        public static string GetHierarchyPath(this Transform transform)
        {
            List<string> path = new();
            for (var parent = transform; parent; parent = parent.parent)
                path.Add(parent.name);
            if (transform.gameObject.scene != null)
                path.Add(transform.gameObject.scene.name);

            path.Reverse();
            return string.Join("/", path);
        }

        static string GetLocalPath(Transform t, Transform root)
        {
            if (t == root) return string.Empty;
            var stack = new System.Collections.Generic.List<string>();
            var cur = t;
            while (cur && cur != root)
            {
                stack.Add(cur.name);
                cur = cur.parent;
            }
            stack.Reverse();
            return string.Join("/", stack);
        }

        public static string GetProjectPath(this Transform transform)
        {
#if UNITY_EDITOR
            if (!transform) return null;
            var go = transform.gameObject;
            var path = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            return string.IsNullOrEmpty(path) ? null : path;
#else
            return null;
#endif
        }
        #endregion
    }
}
