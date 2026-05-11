
// using System.IO;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Assets
// {
//     [CreateAssetMenu(menuName = "PigBrain/Core/3D Text Importer")]
//     public class Text3DMeshImporter : ScriptableObject
//     {
//         [SerializeField] bool test;
//         void OnValidate()
//         {
//             var filename = "image.fbx";
//             Debug.Log(Path.GetExtension(filename));
//         }
//     }
// }

// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.Assets
// {
//     using System.Linq;
//     using PigBrain.LegacyCore.Utility;
//     using UnityEditor;
//     public class MeshImportPostprocessor : AssetPostprocessor
//     {
//         void OnPostprocessModel(GameObject g)
//         {
//             var settings = FindSettingsForAsset(assetPath);
//             if (settings == null) return;

//             foreach (var filter in g.GetComponentsInChildren<MeshFilter>())
//                 ProcessMesh(filter, settings);
//         }

//         private void ProcessMesh(MeshFilter filter, Text3DMeshImporter settings)
//         {
//             filter.transform.position = Vector3.zero;
//             var vertices = filter.sharedMesh.vertices;
//             var center = vertices.GetBounds().center;
//             filter.sharedMesh.vertices.Select(v => v - center).ToArray();
//             filter.sharedMesh.RecalculateBounds();
//         }

//         private Text3DMeshImporter FindSettingsForAsset(string modelPath)
//         {
//             string settingsPath = modelPath.Replace(Path.GetExtension(modelPath), ".asset");
//             return AssetDatabase.LoadAssetAtPath<Text3DMeshImporter>(settingsPath);
//         }
//     }
// }
// #endif
