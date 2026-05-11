using System.Linq;
using UnityEngine;

namespace PigBrain.LegacyCore.Geom
{
    public static class BoundsUtility
    {
        public static Bounds Transform(this Bounds localBounds, Transform transform)
        {
            var center = transform.TransformPoint(localBounds.center);

            // Transform the extents; since Bounds is axis-aligned, you must expand it to fit the rotated box.
            Vector3 extents = localBounds.extents;
            Vector3 axisX = transform.TransformVector(new Vector3(extents.x, 0, 0));
            Vector3 axisY = transform.TransformVector(new Vector3(0, extents.y, 0));
            Vector3 axisZ = transform.TransformVector(new Vector3(0, 0, extents.z));

            // The new extents are the sum of the absolute axes
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            return new Bounds(center, extents * 2);
        }

        public static bool Contains(this Bounds outer, Bounds inner)
        {
            Vector3 innerMin = inner.min;
            Vector3 innerMax = inner.max;

            return outer.Contains(innerMin) && outer.Contains(innerMax);
        }

        public static Vector3[] GetCorners(this Bounds bounds)
        {
            Vector3 a = bounds.min, b = bounds.max;
            return new Vector3[]
            {
                new (a.x, a.y, a.z), new (a.x, a.y, b.z), new (a.x, b.y, a.z), new (a.x, b.y, b.z),
                new (b.x, a.y, a.z), new (b.x, a.y, b.z), new (b.x, b.y, a.z), new (b.x, b.y, b.z),
            };
        }

        public static Rect WorldBoundsToScreenRect(this Bounds bounds, Camera camera)
        {
            Vector3[] corners = GetCorners(bounds).Select(v => camera.WorldToScreenPoint(v)).ToArray();

            Vector2 min = new(corners.Min(v => v.x), corners.Min(v => v.y)),
                max = new(corners.Max(v => v.x), corners.Max(v => v.y));

            return new Rect(min.x, Screen.height - min.y - (max.y - min.y), max.x - min.x, max.y - min.y);
        }
    }
}