using System.Collections;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using static pigbrain.core.UnityPhysics.PhysicsUtility;

namespace pigbrain.core.AI
{
    public static class NavMeshUtility
    {
        public static bool TryWarp(this NavMeshAgent agent, Vector3 pos)
        {
            if (agent.FindPosition(ref pos))
            {
                agent.Warp(pos);
                return true;
            }
            return false;
        }

        public static bool FindPosition(this NavMeshAgent agent, ref Vector3 pos)
        {
            float[] radii = { 1f, 2f, 5f, 10f, 50f };
            foreach (var r in radii)
            {
                if (NavMesh.SamplePosition(pos, out var hit, r, agent.areaMask))
                {
                    pos = hit.position;
                    return true;
                }
            }
            return false;
        }

        public static bool OverlapAgent(this NavMeshAgent agent)
        {
            int count = 0, n = Physics.OverlapSphereNonAlloc(agent.transform.position, agent.radius, Colliders1000);
            for (int i = 0; i < n; i++)
                if (Colliders1000[i].TryGetComponent(out NavMeshAgent other) && other != agent)
                    count++;
            return count != 0;
        }

        public static Bounds GetBounds(this NavMeshAgent agent) => new Bounds(
            agent.transform.position.AddY(agent.baseOffset + agent.height * 0.5f),
            new Vector3(agent.radius * 2f, agent.height, agent.radius * 2f));

        public static Bounds GetBounds(this NavMeshSurface surface) => GetBounds();
        public static Bounds GetBounds()
        {
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0) return default;
            Bounds bounds = new(tri.vertices[0], Vector3.zero);
            for (int i = 1; i < tri.vertices.Length; bounds.Encapsulate(tri.vertices[i]), i++) ;
            return bounds;
        }

        public static bool MoveInside(Vector3 position, out Vector3 result, float distance = 0.05f)
        {
            result = position;
            if (NavMesh.FindClosestEdge(position, out NavMeshHit edge, NavMesh.AllAreas))
            {
                if ((edge.position - position).sqrMagnitude > distance * distance)
                    return false;
                result += edge.normal.normalized * distance;
                return true;
            }
            return false;
        }

        public static bool NearEdge(Vector3 position, float distance = 0.05f) =>
            NavMesh.FindClosestEdge(position, out NavMeshHit edge, NavMesh.AllAreas) &&
                (edge.position - position).sqrMagnitude < distance * distance;

        public static float Length(this NavMeshPath path)
        {
            float length = 0;
            for (int i = 1; i < path.corners.Length; length += (path.corners[i - 1] - path.corners[i]).magnitude, i++) ;
            return length;
        }
    }
}
