using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using static UnityEngine.Splines.PathIndexUnit;

namespace pigbrain.core.Geom
{
    public static class GeomConst
    {
        public const float SQRT2 = 1.4142135624f;
        public const float SQRT3 = 1.7320508076f;
        public const float DIST0 = 0.000001f;
    }

    // public struct Distance
    // {
    //     readonly float d, dsqr;
    //     public Distance(float value) => dsqr = (d = value) * d;
    //     public static implicit operator float(Distance d) => ;
    // }

    public struct Counter
    {
        readonly int max;
        int count;
        public Counter(int max)
        {
            this.max = max;
            this.count = 0;
        }

        public bool Inc()
        {
            if (++count >= max) { count = 0; return true; }
            return false;
        }
    }

    public class SplineBezier
    {
        readonly Spline spline;
        public readonly float length;

        public SplineBezier(params Vector3[] knots)
        {
            spline = new Spline(knots.Select(p => new BezierKnot(p)));
            spline.SetTangentMode(TangentMode.AutoSmooth);
            length = spline.GetLength();
        }

        public void GetPositionAndRotation(float distance, out Vector3 position, out Quaternion rotation)
        {
            float t = spline.ConvertIndexUnit(Mathf.Clamp(distance, 0f, length), Distance, Normalized);
            spline.Evaluate(t, out var pos, out var tan, out var up);
            position = pos;
            rotation = Quaternion.LookRotation(tan, up);
        }

        public void SetPositionAndRotation(float distance, Transform transform)
        {
            GetPositionAndRotation(distance, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}