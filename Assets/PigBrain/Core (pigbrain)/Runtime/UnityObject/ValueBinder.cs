using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static pigbrain.core.Utility.ReflectionUtility;

namespace pigbrain.core.Inspector
{
    [Serializable]
    public class ValueBinder<T> : ValueBinderBase
    {
        [SerializeField] T value;
        public void SetValue(Transform transform, T value)
        {
            var key = (transform, this);
            if (!Connectors.TryGetValue(key, out var connector))
            {
                if (!transform.gameObject.TryGetComponent(Type.GetType(type), out var component)) return;
                Connectors.Add(key, connector = new Connector { component = component });
            }
            connector.SetValue(memberName, value);
        }
    }

    [Serializable]
    public class ValueBinderBase
    {
        public string type;
        public string memberName;
        public OP op;

        public virtual object GetValue() => null;

        // Runtime
        protected readonly static Dictionary<(Transform, ValueBinderBase), Connector> Connectors = new();

        protected class Connector
        {
            public Component component;
            public MemberInfo info;

            internal void SetValue(string memberName, object value)
            {
                info ??= GetMember(memberName, value);
                if (info == null) return;
                switch (info)
                {
                    case FieldInfo f: f.SetValue(component, value); break;
                    case PropertyInfo p: p.SetValue(component, value); break;
                    case MethodInfo m: m.Invoke(component, new[] { value }); break;
                        // default: throw new NotImplementedException($"Unsupported member type: {info}");
                }
            }

            MemberInfo GetMember(string memberName, object value)
            {
                if (component == null || value == null) return null;

                Type valueType = value.GetType(), type = component.GetType();

                if (type.GetProperty(memberName, DefaultBindings) is PropertyInfo prop
                    && prop.CanWrite
                    && prop.PropertyType == valueType)
                    return prop;

                if (type.GetField(memberName, DefaultBindings) is FieldInfo field
                    && field.FieldType == valueType)
                    return field;

                if (type.GetMethod(memberName, DefaultBindings) is MethodInfo method
                    && method.GetParameters() is ParameterInfo[] pi
                    && pi.Length == 1 && pi[0].ParameterType == valueType)
                    return method;

                return null;
            }
        }

        public enum OP
        {
            [InspectorName("=")] Set,
            [InspectorName("+")] Add,
            [InspectorName("−")] Sub,
            [InspectorName("×")] Mul,
            [InspectorName("÷")] Div,
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    using static pigbrain.core.Inspector.ValueBinderBase;

    [CustomPropertyDrawer(typeof(ValueBinderBase), true)]
    public class ValueBinderBase_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            pos = pos.WithH(LineHeight).AddMinX(EditorGUI.indentLevel * IndentSize);
            using var _ = new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel);

            var areas = pos.DivideArea(Padding, 0, 160, MiniFieldWidth).ToArray();
            var typeProp = prop.FindPropertyRelative(nameof(ValueBinderBase.type));
            var memberProp = prop.FindPropertyRelative(nameof(ValueBinderBase.memberName));
            var opProp = prop.FindPropertyRelative(nameof(ValueBinderBase.op));
            var valueProp = prop.FindPropertyRelative("value");

            if (InlineContextScope.TryGetParentContext(prop.serializedObject, out var parent))
            {
                // Draw Component
                Component targetComponent = GetTargetComponent(typeProp, parent);
                Component newComp = EditorGUI.ObjectField(areas[0], targetComponent, typeof(Component), true) as Component;
                if (newComp != null) typeProp.stringValue = newComp.GetType().AssemblyQualifiedName;

                // Draw member
                string[] names = GetMemberNames(valueProp, targetComponent);
                var rawNames = names.Select(n => n.Split(' ')[0]).ToArray();
                int current = Array.IndexOf(rawNames, memberProp.stringValue);
                int next = EditorGUI.Popup(areas[1], current < 0 ? 0 : current, names);

                if (names.Length > 0 && next >= 0 && next < rawNames.Length)
                    memberProp.stringValue = rawNames[next];

                // Draw Op via lookup
                var propType = valueProp.propertyType;
                GetFilteredOps(valueProp.propertyType, out var opArray, out var namesOp);
                int currentOp = Array.IndexOf(opArray, (OP)opProp.enumValueIndex);
                int selected = EditorGUI.Popup(areas[2], currentOp < 0 ? 0 : currentOp, namesOp);

                if (opArray.Length > 0 && selected >= 0 && selected < opArray.Length)
                    opProp.enumValueIndex = (int)opArray[selected];

                // Draw Value
                // if (valueProp != null && 3 < areas.Length)
                // {
                //     EditorGUI.TextField(areas[3], GetValueType(valueProp).Name);
                //     // EditorGUI.PropertyField(areas[3], valueProp, GUIContent.none);
                // }
            }
            else
            {
                string typeName = Type.GetType(typeProp.stringValue)?.FullName ?? "";
                EditorGUI.TextField(areas[0], typeName);
                EditorGUI.PropertyField(areas[1], memberProp, GUIContent.none);
                EditorGUI.PropertyField(areas[2], opProp, GUIContent.none);
                // EditorGUI.TextField(areas[3], GetValueType(valueProp).Name);
            }

