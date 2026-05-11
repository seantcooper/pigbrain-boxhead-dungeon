using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Map;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Geom
{
    public static class BoundsX
    {
        public static Bounds WithCenter(this Bounds b, Vector3 v) => new(v, b.size);
        public static Bounds AddCenter(this Bounds b, Vector3 v) => new(b.center + v, b.size);

        public static Bounds Inflate(this Bounds b, float amount)
        { b.Expand(amount); return b; }

        public static Bounds Inflate(this Bounds b, Vector3 amount)
        { b.Expand(amount); return b; }

        public static Bounds Lerp(Bounds a, Bounds b, float t)
        {
            return new Bounds(
                Vector3.Lerp(a.center, b.center, t),
                Vector3.Lerp(a.size, b.size, t)
            );
        }

        public static Bounds Encapsulate(this IEnumerable<Bounds> bounds) =>
            bounds.Aggregate(bounds.First(), (b, p) => { b.Encapsulate(p); return b; });

        public static IEnumerable<Bounds> Divide(this Bounds bounds, int3 divisions)
        {
            float3 size = bounds.size, min = bounds.min, cell = size / divisions;
            return MapUtility.Range3d(divisions).Select(i => new Bounds(min + ((float3)i + 0.5f) * cell, cell));
        }

        public static Bounds GetBounds(this IEnumerable<Renderer> renderers) =>
            renderers.Select(r => r.bounds).Encapsulate();

        public static Bounds GetRendererBounds(this Transform transform, bool includeInactive = false) =>
            transform.gameObject.GetRendererBounds(includeInactive);

        public static Bounds GetRendererBounds(this GameObject go, bool includeInactive = false) =>
            go.GetComponentsInChildren<Renderer>(includeInactive).GetBounds();
    }
}
