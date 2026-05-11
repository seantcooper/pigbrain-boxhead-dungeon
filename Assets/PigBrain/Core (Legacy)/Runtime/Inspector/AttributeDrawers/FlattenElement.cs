// using System;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Utility
// {
//     [AttributeUsage(AttributeTargets.Field)]
//     public class FlattenElementAttribute : PropertyAttribute
//     {
//         public FlattenElementAttribute() { }
//     }
// }

// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.Utility
// {
//     using UnityEditor;

//     [CustomPropertyDrawer(typeof(FlattenElementAttribute))]
//     public class FlattenElement_PropertyDrawer : PropertyDrawer
//     {
//         new FlattenElementAttribute attribute => base.attribute as FlattenElementAttribute;
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             if (!property.IsArrayElement()) EditorGUI.PropertyField(position, property, label, true);
//             else EditorGUI.PropertyField(position, property, GUIContent.none, true);
//         }

//         // public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
//         //     base.GetPropertyHeight(property, label);
//     }

// }
// #endif
