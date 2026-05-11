using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace pigbrain.core.Geom
{
    public static class Build
    {
        #region Mesh
        public static Mesh ToMesh(Poly[] polys, Vector3[] vertices, Vector2[] uvs = null) =>
            ToMesh(polys.SelectMany(p => p.indices).ToArray(), vertices, uvs);

        public static Mesh ToMesh(int[] triangles, Vector3[] vertices, Vector2[] uvs = null)
        {
            var mesh = new Mesh()
            {
                name = "<generated>",
                indexFormat = vertices.Length < 64 * 1024 ? IndexFormat.UInt16 : IndexFormat.UInt32,
                vertices = vertices,
                triangles = triangles,
                uv = uvs,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }


        public static void ReindexPolys<T>(this IList<T> polys, out Dictionary<int, int> indexRemap) where T : Poly
        {
            indexRemap = new();
            foreach (var poly in polys)
            {
                for (int i = 0, n = poly.indices.Length; i < n; i++)
                {
                    if (!indexRemap.TryGetValue(poly.indices[i], out int ni))
                        indexRemap.Add(poly.indices[i], ni = indexRemap.Count);
                    poly.indices[i] = ni;
                }
            }
        }

        /// <summary>Makes all the polys have their own vertices</summary>
        public static int[] IsolateIndices<T>(this IList<T> polys) where T : Poly
        {
            List<int> remap = new();
            foreach (var poly in polys)
            {
                for (int i = 0, n = poly.indices.Length; i < n; i++)
                {
                    int last = poly.indices[i];
                    poly.indices[i] = remap.Count;
                    remap.Add(last);
                }
            }
            return remap.ToArray();
        }

        public static Vector2 OctahedralUV(Vector3 n)
        {
            n /= (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z));
            Vector2 uv = new(n.x, n.y);
            if (n.z < 0f)
                uv = new Vector2((1f - Mathf.Abs(uv.y)) * Mathf.Sign(uv.x),
                    (1f - Mathf.Abs(uv.x)) * Mathf.Sign(uv.y));
            return uv * 0.5f + Vector2.one * 0.5f;
        }
        #endregion

        #region Poly
        public class Poly
        {
            public readonly int[] indices;
            public int this[int i] => indices[i];
            public int Count => indices.Length;
            public IEnumerable<(int i1, int i2)> EdgeKeys => Edges.Select(GetEdgeKey);
            public IEnumerable<(int i1, int i2)> Edges =>
                indices.Select((v, i) => (v, indices[i == Count - 1 ? 0 : i + 1]));

            public T[] GetVertices<T>(T[] vertices) => indices.Select(i => vertices[i]).ToArray();
            public Poly(params int[] indices) => this.indices = indices;
            public void Reverse() => indices.Reverse().ToArray().ForEach((id, i) => indices[i] = id);
            public static implicit operator bool(Poly empty) => empty != null;
        }

        public static Vector3 GetCentroid(this Poly poly, Vector3[] vertices) =>
            GetCentroid(poly.GetVertices(vertices));
        public static Bounds GetBounds(this Poly poly, Vector3[] vertices) =>
            poly.GetVertices(vertices).Encapsulate();

        public static int GetIndicesKey(this Poly poly) => poly.indices.GetHashCode();
        public static Vector3 GetCentroid(this Poly poly, IList<Vector3> verts) =>
            poly.indices.Select(i => verts[i]).Aggregate(Vector3.zero, (a, v) => a += v) / poly.Count;
        public static Vector3 GetCentroid(IList<Vector3> verts) =>
            verts.Aggregate(Vector3.zero, (a, v) => a += v) / verts.Count;

        public static Vector3 GetNormal(this Poly poly, IList<Vector3> verts)
        {
            float nx = 0, ny = 0, nz = 0;
            foreach (var (i1, i2) in poly.Edges)
            {
                Vector3 a = verts[i1], b = verts[i2];
                nx += (a.y - b.y) * (a.z + b.z);
                ny += (a.z - b.z) * (a.x + b.x);
                nz += (a.x - b.x) * (a.y + b.y);
            }
            return new Vector3(nx, ny, nz).normalized;
        }

        /// <summary>Groups Polys by Vertex Index</summary>
        public static Dictionary<int, T[]> GroupPolysByIndices<T>(IList<T> polys) where T : Poly =>
            polys.GroupBy(p => p.indices.OrderBy(i => i).GetIndicesHash())
                .ToDictionary(g => g.Key, g => g.ToArray());

        /// <summary>Creates a boundsary of the group polys (assumes singular boundary and convex)</summary>
        public static Poly MergePolys<T>(IList<T> polys) where T : Poly
        {
            var polysByEdge = GroupPolysByEdgeKey(polys);
            var boundaryEdges = polysByEdge.Where(kv => kv.Value.Length == 1).Select(kv => kv.Key).ToHashSet();
            var edgeLookup = polys
                .SelectMany(p => p.EdgeKeys.Zip(p.Edges, (key, edge) => (key, edge)))
                .Where(t => boundaryEdges.Contains(t.key))
                .ToDictionary(t => t.key, t => t.edge);

            var edges = polysByEdge.Where(kv => kv.Value.Length == 1)
                .Select(kv => edgeLookup[kv.Key])
                .ToDictionary(e => e.i1, e => e.i2);

            int start = edges.First().Key, next = edges.First().Value;
            List<int> indices = new() { start };
            for (; next != start; indices.Add(next), next = edges[next]) ;
            return new Poly(indices.ToArray());
        }

        /// <summary>Creates a boundsary of the group polys (assumes singular boundary and convex)</summary>
        public static Poly MergePolys<T>(IList<T> polys, Vector3[] verts, out Poly[] holes, float toleranceDeg = 2f)
            where T : Poly
        {
            var polysByEdge = GroupPolysByEdgeKey(polys);
            var boundaryEdges = polysByEdge
                .Where(kv => kv.Value.Length == 1)
                .Select(kv => kv.Key)
                .ToHashSet();
            var edgeLookup = polys
                .SelectMany(p => p.EdgeKeys.Zip(p.Edges, (key, edge) => (key, edge)))
                .Where(t => boundaryEdges.Contains(t.key))
                .ToDictionary(t => t.key, t => t.edge);
            var edges = polysByEdge
                .Where(kv => kv.Value.Length == 1)
                .Select(kv => edgeLookup[kv.Key])
                .ToDictionary(e => e.i1, e => e.i2);

            Poly Trace()
            {
                int start = edges.First().Key, next = edges.First().Value;
                List<int> indices = new() { start };
                for (; next != start; indices.Add(next), next = edges[next]) ;
                indices.ForEach(i => edges.Remove(i));
                return new Poly(indices.ToArray());
            }

            List<Poly> loops = new();
            while (edges.Count > 0) loops.Add(Trace());

            Poly result = loops.OrderBy(p => p.Edges.Sum(e => (verts[e.i1] - verts[e.i2]).sqrMagnitude)).Last();
            loops.Remove(result);

            result = CleanPoly(result, verts, toleranceDeg);
            holes = loops.Select(h => CleanPoly(h, verts, toleranceDeg)).ToArray();
            return result;
        }

        public static Vector2[] Polygon3DTo2D(Vector3[] verts)
        {
            Vector3 origin = verts[0];
            Vector3 e1 = verts[1] - origin, e2 = verts[2] - origin;
            Vector3 normal = Vector3.Cross(e1, e2).normalized;
            Vector3 u = e1.normalized;
            Vector3 v = Vector3.Cross(normal, u).normalized;

            Vector2[] poly2d = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 d = verts[i] - origin;
                poly2d[i] = new Vector2(Vector3.Dot(d, u), Vector3.Dot(d, v));
            }
            return poly2d;
        }

        static Poly CleanPoly(Poly poly, Vector3[] verts, float tolerance = 2f)
        {
            float sinTol = Mathf.Sin(tolerance * Mathf.Deg2Rad);
            List<int> cleaned = new(poly.Count);
            for (int n = poly.Count, i0 = n - 2, i1 = n - 1, i2 = 0; i2 < n; i0 = i1, i1 = i2, i2++)
            {
                Vector3 a = verts[poly.indices[i0]], b = verts[poly.indices[i1]], c = verts[poly.indices[i2]];
                if (Vector3.Cross((b - a).normalized, (c - b).normalized).magnitude > sinTol)
                    cleaned.Add(poly.indices[i1]);
            }
            return new Poly(cleaned.ToArray());
        }
        #endregion

        #region Triangle
        public static List<Poly> TriangulateCenter(this Poly poly, IList<Vector3> vertices)
        {
            List<Poly> triangles = new();
            var center = poly.indices.Aggregate(Vector3.zero, (a, i) => a + vertices[i]) / poly.Count;
            var icenter = vertices.Count;
            for (int i = 0, n = poly.Count - 1; i <= n; i++)
                triangles.Add(new Poly(poly[i], poly[i == n ? 0 : i + 1], icenter));
            vertices.Add(center);
            return triangles;
        }

        public static Poly[] TriangulateCenter(Poly[] polys, IList<Vector3> vertices) =>
            polys.SelectMany(p => p.TriangulateCenter(vertices)).ToArray();

        public static Poly[] TriangulateSimple(IList<Poly> polys) =>
            polys.SelectMany(p => Enumerable.Range(0, p.Count - 1).Select(i => new Poly(p[0], p[i], p[i + 1]))).ToArray();

        #endregion

        #region Edge
        public static (int i1, int i2) GetEdgeKey(int i1, int i2) => GetEdgeKey((i1, i2));
        public static (int i1, int i2) GetEdgeKey((int i1, int i2) e) => e.i1 < e.i2 ? e : (e.i2, e.i1);
        public static (int i1, int i2)[] GetUniqueEdges(IList<Poly> polys) =>
            polys.SelectMany(p => p.EdgeKeys).Distinct().ToArray();

        public static Dictionary<(int i1, int i2), T[]> GroupPolysByEdgeKey<T>(IList<T> polys) where T : Poly =>
            polys.SelectMany(p => p.EdgeKeys.Select(e => (p, e)))
            .GroupBy(t => t.e)
            .ToDictionary(g => g.Key, g => g.Select(t => t.p).ToArray());

        /// <summary>Groups Polys by connecting groups (Flood fill)</summary>
        public static List<List<T>> GroupPolysByEdgeConnection<T>(IList<T> polys)
            where T : Poly
        {
            var edges = GroupPolysByEdgeKey(polys);
            List<List<T>> groups = new();

            for (HashSet<T> unvisited = new(polys); unvisited.Count > 0;)
            {
                T start = unvisited.First();

                List<T> group = new();
                Queue<T> queue = new();

                queue.Enqueue(start);
                unvisited.Remove(start);
                group.Add(start);

                while (queue.Count > 0)
                {
                    foreach (var e in queue.Dequeue().EdgeKeys)
                    {
                        if (!edges.TryGetValue(e, out var touching)) continue;
                        foreach (var other in touching)
                        {
                            if (unvisited.Remove(other))
                            {
                                queue.Enqueue(other);
                                group.Add(other);
                            }
                        }
                    }
                }
                groups.Add(group);
            }
            return groups;
        }
        #endregion

        #region Indices
        /// <summary>Groups Polys by Vertex Index</summary>
        public static Dictionary<int, Poly[]> GroupPolysByIndex(IList<Poly> polys) =>
            polys.SelectMany(p => p.indices.Select(i => (p, i)))
                .GroupBy(t => t.i)
                .ToDictionary(g => g.Key, g => g.Select(t => t.p).ToArray());

        /// <summary>Groups Indices by Vertex Index</summary>
        public static Dictionary<int, int[]> GroupIndicesByIndex(IList<Poly> polys) =>
            polys.SelectMany(p => p.indices.Select((id, i) => (i1: id, i2: p.indices[i >= p.indices.Length - 1 ? 0 : i + 1])))
                .GroupBy(t => t.i1)
                .ToDictionary(g => g.Key, g => g.Select(t => t.i2).ToArray());
        #endregion

        #region Subdivision
        public static List<Poly> SubdivideTriangles(List<Poly> tri, List<Vector3> vts) =>
            SubdivideTriangles(tri, vts, new());

        public static List<Poly> SubdivideTriangles(List<Poly> tri, List<Vector3> vts, Dictionary<(int i1, int i2), int> divisions)
        {
            int GetMid(int i1, int i2)
            {
                var edge = GetEdgeKey(i1, i2);
                if (!divisions.TryGetValue(edge, out int index))
                {
                    index = divisions[edge] = vts.Count;
                    vts.Add((vts[i1] + vts[i2]) * 0.5f);
                }
                return index;
            }

            List<Poly> ntri = new();
            void SubdivideEdges(Poly t)
            {
                int a = t.indices[0], b = t.indices[1], c = t.indices[2];
                int ab = GetMid(a, b), bc = GetMid(b, c), ca = GetMid(c, a);
                ntri.Add(new Poly(a, ab, ca));
                ntri.Add(new Poly(b, bc, ab));
                ntri.Add(new Poly(c, ca, bc));
                ntri.Add(new Poly(ab, bc, ca));
            }

            tri.ForEach(t => SubdivideEdges(t));
            return ntri;
        }

        public static Poly[] Quadrangulate(IList<Poly> polys, IList<Vector3> verts)
        {
            Dictionary<(int, int), int> splits = new();
            Dictionary<Poly, (int i1, int i2)[]> edges = polys.ToDictionary(p => p, p => p.EdgeKeys.ToArray());
            polys.SelectMany(q => edges[q]).Distinct().ForEach(edge =>
            {
                splits[edge] = verts.Count;
                verts.Add(((verts[edge.i1] + verts[edge.i2]) * 0.5f));
            });

            List<Poly> result = new();
            foreach (var p in polys)
            {
                int n = p.Count, ic = verts.Count;
                verts.Add((p.indices.Aggregate(Vector3.zero, (a, v) => a += verts[v]) / n));
                for (int i1 = 0, i2 = n - 1; i1 < n; i2 = i1, i1++)
                    result.Add(new Poly(p.indices[i1], splits[edges[p][i1]], ic, splits[edges[p][i2]]));
            }
            return result.ToArray();
        }
        #endregion

        #region Icosphere
        const float IU = 0.5257311f, IV = 0.8506508f;
        static readonly Vector3[] IcosphereVertices = new Vector3[] {
            new(-IU, IV, 0), new(IU, IV, 0), new(-IU, -IV, 0), new(IU, -IV, 0),
            new(0, -IU, IV), new(0, IU, IV), new(0, -IU, -IV), new(0, IU, -IV),
            new(IV, 0, -IU), new(IV, 0, IU), new(-IV, 0, -IU), new(-IV, 0, IU) };
        static readonly Poly[] IcosphereTriangles = new Poly[] {
            new(0, 11, 5), new(0, 5, 1), new(0, 1, 7), new(0, 7, 10), new(0, 10, 11),
            new(1, 5, 9), new(5, 11, 4), new(11, 10, 2), new(10, 7, 6), new(7, 1, 8),
            new(3, 9, 4), new(3, 4, 2), new(3, 2, 6), new(3, 6, 8), new(3, 8, 9),
            new(4, 9, 5), new(2, 4, 11), new(6, 2, 10), new(8, 6, 7), new(9, 8, 1) };

        public static void BuildIcosphere(int subdivisions, out Vector3[] verts, out Poly[] triangles)
        {
            List<Vector3> vts = new(IcosphereVertices);
            List<Poly> tri = new(IcosphereTriangles);

            for (; subdivisions > 0; --subdivisions)
            {
                int index = vts.Count;
                tri = SubdivideTriangles(tri, vts);
                for (int i = 0, n = vts.Count; i < n; vts[i] = vts[i].normalized, i++) ;
            }
            verts = vts.ToArray();
            triangles = tri.ToArray();
        }
        #endregion

        #region Goldberg
        public static void BuildGoldberg(int subdivisions, out Vector3[] verts, out Poly[] polys)
        {
            BuildIcosphere(subdivisions, out Vector3[] vts, out Poly[] tris);

            var bvts = vts.ToList();
            List<int> tri = tris.SelectMany(t => t.indices).ToList();

            var dvts = Enumerable.Range(0, tri.Count / 3)
                .Select(i => i * 3)
                .Select(i => (bvts[tri[i + 0]] + bvts[tri[i + 1]] + bvts[tri[i + 2]]) / 3f) // triangle center
                .ToList();

            var trisAtVert = Enumerable.Range(0, bvts.Count).Select(i => new List<int>(6)).ToArray(); //new List<int>[bvts.Count];
            for (int i = 0, i3 = 0, n = tri.Count / 3; i < n; i++, i3 += 3)
            {
                trisAtVert[tri[i3 + 0]].Add(i);
                trisAtVert[tri[i3 + 1]].Add(i);
                trisAtVert[tri[i3 + 2]].Add(i);
            }

            int[] OrderRing(int vi)
            {
                Vector3 p = bvts[vi], n = p.normalized, D = Vector3.Cross(n, Vector3.up);
                if (D.sqrMagnitude < 1e-6f) D = Vector3.Cross(n, Vector3.right);
                D.Normalize();
                return trisAtVert[vi].Select(ti => (a: Vector3.SignedAngle(D, (dvts[ti] - p).normalized, n), i: ti))
                    .OrderBy(t => t.a)
                    .Select(t => t.i).ToArray();
            }

            // List<Poly> p5 = new(12), p6 = new(bvts.Count - 12);
            // for (int vIndex = 0; vIndex < bvts.Count; vIndex++)
            // {
            //     int count = trisAtVert[vIndex].Count;
            //     if (count == 5) p5.Add(new Poly(OrderRing(vIndex)));
            //     else if (count == 6) p6.Add(new Poly(OrderRing(vIndex)));
            // }

            polys = Enumerable.Range(0, bvts.Count)
                .Where(i => trisAtVert[i].Count >= 5)
                .Select(i => new Poly(OrderRing(i)))
                .ToArray();

            // pentagons = p5.ToArray();
            // hexagons = p6.ToArray();
            verts = dvts.ToArray();
        }
        #endregion

        #region  Cube Sphere
        public static void BuildCubeSphere(int resolution, out Vector3[] verts, out Poly[] quads)
        {
            if (resolution < 1) resolution = 1;

            List<Vector3> vertList = new(); // unique cube-space vertices
            List<Poly> quadList = new();

            Dictionary<Vector3, int> vertToIndex = new(); // ensures shared verts at seams

            int GetIndex(Vector3 cubePos)
            {
                if (!vertToIndex.TryGetValue(cubePos, out int idx))
                {
                    idx = vertList.Count;
                    vertList.Add(cubePos);
                    vertToIndex.Add(cubePos, idx);
                }
                return idx;
            }

            // Map (face, u, v) -> cube position in [-1,1]
            Vector3 CubePosForFace(int face, float u, float v)
            {
                // u,v are in [-1,1]
                return face switch
                {
                    0 => new Vector3(1f, v, u),  // +X
                    1 => new Vector3(-1f, v, -u),  // -X
                    2 => new Vector3(u, 1f, v),  // +Y
                    3 => new Vector3(u, -1f, -v),  // -Y
                    4 => new Vector3(u, v, 1f),  // +Z
                    5 => new Vector3(-u, v, -1f),   // -Z
                    _ => Vector3.zero
                };
            }

            float step = 2f / resolution; // param step on [-1,1]

            // Build 6 cube faces, sharing edges via vertToIndex
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float v0 = -1f + step * y;
                    float v1 = -1f + step * (y + 1);

                    for (int x = 0; x < resolution; x++)
                    {
                        float u0 = -1f + step * x;
                        float u1 = -1f + step * (x + 1);

                        // 4 corner positions on cube
                        Vector3 p00 = CubePosForFace(face, u0, v0); // bottom-left
                        Vector3 p10 = CubePosForFace(face, u1, v0); // bottom-right
                        Vector3 p11 = CubePosForFace(face, u1, v1); // top-right
                        Vector3 p01 = CubePosForFace(face, u0, v1); // top-left

                        int i00 = GetIndex(p00);
                        int i10 = GetIndex(p10);
                        int i11 = GetIndex(p11);
                        int i01 = GetIndex(p01);

                        // Consistent winding (clockwise when looking outward)
                        quadList.Add(new Poly(i00, i10, i11, i01));
                    }
                }
            }

            // Project cube vertices to a "spherified" sphere
            verts = new Vector3[vertList.Count];
            for (int i = 0; i < vertList.Count; i++)
            {
                Vector3 p = vertList[i];

                // Spherified cube mapping for more uniform quads than simple normalize
                float x = p.x, y = p.y, z = p.z;
                float x2 = x * x, y2 = y * y, z2 = z * z;

                float sx = x * Mathf.Sqrt(1f - (y2 + z2) * 0.5f + (y2 * z2) / 3f);
                float sy = y * Mathf.Sqrt(1f - (z2 + x2) * 0.5f + (z2 * x2) / 3f);
                float sz = z * Mathf.Sqrt(1f - (x2 + y2) * 0.5f + (x2 * y2) / 3f);

                Vector3 s = new Vector3(sx, sy, sz).normalized;
                verts[i] = s;
            }

            quads = quadList.ToArray();
        }
        #endregion

        #region Hashing
        public static int GetIndicesHash(this IEnumerable<int> ints) =>
            ints.Aggregate(unchecked((int)2166136261), (a, i) => a = (a ^ i) * 16777619);
        #endregion
    }
}

