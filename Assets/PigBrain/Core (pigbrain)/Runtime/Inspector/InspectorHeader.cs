// #pragma warning disable UDR0001
// using System;
// using UnityEngine;

// namespace pigbrain.Core.Inspector
// {
//     [AttributeUsage(AttributeTargets.Method)]
//     public class InspectorHeaderButtonAttribute : Attribute { }
// }

// #if UNITY_EDITOR
// namespace pigbrain.Core.Inspector
// {
//     using System.Linq;
//     using UnityEditor;
//     [InitializeOnLoad]
//     public static class InspectorHeaderToolbarDrawer
//     {
//         static Action<Editor> OnDrawToolbar;
//         static InspectorHeaderToolbarDrawer()
//         {
//             var methods = TypeCache.GetMethodsWithAttribute<InspectorHeaderButtonAttribute>();
//             foreach (var method in methods)
//             {
//                 if (!method.IsStatic) continue;
//                 var action = (Action<Editor>)Delegate.CreateDelegate(typeof(Action<Editor>), method);
//                 OnDrawToolbar += action;
//             }
//             Editor.finishedDefaultHeaderGUI += OnFinishedHeaderGUI;
//         }

//         private static void OnFinishedHeaderGUI(Editor editor) => OnDrawToolbar?.Invoke(editor);

//         // [InspectorHeaderButton]
//         static void OnHeaderButton(Editor editor)
//         {
//             GUILayout.TextArea("Header Button");
//             // if (GUILayout.Button(EditorGUIUtility.IconContent("d_SceneViewAudio"), EditorStyles.toolbarButton))
//             // {
//             //     GUILayout.TextArea("Header Button");
//             //     // Debug.Log("something special");
//             // }
//         }

//         // [InspectorHeaderButton]
//         static void OnAudioButton(Editor editor)
//         {
//             if (!(editor.target is GameObject gameObject))
//                 return;

//             if (gameObject.TryGetComponent<AudioSource>(out AudioSource audioSource)
//                 && audioSource.clip != null)
//             {
//                 GUILayout.TextArea("A description");
//                 // if (GUILayout.Button(
//                 //     EditorGUIUtility.IconContent("preAudioAutoPlayOff", "Play Audio"),
//                 //     EditorStyles.toolbarButton))
//                 // {
//                 //     audioSource.Play();
//                 // }
//             }
//         }
//     }
// }
// #endif