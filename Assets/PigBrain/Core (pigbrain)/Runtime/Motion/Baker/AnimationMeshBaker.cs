// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using pigbrain.Core.Collections;
// using pigbrain.Core.Inspector;
// using pigbrain.Core.UnityObject;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Rendering;

// [InlineButton(nameof(Bake))]
// public class AnimationMeshBaker : MonoBehaviour
// {
//     public string path = "Assets/$A";
//     public GameObject prefab;
//     public AnimationClip[] clips;
//     public int fps = 15;

//     public void Bake()
//     {
//         var instance = prefab.Instantiate();
//         var assetPath = $"{path}/{instance.name}";

//         Debug.Log($"Asset Path: {assetPath}");
//         if (Directory.Exists(assetPath)) Directory.Delete(assetPath, true);
//         Directory.CreateDirectory(assetPath);

//         foreach (var clip in clips)
//         {
//             var frames = MeshFrameBaker.Bake(instance, clip, fps);
//             MeshFrameBaker.WriteAnimation(assetPath, frames);
//             // frames.ForEach(o => DestroyImmediate(o));
//         }
//         DestroyImmediate(instance);
//     }

//     public static class MeshFrameBaker
//     {
//         public static void WriteAnimation(string path, Mesh[] meshes)
//         {
//             foreach (var mesh in meshes)
//             {
//                 string assetPath = $"{path}/{mesh.name}.asset";
//                 Debug.Log($"Asset Path: {assetPath}");
//                 AssetDatabase.CreateAsset(mesh, assetPath);
//             }
//             AssetDatabase.SaveAssets();
//         }

//         public static Mesh[] Bake(GameObject root, AnimationClip clip, int fps)
//         {
//             var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>();
//             var filters = root.GetComponentsInChildren<MeshFilter>();

//             int frames = Mathf.CeilToInt(clip.length * fps);
//             List<Mesh> meshFrames = new();

//             for (int f = 0; f < frames; f++)
//             {
//                 float t = f / (float)fps;
//                 clip.SampleAnimation(root, t);

//                 var combines = new List<CombineInstance>();

//                 foreach (var smr in skinned)
//                 {
//                     var m = new Mesh();
//                     smr.BakeMesh(m);
//                     combines.Add(new CombineInstance
//                     {
//                         mesh = m,
//                         transform = smr.localToWorldMatrix
//                     });
//                 }

//                 foreach (var mf in filters)
//                 {
//                     combines.Add(new CombineInstance
//                     {
//                         mesh = mf.sharedMesh,
//                         transform = mf.transform.localToWorldMatrix
//                     });
//                 }

//                 var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
//                 combined.CombineMeshes(combines.ToArray(), true, true);
//                 combined.name = $"{clip.name}-{f:D04}";
//                 meshFrames.Add(combined);
//             }
//             return meshFrames.ToArray();
//         }
//     }
// }
