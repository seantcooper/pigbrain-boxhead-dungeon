using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class HorizontalAttribute : PropertyAttribute
    {
        public readonly float[] widths;
        public HorizontalAttribute(params float[] widths) => this.widths = widths;
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using pigbrain.core.Collections;
    using pigbrain.core.Geom;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(HorizontalAttribute), true)]
    public class HorizontalAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            // var a = attribute as HorizontalAttribute;
            // pos.WithH(LineHeight).DivideArea(Padding, a.widths);

            // prop.IterateProperties().ForEach(p =>
            // {
            //     pos.height = EditorGUI.GetPropertyHeight(p, true);
            //     EditorGUI.PropertyField(pos, p, true);
            //     pos.y = pos.yMax + VerticalSpacing;
            // });

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            prop.IterateProperties().Sum(p => EditorGUI.GetPropertyHeight(p, true));
    }
}
#endif