// #pragma warning disable UDR0005
// using System;
// using System.Linq;
// using UnityEditor;
// using UnityEngine;
// using static UnityEngine.Object;
// namespace PigBrain.LegacyCore.Utility
// {
//     public static class UnityObjectExtensions
//     {
//         // Safe destroy of Unity Objects
//         public static void SafeDestroy(this UnityEngine.Object unityObject)
//         {
//             if (unityObject == null) return;
//             if (Application.isPlaying) Destroy(unityObject);
//             else UnityEngine.Object.DestroyImmediate(unityObject);
//         }

//         // public static T TryAddComponent<T>(this MonoBehaviour behaviour) where T : Component =>
//         //     behaviour.transform.TryAddComponent<T>();
//         // public static T TryAddComponent<T>(this GameObject gameObject) where T : Component =>
//         //     gameObject.transform.TryAddComponent<T>();
//         // public static T TryAddComponent<T>(this Transform transform) where T : Component =>
//         //     transform.TryGetComponent(out T component) ? component : transform.gameObject.AddComponent<T>();

//         public static T As<T>(MonoBehaviour mb) where T : MonoBehaviour => mb as T;

//         public static float Distance(this Transform transform, Transform target) => (target.position - transform.position).magnitude;
//         public static Vector3 Direction(this Transform transform, Transform target) => target.position - transform.position;
//         public static Vector3 DirectionNormal(this Transform transform, Transform target) => transform.Direction(target).normalized;

//         public static T Instantiate<T>(this T target, Vector3 position, Quaternion rotation, Transform parent = null) where T : UnityEngine.Object
//         {
//             if (target is null) return null;
//             var instance = UnityEngine.Object.Instantiate(target, position, rotation, parent);
//             instance.name = target.name;
//             return instance;
//         }
//         public static T Instantiate<T>(this T target, Vector3 position, Transform parent = null) where T : UnityEngine.Object =>
//             target.Instantiate(position, Quaternion.identity, parent);

//         public static T Instantiate<T>(this T target, Transform parent = null) where T : UnityEngine.Object =>
//             target.Instantiate(Vector3.zero, Quaternion.identity, parent);

//         public static T Clone<T>(this T target) where T : UnityEngine.Object
//         {
//             var instance = UnityEngine.Object.Instantiate(target);
//             instance.name = target.name;
//             return instance;
//         }

//         public static bool Destroyed(this UnityEngine.Object target) =>
//             (object)target != null && target == null;

//         public static void With<T>(this T target, Action<T> set) where T : UnityEngine.Object => set(target);

//         public static void CopyFrom(this Transform target, Transform source)
//         {
//             target.SetPositionAndRotation(source.position, source.rotation);
//             target.localScale = source.localScale;
//         }

//         public static Bounds GetBoundsFromRenderers(this GameObject gameObject)
//         {
//             return gameObject.GetComponentsInChildren<Renderer>()
//                 .Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
//         }

//         public static string FullPath(this GameObject gameObject) =>
//             gameObject.transform.FullPath();

//         public static string FullPath(this Transform transform) =>
//             string.Join("/", transform.Iterate(rt => rt.parent).Select(t => t.name).Reverse());


//     }
// }

// namespace PigBrain.LegacyCore.Utility
// {
//     public static class UnityObjectExtensionsEditor
//     {
//         public static void OnValidateDelay(this MonoBehaviour behaviour, Action action)
//         {
// #if UNITY_EDITOR
//             void TheAction()
//             {
//                 action();
//                 UnityEditor.EditorApplication.delayCall -= TheAction;
//             }
//             UnityEditor.EditorApplication.delayCall += TheAction;
// #endif
//         }

//         public static void SetDirtyInEditor(this UnityEngine.Object target)
//         {
// #if UNITY_EDITOR
//             EditorUtility.SetDirty(target);
// #endif
//         }

//         public static bool GetGuid(this GameObject asset, out string guid)
//         {
// #if UNITY_EDITOR
//             if (asset && UnityEditor.AssetDatabase.GetAssetPath(asset) is string path && !string.IsNullOrEmpty(path))
//             {
//                 guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
//                 return true;
//             }
// #endif
//             guid = null;
//             return false;
//         }

//     }
// }
