using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using pigbrain.core.AI;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapDataTriangleSampler
    {
        const float EPS = 0.001f;
        public readonly float spacing, spacingInv, maxPathDist;
        public readonly Dictionary<int2, NavMapData.Cell> map = new();
        public readonly NavMeshPath path;
        public readonly NavMeshSurface surface;
        public Vector3[] vertices;
        public int[] triangles;
        public HashSet<(int i1, int i2)> edges;

        public Bounds bounds;
        Vector3 min;
        HashSet<(int t1, int t2)> edgeMatch;
        NavMeshBuildSettings settings;

        public NavMapDataTriangleSampler(NavMeshSurface surface, float spacing)
        {
            this.surface = surface;
            this.spacing = spacing;
            settings = surface.GetBuildSettings();
            spacingInv = 1f / spacing;
            maxPathDist = spacing * 2f;
            path = new();
        }

        #region Sample Nav Mesh
        public void SampleNavMesh()
        {
            var p0 = Profiler.Start();
            Triangulate();
            Profiler.StopAndLog(p0, "Triangulate");

            var p1 = Profiler.Start();
            SampleTriangles();
            Profiler.StopAndLog(p1, "Sample Points");

            var p2 = Profiler.Start();
            Connect();
            Profiler.StopAndLog(p2, "Connect");
        }

        #region Triangulate
        void Triangulate()
        {
            // triangulation
            var nmtri = NavMesh.CalculateTriangulation();
            var map = new Dictionary<int3, int>();
            var verts = new List<Vector3>();
            var tris = new int[nmtri.indices.Length];

            int3 Quant(Vector3 v) => new(Mathf.RoundToInt(v.x / EPS), Mathf.RoundToInt(v.y / EPS),
                Mathf.RoundToInt(v.z / EPS));

            for (int i = 0; i < nmtri.vertices.Length; i++)
            {
                var q = Quant(nmtri.vertices[i]);
                if (!map.TryGetValue(q, out int idx))
                {
                    map[q] = verts.Count;
                    verts.Add(nmtri.vertices[i]);
                }
            }

            for (int i = 0; i < nmtri.indices.Length; i++)
                tris[i] = map[Quant(nmtri.vertices[nmtri.indices[i]])];

            // create shared edges
            edgeMatch = new();
            Dictionary<(int i0, int i1), int> edgeLookup = new();
            void AddEdge(int i0, int i1, int t1)
            {
                var key = i0 < i1 ? (i0, i1) : (i1, i0);
                if (!edgeLookup.TryGetValue(key, out int t2)) edgeLookup.Add(key, t1);
                else edgeMatch.Add(t1 < t2 ? (t1, t2) : (t2, t1));
            }

            for (int i = 0; i < tris.Length; i += 3)
            {
                int t = i / 3;
                AddEdge(tris[i + 0], tris[i + 1], t);
                AddEdge(tris[i + 1], tris[i + 2], t);
                AddEdge(tris[i + 2], tris[i + 0], t);
                edgeMatch.Add((t, t));
            }
            edges = edgeLookup.Keys.ToHashSet();
            vertices = verts.ToArray();
            triangles = tris;
            bounds = vertices.Encapsulate().Inflate(spacing * spacing);
            min = bounds.min;
        }
        #endregion

        void SampleTriangles()
        {
            var heightSqr = settings.agentHeight * settings.agentHeight;

            for (int i = 0; i < triangles.Length; i += 3)
                SampleTriangle(
                   vertices[triangles[i + 0]],
                   vertices[triangles[i + 1]],
                   vertices[triangles[i + 2]], i / 3);

            void AddNode(int2 index, Vector3 world, int triangleIndex)
            {
                if (!map.TryGetValue(index, out NavMapData.Cell cell))
                    map[index] = cell = new NavMapData.Cell(index);
                if (!NavMesh.SamplePosition(world, out NavMeshHit hit, settings.agentHeight, NavMesh.AllAreas)) return;
                var position = hit.position;
                for (int i = 0; i < cell.nodes.Count; i++)
                    if ((cell.nodes[i].position - position).sqrMagnitude < heightSqr) return;
                cell.nodes.Add(new(position, triangleIndex));
            }

            void SampleTriangle(Vector3 p1, Vector3 p2, Vector3 p3, int triangleIndex)
            {
                float inv = spacingInv, pad = 0.5f;

                float2 v0 = new((p1.x - min.x) * inv, (p1.z - min.z) * inv);
                float2 v1 = new((p2.x - min.x) * inv, (p2.z - min.z) * inv);
                float2 v2 = new((p3.x - min.x) * inv, (p3.z - min.z) * inv);

                float minX = Mathf.Min(v0.x, v1.x, v2.x) - pad;
                float maxX = Mathf.Max(v0.x, v1.x, v2.x) + pad;
                float minZ = Mathf.Min(v0.y, v1.y, v2.y) - pad;
                float maxZ = Mathf.Max(v0.y, v1.y, v2.y) + pad;

                int x0 = Mathf.FloorToInt(minX), x1 = Mathf.FloorToInt(maxX);
                int z0 = Mathf.FloorToInt(minZ), z1 = Mathf.FloorToInt(maxZ);

                float area = Edge(v0, v1, v2);
                if (Mathf.Abs(area) < 1e-6f) return;

                for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                {
                    float2 p = new(x + 0.5f, z + 0.5f);
                    if (!InsideExpanded(p, v0, v1, v2, pad)) continue;

                    float w0 = Edge(v1, v2, p) / area;
                    float w1 = Edge(v2, v0, p) / area;
                    float w2 = 1f - w0 - w1;
                    w0 = Mathf.Clamp01(w0); w1 = Mathf.Clamp01(w1); w2 = Mathf.Clamp01(w2);
                    float sum = w0 + w1 + w2;
                    if (sum > 1e-6f) { w0 /= sum; w1 /= sum; w2 /= sum; }

                    AddNode(
                        new int2(x, z),
                        new(min.x + p.x * spacing, p1.y * w0 + p2.y * w1 + p3.y * w2, min.z + p.y * spacing),
                        triangleIndex);
                }

                static float Edge(float2 a, float2 b, float2 p) =>
                    (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);

                static bool InsideExpanded(float2 p, float2 a, float2 b, float2 c, float pad)
                {
                    if (SameSide(p, a, b, c) && SameSide(p, b, c, a) && SameSide(p, c, a, b)) return true;
                    float pad2 = pad * pad;
                    return DistSegSq(p, a, b) <= pad2 || DistSegSq(p, b, c) <= pad2 || DistSegSq(p, c, a) <= pad2;
                }

                static bool SameSide(float2 p, float2 a, float2 b, float2 c)
                {
                    float e1 = Edge(a, b, p), e2 = Edge(a, b, c);
                    return e2 >= 0 ? e1 >= -1e-5f : e1 <= 1e-5f;
                }

                static float DistSegSq(float2 p, float2 a, float2 b)
                {
                    float2 ab = b - a;
                    float t = math.lengthsq(ab) > 1e-8f ? math.saturate(math.dot(p - a, ab) / math.lengthsq(ab)) : 0f;
                    float2 d = p - (a + ab * t);
                    return math.lengthsq(d);
                }
            }
        }
        #endregion

        #region Connect
        void Connect()
        {
            bool MatchingEdge(int t1, int t2) => edgeMatch.Contains(t1 < t2 ? (t1, t2) : (t2, t1));
            bool ConnectNodes(NavMapData.Node n1, NavMapData.Node n2)
            {
                bool HasConnection() => MatchingEdge(n1.triangleIndex, n2.triangleIndex)
                    || (NavMesh.CalculatePath(n1.position, n2.position, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete
                    && path.Length() < maxPathDist);

                if (!HasConnection()) return false;
                n1.connectors.Add(n2);
                n2.connectors.Add(n1);
                return true;
            }

            HashSet<(int2, int2)> used = new();

            foreach (var s1 in map.Values) foreach (var d in Array2.Neighbor4)
            {
                int2 i1 = s1.index, i2 = s1.index + d;
                var key = (i1.x < i2.x || (i1.x == i2.x && i1.y <= i2.y)) ? (i1, i2) : (i2, i1);
                if (!used.Add(key)) continue;
                if (map.TryGetValue(i2, out NavMapData.Cell s2))
                    foreach (var n1 in s1.nodes) foreach (var n2 in s2.nodes)
                        ConnectNodes(n1, n2);
            }
        }
        #endregion
    }
}