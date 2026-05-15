using System.Collections.Generic;
using System.Linq;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.UIElements;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(Health))]
    public class HitEffectMesh : MonoBehaviour
    {
        [SerializeField] Traits traits = Traits.OverlayMaterials;
        [SerializeField] Material flashMaterial;
        [SerializeField] float duration = 0.06f;
        [SerializeField] float scale = 1.1f;
        [SerializeField] Transform container;
        (Renderer r, MaterialStack stack)[] renderers;
        float timer;
        Vector3 originalScale;

        static readonly Dictionary<int, MaterialStack> Stacks = new();

        class MaterialStack
        {
            public Material[] original, flash;
            public static int Key(Material[] original) => original[0].GetHashCode();
        }

        void Start()
        {
            GetComponent<Health>().onDamageApplied += OnDamage;
            timer = -1;
        }

        Transform GetContainer() => container ? container : transform;

        void InitialiseRenderers()
        {
            if (renderers != null) return;
            renderers = GetContainer().GetComponentsInChildren<Renderer>().Select(r =>
            {
                var mats = r.sharedMaterials;
                if (mats.Length > 1) throw new System.Exception("Only supports single materials!");
                int key = MaterialStack.Key(mats);

                if (!Stacks.TryGetValue(key, out MaterialStack stack))
                {
                    Stacks[key] = stack = new MaterialStack
                    {
                        original = r.sharedMaterials,
                        flash = r.sharedMaterials.Append(flashMaterial).ToArray()
                    };
                }
                return (r, stack);
            }).ToArray();
            originalScale = GetContainer().localScale;
        }

        void OnDamage(Health.Damage damage)
        {
            InitialiseRenderers();
            timer = duration;
            foreach (var (r, stack) in renderers)
                r.sharedMaterials = stack.flash;
            GetContainer().localScale = originalScale * scale;
        }

        void LateUpdate()
        {
            if (timer <= 0f) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            foreach (var (r, stack) in renderers)
                r.sharedMaterials = stack.original;
            GetContainer().localScale = originalScale;
        }

        enum Traits
        {
            None = 0,
            OverlayMaterials = 1 << 0,
            Other = 1 << 16,
        }
    }
}