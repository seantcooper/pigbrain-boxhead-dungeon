#pragma warning disable UDR0001
using System;
using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class InlineObjectAttribute : PropertyAttribute
    {
        internal readonly Color color;
        public InlineObjectAttribute(string color = null)
        {
            ColorUtility.TryParseHtmlString(string.IsNullOrEmpty(color) ? ColorHex.grey : color, out this.color);
            this.color = this.color.WithA(0.33f);
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using static UnityEditor.EditorGUI;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(InlineObjectAttribute), true)]
    public class InlineObjectAttribute_PropertyDrawer : PropertyDrawer
    {
        new InlineObjectAttribute attribute => base.attribute as InlineObjectAttribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BeginProperty(position, label, property);
            DrawHeader(position.WithH(HeaderHeight), label.text, attribute.color);

            property.IterateToEndDraw(position.AddY(FullHeaderHeight).AddMinX(IndentSize),
                (r, p) => PropertyField(r, p, true));

            EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            property.IterateToEndHeight() + FullHeaderHeight;
    }
}
#endif