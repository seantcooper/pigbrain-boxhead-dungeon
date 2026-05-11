using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute
    {
        internal readonly ReadOnlyState state;
        internal readonly string member;
        public ReadOnlyAttribute(ReadOnlyState state = ReadOnlyState.Always) => this.state = state;
        public ReadOnlyAttribute(string isReadOnly) => member = isReadOnly;
    }
    public enum ReadOnlyState { Always, Runtime, Editor }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        new ReadOnlyAttribute attribute => base.attribute as ReadOnlyAttribute;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enabled = GUI.enabled;
            GUI.enabled = IsEnabled(property);
            // Color color = GUI.color;
            // GUI.color = Color.aliceBlue;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = enabled;
            // GUI.color = color;
        }

        bool IsEnabled(SerializedProperty property)
        {
            var value = property.GetOwner().GetMemberValue(attribute.member);
            if (value is bool b)
                return !b;

            return attribute.state switch
            {
                ReadOnlyState.Runtime => !Application.isPlaying,
                ReadOnlyState.Editor => Application.isPlaying,
                _ => false,
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif