// #pragma warning disable UDR0001

// namespace PigBrain.LegacyCore.Analytics
// {
//     public interface IMessage { }
// }

// namespace PigBrain.LegacyCore.Analytics
// {
//     using System;
//     using System.Linq;
//     using System.Reflection;
//     using System.Collections.Generic;
//     using UnityEngine;
//     using PigBrain.LegacyCore.Utility;
//     using UnityEngine.SceneManagement;
//     using pigbrain.Core.Collections;

//     public static class GameObjectMessages
//     {
//         public static void Message<T>(this Component c, MessageScope scope, params object[] parameters) =>
//             c.gameObject.Message<T>(scope, parameters);

//         public static void Message<T>(this Component c, MessageScope scope, string name, params object[] parameters) =>
//             c.gameObject.Message<T>(scope, name, parameters);

//         public static void Message<T>(this GameObject g, MessageScope scope, params object[] parameters) =>
//             g.Message<T>(scope, null, parameters);

//         public static void Message<T>(this GameObject g, MessageScope scope, string name, params object[] parameters)
//         {
//             if (GetMethod<T>(name) is not MethodInfo method)
//             {
//                 Debug.LogError($"Message method ({name}) == null!");
//                 return;
//             }
//             IEnumerable<T> objects = g.GetComponents<T>(scope);
//             objects.ForEach(c => method.Invoke(c, parameters));
//         }

//         static IEnumerable<T> GetComponents<T>(this GameObject gameObject, MessageScope scope) => scope switch
//         {
//             MessageScope.Self => gameObject.GetComponents<T>(),
//             MessageScope.Children => gameObject.GetComponentsInChildren<T>(true),
//             MessageScope.TopParent => GetTopParent(gameObject).GetComponentsInChildren<T>(true),
//             MessageScope.Global => GetSceneItems<T>(),
//             _ => throw new Exception($"Unknown Message Scope {scope}!")
//         };

//         static Transform GetTopParent(GameObject go)
//         {
//             Transform top = go.transform;
//             while (top.parent) top = top.parent;
//             return top;
//         }

//         static IEnumerable<T> GetSceneItems<T>()
//         {
//             var components = new List<T>();
//             for (int i = 0, n = SceneManager.sceneCount; i < n; i++)
//                 if (SceneManager.GetSceneAt(i) is Scene scene && scene.isLoaded)
//                     components.AddRange(scene.GetRootGameObjects()
//                         .SelectMany(g => g.GetComponentsInChildren<T>(true)));
//             return components;
//         }

//         const BindingFlags bindings = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
//         static Dictionary<Type, MethodInfo> Methods = new Dictionary<Type, MethodInfo>();

//         static MethodInfo GetMethod<T>(string name = null) =>
//             name == null ? GetMethod(typeof(T)) : GetMethod(typeof(T), name);

//         static MethodInfo GetMethod(Type type)
//         {
//             Methods.TryAdd(type, type.GetMethods(bindings).FirstOrDefault());
//             return Methods[type];
//         }

//         static MethodInfo GetMethod(Type type, string name = null)
//         {
//             Methods.TryAdd(type, type.GetMethod(name, bindings));
//             return Methods[type];
//         }

//     }

//     public enum MessageScope
//     {
//         Self,
//         Children,
//         TopParent,
//         Global,
//     }
// }