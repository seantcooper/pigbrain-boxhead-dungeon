// using System;
// using System.Collections.Generic;
// using System.Linq;
// using PigBrain.LegacyCore.Utility;
// using PigBrain.Generated;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Game.AI
// {
//     public class Proximity : MonoBehaviour
//     {
//         [SerializeField] LayerMask layerMask = ~0;
//         [SerializeField] GameTagMask tagMask = GameTagMask.All;
//         [SerializeField][Range(0.5f, 15)] float distance = 5;
//         // [SerializeField] Closest closest = Closest.Distance;

//         SphereCollider trigger, triggerer;

//         internal Dictionary<GameTag, HashSet<Collider>> objects = new();
//         public IEnumerable<Collider> Objects() => objects.SelectMany(p => p.Value);

//         public float Distance
//         {
//             get => distance;
//             set
//             {
//                 this.distance = value;
//                 if (trigger) trigger.radius = distance;
//             }
//         }

//         public GameTagMask TagMask() => tagMask;

//         GameTag Handle(string tag) => GameTags.Lookup[tag];

//         float ScaleDistance(float scalar) => distance * scalar;

//         void Start()
//         {
//             triggerer = gameObject.GetComponent<SphereCollider>();
//             trigger = gameObject.AddComponent<SphereCollider>();
//             trigger.radius = distance;
//             trigger.isTrigger = true;
//             trigger.includeLayers = layerMask;
//             objects = tagMask.AsTags().ToDictionary(k => k, v => new HashSet<Collider>());
//         }

//         ////////////////////////////////////////////////////////////
//         // TRIGGERS / UPDATE

//         void OnTriggerEnter(Collider other) => Add(other);
//         void OnTriggerExit(Collider other) => Remove(other);

//         void Add(Collider other)
//         {
//             if (IsValid(other))
//             {
//                 objects[Handle(other.tag)].Add(other);
//                 OnAdd?.Invoke(other.transform);
//             }
//         }

//         void Remove(Collider other)
//         {
//             if (IsValid(other))
//             {
//                 objects[Handle(other.tag)].Remove(other);
//                 OnRemove?.Invoke(other.transform);
//             }
//         }

//         bool IsValid(Collider other) => !other.isTrigger && (layerMask & (1 << other.gameObject.layer)) != 0
//             && objects.ContainsKey(Handle(other.tag));

//         ////////////////////////////////////////////////////////////
//         // EVENTS
//         public event Action<Transform> OnAdd;
//         public event Action<Transform> OnRemove; // Transform can be null (when destroyed)

//         ////////////////////////////////////////////////////////////
//         // SEARCH

//         public bool Valid(Collider target, Proximity other = null, float radiusScalar = 1)
//         {
//             if (!target) return false;
//             if (!GetHashset(Handle(target.tag), out HashSet<Collider> hashset) || !hashset.Contains(target)) return false;
//             return !other || (other.GetHashset(Handle(target.tag), out HashSet<Collider> hs)
//                 && hs.Contains(target) && (transform.position -
//                     target.transform.position).magnitude < ScaleDistance(radiusScalar));
//         }

//         #region HasCount
//         public bool HasCount(GameTag[] tags, Proximity other = null) =>
//             tags.Any(tag => HasCount(tag, other));

//         public bool HasCount(GameTag tag, Proximity other = null)
//         {
//             if (!GetHashset(tag, out HashSet<Collider> hashset) || hashset.Count == 0) return false;
//             if (other) return !other.GetHashset(tag, out HashSet<Collider> otherHashset) || otherHashset.Overlaps(hashset);
//             return true;
//         }
//         public bool HasCount() => objects.Any(p => p.Value.Count > 0);
//         #endregion

//         #region Closest
//         public (Collider collider, float distance) GetClosest(GameTag tag, Proximity other = null, float radiusScalar = 1)
//         {
//             (Collider collider, float distance) closest = (null, ScaleDistance(radiusScalar));
//             if (!GetHashset(tag, out HashSet<Collider> hashset) || hashset.Count == 0) return closest;

//             Vector3 position = transform.position;
//             return GetObjects(tag, other).OrderBy(obj => (obj.transform.position - position).sqrMagnitude)
//                 .FirstOrDefault() is Collider t ? (t, (t.transform.position - position).magnitude) : closest;
//         }

//         public IEnumerable<Collider> GetObjects(GameTag tag, Proximity other = null)
//         {
//             if (!GetHashset(tag, out HashSet<Collider> hashset) || hashset.Count == 0) yield break;
//             HashSet<Collider> otherHashset = null;
//             if (other && !other.GetHashset(tag, out otherHashset)) yield break;

//             foreach (Collider obj in hashset)
//                 if (otherHashset?.Contains(obj) ?? true)
//                     yield return obj;
//         }
//         #endregion

//         public enum Closest
//         {
//             Distance = 0,
//             AngularDistance = 1,
//         }

//         ////////////////////////////////////////////////////////////
//         // SYSTEM

//         bool GetHashset(GameTag tag, out HashSet<Collider> hashset)
//         {
//             if (objects.TryGetValue(tag, out hashset))
//             {
//                 hashset.RemoveWhere(t => t == null);
//                 OnRemove?.Invoke(null);
//                 return true;
//             }
//             return false;
//         }

//         ////////////////////////////////////////////////////////////
//         // DEBUG

//         void OnDrawGizmosSelected()
//         {
//             Gizmos.DrawWireSphere(transform.position, distance);
//         }

//         void OnValidate()
//         {
//             if (trigger) trigger.radius = distance;
//         }
//     }
// }

// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.Game.AI
// {
//     using UnityEditor;
//     [CustomEditor(typeof(Proximity))]
//     public class Stats_Editor : Editor
//     {
//         Proximity proximity => target as Proximity;
//         readonly Dictionary<string, bool> expanded = new();

//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();
//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

//             int count = 0;
//             if (Application.isPlaying)
//             {
//                 foreach (var (tag, objects) in proximity.objects)
//                 {
//                     count += objects.Count;
//                     EditorGUILayout.LabelField($"{tag}", $"{objects.Count}");
//                 }
//             }
//             else
//             {
//                 foreach (var tag in proximity.TagMask().AsTags())
//                     EditorGUILayout.LabelField($"{tag}", "0");
//             }
//             EditorGUILayout.LabelField($"Tracking Total", $"{count}");
//             EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
//         }
//     }
// }
// #endif