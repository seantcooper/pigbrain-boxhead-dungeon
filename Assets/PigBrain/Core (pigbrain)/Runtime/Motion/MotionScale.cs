using System.Collections;
using UnityEngine;

namespace pigbrain.core.Motion
{
    public class MotionScale : MotionData
    {
        [Header("Axis Move")]
        [SerializeField][Range(0, 50)] float startScale = 1;
        [SerializeField][Range(0, 50)] float endScale = 2;

        protected override IEnumerator Animate(Transform transform)
        {
            Vector3 scale = transform.localScale;
            yield return OverTimeRepeat((t) => transform.localScale = scale * Mathf.Lerp(startScale, endScale, t));
        }
    }
}

