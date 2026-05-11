using System;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class OneLinerAttribute : PropertyAttribute
    {
        public readonly string[] fields;
        public readonly float[] sizes;
        public OneLinerAttribute(string[] fields, float[] sizes = null)
        {
            this.fields = fields;
            this.sizes = sizes.IsNullOrEmpty() ? new float[fields.Length] : sizes;
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using UnityEditor;
    using UnityEngine;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(OneLinerAttribute), true)]
    public class OneLiner_Drawer : PropertyDrawer
    {
        public virtual string[] fields => null;
        public virtual float[] sizes => null;
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            OneLinerAttribute attribute = this.attribute as OneLinerAttribute;

            string[] fields = this.fields;
            float[] sizes = this.sizes;
            if (attribute != null)
            {
                fields = attribute.fields;
                sizes = attribute.sizes;
            }

            prop.GetProperties(fields).DrawPropertyFields(
                pos.WithH(LineHeight).DivideArea(Padding, sizes));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            LineHeight + VerticalSpacing;
    }
}
#endif

