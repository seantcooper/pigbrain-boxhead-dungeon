#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.Utility
{
    public static class ReflectionUtility
    {
        public const BindingFlags DefaultBindings =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsAny(this Type type, params Type[] types) => types.Contains(type);

        #region Display Name
        public static string GetDisplayName(this string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            string humanReadable = name.Replace("_", " ");
            humanReadable = Regex.Replace(humanReadable, "([a-z])([A-Z0-9])", "$1 $2");
            humanReadable = Regex.Replace(humanReadable, "([A-Z])([0-9])", "$1 $2");
            humanReadable = Regex.Replace(humanReadable, "([0-9])([A-Za-z])", "$1 $2");
            return char.ToUpper(humanReadable[0]) + humanReadable[1..];
        }
        #endregion

        #region Interface
        public static MemberInfo[] GetNumericMembers(this Type type) =>
            type.GetMemberTree().numerics.Values.ToArray();

        public static IEnumerable<Type> GetInheritedTypes(this Type type, bool includeSelf = true)
        {
            if (includeSelf) yield return type;
            for (var current = type.BaseType; current != null; current = current.BaseType) yield return current;
        }

        public static IEnumerable<FieldInfo> GetFieldsOfType<T>(this Type type) =>
            type.GetFields(DefaultBindings).Where(field => typeof(T).IsAssignableFrom(field.FieldType));
        #endregion

        #region Assembly
        static Type[] CurrentDomainAssemblyTypes_Cached;
        public static Type[] CurrentDomainAssemblyTypes =>
            CurrentDomainAssemblyTypes_Cached ??=
                AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes()).ToArray();
        #endregion

        #region Member Tree
        readonly static Dictionary<Type, MemberTree> MemberTrees = new();
        public class MemberTree
        {
            public readonly Type type;
            public readonly MemberTree parent;
            public readonly string path;
            public readonly MemberInfo member;
            public readonly Dictionary<string, MemberInfo> numerics;
            public readonly Dictionary<string, MemberTree> children;

            public MemberTree(Type type, MemberTree parent = null, MemberInfo member = null)
            {
                if (parent.Iterate(p => p.parent).Any(p => p.type == type))
                {
                    Debug.LogError($"MemberTree '{type.Name}' is recursive");
                    return;
                }

                this.type = type;
                this.member = member;
                this.parent = parent;
                this.path = member == null ? "" : $"{path}/{member.Name}";
                this.numerics = type.GetFields(DefaultBindings)
                    .Where(f => f.FieldType.IsNumeric())
                    .Cast<MemberInfo>()
                    .Concat(type.GetProperties(DefaultBindings)
                        .Where(p => p.PropertyType.IsNumeric() && p.CanWrite
                            && p.GetIndexParameters().Length == 0))
                    .ToDictionary(m => m.Name, m => m);

                static bool IsClassObject(Type type) =>
                    type.IsClass && type != typeof(string)
                    && !type.IsSubclassOf(typeof(UnityEngine.Object))
                    && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type);

                children = type.GetFields(DefaultBindings)
                    .Where(f => IsClassObject(f.FieldType))
                    .Select(f => (member: (MemberInfo)f, type: f.FieldType))
                    .Select(t => new MemberTree(t.type, this, t.member))
                    .ToDictionary(m => m.member.Name, m => m);
            }

            object GetMemberValue(object target, MemberInfo member) => member switch
            {
                FieldInfo f => f.GetValue(target),
                PropertyInfo p => p.GetValue(target),
                _ => null
            };

            public (MemberTree tree, object target) GetObject(object root, string path)
            {
                if (root == null) return (null, null);
                (MemberTree tree, object target) current = (this, root);
                if (!string.IsNullOrEmpty(path))
                {
                    string[] parts = path.Split('/');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (current.tree.children.TryGetValue(parts[i], out var ctree)
                            && GetMemberValue(current.target, ctree.member) is object o)
                            current = (ctree, o);
                        else return (null, null);
                    }
                }
                return current;
            }

            public object GetValue(object root, string path)
            {
                var parts = path.Split('/');
                var current = GetObject(root, string.Join("/", parts[..^1]));
                if (current.target == null) return default;
                if (current.tree.numerics.TryGetValue(parts[^1], out var member))
                    return GetMemberValue(current.target, member);
                return null;
            }

            public void SetValue<T>(object root, string path, T value)
            {
                var parts = path.Split('/');
                var current = GetObject(root, string.Join("/", parts[..^1]));
                if (current.target == null) return;
                if (current.tree.numerics.TryGetValue(parts[^1], out var member))
                {
                    if (member is FieldInfo f) f.SetValue(current.target, value);
                    else if (member is PropertyInfo p) p.SetValue(current.target, value);
                }
            }

            public (string path, MemberInfo member)[] GetNumerics() =>
                Collect(this, string.Empty).ToArray();

            static IEnumerable<(string, MemberInfo)> Collect(MemberTree tree, string prefix)
            {
                string GetPath(string name) => string.IsNullOrEmpty(prefix) ? name : $"{prefix}/{name}";
                return tree.numerics.Values.Select(m => (GetPath(m.Name), m))
                    .Concat(tree.children.Values.SelectMany(child => Collect(child, GetPath(child.type.Name))));
            }

            bool IsRecursive(MemberTree p)
            {
                for (; p; p = p.parent)
                    if (p.type == type) return true;
                return false;
            }
            public static implicit operator bool(MemberTree empty) => empty != null;
        }

        public static bool IsNumeric(this Type type) => type == typeof(int) || type == typeof(float);

        public static MemberTree GetMemberTree(this Type type) =>
            MemberTrees.TryGetValue(type, out MemberTree tree) ? tree
                : MemberTrees[type] = new MemberTree(type);

        #endregion
    }
}

// .Concat(type.GetProperties(BindingFlags)
//     .Where(p => IsClassObject(p.PropertyType) && p.GetIndexParameters().Length == 0)
//     .Select(p => (member: (MemberInfo)p, type: p.PropertyType)))
// .Distinct()