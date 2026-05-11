using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float min, max;
        public MinMaxRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using static UnityEditor.EditorGUI;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxPropertyDrawer : PropertyDrawer
    {
        // Set your overall range here
        private const int SLIDER_MIN = 0;
        private const int SLIDER_MAX = 100;

        // public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        // {
        //     // if (property.serializedObject.isEditingMultipleObjects)
        //     return EditorGUIUtility.singleLineHeight;
        //     // return EditorGUI.GetPropertyHeight(property, label, true);
        // }

        public override void OnGUI(Rect p, SerializedProperty prop, GUIContent label)
        {
            var attribute = this.attribute as MinMaxRangeAttribute;

            SerializedProperty minProp = prop.FindPropertyRelative("min"), maxProp = prop.FindPropertyRelative("max");
            BeginProperty(p, label, prop);
            p = PrefixLabel(p, label);

            var areas = p.DivideArea(Padding, 0, MiniFieldWidth, MiniFieldWidth).ToArray();
            new[] { minProp, maxProp }.DrawPropertyFields(areas.Skip(1));

            if (prop.serializedObject.isEditingMultipleObjects)
            {
                HelpBox(areas[0], "MinMaxSlider cannot be multi-edited.", MessageType.Warning);
            }

            else if (minProp.GetFieldInfo().FieldType == typeof(int))
            {
                (float min, float max) = (minProp.intValue, maxProp.intValue);
                MinMaxSlider(areas[0], ref min, ref max, attribute.min, attribute.max);
                minProp.intValue = Mathf.RoundToInt(min); maxProp.intValue = Mathf.RoundToInt(max);
            }

            else
            {
                (float min, float max) = (minProp.floatValue, maxProp.floatValue);
                MinMaxSlider(areas[0], ref min, ref max, attribute.min, attribute.max);
                minProp.floatValue = min; maxProp.floatValue = max;
            }
            EndProperty();
        }
    }
}
#endif
