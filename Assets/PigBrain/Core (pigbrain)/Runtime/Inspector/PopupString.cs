using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class PopupStringAttribute : PropertyAttribute
    {
        internal readonly string member;
        public PopupStringAttribute(string member) => this.member = member;
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using pigbrain.core.Collections;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(PopupStringAttribute))]
    public class PopupStringAttribute_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (PopupStringAttribute)attribute;
            var value = property.GetOwner().GetMemberValue(attr.member);
            if (value is string[] array)
            {
                if (!array.IsNullOrEmpty())
                {
                    int index = System.Array.IndexOf(array, property.stringValue);
                    if (index < 0) index = 0;
                    int newIndex = EditorGUI.Popup(position, label.text, index, array);
                    if (newIndex >= 0 && newIndex < array.Length)
                        property.stringValue = array[newIndex];
                    return;
                }
            }

            //Draw normally
            // using (new EditorGUI.DisabledScope(true))
            EditorGUI.PropertyField(position, property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
