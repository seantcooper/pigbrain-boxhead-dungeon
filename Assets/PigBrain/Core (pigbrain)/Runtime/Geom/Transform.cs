using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Geom
{
    using static pigbrain.core.Geom.GeomConst;
    public static class TransformX
    {
        public static Vector3 DirectionTo(this Transform t1, Transform t2)
        {
            if (t1 && t2) return (t2.position - t1.position).normalized;
            return Vector3.zero;
        }

        public static float DistanceTo(this Transform t1, Transform t2)
        {
            if (t1 && t2) return (t1.position - t2.position).magnitude;
            return float.MaxValue;
        }

        public static Vector3 ToWorld(this RectTransform ui, Camera uiCam, Camera worldCam, float worldZ = 0f) =>
            worldCam.ScreenToWorldPoint((uiCam ? uiCam.WorldToScreenPoint(ui.position)
                : (Vector3)RectTransformUtility.WorldToScreenPoint(null, ui.position))
                    .WithZ(Mathf.Abs(worldCam.transform.position.z - worldZ)));
    }
}
