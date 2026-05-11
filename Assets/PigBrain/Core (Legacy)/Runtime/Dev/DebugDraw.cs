using UnityEngine;

namespace PigBrain.LegacyCore.Dev
{
    public static class DebugDraw
    {
        public static void Circle(Vector3 p, float r, Color color, float duration = 0)
        {
            Debug.DrawLine(p - r * Vector3.right, p + r * Vector3.right, color, duration);
            Debug.DrawLine(p - r * Vector3.up, p + r * Vector3.up, color, duration);
            Debug.DrawLine(p - r * Vector3.forward, p + r * Vector3.forward, color, duration);
        }
        public static void Point(Vector3 p, Color color, float duration = 0) =>
            Circle(p, 0.2f, color, duration);

    }
}
