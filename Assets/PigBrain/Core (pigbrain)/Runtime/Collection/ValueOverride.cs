using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace pigbrain.core.Collections
{
    public static class OverrideResolver
    {
        public static T ResolveScriptableObject<T>(params T[] resolveObjects) where T : ScriptableObject =>
            Resolve(resolveObjects,
                empty: () => null,
                clone: (o) => UnityEngine.Object.Instantiate(o));

        public static T ResolveObject<T>(params T[] resolveObjects) where T : class, new() =>
            Resolve(resolveObjects,
                empty: () => throw new Exception("ResolveObject: No items have values!"),
                clone: (o) => JsonUtility.FromJson<T>(JsonUtility.ToJson(o)));

        // public static T ResolveObject<T>(params T[] resolveObjects) where T : ICloneable =>
        //     Resolve(resolveObjects,
        //         empty: () => throw new Exception("ResolveObject: No items have values!"),
        //         clone: (o) => (T)o.Clone());

        static T Resolve<T>(T[] resolveObjects, Func<T> empty, Func<T, T> clone)
        {
            T[] items = resolveObjects.Where(r => r != null).ToArray();
            if (items.Length == 0) return empty();
            T instance = clone(items[0]);
            if (items.Length > 1)
            {
                var overrideFields = GetOverrideFields(typeof(T)).ToArray();
                var used = overrideFields.Where(f => (bool)f.setField.GetValue(f.field.GetValue(instance))).ToHashSet();
                for (int i = 1; i < items.Length && overrideFields.Length != used.Count; i++)
                {
                    foreach (var f in overrideFields)
                    {
                        if (used.Contains(f)) continue;
                        var srcOverride = f.field.GetValue(items[i]);
                        if ((bool)f.setField.GetValue(srcOverride))
                        {
                            f.field.SetValue(instance, srcOverride);
                            used.Add(f);
                        }
                    }
                }
            }
            return instance;
        }

        class OverrideField
        {
            public readonly FieldInfo field, setField;
            public readonly Type fieldType, genericArg;

            public OverrideField(FieldInfo field)
            {
                this.field = field;
                this.fieldType = field.FieldType;
                this.genericArg = fieldType.GetGenericArguments()[0];
                this.setField = fieldType.GetField("set");
            }
        }

        static readonly Dictionary<Type, OverrideField[]> typeFields = new();
        const BindingFlags BFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static OverrideField[] GetOverrideFields(Type type)
        {
            if (!typeFields.TryGetValue(type, out var fields))
            {
                fields = typeFields[type] = type.GetFields(BFlags)
                    .Where(f =>
                        f.FieldType.IsGenericType &&
                        f.FieldType.GetGenericTypeDefinition() == typeof(Override<>))
                .Select(f => new OverrideField(f)).ToArray();
            }
            return fields;
        }
    }

    [Serializable]
    public struct Override<T>
    {
        public bool set;
        public T value;
        public Override(T value)
        {
            this.value = value;
            this.set = true;
        }
        public static implicit operator T(Override<T> obj) => (T)obj.value;
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Collections
{
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    [CustomPropertyDrawer(typeof(Override<>))]
    public class Override_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect p, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(p, label, property);

            var setProp = property.FindPropertyRelative("set");
            var valueProp = property.FindPropertyRelative("value");

            EditorGUI.PropertyField(p.WithW(16), setProp, GUIContent.none);
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUI.DisabledScope(!setProp.boolValue))
                EditorGUI.PropertyField(p, valueProp, label, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => LineHeight + VerticalSpacing;
    }
}
#endif
