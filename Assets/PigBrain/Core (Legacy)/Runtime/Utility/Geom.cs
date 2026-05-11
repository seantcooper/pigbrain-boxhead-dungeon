using System.Text.RegularExpressions;
using UnityEngine;

namespace PigBrain.LegacyCore.Utility
{
    public static class GeomUtility
    {
        public static Vector4 WithW(this Vector3 vector, float w) => new Vector4(vector.x, vector.y, vector.z, w);

        public static float RotateToTarget(this Transform transform, Vector3 target, float rotationSpeed)
        {
            if (!transform) return 0;

            // Direction to target
            Vector3 direction = target - transform.position;
            if (direction == Vector3.zero) return 0;

            // Desired rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Smooth rotate towards target
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed
            );

            return Vector3.Angle(transform.forward, direction);
        }
    }
}
