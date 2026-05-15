using System.Collections.Generic;
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

        readonly Dictionary<string, Shake> shakes = new();
        readonly List<string> remove = new(32);

        public Vector3 offset { get; private set; }

        void Update() => offset = UpdateShake();

        Vector3 UpdateShake()
        {
            magnitude = 0;

            foreach (var shake in shakes.Values) magnitude += shake.magnitude;

            if (magnitude > 0.0001f)
            {
                foreach (var kv in shakes)
                {
                    float value = Mathf.Lerp(kv.Value.magnitude, 0f, shakeDecay * Time.deltaTime);
                    if (value < 0.001f) remove.Add(kv.Key);
                    else shakes[kv.Key].magnitude = value;
                }

                if (remove.Count > 0)
                {
                    remove.ForEach(k => shakes.Remove(k));
                    remove.Clear();
                }

                return Random.insideUnitCircle * magnitude;
            }
            return Vector3.zero;
        }

        void ApplyLocal(Vector3 position, float magnitude, string group, Action action)
        {
            float dist = Vector3.Distance(transform.position, position);
            float t = Mathf.Clamp01(1f - dist / distance);
            float value = magnitude * t;

            switch (action)
            {
                case Action.Add:
                    if (shakes.TryGetValue(group, out var current))
                        current.magnitude += value;
                    else shakes[group] = new Shake(value);
                    break;

                case Action.Replace:
                    if (shakes.TryGetValue(group, out current))
                        current.magnitude = Mathf.Max(current.magnitude, value);
                    else shakes[group] = new Shake(value);
                    break;
            }
        }

        class Shake
        {
            public float magnitude;
            public Shake(float magnitude) => this.magnitude = magnitude;
        }

        public static void Apply(Vector3 position, float magnitude, string id, Action action = Action.Add)
        {
            if (!Instance) return;
            Instance.ApplyLocal(position, magnitude, id, action);
        }

        public enum Action { Add, Replace, }
    }
}
