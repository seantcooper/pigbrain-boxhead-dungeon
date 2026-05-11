using pigbrain.core.Graphics;
using UnityEngine;
using UnityEngine.Rendering;

namespace pigbrain.game.Boxhead
{
    public class DamageVolume : MonoBehaviour
    {
        [SerializeField] Volume volume;
        [SerializeField] Health health;
        [SerializeField][Range(0.01f, 1)] float duration = 0.25f;
        float t;

        void OnValidate()
        {
            if (!volume) volume = GetComponent<Volume>();
            if (!health) health = GetComponentInParent<Health>();
            if (!health) health = GetComponentInChildren<Health>();
        }

        void Start()
        {
            health.onDamageApplied += OnDamage;
            volume.weight = 0;
        }

        void OnDamage(Health.Damage damage)
        {
            var unit = damage.amount / health.maxDamage;
            t = Mathf.Clamp01(t + unit);
        }

        void LateUpdate()
        {
            if (t <= 0f) return;

            t -= Time.deltaTime / duration;
            volume.weight = Mathf.Clamp01(t);
        }
    }
}