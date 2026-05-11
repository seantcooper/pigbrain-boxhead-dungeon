using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace pigbrain.core.Geom
{
    public static class RectX
    {
        public static Rect Encapsulate(this IEnumerable<Rect> rects)
        {
            if (rects.Count() == 0) return default;
            Rect r = rects.First();
            foreach (var e in rects)
            {
                r.min = Vector2.Min(r.min, e.min);
                r.max = Vector2.Max(r.max, e.max);
            }
            return r;
        }

        public static Rect Shrink(this Rect rect, float value) => Expand(rect, -value);
        public static Rect Expand(this Rect rect, float value)
        {
            rect.min -= Vector2.one * value;
            rect.max += Vector2.one * value;
            return rect;
        }
        public static Rect WithP(this Rect r, Vector2 p) => new(p.x, p.y, r.width, r.height);
        public static Rect WithS(this Rect r, Vector2 p) => new(r.x, r.y, p.x, p.y);
        public static Rect WithX(this Rect r, float x) => new(x, r.y, r.width, r.height);
        public static Rect WithY(this Rect r, float y) => new(r.x, y, r.width, r.height);
        public static Rect WithW(this Rect r, float w) => new(r.x, r.y, w, r.height);
        public static Rect WithH(this Rect r, float h) => new(r.x, r.y, r.width, h);
        public static Rect AddP(this Rect r, Vector2 p) => new(r.x + p.x, r.y + p.y, r.width, r.height);
        public static Rect AddS(this Rect r, Vector2 p) => new(r.x, r.y, r.width + p.x, r.height + p.y);
        public static Rect AddX(this Rect r, float x) => new(r.x + x, r.y, r.width, r.height);
        public static Rect AddY(this Rect r, float y) => new(r.x, r.y + y, r.width, r.height);
        public static Rect AddW(this Rect r, float w) => new(r.x, r.y, r.width + w, r.height);
        public static Rect AddH(this Rect r, float h) => new(r.x, r.y, r.width, r.height + h);

        public static Rect WithMin(this Rect r, Vector2 m) => new(r) { min = m };
        public static Rect WithMax(this Rect r, Vector2 m) => new(r) { max = m };

        public static Rect WithMinX(this Rect r, float x) => new(r) { xMin = x };
        public static Rect WithMaxX(this Rect r, float x) => new(r) { xMax = x };
        public static Rect WithMinY(this Rect r, float y) => new(r) { yMin = y };
        public static Rect WithMaxY(this Rect r, float y) => new(r) { yMax = y };
        public static Rect AddMin(this Rect r, Vector2 m) => new(r) { min = r.min + m };
        public static Rect AddMax(this Rect r, Vector2 m) => new(r) { max = r.max + m };
        public static Rect AddMinX(this Rect r, float x) => new(r) { xMin = r.xMin + x };
        public static Rect AddMaxX(this Rect r, float x) => new(r) { xMax = r.xMax + x };
        public static Rect AddMinY(this Rect r, float y) => new(r) { yMin = r.yMin + y };
        public static Rect AddMaxY(this Rect r, float y) => new(r) { yMax = r.yMax + y };
    }
}
