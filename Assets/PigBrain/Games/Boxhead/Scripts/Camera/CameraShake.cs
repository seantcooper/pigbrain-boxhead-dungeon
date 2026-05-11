using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [DefaultExecutionOrder(100)]
    public class CameraShake : MonoBehaviourSingleton<CameraShake>
    {
        [SerializeField][Range(0, 100)] float distance = 50;
        [SerializeField][Range(0, 25)] float shakeDecay = 8;
        [SerializeField][ReadOnly] float magnitude;
        public Vector3 offset { get; private set; }

        void Update() => offset = UpdateShake();

        Vector3 UpdateShake()
        {
            if (magnitude > 0.0001f)
            {
                magnitude = Mathf.Lerp(magnitude, 0f, shakeDecay * Time.deltaTime);
                return Random.insideUnitCircle * magnitude;
            }
            magnitude = 0f;
            return Vector3.zero;
        }

        void ApplyLocal(Vector3 position, float magnitude = 10)
        {
            float dist = Vector3.Distance(transform.position, position);
            float t = Mathf.Clamp01(1f - dist / distance);
            this.magnitude = magnitude * t;
        }

        public static void Apply(Vector3 position, float magnitude = 10)
        {
            if (!Instance) return;
            Instance.ApplyLocal(position, magnitude);
        }
    }
}
