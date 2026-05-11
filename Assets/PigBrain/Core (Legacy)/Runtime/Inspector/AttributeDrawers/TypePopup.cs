// #pragma warning disable UDR0001
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Utility
// {
//     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
//     public class TypePopupAttribute : PropertyAttribute
//     {
//         internal readonly Type type;
//         internal readonly bool subclassesOnly;
//         public TypePopupAttribute(Type type, bool subclassesOnly = true)
//         {
//             this.type = type;
//             this.subclassesOnly = subclassesOnly;
//         }

//         public IEnumerable<Type> GetTypes() =>
//             ReflectionUtility.CurrentDomainAssemblyTypes.Where(t => t.IsSubclassOf(type) || (!subclassesOnly && type == t));
//     }
// }

// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.Utility
// {
//     using UnityEditor;
//     using static UnityEditor.EditorGUI;
//     using static Inspector;

//     [CustomPropertyDrawer(typeof(TypePopupAttribute), true)]
//     public class TypePopupAttribute_PropertyDrawer : PropertyDrawer
//     {
//         Type[] types;
//         public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//         {
//             BeginProperty(position, label, property);

//             if (property.propertyType != SerializedPropertyType.String)
//             {
//                 HelpBox(position, "Can only be used with string!", MessageType.Warning);
//             }
//             else
//             {
//                 var attribute = this.attribute as TypePopupAttribute;
//                 types ??= attribute.GetTypes().ToArray();

//                 var names = types.Select(t => t.Name).ToArray();
//                 var index = Array.IndexOf(names, property.stringValue);

//                 // public static int Popup(Rect position, string label, int selectedIndex, string[] displayedOptions)
//                 if ((index = Popup(position, label.text, index, names)) != -1)
//                     property.stringValue = names[index];

//             }
//             EndProperty();
//         }
//     }

// }
// #endif