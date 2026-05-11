using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Utility
{
    public static class Vector2X
    {
        public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross(b - a, p - a), c2 = Cross(c - b, p - b), c3 = Cross(a - c, p - c);
            return (c1 >= 0 && c2 >= 0 && c3 >= 0) || (c1 <= 0 && c2 <= 0 && c3 <= 0);
        }

        public static bool IsCCW(this Vector2[] vts)
        {
            float area = 0f;
            for (int n = vts.Length, j = n - 1, i = 0; i < n;
                 area += vts[i].x * vts[j].y - vts[j].x * vts[i].y, j = i, i++) ;
            return area > 0f;
        }

        public static bool InsideTriangle(this Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float ax = c.x - b.x, ay = c.y - b.y;
            float bx = a.x - c.x, by = a.y - c.y;
            float cx = b.x - a.x, cy = b.y - a.y;
            float apx = p.x - a.x, apy = p.y - a.y;
            float bpx = p.x - b.x, bpy = p.y - b.y;
            float cpx = p.x - c.x, cpy = p.y - c.y;

            float aCROSSbp = ax * bpy - ay * bpx;
            float cCROSSap = cx * apy - cy * apx;
            float bCROSScp = bx * cpy - by * cpx;

            return aCROSSbp >= 0.0f && bCROSScp >= 0.0f && cCROSSap >= 0.0f;
        }

        public static float SignedArea(this Vector2[] points)
        {
            float A = 0.0f;
            for (int n = points.Length, p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = points[p], qval = points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        public static Vector2 WithX(this Vector2 v, float x) => new(x, v.y);
        public static Vector2 WithY(this Vector2 v, float y) => new(v.x, y);
        public static Vector2 AddX(this Vector2 v, float x) => new(v.x + x, v.y);
        public static Vector2 AddY(this Vector2 v, float y) => new(v.x, v.y + y);

        public static int2 WithX(this int2 v, int x) => new(x, v.y);
        public static int2 WithY(this int2 v, int y) => new(v.x, y);
        public static int2 AddX(this int2 v, int x) => new(v.x + x, v.y);
        public static int2 AddY(this int2 v, int y) => new(v.x, v.y + y);

    }
}
