using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class FlattenObjectAttribute : PropertyAttribute
    {
        public readonly bool show;
        public readonly float labelWidth;
        public FlattenObjectAttribute(bool show = false, float labelWidth = -1)
        {
            this.show = show;
            this.labelWidth = labelWidth;
        }
        public bool isVisible => show;
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(FlattenObjectAttribute), true)]
    public class FlattenObjectAttribute_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            var element = attribute as FlattenObjectAttribute;

            if (prop.HasFoldout())
            {
                using (new LabelWidthScope(element.labelWidth > 0 ? element.labelWidth : LabelWidth))
                    prop.IterateProperties().DrawPropertyFields(pos);
            }
            else EditorGUI.PropertyField(pos, prop, element.isVisible ? label : GUIContent.none, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            if (prop.HasFoldout()) return prop.IterateProperties().GetPropertyFieldsHeight() + VerticalSpacing;
            return LineHeight + VerticalSpacing;
        }
    }
}
#endif