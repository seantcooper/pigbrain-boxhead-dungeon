using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    public static class GizmosUtility
    {
#if UNITY_EDITOR
        static readonly GUIStyle style = new() { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter };
#endif
        public static void DrawText(Vector3 position, object text)
        {
#if UNITY_EDITOR
            style.normal.textColor = Color.white;
            UnityEditor.Handles.Label(position, $"{text}", style);
#endif
        }

        public static void DrawArrowHead(Vector3 a, Vector3 b, float size = 0.5f, float thickness = 0.01f)
        {
#if UNITY_EDITOR
            Vector3 n = b - a;
            float d = n.magnitude;
            DrawLine(a, b, thickness);
            var (forward, right, up) = n.normalized.GetBasis();
            DrawLine(b, b + (right - forward).normalized * size, thickness);
            DrawLine(b, b - (right - forward).normalized * size, thickness);
#endif
        }

        public static void DrawCircle(Vector3 p, float radius, float thickness = 0.01f)
        {
#if UNITY_EDITOR
            const int segments = 32;
            float step = Mathf.PI * 2f / segments;
            Vector3 prev = p + Vector3.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float a = step * i;
                Vector3 next = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                DrawLine(prev, next, thickness);
                prev = next;
            }
#endif
        }

        public static void DrawWireCube(Vector3 p, Vector3 s, float thickness = 0.01f)
        {
#if UNITY_EDITOR
            Vector3 h = s * 0.5f;

            Vector3 p0 = p + new Vector3(-h.x, -h.y, -h.z);
            Vector3 p1 = p + new Vector3(h.x, -h.y, -h.z);
            Vector3 p2 = p + new Vector3(h.x, -h.y, h.z);
            Vector3 p3 = p + new Vector3(-h.x, -h.y, h.z);
            Vector3 p4 = p + new Vector3(-h.x, h.y, -h.z);
            Vector3 p5 = p + new Vector3(h.x, h.y, -h.z);
            Vector3 p6 = p + new Vector3(h.x, h.y, h.z);
            Vector3 p7 = p + new Vector3(-h.x, h.y, h.z);

            DrawLines(new[] { p0, p1, p2, p3 }, thickness, true);
            DrawLines(new[] { p4, p5, p6, p7 }, thickness, true);
            DrawLine(p0, p4, thickness);
            DrawLine(p1, p5, thickness);
            DrawLine(p2, p6, thickness);
            DrawLine(p3, p7, thickness);
#endif
        }

        public static void DrawLines(IList<Vector3> positions, float thickness = 0.02f, bool loop = false)
        {
            if (positions.Count < 2) return;
            for (int i = 1; i < positions.Count; DrawLine(positions[i - 1], positions[i], thickness), i++) ;
            if (loop) DrawLine(positions[^1], positions[0], thickness);
        }
        public static void DrawPoints(IList<Vector3> positions, float size = 0.02f)
        {
            Vector3 s = Vector3.one * size;
            for (int i = 0; i < positions.Count; i++) Gizmos.DrawCube(positions[i], s);
        }

        public static void DrawLine(Vector3 a, Vector3 b, float thickness)
        {
#if UNITY_EDITOR
            Vector3 dir = b - a;
            float len = Mathf.Max(thickness, dir.magnitude);
            using var matrix = new MatrixScope(
                // Gizmos.matrix * 
                Matrix4x4.TRS(a + dir * 0.5f, dir.normalized.GetRotation(),
                    new Vector3(thickness, thickness, len)));
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
#endif
        }

        public class MatrixScope : System.IDisposable
        {
            public Matrix4x4 matrix;
            readonly bool immediate;
            public MatrixScope(Matrix4x4 matrix, bool concat = true)
            {

#if UNITY_EDITOR
                this.matrix = Gizmos.matrix;
                Gizmos.matrix = concat ? this.matrix * matrix : matrix;
#endif
            }
            void System.IDisposable.Dispose()
            {
#if UNITY_EDITOR
                Gizmos.matrix = matrix;
#endif
            }
        }
    }
}