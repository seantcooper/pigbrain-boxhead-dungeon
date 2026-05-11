using System.Linq;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(Health))]
    public class HitEffectMesh : MonoBehaviour
    {
        [SerializeField] Material flashMaterial;
        [SerializeField] float duration = 0.06f;
        [SerializeField] Transform container;
        Renderer[] renderers;
        Material[][] original;
        float timer;

        void Start()
        {
            GetComponent<Health>().onDamageApplied += OnDamage;
            timer = -1;
        }

        void OnDamage(Health.Damage damage)
        {
            renderers ??= (container ? container : transform).GetComponentsInChildren<Renderer>();
            original ??= renderers.Select(r => r.sharedMaterials).ToArray();
            timer = duration;
            foreach (var r in renderers)
            {
                if (!r) continue;
                var mats = r.sharedMaterials;
                System.Array.Resize(ref mats, mats.Length + 1);
                mats[^1] = flashMaterial;
                r.sharedMaterials = mats;
            }
        }

        void LateUpdate()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i]) continue;
                renderers[i].sharedMaterials = original[i];
            }
        }
    }
}