using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace pigbrain.core.Collections
{
    public static class CoroutineUtility
    {
        public static WaitForSeconds WaitSecondsTenth => new(0.1f);
        public static WaitForSeconds WaitSecondsQuarter => new(0.25f);
        public static WaitForSeconds WaitSecondsHalf => new(0.5f);
        public static WaitForSeconds WaitSecondsOne => new(1);

        public static IEnumerator RunImmediate(this IEnumerator e)
        {
            if (e == null) yield break;
            if (!e.MoveNext()) yield break;
            yield return e.Current;
            while (e.MoveNext()) yield return e.Current;
        }

        public static IEnumerator DelayFrames(int frameCount = 1, Action callback = null)
        {
            for (int i = 0; i < frameCount; i++)
                yield return null;
            callback?.Invoke();
        }

        public static IEnumerator Delay(float time, Action callback = null)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        public static IEnumerator OverTime(float duration, Action<float> callback = null)
        { yield return new OverTime(duration, callback); }
    }

    #region  Button Click
    public class WaitForButtonClick : CustomYieldInstruction
    {
        Button button;
        InputAction inputAction;
        bool waiting = true;
        public WaitForButtonClick(Button button, InputAction inputAction = null)
        {
            this.button = button;
            this.inputAction = inputAction;
            if (inputAction != null)
            {
                inputAction.performed += OnInput;
                if (!inputAction.enabled) inputAction.Enable();
            }
            this.button.onClick.AddListener(OnClick);
        }

        void OnClick() => Stop();
        void OnInput(InputAction.CallbackContext ctx) => Stop();
        void Stop()
        {
            if (button) button.onClick.RemoveListener(OnClick);
            if (inputAction != null) inputAction.performed -= OnInput;
            waiting = false;
        }
        public override bool keepWaiting => waiting;
    }
    #endregion

    #region  Next Update
    public class WaitForSecondsOrUntil : CustomYieldInstruction
    {
        Func<bool> predicate;
        float timestamp;
        public WaitForSecondsOrUntil(float duration, Func<bool> predicate)
        {
            this.predicate = predicate;
            this.timestamp = Time.time + duration;
        }
        public override bool keepWaiting => Time.time >= timestamp || predicate();
    }
    #endregion

    #region  Next Update
    public class WaitForNextUpdate : CustomYieldInstruction
    {
        bool waited = false;
        public override bool keepWaiting =>
            Time.timeScale == 0 || (!waited && (waited = true));
    }
    #endregion

    #region  Next Update
    public class RepeatUntil : CustomYieldInstruction
    {
        float time;
        readonly float frequency;
        readonly Action update;
        readonly Func<bool> condition;
        public RepeatUntil(float frequency, Action update, Func<bool> condition)
        {
            this.frequency = frequency;
            this.update = update;
            this.condition = condition;
        }

        public override bool keepWaiting
        {
            get
            {
                if ((time += Time.deltaTime) >= frequency)
                {
                    time -= frequency;
                    update();
                }
                return !condition();
            }
        }
    }
    #endregion

    #region  Wait for Object
    public class WaitForObject<T> : CustomYieldInstruction where T : UnityEngine.Object
    {
        readonly FindObjectsInactive flags;
        public WaitForObject(FindObjectsInactive flags = FindObjectsInactive.Exclude) =>
            this.flags = flags;
        public override bool keepWaiting =>
            UnityEngine.Object.FindAnyObjectByType<T>(flags) == null;
    }
    #endregion

    #region  Accumulator
    public sealed class Accumulator : CustomYieldInstruction
    {
        readonly Action action;
        readonly float frequency;
        int count;
        float timer = 0, lastTime;

        public Accumulator(float frequency, int count, Action action)
        {
            this.frequency = frequency;
            this.count = count;
            this.action = action;
            lastTime = Time.time;
        }

        int Update()
        {
            float now = Time.time;
            timer += now - lastTime;
            lastTime = now;
            for (; timer >= frequency && count > 0; timer -= frequency, action(), count--) ;
            return count;
        }
        public override bool keepWaiting => Update() > 0;
    }
    #endregion

    #region Over Time
    public abstract class OverTimeBase : CustomYieldInstruction
    {
        readonly Action<float> action;
        readonly float duration;
        readonly float startTime = 0;
        // readonly float ease; // -1 in +1 out

        protected abstract float time { get; }
        public OverTimeBase(float duration, Action<float> action)
        {
            this.action = action;
            this.duration = Mathf.Max(duration, 0.0001f);
            this.startTime = time;
        }

        float Update()
        {
            float t = Mathf.Clamp01((time - startTime) / duration);
            action(t);
            return t;
        }
        public override bool keepWaiting => Update() < 1;
    }

    public sealed class OverTime : OverTimeBase
    {
        protected override float time => Time.time;
        public OverTime(float duration, Action<float> action) : base(duration, action) { }
        public static IEnumerator Coroutine(float duration, Action<float> action)
        { yield return new OverTime(duration, action); }
    }

    public sealed class OverTimeUnscaled : OverTimeBase
    {
        protected override float time => Time.unscaledTime;
        public OverTimeUnscaled(float duration, Action<float> action) : base(duration, action) { }
        public static IEnumerator Coroutine(float duration, Action<float> action)
        { yield return new OverTimeUnscaled(duration, action); }
    }
    #endregion
}