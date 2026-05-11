using UnityEngine;
using System;
using System.Collections;

namespace pigbrain.core.Motion
{
    public static class Ease
    {
        public static float Out(float t, float magnitude = 1) =>
            1f - Mathf.Pow(1f - t, Mathf.Lerp(1f, 3f, Mathf.Clamp01(magnitude)));
        public static float In(float t, float magnitude = 1) =>
            Mathf.Pow(t, Mathf.Lerp(3f, 1f, 1f - magnitude));
    }

    public static class MotionX
    {
        public static Coroutine AnimationBounce(this MonoBehaviour mb, float height, float speed) =>
            mb.StartCoroutine(new Bounce(mb.transform, height, speed));
    }

    public class Bounce : IEnumerator
    {
        readonly Transform transform;
        readonly float height;
        readonly float speed;
        readonly Vector3 basePos;

        float startTime;

        public Bounce(Transform transform, float height, float speed)
        {
            this.transform = transform;
            this.height = height;
            this.speed = speed;
            basePos = transform.position;
            startTime = Time.time;
        }

        public bool MoveNext()
        {
            float y = Mathf.Sin((Time.time - startTime) * speed) * height;
            transform.position = basePos + Vector3.up * y;
            return true;
        }

        public void Reset() { }
        public object Current => null;
    }
}