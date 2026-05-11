#pragma warning disable CS0162
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace pigbrain.core.Analysis
{
    public interface IMessage { }

    public static class Message
    {
        public static void SendMessage<T>(this Component component,
            MessageScope scope = MessageScope.Self, object parameter = null) =>
                component.gameObject.SendMessage<T>(scope, parameter);

        public static void SendMessage<T>(this Component component, string name,
            MessageScope scope = MessageScope.Self, object parameter = null) =>
                component.gameObject.SendMessage<T>(name, scope, parameter);

        public static void SendMessage<T>(this GameObject gameObject,
            MessageScope scope = MessageScope.Self, object parameter = null) =>
                gameObject.SendMessage<T>(null, scope, parameter);

        public static void SendMessage<T>(this GameObject gameObject, string name = null,
            MessageScope scope = MessageScope.Self, object parameter = null)
        {
            if (!gameObject) return;
            var targets = gameObject.GetComponents<T>(scope);
            if (targets.Count == 0) return;
            var method = MethodCache<T>.Get(name);
            if (method == null) return;
            foreach (var o in targets)
                method(o, parameter);
        }

        static IList<T> GetComponents<T>(this GameObject gameObject, MessageScope scope) => scope switch
        {
            MessageScope.Self => gameObject.GetComponents<T>(),
            MessageScope.Children => gameObject.GetComponentsInChildren<T>(true),
            MessageScope.TopParent => GetTopParent(gameObject).GetComponentsInChildren<T>(true),
            MessageScope.Global => GetSceneItems<T>(),
            _ => throw new Exception($"Unknown Message Scope {scope}!")
        };

        static Transform GetTopParent(GameObject go)
        {
            Transform top = go.transform;
            while (top.parent) top = top.parent;
            return top;
        }

        static IList<T> GetSceneItems<T>() => ObjectUtility.GetSceneItems<T>();
        // {
        //     var components = new List<T>();
        //     for (int i = 0, n = SceneManager.sceneCount; i<n; i++)
        //         if (SceneManager.GetSceneAt(i) is Scene scene && scene.isLoaded)
        //             components.AddRange(scene.GetRootGameObjects()
        //                 .SelectMany(g => g.GetComponentsInChildren<T>(true)));
        //     return components;
        // }
    }

    public enum MessageScope { Self, Children, TopParent, Global, }

    static class MethodCache<T>
    {
        static readonly Dictionary<string, Action<T, object>> Cache = new();

        public static Action<T, object> Get(string name)
        {
            name ??= string.Empty;
            if (Cache.TryGetValue(name, out var action)) return action;

            var methods = typeof(T).GetMethods();
            MethodInfo method = string.IsNullOrEmpty(name)
                ? methods.Length == 1 ? methods[0] : null
                : methods.FirstOrDefault(m => m.Name == name);

            if (method == null)
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(name))
                    throw new Exception($"{typeof(T).Name} has {methods.Length} methods. Pass a method name.");
                throw new Exception($"{typeof(T).Name} does not contain method '{name}'.");
#endif
                Cache[name] = null;
                return null;
            }
            return Cache[name] = action = Create(method);
        }

        static Action<T, object> Create(MethodInfo method)
        {
            if (method == null) return null;

            ParameterExpression target = Expression.Parameter(typeof(T), "target");
            ParameterExpression args = Expression.Parameter(typeof(object[]), "args");

            var parameters = method.GetParameters()
                .Select((p, i) => Expression.Convert(
                    Expression.ArrayIndex(args, Expression.Constant(i)), p.ParameterType))
                .ToArray();

            var call = Expression.Call(
                Expression.Convert(target, method.DeclaringType), method, parameters);

            return Expression
                .Lambda<Action<T, object>>(call, target, args)
                .Compile();
        }
    }
}