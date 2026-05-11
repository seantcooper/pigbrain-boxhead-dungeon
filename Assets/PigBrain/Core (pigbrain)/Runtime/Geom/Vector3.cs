using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Geom
{
    using static pigbrain.core.Geom.GeomConst;
    public static class Vector3X
    {
        public static float TimeToTarget(this Vector3 delta, float speed) =>
            delta.magnitude / speed;

        public static float Min(this Vector3 scale) =>
            Mathf.Min(scale.x, scale.y, scale.z);
        public static float Max(this Vector3 scale) =>
            Mathf.Max(scale.x, scale.y, scale.z);

        public static Vector3 GetNormal(this Vector3 a, Vector3 b, Vector3 c) =>
            Vector3.Normalize(Vector3.Cross(b - a, c - a));

        public static Vector3 ToVector3(this Color c) => new(c.r, c.g, c.b);

        public static Vector3 AxisNormal(int axis) => axis switch
        { 0 => Vector3.right, 1 => Vector3.up, _ => Vector3.forward, };

        public static Quaternion GetRotation(this Vector3 n)
        {
            if (n.sqrMagnitude < DIST0) return Quaternion.identity;
            n = n.normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(n, up);
        }
        public static float GetAngle(this Vector3 n)
        {
            if (n.sqrMagnitude < DIST0) return 0;
            return Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        }

        public static bool IsZero(this Vector3 v) => v.sqrMagnitude < DIST0;

        public static (Vector3 forward, Vector3 right, Vector3 up) GetBasis(this Vector3 direction)
        {
            Vector3 forward = direction.normalized;
            Vector3 right = Vector3.Cross(forward, Mathf.Abs(forward.y) > 0.999f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, forward);
            return (forward, right, up);
        }
        public static Vector3 Transform(this Vector3 position, (Vector3 forward, Vector3 right, Vector3 up) basis) =>
            position.x * basis.right + position.y * basis.up + position.z * basis.forward;
        public static float3 Transform(this float3 position, (Vector3 forward, Vector3 right, Vector3 up) basis) =>
            position.x * basis.right + position.y * basis.up + position.z * basis.forward;

        public static Vector3 Inverse(this Vector3 v) => new(0 - v.x, 0 - v.y, 0 - v.z);

        public static Vector3 WithX(this Vector3 v, float x) => new(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new(v.x, v.y, z);
        public static Vector3 With(this Vector3 v, int axis, float u) { v[axis] = u; return v; }

        public static Vector3 WithXZ(this Vector3 v, float x, float z) => new(x, v.y, z);
        public static Vector3 WithXY(this Vector3 v, float x, float y) => new(x, y, v.z);
        public static Vector3 WithYZ(this Vector3 v, float y, float z) => new(v.x, y, z);

        public static Vector3 AddX(this Vector3 v, float x) => new(v.x + x, v.y, v.z);
        public static Vector3 AddY(this Vector3 v, float y) => new(v.x, v.y + y, v.z);
        public static Vector3 AddZ(this Vector3 v, float z) => new(v.x, v.y, v.z + z);
        public static Vector3 Add(this Vector3 v, int axis, float u) { v[axis] += u; return v; }

        public static Vector3 MulX(this Vector3 v, float x) => new(v.x * x, v.y, v.z);
        public static Vector3 MulY(this Vector3 v, float y) => new(v.x, v.y * y, v.z);
        public static Vector3 MulZ(this Vector3 v, float z) => new(v.x, v.y, v.z * z);

        public static Vector3 DivX(this Vector3 v, float x) => new(v.x / x, v.y, v.z);
        public static Vector3 DivY(this Vector3 v, float y) => new(v.x, v.y / y, v.z);
        public static Vector3 DivZ(this Vector3 v, float z) => new(v.x, v.y, v.z / z);

        public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);
        public static Vector2 To2D(this Vector3 v, int2 i) => new(v[i.x], v[i.y]);
        public static Vector2 To2D(this Vector3 v, int ix, int iy) => new(v[ix], v[iy]);
        public static Vector3 X_Y(this Vector2 v, float yValue = 0) => new(v.x, yValue, v.y);

        public static Vector3 AbsX(this Vector3 v) => new(Mathf.Abs(v.x), v.y, v.z);
        public static Vector3 AbsY(this Vector3 v) => new(v.x, Mathf.Abs(v.y), v.z);
        public static Vector3 AbsZ(this Vector3 v) => new(v.x, v.y, Mathf.Abs(v.z));

        public static float Distance(this IEnumerable<Vector3> positions) =>
            positions.Zip(positions.Skip(1), (a, b) => Vector3.Distance(a, b)).Sum();

        public static Bounds GetBounds(this IEnumerable<Vector3> positions) =>
            positions.Encapsulate();
        public static Bounds Encapsulate(this IEnumerable<Vector3> positions) =>
            positions.Skip(1).Aggregate(new Bounds(positions.First(), Vector3.zero),
                (b, p) => { b.Encapsulate(p); return b; });

        public static IEnumerable<Vector3> ResamplePath(this IEnumerable<Vector3> positions, float interval)
        {
            yield return positions.First();

            float D = 0f;
            foreach (var (a, b) in positions.Zip(positions.Skip(1), (a, b) => (a, b)))
            {
                float L = Vector3.Distance(a, b);
                Vector3 P = a;
                while (D + L >= interval)
                {
                    float t = (interval - D) / L;
                    Vector3 p = Vector3.Lerp(P, b, t);
                    yield return p;
                    L -= (interval - D);
                    P = p;
                    D = 0f;
                }
                D += L;
            }
        }

        public static Vector3 PositionAlong(this Vector3[] positions, float t)
        {
            if (positions == null || positions.Length == 0) return Vector3.zero;
            if (positions.Length == 1) return positions[0];

            t = Mathf.Clamp01(t);

            float total = 0f;
            for (int i = 0; i < positions.Length - 1; i++)
                total += Vector3.Distance(positions[i], positions[i + 1]);

            if (total <= 0f) return positions[0];

            float target = total * t, acc = 0f;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector3 a = positions[i], b = positions[i + 1];
                float d = Vector3.Distance(a, b);
                if (acc + d >= target)
                {
                    float u = (target - acc) / d;
                    return Vector3.Lerp(a, b, u);
                }
                acc += d;
            }

            return positions[^1];
        }
    }
}
