// using System;
// using System.Reflection;

// namespace PigBrain.LegacyCore.Utility
// {
//     [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
//     public class InlineButton : Attribute
//     {
//         public const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//         public string[] methods;
//         public bool populateAll = false;
//         public int groupCount = 3;
//         public BindingFlags bindingFlags = bindings;

//         public InlineButton(params string[] methods) => this.methods = methods;

//         public InlineButton(bool populateAll, params string[] methods) : this(methods) =>
//             this.populateAll = populateAll;

//         public InlineButton(bool populateAll, int groupCount, params string[] methods) : this(populateAll, methods) =>
//             this.groupCount = groupCount;
//     }
// }

// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.Utility
// {
//     using UnityEditor;
//     using UnityEngine;
//     using System.Reflection;
//     using System.Linq;

//     [CanEditMultipleObjects]
//     [CustomEditor(typeof(ScriptableObject), true)]
//     class ScriptableObject_AttributeButtonEditor : AttributeButtonEditor { }

//     [CanEditMultipleObjects]
//     [CustomEditor(typeof(MonoBehaviour), true)]
//     class MonoBehaviour_AttributeButtonEditor : AttributeButtonEditor { }

//     [CanEditMultipleObjects]
//     class AttributeButtonEditor : Editor
//     {
//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();

//             var type = target.GetType();
//             var attributes = type.GetCustomAttributes<InlineButton>(true).ToArray();
//             if (attributes.Length == 0) return;

//             void Invoke(string displayName, params MethodInfo[] methods)
//             {
//                 Undo.RecordObject(target, $"Invoke {displayName}");
//                 foreach (var method in methods)
//                     method.Invoke(target, null);
//                 EditorUtility.SetDirty(target);

//                 // calls the validate method (if it has it)
//                 if (type.GetMethod("OnValidate", InlineButton.bindings) is MethodInfo validate)
//                     validate.Invoke(target, null);
//             }

//             void DrawButton(InlineButton attribute, int index, string displayName, params MethodInfo[] methods)
//             {
//                 if ((index % attribute.groupCount) == 0)
//                 {
//                     if (index > 0) GUILayout.EndHorizontal();
//                     GUILayout.BeginHorizontal();
//                 }
//                 if (GUILayout.Button(displayName)) Invoke(displayName, methods);
//             }

//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
//             foreach (var attribute in attributes)
//             {
//                 if (attribute.methods.Count() == 0) continue;

//                 GUILayout.BeginVertical("box");
//                 var methods = attribute.methods.Select(n => type.GetMethod(n, attribute.bindingFlags)).ToArray();
//                 methods.ForEach((m, i) => DrawButton(attribute, i, ObjectNames.NicifyVariableName(m.Name), m));

//                 if (attribute.populateAll)
//                     DrawButton(attribute, methods.Count(), "All", methods);

//                 GUILayout.EndHorizontal();
//                 GUILayout.EndVertical();
//             }
//         }
//     }
// }
// #endif