//     #region Triangulation
//     public static class Triangulator
//     {
//         public static Poly[] Triangulate(Poly poly, Vector3[] vertices)
//         {
//             Vector3 polyNormal = poly.GetNormal(vertices);
//             Vector2[] points = Polygon3DTo2D(poly.GetVertices(vertices));
//             Dictionary<int, int> mapping = poly.indices.Select((i0, i1) => (i0, i1)).ToDictionary(t => t.i1, t => t.i0);

//             List<Poly> triangles = new();

//             int n = points.Length;
//             if (n < 3) return new Poly[0];

//             int[] V = new int[n];
//             if (points.SignedArea() > 0) for (int v = 0; v < n; V[v] = v, v++) ;
//             else for (int v = 0; v < n; V[v] = n - 1 - v, v++) ;

//             int nv = n, count = 2 * nv, u, w;
//             for (int m = 0, v = nv - 1; nv > 2;)
//             {
//                 if (count-- <= 0)
//                 {
//                     Debug.LogError("Triangulate::Exiting Early");
//                     break;
//                 }

//                 u = v; if (nv <= u) u = 0;
//                 v = u + 1; if (nv <= v) v = 0;
//                 w = v + 1; if (nv <= w) w = 0;

//                 if (Snip(u, v, w, nv, V))
//                 {
//                     triangles.Add(new(mapping[V[u]], mapping[V[v]], mapping[V[w]]));
//                     for (int s = v, t = v + 1; t < nv; V[s] = V[t], s++, t++) ;
//                     m++;
//                     nv--;
//                     count = 2 * nv;
//                 }
//             }

//             if (Vector3.Dot(polyNormal, triangles.First().GetNormal(vertices)) < 0.99f)
//                 triangles.ForEach(t => t.Reverse());
//             return triangles.ToArray();

//             // static bool IsCCW(Vector2 a, Vector2 b, Vector2 c) =>
//             //     (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0f;

//             bool Snip(int u, int v, int w, int n, int[] V)
//             {
//                 int p;
//                 Vector2 A = points[V[u]], B = points[V[v]], C = points[V[w]];
//                 if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
//                     return false;
//                 for (p = 0; p < n; p++)
//                 {
//                     if ((p == u) || (p == v) || (p == w))
//                         continue;
//                     Vector2 P = points[V[p]];
//                     if (P.InsideTriangle(A, B, C))
//                         return false;
//                 }
//                 return true;
//             }

//         }

//     }
//     #endregion
// }
