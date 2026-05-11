using System;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;
using static pigbrain.core.Motion.MotionData;
namespace pigbrain.core.Motion
{
    public class MotionData : ScriptableObject
    {
        [SerializeField] protected Traits traits;
        [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // [SerializeField][Range(-2, 2)] protected float easeIn = 0;
        // [SerializeField][Range(-2, 2)] protected float easeOut = 0;
        [SerializeField][Range(0, 10)] protected float duration = 1;
        [SerializeField][Range(0, 100)] protected int repeat = 1;

        public void Play(MotionEvent motion, Transform target) =>
            motion.StartCoroutine(Control(target));

        IEnumerator Control(Transform transform)
        {
            yield return null;
            yield return Animate(transform);
            if (traits.HasFlag(Traits.AutoDestroy))
                Destroy(transform.gameObject);
        }
        protected virtual IEnumerator Animate(Transform transform) { yield break; }

        protected IEnumerator OverTimeRepeat(Action<float> action)
        {
            for (int i = repeat; repeat == 0 || i > 0; --i)
            {
                yield return new OverTime(duration, (t) => action(ease.Evaluate(t)));
                if (traits.HasFlag(Traits.PingPong))
                    yield return new OverTime(duration, (t) => action(1 - ease.Evaluate(t)));
            }
        }

        public enum Axis { X, Y, Z }
        public enum Traits
        {
            None = 0,
            PingPong = 1 << 0,
            AutoDestroy = 1 << 1,
            Other = 1 << 16,
        }

        // protected float Ease(float t)
        // {
        //     float ei = Mathf.Max(0.0001f, 1f + easeIn), eo = Mathf.Max(0.0001f, 1f + easeOut);
        //     if (t < 0.5f) return 0.5f * Mathf.Pow(t * 2f, ei);
        //     else return 1f - 0.5f * Mathf.Pow((1f - t) * 2f, eo);
        // }
    }

    public static class MotionDataX
    {
        public static Vector3 GetAxisDirection(this Axis axis) => axis switch
        { Axis.X => Vector3.right, Axis.Y => Vector3.up, _ => Vector3.forward, };
    }
}
