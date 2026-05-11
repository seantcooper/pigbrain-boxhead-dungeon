// using System.Collections.Generic;
// using System.Linq;
// using Unity.Mathematics;
// using UnityEngine;

// public static class PhysicsSearch
// {
//     //////////////////////////////////////////////////////////////////////////////////////////
//     // SPHERE SEARCH

//     public static readonly Collider[] ColliderCache = new Collider[1000];
//     public static IEnumerable<Collider> Sphere(Vector3 position, float radius, LayerMask layerMask, bool ordered = true)
//     {
//         var colliders = ColliderCache.Take(Physics.OverlapSphereNonAlloc(position, radius, ColliderCache, layerMask));
//         return ordered ? colliders.OrderBy(c => (position - c.transform.position).sqrMagnitude) : colliders;
//     }

//     public static IEnumerable<T> Sphere<T>(Vector3 position, float radius, LayerMask layerMask, bool ordered = true) where T : Component =>
//         Sphere(position, radius, layerMask, ordered).Select(c => c.GetComponentInParent<T>()).Where(c => c);

//     //////////////////////////////////////////////////////////////////////////////////////////
//     // RAYCAST SEARCH

//     public static readonly RaycastHit[] RaycastCache = new RaycastHit[1000];
//     public static IEnumerable<RaycastHit> Ray(Vector3 start, Vector3 end, LayerMask layerMask, float thickness = 0)
//     {
//         Ray ray = new(start, end - start);
//         float distance = (end - start).magnitude;
//         // Debug.DrawLine(start, end, Color.red, 1);

//         int count = thickness <= 0 ? Physics.RaycastNonAlloc(ray, RaycastCache, distance)
//             : Physics.SphereCastNonAlloc(ray, thickness / 2, RaycastCache, distance);

//         Debug.Log($"Ray: start:{start} end:{end} dist:{distance} count:{count}");

//         return RaycastCache.Take(count);
//     }

//     public static IEnumerable<T> Ray<T>(Vector3 start, Vector3 end, LayerMask layerMask, float thickness = 0) where T : Component =>
//         Ray(start, end, layerMask, thickness).Select(h => h.collider.GetComponentInParent<T>()).Where(h => h);
// }