using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.core.Motion
{
    public class MotionDrop : MotionData
    {
        [SerializeField] float height = 10;

        protected override IEnumerator Animate(Transform transform)
        {
            Vector3 start = transform.localPosition.AddY(height), end = transform.localPosition;
            yield return new OverTime(duration, (t) => transform.localPosition = Vector3.Lerp(start, end, t));
        }
    }
}
