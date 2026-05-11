using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class Row : PropertyAttribute
    {
        public Row() { }

        public class Width : PropertyAttribute
        {
            public readonly float width;
            public Width(float width) => this.width = width;
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using pigbrain.core.Geom;
    using pigbrain.core.Utility;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(Row))]
    public class UStat_PropertyDrawer : PropertyDrawer
    {
        // new Row attribute => base.attribute as Row;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var props = property.IterateProperties().ToArray();
            float[] widths = new float[props.Length];
            for (int i = 0; i < props.Length; i++)
            {
                var type = fieldInfo.FieldType;

                var parentType = type.IsArray
                    ? type.GetElementType()
                    : (type.IsGenericType && typeof(System.Collections.IList).IsAssignableFrom(type)
                        ? type.GetGenericArguments()[0]
                        : type);

                var field = parentType.GetField(props[i].name, ReflectionUtility.DefaultBindings);
                var attribute = field?.GetCustomAttributes(typeof(Row.Width), true).FirstOrDefault() as Row.Width;
                widths[i] = attribute?.width ?? 0;
            }
            var areas = position.DivideArea(Padding, widths).ToArray();

            for (int i = 0; i < props.Length; i++)
            {
                var (p, r) = (props[i], areas[i]);
                EditorGUI.PropertyField(r, p, GUIContent.none);
                if (p.propertyType == SerializedPropertyType.String && string.IsNullOrEmpty(p.stringValue))
                {
                    using (new GUIColorScope(new Color(1f, 1f, 1f, 0.35f)))
                        EditorGUI.LabelField(r.AddMinX(4), p.displayName);
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
