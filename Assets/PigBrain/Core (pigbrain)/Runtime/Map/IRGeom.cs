using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Map
{
    public class IRGeom
    {
        #region Construct
        public static readonly int2[] NeighborsX = new int2[] { new(1, 3), new(2, 0), new(1, -3), new(-1, -3), new(-2, 0), new(-1, 3) };
        public static readonly int2[] Neighbors = NeighborsX;

        public static IEnumerable<int2> GetHexPoints(int2 p1, int radius) =>
            Enumerable.Range(1, radius).SelectMany(r => HexRing(p1, r)).Prepend(p1);

        static IEnumerable<int2> HexRing(int2 p1, int radius)
        {
            p1 += Neighbors[4] * radius;
            for (int h = 0; h < 6; h++)
                for (int c = 0; c < radius; c++, p1 += Neighbors[h])
                    yield return p1;
        }

        public static IEnumerable<Poly> GetTriangles(IList<int2> points)
        {
            var indexLookup = points.Select((pos, i) => (pos, i)).ToDictionary(x => x.pos, x => x.i);
            for (int i = 0; i < points.Count; i++)
                for (int n1 = 5, n2 = 0; n2 < 6; n1 = n2, n2++)
                    if (indexLookup.TryGetValue(points[i] + Neighbors[n1], out int i1))
                        if (i < i1 && indexLookup.TryGetValue(points[i] + Neighbors[n2], out int i2) && i < i2)
                            yield return new Poly(i, i1, i2);
        }

        #endregion

        #region Metric
        const float SQRT3 = 1.7320508076f;
        const float SQRT2 = 1.4142135624f;
        const float H6 = 1f, R6 = H6 / 2f, W6 = SQRT3 * R6;
        const float YS = H6 / 2, XS = W6;
        const float EPS = 1e-5f;

        static readonly Vector2 XYS = new(XS, YS);
        public static Vector2 ToVert(int2 i) => new(i.x * XS, i.y * YS);
        public static Vector2 ToVert(int2 i, float scale) => (float2)i * (float2)XYS * scale;
        public static T[] Rotate<T>(T[] array, int d)
        {
            while (d < 0) d += array.Length;
            while (d >= array.Length) d -= array.Length;
            return d == 0 ? array : array.Skip(d).Concat(array.Take(d)).ToArray();
        }
        static float QuadArea(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float a1 = Mathf.Abs((v0.x * (v1.y - v2.y) + v1.x * (v2.y - v0.y) + v2.x * (v0.y - v1.y)) * 0.5f);
            float a2 = Mathf.Abs((v0.x * (v2.y - v3.y) + v2.x * (v3.y - v0.y) + v3.x * (v0.y - v2.y)) * 0.5f);
            return a1 + a2;
        }
        public static Vector2 QuadCentroid(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float a1 = 0.5f * Mathf.Abs(v0.x * (v1.y - v2.y) + v1.x * (v2.y - v0.y) + v2.x * (v0.y - v1.y));
            float a2 = 0.5f * Mathf.Abs(v0.x * (v2.y - v3.y) + v2.x * (v3.y - v0.y) + v3.x * (v0.y - v2.y));
            return ((v0 + v1 + v2) / 3f * a1 + (v0 + v2 + v3) / 3f * a2) / (a1 + a2);
        }
        #endregion

        #region Split
        public static List<Poly> SplitToQuads(IList<Poly> polys, List<Vector2> verts)
        {
            Dictionary<(int, int), int> splits = new();
            polys.SelectMany(q => q.Edges).Distinct().ForEach(edge =>
            {
                splits[edge] = verts.Count;
                verts.Add((verts[edge.i1] + verts[edge.i2]) * 0.5f);
            });

            List<Poly> result = new();
            foreach (var p in polys)
            {
                int n = p.indices.Length, ic = verts.Count;
                verts.Add(p.indices.Aggregate(Vector2.zero, (a, v) => a += verts[v]) / n);
                for (int i1 = 0, i2 = n - 1; i1 < n; i2 = i1, i1++)
                    result.Add(new Poly(p.indices[i1], splits[p.Edges[i1]], ic, splits[p.Edges[i2]]));
            }
            return result;
        }
        #endregion

        #region Relax
        public class Relax2D
        {
            readonly Poly[] quads;
            readonly Vector2[] verts;
            readonly Vector2[] forces;
            readonly int[] movables;
            readonly Dictionary<(int i1, int i2), HashSet<Poly>> edges;
            readonly Dictionary<int, List<int>> vertToQuads;
            public Settings settings = new();

            [Serializable]
            public class Settings
            {
                public float strength = 0.35f;
                public float edgeStrength = 1;
                public float smooth = 0.2f;
                public Settings(float strength = 0.35f, float edgeStrength = 1, float smooth = 0.2f)
                {
                    this.strength = strength;
                    this.edgeStrength = edgeStrength;
                    this.smooth = smooth;
                }
            }

            public Relax2D(Poly[] quads, Vector2[] verts)
            {
                this.quads = quads;
                this.verts = verts;

                Vector2[] forces = new Vector2[verts.Length];

                this.edges = Edges.GetShared(quads);
                this.movables = Enumerable.Range(0, verts.Length)
                     .Except(edges.Where(e => e.Value.Count == 1)
                     .SelectMany(e => new[] { e.Key.i1, e.Key.i2 }).Distinct())
                     .ToArray();

                this.vertToQuads = quads.SelectMany((q, qi) => q.indices.Select(vi => (vi, qi)))
                    .GroupBy(x => x.vi).ToDictionary(g => g.Key, g => g.Select(x => x.qi).ToList());
            }

            public void Process(int iterations)
            {
                for (; iterations > 0; --iterations)
                    Step();
            }

            Vector2[] GetCentroids()
            {
                Vector2[] centroids = new Vector2[quads.Length];
                for (int qi = 0; qi < quads.Length; qi++)
                {
                    var ids = quads[qi].indices;
                    centroids[qi] = (verts[ids[0]] + verts[ids[1]] + verts[ids[2]] + verts[ids[3]]) * 0.25f;
                }
                return centroids;
            }

            void Step()
            {
                Array.Clear(forces, 0, verts.Length);

                Vector2[] centroids = GetCentroids();

                for (int vi = 0; vi < verts.Length; vi++)
                {
                    if (!vertToQuads.TryGetValue(vi, out var list) || list.Count == 0)
                        continue;
                    Vector2 avg = list.Aggregate(Vector2.zero, (a, i) => a += centroids[i]) / list.Count;
                    forces[vi] = (avg - verts[vi]) * settings.strength;
                }
                movables.ForEach(i => verts[i] += forces[i]);

                // 4. Smooth it
                Vector2[] vsmooth = new Vector2[verts.Length];
                int[] count = new int[verts.Length];

                foreach (var e in edges)
                {
                    int i1 = e.Key.i1, i2 = e.Key.i2;
                    vsmooth[i1] += verts[i2]; count[i1]++;
                    vsmooth[i2] += verts[i1]; count[i2]++;
                }

                foreach (int i in movables)
                    verts[i] = Vector2.Lerp(verts[i], vsmooth[i] / count[i], settings.smooth);
            }
        }
        #endregion

        #region Poly
        public class Poly
        {
            public int[] indices;

            (int i1, int i2)[] edges;
            public (int i1, int i2)[] Edges => edges ??= indices.Select((v, i) => (i1: v, i2: indices[(i + 1) % indices.Length]))
                .Select(t => t.i1 > t.i2 ? (t.i2, t.i1) : t).ToArray();

            internal void Rotate(int r)
            {
                indices = IRGeom.Rotate(indices, r);
                edges = IRGeom.Rotate(edges, r);
            }

            public T[] GetVertices<T>(T[] vertices) => indices.Select(i => vertices[i]).ToArray();
            public (Vector2 a, Vector2 b, Vector2 c, Vector2 d) GetVertices4(Vector2[] vertices) =>
                (vertices[indices[0]], vertices[indices[1]], vertices[indices[2]], vertices[indices[3]]);

            public Poly(params int[] indices) => this.indices = indices;
            public static implicit operator bool(Poly empty) => empty != null;
        }
        #endregion

        #region Edges
        public class Edges
        {
            readonly Dictionary<(int i1, int i2), HashSet<Poly>> sharedEdges;

            public Edges(IEnumerable<Poly> triangles) =>
                sharedEdges = GetShared(triangles);

            IEnumerable<(int i1, int i2)> GetEdgePairs() =>
               sharedEdges.Where(e => e.Value.Count == 2).Select(p => p.Key);

            public static Poly CombineTriangles(Poly t1, Poly t2, (int i1, int i2) e)
            {
                int a1 = t1.indices.First(i => i != e.i1 && i != e.i2);
                int a2 = t2.indices.First(i => i != e.i1 && i != e.i2);
                int i0 = Array.IndexOf(t1.indices, e.i1);
                int i1 = Array.IndexOf(t1.indices, e.i2);
                return new Poly(a1, (i1 == (i0 + 1) % 3) ? e.i1 : e.i2, a2, (i1 == (i0 + 1) % 3) ? e.i2 : e.i1);
            }

            Poly CombineEdge((int i1, int i2) edge)
            {
                var triangles = sharedEdges[edge];
                if (triangles.Count != 2) return null;

                Poly t1 = triangles.ElementAt(0), t2 = triangles.ElementAt(1);
                Poly poly = CombineTriangles(t1, t2, edge);

                void Remove(Poly p) => p.Edges.ForEach(e =>
                {
                    if (sharedEdges[e].Remove(p) && sharedEdges[e].Count == 0)
                        sharedEdges.Remove(e);
                });

                Remove(t1); Remove(t2);
                return poly;
            }

            // Combines common edged triangles into quads
            public IEnumerable<Poly> GetCombined(Rnd rnd)
            {
                // combine all paried edges
                var pairs = GetEdgePairs().OrderBy(p => rnd.NextFloat()).ToArray();
                foreach (var edge in pairs)
                    if (sharedEdges.ContainsKey(edge) && CombineEdge(edge) is Poly poly)
                        yield return poly;

                // return the leftover triangles
                foreach (var poly in sharedEdges.SelectMany(p => p.Value).Distinct())
                    yield return poly;
            }

            public static Dictionary<(int i1, int i2), HashSet<Poly>> GetShared(IEnumerable<Poly> polys) =>
                polys.SelectMany(p => p.Edges.Select(e => (p, e)))
                    .GroupBy(t => t.e)
                    .ToDictionary(g => g.Key, g => g.Select(t => t.p).ToHashSet());
        }
        #endregion

        #region Points/Verts
        public static HashSet<int>[] GetNeighboringVerts(IEnumerable<Poly> polys, int totalVerts)
        {
            HashSet<int>[] points = Enumerable.Range(0, totalVerts).Select(i => new HashSet<int>()).ToArray();
            polys.ForEach(p => p.Edges.ForEach(e =>
            {
                points[e.i1].Add(e.i2);
                points[e.i2].Add(e.i1);
            }));
            return points;
        }

        public static HashSet<Poly>[] GetPointPolyShared(IEnumerable<Poly> polys, int totalVerts)
        {
            HashSet<Poly>[] points = Enumerable.Range(0, totalVerts).Select(i => new HashSet<Poly>()).ToArray();
            polys.ForEach(p => p.indices.ForEach(i => points[i].Add(p)));
            return points;
        }
        #endregion

        #region Neighbors
        public class Neighbor
        {
            public Poly quad;
            public int offset;
            public Neighbor(Poly quad, int offset)
            {
                this.quad = quad;
                this.offset = (offset + 4) % 4;
            }
            public static implicit operator bool(Neighbor empty) => empty != null;
        }

        public static Dictionary<Poly, Neighbor[]> CreateNeighbors(IList<Poly> quads)
        {
            var edges = Edges.GetShared(quads);

            IEnumerable<Neighbor> Neighbors(Poly quad)
            {
                foreach (var n in quad.Edges.Select(e => edges[e].FirstOrDefault(p => p != quad)))
                {
                    if (n != null)
                    {
                        var edge = n.Edges.Intersect(quad.Edges).First();
                        yield return new(n, Array.IndexOf(n.Edges, edge) -
                            Array.IndexOf(quad.Edges, edge) + 2);
                    }
                    else yield return null;
                }
            }
            return quads.ToDictionary(k => k, v => Neighbors(v).ToArray());
        }
        #endregion

        #region Projection
        public static Mesh ProjectMesh(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Mesh mesh, Mesh target)
        {
            var vertices = mesh.vertices;

            for (int n = mesh.vertexCount, i = 0; i < n; i++)
            {
                Vector3 v = vertices[i];
                var r = Vector2.Lerp(Vector2.Lerp(a, b, v.x), Vector2.Lerp(d, c, v.x), v.z);
                vertices[i] = new(r.x, v.y, r.y);
            }

            target.vertices = vertices;
            target.RecalculateNormals();
            target.RecalculateBounds();
            return target;
        }
        #endregion
    }
}
