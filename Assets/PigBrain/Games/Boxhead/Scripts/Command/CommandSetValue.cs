using System;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.core.Utility;
using pigbrain.game.Boxhead.Statistic;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Set Value")]
    public class CommandSetValue : Command
    {
        [Header("Set Value")]
        [SerializeField] TypeProperty propertyType;
        [SerializeField] internal Assignment assignment;
        [SerializeField] internal GameObject assignEffect;

        protected override bool OnInvoke(Transform target)
        {
            propertyType.Assign(target, assignment, assignEffect ? CreateEffect : null);
            return true;
        }

        void CreateEffect(Transform transform) =>
            assignEffect.Instantiate(transform).transform
                .SetLocalPositionAndRotation(default, Quaternion.identity);

        public enum Assignment { TargetOnly, TargetType, }

        [Serializable]
        public class TypeProperty
        {
            [SerializeField] internal string componentType;
            [SerializeField] internal string filterName;
            [SerializeField] internal string propertyName;
            [SerializeField] internal Upgrade.UStat.OP op;
            [SerializeField] internal float value;

            internal void Assign(Transform target, Assignment assignment = Assignment.TargetOnly, Action<Transform> onAssign = null)
            {
                if (!target) return;

                if (string.IsNullOrEmpty(componentType) || string.IsNullOrEmpty(propertyName)) return;

                var type = Type.GetType(componentType);
                if (type == null) return;

                var prop = type.GetProperty(propertyName, ReflectionUtility.DefaultBindings);
                if (prop == null) return;

                switch (assignment)
                {
                    case Assignment.TargetOnly:
                        if (target.TryGetComponent(type, out Component component)) AssignValue(component);
                        break;

                    case Assignment.TargetType:
                        foreach (Component o in FindObjectsByType(type))
                        {
                            if (!o) continue;
                            if (!string.IsNullOrEmpty(filterName)
                                && !o.name.Contains(filterName, StringComparison.OrdinalIgnoreCase)) continue;
                            AssignValue(o);
                        }
                        break;
                }

                void AssignValue(Component component)
                {
                    float input = (float)Convert.ChangeType(prop.GetValue(component), typeof(float));
                    object useValue = Upgrade.UStat.Apply(input, value, op);
                    prop.SetValue(component, Convert.ChangeType(useValue, prop.PropertyType));
                    onAssign?.Invoke(component.transform);
                }
            }
        }
    }

    public interface ISetValue { }

    [AttributeUsage(AttributeTargets.Property)]
    public class SetValueAttribute : Attribute { public SetValueAttribute() { } }
}

#if UNITY_EDITOR
namespace pigbrain.game.Boxhead
{
    using System.Linq;
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(CommandSetValue.TypeProperty))]
    public class CommandSetValue_TypeProperty_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            pos.height = LineHeight;

            #region └Component Type
            var componentType = prop.FindPropertyRelative(nameof(CommandSetValue.TypeProperty.componentType));

            var types = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(t => typeof(ISetValue).IsAssignableFrom(t) && !t.IsAbstract).ToArray();

            var typeNames = types.Select(t => t.FullName).ToArray();

            int sti = Mathf.Max(0, types.IndexOf(t => t.AssemblyQualifiedName == componentType.stringValue));
            sti = EditorGUI.Popup(pos, componentType.displayName, sti, typeNames);
            if (types.Length > 0) componentType.stringValue = types[sti].AssemblyQualifiedName;
            #endregion

            pos = pos.AddY(FullLineHeight);
            var areas = pos.DivideArea(Padding, 0, TinyFieldWidth, MiniFieldWidth).ToArray();

            #region └Filter
            var filter = prop.FindPropertyRelative(nameof(CommandSetValue.TypeProperty.filterName));
            EditorGUILayout.PropertyField(filter);
            #endregion

            #region └Property
            var propertyName = prop.FindPropertyRelative(nameof(CommandSetValue.TypeProperty.propertyName));

            var selectedComponentType = types.Length > 0 && sti > -1 ? types[sti] : null;
            string[] properties = selectedComponentType == null ? Array.Empty<string>()
                : selectedComponentType.GetProperties(ReflectionUtility.DefaultBindings)
                    .Where(p => p.CanWrite && p.GetCustomAttributes(typeof(SetValueAttribute), true).Length > 0)
                    .Select(p => p.Name)
                    .ToArray();

            int spi = Mathf.Max(0, Array.IndexOf(properties, propertyName.stringValue));
            spi = EditorGUI.Popup(areas[0], propertyName.displayName, spi, properties);
            if (properties.Length > 0) propertyName.stringValue = properties[spi];
            #endregion

            #region └OP
            var op = prop.FindPropertyRelative(nameof(CommandSetValue.TypeProperty.op));
            EditorGUI.PropertyField(areas[1], op, GUIContent.none);
            #endregion

            #region └Value
            var value = prop.FindPropertyRelative(nameof(CommandSetValue.TypeProperty.value));
            EditorGUI.PropertyField(areas[2], value, GUIContent.none);
            #endregion

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight * 2;
    }
}
#endif