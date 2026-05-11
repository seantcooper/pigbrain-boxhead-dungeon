using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.core.Motion
{
    public class MotionAxisMove : MotionData
    {
        [Header("Axis Move")]
        [SerializeField] Axis axis = Axis.Z;
        [SerializeField][Range(-10, 10)] float minDistance = 0;
        [SerializeField][Range(-10, 10)] float maxDistance = 1;

        protected override IEnumerator Animate(Transform transform)
        {
            Vector3 localPosition = transform.localPosition;
            Vector3 d = axis.GetAxisDirection();
            yield return OverTimeRepeat((t) => transform.localPosition =
                Vector3.Lerp(localPosition + d * minDistance, localPosition + d * maxDistance, t));
        }
    }
}


