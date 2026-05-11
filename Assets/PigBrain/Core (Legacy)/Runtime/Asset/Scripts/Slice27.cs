// using System;
// using System.Collections.Generic;
// using System.Linq;
// using PigBrain.LegacyCore.Geom;
// using PigBrain.LegacyCore.Utility;
// using Unity.VisualScripting;
// using UnityEngine;
// using Unity.Mathematics;

// public class Slice27 : MonoBehaviour
// {
// #if !UNITY_WEBGL
//     [SerializeField] Vector3 size = new(5, 5, 5);
//     [SerializeField] float margin = 1;

//     [Header("Editor")]
//     [SerializeField] Vector3 editSize = new(1, 1, 1);

//     [Header("Pieces")]
//     [SerializeField][BoolButton(nameof(AutoDetect))] bool autoDetect;
//     [SerializeField] AreaPiece[] pieces;

//     void AutoDetect()
//     {
// #if UNITY_EDITOR
//         UnityEditor.Undo.RecordObject(this, "Auto Detect Areas");
//         pieces = GetComponentsInChildren<MeshRenderer>().Select(r => new AreaPiece(r)).ToArray();
//         var checkPieces = pieces.ToList();
//         foreach (var area in Areas)
//         {
//             var bounds = GetBounds(area, size);
//             bounds.Expand(margin * 0.5f);
//             foreach (var piece in checkPieces.ToArray())
//             {
//                 if (bounds.Contains(piece.bounds))
//                 {
//                     piece.area = area;
//                     checkPieces.Remove(piece);
//                 }
//             }
//         }
//         editSize = size;
//         UnityEditor.EditorUtility.SetDirty(this);
// #endif
//     }

//     void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.softGreen;
//         void DrawArea(Area a, Bounds bounds) => Gizmos.DrawWireCube(bounds.center, bounds.size);
//         Areas.ForEach(a => DrawArea(a, GetBounds(a, (pieces == null || pieces.Length == 0) ? size : editSize)));
//     }
// #if UNITY_EDITOR
//     void OnValidate()
//     {
//         if (IsEditingPrefabAsset()) return;
//         float3 move = (editSize - size) / 2;
//         Vector3 scale = ((float3)editSize - margin * 2) / ((float3)size - margin * 2);
//         pieces.ForEach(p => Move(p, move, scale));
//     }
//     bool IsEditingPrefabAsset()
//     {
//         var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
//         return stage != null && stage.IsPartOfPrefabContents(this.gameObject);
//     }
// #endif

//     void Move(AreaPiece piece, Vector3 move, Vector3 scale)
//     {
//         var localPosition = piece.localPosition;
//         var localScale = Quaternion.Inverse(piece.localRotation) * piece.localScale;

//         if (piece.area.HasFlag(Area.PosX)) localPosition.x += move.x;
//         else if (piece.area.HasFlag(Area.MidX)) localScale.x *= scale.x;
//         else if (piece.area.HasFlag(Area.NegX)) localPosition.x -= move.x;
//         if (piece.area.HasFlag(Area.PosY)) localPosition.y += move.y;
//         else if (piece.area.HasFlag(Area.MidY)) localScale.y *= scale.y;
//         else if (piece.area.HasFlag(Area.NegY)) localPosition.y -= move.y;
//         if (piece.area.HasFlag(Area.PosZ)) localPosition.z += move.z;
//         else if (piece.area.HasFlag(Area.MidZ)) localScale.z *= scale.z;
//         else if (piece.area.HasFlag(Area.NegZ)) localPosition.z -= move.z;

//         piece.renderer.transform.localPosition = localPosition;
//         piece.renderer.transform.localScale = piece.localRotation * localScale;
//     }

//     Bounds GetBounds(Area area, Vector3 size)
//     {
//         Vector3 min = -size / 2, max = size / 2;
//         if (area.HasFlag(Area.PosX)) min.x = max.x - margin;
//         else if (area.HasFlag(Area.MidX)) { min.x += margin; max.x -= margin; }
//         else if (area.HasFlag(Area.NegX)) max.x = min.x + margin;
//         if (area.HasFlag(Area.PosY)) min.y = max.y - margin;
//         else if (area.HasFlag(Area.MidY)) { min.y += margin; max.y -= margin; }
//         else if (area.HasFlag(Area.NegY)) max.y = min.y + margin;
//         if (area.HasFlag(Area.PosZ)) min.z = max.z - margin;
//         else if (area.HasFlag(Area.MidZ)) { min.z += margin; max.z -= margin; }
//         else if (area.HasFlag(Area.NegZ)) max.z = min.z + margin;
//         return new Bounds((min + max) / 2, max - min).Transform(transform);
//     }

//     [Flags]
//     enum Area
//     {
//         PosX = 1 << 0, MidX = 1 << 1, NegX = 1 << 2,
//         PosY = 1 << 3, MidY = 1 << 4, NegY = 1 << 5,
//         PosZ = 1 << 6, MidZ = 1 << 7, NegZ = 1 << 8,
//     }

//     static readonly Area[] Areas = GetArea().ToArray();
//     static IEnumerable<Area> GetArea()
//     {
//         foreach (var z in new[] { Area.NegZ, Area.MidZ, Area.PosZ })
//             foreach (var y in new[] { Area.NegY, Area.MidY, Area.PosY })
//                 foreach (var x in new[] { Area.NegX, Area.MidX, Area.PosX })
//                     yield return x | y | z;
//     }

//     [Serializable]
//     class AreaPiece
//     {
//         public string name;
//         public Area area;
//         public MeshRenderer renderer;
//         public Bounds bounds;
//         public Vector3 localPosition, localScale;
//         public Quaternion localRotation;

//         public AreaPiece(MeshRenderer renderer)
//         {
//             this.name = renderer.name;
//             this.renderer = renderer;
//             this.bounds = renderer.bounds;
//             this.localPosition = renderer.transform.localPosition;
//             this.localScale = renderer.transform.localScale;
//             this.localRotation = renderer.transform.localRotation;
//         }
//     }
// #endif
// }
