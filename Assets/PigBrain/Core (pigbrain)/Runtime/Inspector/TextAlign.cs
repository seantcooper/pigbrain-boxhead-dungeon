using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class TextAlignAttribute : PropertyAttribute
    {
        public TextAlign alignment;
        public TextAlignAttribute(TextAlign alignment = TextAlign.Left)
        {
            this.alignment = alignment;
        }
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TextAlignAttribute))]
    public class TextAlignDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var attr = (TextAlignAttribute)attribute;

            EditorGUI.BeginProperty(pos, label, prop);

            var labelRect = EditorGUI.PrefixLabel(pos, label);
            var fieldRect = labelRect;
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;

            var style = new GUIStyle(EditorStyles.textField);

            style.alignment = attr.alignment switch
            {
                TextAlign.Center => TextAnchor.MiddleCenter,
                TextAlign.Right => TextAnchor.MiddleRight,
                _ => TextAnchor.MiddleLeft,
            };

            EditorGUI.BeginChangeCheck();

            if (prop.propertyType == SerializedPropertyType.String)
            {
                var value = EditorGUI.TextField(fieldRect, prop.stringValue, style);
                if (EditorGUI.EndChangeCheck())
                    prop.stringValue = value;
            }
            else if (prop.propertyType == SerializedPropertyType.Integer)
            {
                var value = EditorGUI.IntField(fieldRect, prop.intValue, style);
                if (EditorGUI.EndChangeCheck())
                    prop.intValue = value;
            }
            else if (prop.propertyType == SerializedPropertyType.Float)
            {
                var value = EditorGUI.FloatField(fieldRect, prop.floatValue, style);
                if (EditorGUI.EndChangeCheck())
                    prop.floatValue = value;
            }
            else
            {
                EditorGUI.PropertyField(fieldRect, prop, GUIContent.none);
                EditorGUI.EndChangeCheck();
            }

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }
    }
}
#endif
