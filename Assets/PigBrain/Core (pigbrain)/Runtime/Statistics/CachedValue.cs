#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using pigbrain.core.Utility;
using UnityEngine;

namespace pigbrain.core.Statistics
{
    public struct ScopeState : IDisposable
    {
        readonly Action onDispose;
        public ScopeState(Action onEnter, Action onDispose)
        {
            onEnter?.Invoke();
            this.onDispose = onDispose;
        }
        public void Dispose() => onDispose?.Invoke();
    }

    #region Frame
    public struct CacheValueByFrame<T>
    {
        static int FreqOffset = 0;

        public T value;
        int frame;
        readonly int freq, freqOffset;

        public CacheValueByFrame(T value = default, int freq = 1)
        {
            this.value = value;
            this.freq = Math.Max(1, freq);
            this.freqOffset = this.freq == 1 ? 0 : (++FreqOffset) % this.freq;
            this.frame = Time.frameCount;
        }
        public bool isValid => freq <= 1
            ? frame == Time.frameCount
            : (Time.frameCount + freqOffset) % freq != 0
                || frame == Time.frameCount;

        public void Validate(T value)
        {
            this.value = value;
            frame = Time.frameCount;
        }
        public static implicit operator T(CacheValueByFrame<T> t) => t.value;
    }
    #endregion

    #region Time
    public struct CacheValueByTime<T>
    {
        public const float SingleFrame = 1 / 60f;
        public T value;
        public float frequency, frequencyOffset, nextTime;
        readonly Func<T> setValue;

        public CacheValueByTime(T value = default, float frequency = 1 / 30f, Func<T> setValue = null)
        {
            this.value = value;
            this.frequency = frequency;
            this.frequencyOffset = GetFrequencyOffset(frequency);
            this.setValue = setValue;
            this.nextTime = 0;
        }

        public bool isValid => nextTime > 0 && Time.time < nextTime;
        public void Validate(T value)
        {
            this.value = value;
            nextTime = ((int)(Time.time / frequency) + 1) * frequency + frequencyOffset;
        }
        public void AutoValidate() { if (!isValid) Validate(setValue()); }
        static readonly Dictionary<int, int> Offsets = new();
        static float GetFrequencyOffset(float frequency)
        {
            int key = (int)(frequency / SingleFrame); // frames
            int index = Offsets.ContainsKey(key) ? Offsets[key]++ : (Offsets[key] = 1) - 1;
            return (index * SingleFrame) % frequency;
        }
        public static implicit operator T(CacheValueByTime<T> t) => t.value;
    }
    #endregion
}
