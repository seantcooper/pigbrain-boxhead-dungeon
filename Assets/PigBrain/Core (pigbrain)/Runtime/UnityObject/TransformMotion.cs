using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class TransformMotion : MonoBehaviour
    {
        [SerializeField][Range(0, 1)] float distanceThreshold = 0.2f;
        [SerializeField][Range(0, 5)] float timeThreshold = 0.5f;
        [SerializeField][ReadOnly] Vector3 velocity;
        [SerializeField][ReadOnly] Vector3 smoothedVelocity;
        [SerializeField][ReadOnly] float speed;

        public MotionState state { get; private set; }
        public bool isMoving => state == MotionState.Moving;
        public bool isStationary => state == MotionState.Stationary;

        Vector3 lastSamplePos;
        float stationaryTime;

        void OnEnable()
        {
            lastSamplePos = transform.position;
            stationaryTime = 0f;
            state = MotionState.Stationary;
        }

        public Vector3 GetVelocity() => smoothedVelocity;
        public Vector3 GetFrameVelocity() => velocity;
        void Update()
        {
            var current = transform.position;

            // raw velocity
            velocity = (current - lastSamplePos) / Mathf.Max(Time.deltaTime, 0.0001f);

            // smooth velocity
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, 0.2f);
            speed = smoothedVelocity.magnitude;

            var moved = smoothedVelocity.sqrMagnitude >= distanceThreshold * distanceThreshold;
            if (moved)
            {
                SetState(MotionState.Moving);
                stationaryTime = Time.time + timeThreshold;
            }
            else if (Time.time >= stationaryTime)
                SetState(MotionState.Stationary);

            lastSamplePos = current;
        }

        void SetState(MotionState newState)
        {
            if (state == newState) return;
            state = newState;
        }

        public enum MotionState { Stationary, Moving }
        public struct StateChange
        {
            public TransformMotion motion;
        }
    }
}