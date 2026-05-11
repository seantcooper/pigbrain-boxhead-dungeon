using System.Collections;
using UnityEngine;

namespace pigbrain.core.Utility
{
    public class FreqTime
    {
        readonly float startTime, frequency;

        public FreqTime(float offset, float frequency)
        {
            this.startTime = Time.time + offset;
            this.frequency = frequency;
        }

        public IEnumerator WaitForFrequency()
        {
            int findex = GetFrequencyIndex(Time.time);
            float nextTime = startTime + (findex + 1) * frequency;
            yield return new WaitForSeconds(Mathf.Max(nextTime - Time.time));
        }

        int GetFrequencyIndex(float time) =>
            Mathf.FloorToInt((time - startTime) / frequency);
    }
}