            EditorGUI.EndProperty();
        }

        static Component GetTargetComponent(SerializedProperty prop, SerializedObject parent)
        {
            if (parent.targetObject && parent.targetObject is Component component)
                if (!string.IsNullOrEmpty(prop.stringValue))
                    if (Type.GetType(prop.stringValue) is Type t)
                        return component.GetComponent(t);
            return null;
        }

        static string[] GetMemberNames(SerializedProperty prop, Component targetComponent)
        {
            Type valueType = GetValueType(prop);
            if (targetComponent != null && valueType != null)
            {
                var t = targetComponent.GetType();
                return t.GetMembers(DefaultBindings)
                    .Where(m => m switch
                        {
                            FieldInfo f => f.FieldType == valueType,
                            PropertyInfo pr => pr.CanWrite && pr.PropertyType == valueType,
                            MethodInfo me => me.GetParameters() is ParameterInfo[] ps &&
                                ps.Length == 1 && ps[0].ParameterType == valueType,
                            _ => false
                        }
                    )
                    .Select(m => $"{m.Name} ({prop.propertyType})")
                    // .Select(m => m switch
                    // {
                    //     FieldInfo f => $"{f.Name} ({f.FieldType.Name})",
                    //     PropertyInfo pr => $"{pr.Name} ({pr.PropertyType.Name})",
                    //     MethodInfo me => $"{me.Name} ({me.GetParameters()[0].ParameterType.Name})",
                    //     _ => m.Name
                    // })
                    .ToArray();
            }
            return Array.Empty<string>();
        }

        static Type GetValueType(SerializedProperty prop) =>
            PropertyTypeLookup.TryGetValue(prop.propertyType, out Type type) ? type : null;

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            FullLineHeight;

        static void GetFilteredOps(SerializedPropertyType propType, out OP[] ops, out string[] names)
        {
            ops = OpsByType.TryGetValue(propType, out var mapped) ? mapped
            : Enum.GetValues(typeof(OP)).Cast<OP>().ToArray();

            names = ops.Select(o =>
            {
                var fi = typeof(OP).GetField(o.ToString());
                var attr = fi.GetCustomAttributes(typeof(InspectorNameAttribute), false)
                                .FirstOrDefault() as InspectorNameAttribute;
                return attr != null ? attr.displayName : o.ToString();
            }).ToArray();
        }

        static readonly Dictionary<SerializedPropertyType, OP[]> OpsByType = new()
        {
            { SerializedPropertyType.String,  new[]{ OP.Set } },
            { SerializedPropertyType.Boolean, new[]{ OP.Set } },

            // numeric
            { SerializedPropertyType.Integer, new[]{  OP.Set,OP.Add, OP.Sub, OP.Mul, OP.Div } },
            { SerializedPropertyType.Float,   new[]{  OP.Set,OP.Add, OP.Sub, OP.Mul, OP.Div } },

            // vectors / structs (typically only set makes sense)
            { SerializedPropertyType.Vector2, new[]{ OP.Set } },
            { SerializedPropertyType.Vector3, new[]{ OP.Set } },
            { SerializedPropertyType.Vector4, new[]{ OP.Set } },
            { SerializedPropertyType.Vector2Int, new[]{ OP.Set } },
            { SerializedPropertyType.Vector3Int, new[]{ OP.Set } },

            { SerializedPropertyType.Color, new[]{ OP.Set } },
            { SerializedPropertyType.Quaternion, new[]{ OP.Set } },

            { SerializedPropertyType.Rect, new[]{ OP.Set } },
            { SerializedPropertyType.Bounds, new[]{ OP.Set } },
            { SerializedPropertyType.RectInt, new[]{ OP.Set } },
            { SerializedPropertyType.BoundsInt, new[]{ OP.Set } },

            { SerializedPropertyType.ObjectReference, new[]{ OP.Set } },
            { SerializedPropertyType.Enum, new[]{ OP.Set } },
            { SerializedPropertyType.LayerMask, new[]{ OP.Set } },

            { SerializedPropertyType.AnimationCurve, new[]{ OP.Set } },
            { SerializedPropertyType.ExposedReference, new[]{ OP.Set } },
            { SerializedPropertyType.FixedBufferSize, new[]{ OP.Set } }
        };
    }
}
#endif



