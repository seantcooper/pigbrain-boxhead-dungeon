using System;
using pigbrain.core.AI;
using pigbrain.core.Geom;
using pigbrain.core.Graphics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(Health))]
    public class HealthBar : MonoBehaviour
    {
        static readonly int FillID = Shader.PropertyToID("_Fill");
        [SerializeField] Traits traits;
        [SerializeField] Material material;
        [SerializeField] Vector3 offset = new(0, 2f, 0);
        [SerializeField] Vector2 size = new(1.5f, 0.2f);

        [Flags]
        enum Traits
        {
            None = 0,
            OnDamage = 1 << 0,
            UseAgentHeight = 1 << 1,
            Another = 1 << 10,
        }

        Camera cam;
        MaterialPropertyBlock mpb;
        Mesh quad;
        Health health;
        NavMeshAgent agent;
        float time;

        void Awake()
        {
            health = GetComponent<Health>();
            quad = MeshPrimitive.GetQuadMesh();
            mpb = new MaterialPropertyBlock();
            if (traits.HasFlag(Traits.OnDamage))
            {
                time = 0.1f;
                health.onDamageApplied += OnDamage;
            }
            if (traits.HasFlag(Traits.UseAgentHeight))
                agent = GetComponent<NavMeshAgent>();
        }

        void OnDamage(Health.Damage damage)
        {
            time = Time.time + 2;
        }

        void LateUpdate()
        {
            if (time > 0 && Time.time > time) return;
            if (health.isDead) return;
            if (quad == null || material == null) return;

            if (!cam) cam = Camera.main;
            if (!cam) return;

            var currentFill = health.unit;
            mpb.SetFloat(FillID, currentFill);

            var scalar = transform.localScale.Min();
            Vector3 offset = this.offset;
            if (agent) offset = offset.AddY(agent.height);

            var matrix = Matrix4x4.TRS(
                transform.position + offset * scalar,
                Quaternion.LookRotation(cam.transform.forward),
                new Vector3(size.x * scalar, size.y * scalar, 1f));

            Graphics.RenderMesh(
                new RenderParams(material)
                {
                    matProps = mpb,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false
                },
                quad,
                0,
                matrix);
        }
    }
}