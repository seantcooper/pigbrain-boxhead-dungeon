// using System;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Geom
// {
//     public static class RectX
//     {
//         public static Rect Shrink(this Rect rect, float value) => Expand(rect, -value);
//         public static Rect Expand(this Rect rect, float value)
//         {
//             rect.min -= Vector2.one * value;
//             rect.max += Vector2.one * value;
//             return rect;
//         }
//         public static Rect WithX(this Rect r, float x) => new(x, r.y, r.width, r.height);
//         public static Rect WithY(this Rect r, float y) => new(r.x, y, r.width, r.height);
//         public static Rect WithW(this Rect r, float w) => new(r.x, r.y, w, r.height);
//         public static Rect WithH(this Rect r, float h) => new(r.x, r.y, r.width, h);
//         public static Rect AddX(this Rect r, float x) => new(r.x + x, r.y, r.width, r.height);
//         public static Rect AddY(this Rect r, float y) => new(r.x, r.y + y, r.width, r.height);
//         public static Rect AddW(this Rect r, float w) => new(r.x, r.y, r.width + w, r.height);
//         public static Rect AddH(this Rect r, float h) => new(r.x, r.y, r.width, r.height + h);
//         public static Rect WithMinX(this Rect r, float x) => new(r) { xMin = x };
//         public static Rect WithMaxX(this Rect r, float x) => new(r) { xMax = x };
//         public static Rect WithMinY(this Rect r, float y) => new(r) { yMin = y };
//         public static Rect WithMaxY(this Rect r, float y) => new(r) { yMax = y };
//         public static Rect AddMinX(this Rect r, float x) => new(r) { xMin = r.xMin + x };
//         public static Rect AddMaxX(this Rect r, float x) => new(r) { xMax = r.xMax + x };
//         public static Rect AddMinY(this Rect r, float y) => new(r) { yMin = r.yMin + y };
//         public static Rect AddMaxY(this Rect r, float y) => new(r) { yMax = r.yMax + y };
//     }
// }