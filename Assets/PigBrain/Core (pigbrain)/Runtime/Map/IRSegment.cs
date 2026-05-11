using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Geom;
using Unity.Mathematics;
using UnityEngine;
using static pigbrain.core.Map.IRGeom;

namespace pigbrain.core.Map
{
    [Serializable]
    public class IRSegment
    {
        [SerializeField][Range(0, 250)] int iterations = 100;
        [SerializeField] Relax2D.Settings settings;


        int2 position;
        int radius = 2, seed = 10001;
        float scale = 1;

        Geometry geometry;

        public Geometry Geom => geometry ??= Rebuild();

        public void SetSeed(int seed) => Invalidate(() => this.seed = seed);
        public void SetPosition(int2 position) => Invalidate(() => this.position = position);
        public void SetRadius(int radius) => Invalidate(() => this.radius = Mathf.Max(1, radius));
        public void SetScale(float scale) => Invalidate(() => this.scale = Mathf.Max(0.01f, scale));

        public void Invalidate(Action setter = null)
        {
            setter?.Invoke();
            geometry = null;
        }

        Geometry Rebuild()
        {
            Poly[] quads;
            List<Vector2> points;

            // using (var _ = new Profiler.Scoped("Construct"))
            {
                List<int2> ipoints = GetHexPoints(int2.zero, radius).ToList();
                Edges edges = new(GetTriangles(ipoints).ToArray());
                points = ipoints.Select(i => IRGeom.ToVert(i)).ToList();
                quads = SplitToQuads(edges.GetCombined(new Rnd(seed)).ToArray(), points).ToArray();
            }

            Geometry geom = new(quads, points.Select(p => p * scale).ToArray(),
                CreateNeighbors(quads));

            // using (var _ = new Profiler.Scoped("Relax"))
            {
                Relax2D relax = new(geom.quads, geom.points) { settings = settings };
                relax.Process(iterations);
            }

            return geom;

        }

        public class Geometry
        {
            public readonly Vector2[] points;
            public readonly Poly[] quads;
            public readonly Dictionary<Poly, Neighbor[]> neighbors;

            public Geometry(Poly[] quads, Vector2[] points, Dictionary<Poly, Neighbor[]> neighbors)
            {
                this.quads = quads;
                this.points = points;
                this.neighbors = neighbors;
            }
        }
    }
}
