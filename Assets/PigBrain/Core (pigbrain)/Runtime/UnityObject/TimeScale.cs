#pragma warning disable UDR0001
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.game.Boxhead.Audio;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public static class TimeScale
    {
        const float DefaultTimeScale = 1;
        static float BaseScale = DefaultTimeScale;
        static readonly List<Scope> Scopes = new();
        public static bool IsPaused => Time.timeScale == 0;

        public static void SetBaseScale(float timeScale)
        {
            BaseScale = timeScale;
            if (Scopes.Count == 0) Time.timeScale = BaseScale;
        }

        static void Restore()
        {
            Time.timeScale = Scopes.Count > 0 ? Scopes.Last().timeScale : BaseScale;
            AudioMixer.Instance.SetPitch(Time.timeScale);
        }

        public class Scope : IDisposable
        {
            internal float timeScale;
            readonly MonoBehaviour handler;
            readonly Coroutine transition;
            public Scope(float timeScale) { Scopes.Add(this); Set(timeScale); }
            public Scope(MonoBehaviour handler, float duration, float timeScale)
            {
                Scopes.Add(this);
                if (duration <= 0) Set(timeScale);
                else transition = (this.handler = handler).StartCoroutine(
                    Transition(duration, Time.timeScale, timeScale));
            }

            IEnumerator Transition(float duration, float start, float end)
            { yield return new OverTime(duration, (t) => this?.Set(Mathf.Lerp(start, end, t))); }

            public void Set(float timeScale) { this.timeScale = timeScale; Restore(); }
            public void Dispose()
            {
                if (!Scopes.Contains(this)) return;
                if (transition != null) handler.StopCoroutine(transition);
                Scopes.Remove(this);
                Restore();
            }
            public static implicit operator bool(Scope empty) => empty != null;
        }
    }

    static class TimeScaleX
    {
        public static void TryDispose(this TimeScale.Scope scope)
        {
            if (scope) scope.Dispose();
        }
    }
}