using UnityEngine;

namespace pigbrain.core.Utility
{
    public class DeltaTime
    {
        float lastTime, dTime;
        int frameCount;

        public DeltaTime(float startTime)
        {
            lastTime = startTime;
            frameCount = Time.frameCount;
        }

        public float deltaTime
        {
            get
            {
                if (frameCount != Time.frameCount) return dTime;
                float now = Time.time;
                dTime = now - (lastTime == 0 ? now : lastTime);
                frameCount = Time.frameCount;
                lastTime = now;
                return dTime;
            }
        }
    }
}
