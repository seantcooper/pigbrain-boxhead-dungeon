using System;
using PigBrain.Generated;
using UnityEngine;

namespace pigbrain.core.UnityPhysics
{
    public static class PhysicsUtility
    {
        public readonly static Collider[] Colliders1000 = new Collider[1000];
        public readonly static RaycastHit[] Hits1000 = new RaycastHit[1000];

        #region OverlapSphere
        public static ArraySegment<Collider> OverlapSphere(Vector3 position, float radius, LayerMask mask)
        {
            int n = Physics.OverlapSphereNonAlloc(position, radius, Colliders1000, mask, QueryTriggerInteraction.Ignore);
            return new(Colliders1000, 0, n);
        }
        #endregion

        #region LineCast
        public static ArraySegment<RaycastHit> LineCast(Vector3 p1, Vector3 p2, LayerMask mask) =>
            LineCast(p1, p2, (int)mask);

        public static ArraySegment<RaycastHit> LineCast(Vector3 p1, Vector3 p2, int mask)
        {
            Vector3 d = p2 - p1;
            int n = Physics.RaycastNonAlloc(new Ray(p1, d), Hits1000, d.magnitude + 0.0001f, mask, QueryTriggerInteraction.Ignore);
            return new(Hits1000, 0, n);
        }
        #endregion

        #region Backface Scope
        public class BackfaceScope : System.IDisposable
        {
            private readonly bool previous;
            public BackfaceScope(bool enable = true)
            {
                previous = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = enable;
            }
            public void Dispose() => Physics.queriesHitBackfaces = previous;
        }
        #endregion

        #region Raycast
        public static int RaycastSweep(this Ray ray, RaycastHit[] hits, float distance = 1000f, int layerMask = -1)
        {
            const float EPSILON = 0.00001f;
            Vector3 move = ray.origin, dir = ray.direction.normalized;
            int count = 0;

            float remaining = distance;

            for (int i = 0; i < 256 && remaining > 0f; i++)
            {
                if (!Physics.Raycast(move, dir, out RaycastHit hit, remaining, layerMask)) break;

                float localDist = hit.distance;
                move += dir * (hit.distance + EPSILON);
                remaining -= hit.distance + EPSILON;
                hit.distance = (hit.point - ray.origin).magnitude;
                hits[count++] = hit;
            }
            return count;
        }
        #endregion

    }
}
