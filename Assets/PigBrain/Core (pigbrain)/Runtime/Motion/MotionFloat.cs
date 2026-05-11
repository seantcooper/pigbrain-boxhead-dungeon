using System;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;
using static pigbrain.game.Boxhead.CommandSetValue;

namespace pigbrain.core.Motion
{
    public class MotionFloat : MotionData
    {
        [Header("Float")]
        [SerializeField] ValueBinder<float>[] bindings;
        [SerializeField][Range(0, 10)] float start = 1;
        [SerializeField][Range(0, 10)] float end = 2;
        [SerializeField] float value;


        protected override IEnumerator Animate(Transform transform)
        {
            yield return OverTimeRepeat((t) => SetValue(transform, value = Mathf.Lerp(start, end, t)));
        }

        void SetValue(Transform transform, float value)
        {
            foreach (var binding in bindings)
                binding.SetValue(transform, value);
        }
    }
}
