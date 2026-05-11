using UnityEngine;
using pigbrain.core.Geom;
using System.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using Unity.Mathematics;
using System.Collections.Generic;
using pigbrain.core.Collections;

namespace pigbrain.game.Boxhead
{
    public class ShotShield : Shot
    {
        [Header("Shield")]
        // [SerializeField][Range(1, 100)] float strength = 100;
        [SerializeField][Range(1, 20)] float radius = 8;
        [SerializeField][Range(0.1f, 10)] float decay = 1;
        [SerializeField] Collider trigger;
        [SerializeField] Material material;
        // [SerializeField][ReadOnly] float totalDamage;

        Material materialInstance;
        Health health;
        float diameter => radius * 2;

        protected override void Awake()
        {
            base.Awake();
            materialInstance = material;
            health = GetComponent<Health>();
            health.onDamage += OnDamage;
        }

        Health ownerHealth;
        protected override void PoolStart()
        {
            base.PoolStart();
            ownerHealth = owner.GetComponentInParent<Health>();
            ownerHealth.onDamage += OnOwnerDamage;
        }

        protected override void PoolDestroy()
        {
            base.PoolDestroy();
            ownerHealth.onDamage -= OnOwnerDamage;
        }

        void OnOwnerDamage(Health.Damage damage)
        {
            health.ApplyDamage(damage.affector, damage.amount);
            damage.Cancel();
        }
        void OnDamage(Health.Damage damage) => SetSize();

        #region Boundary
        void OnTriggerStay(Collider other)
        {
            if (other.isTrigger) return;
            if (!other.TryGetComponent(out IShield shield)) return;

            Vector3 point = other.ClosestPoint(transform.position);
            Vector3 delta = point - transform.position;
            float dist = delta.magnitude;
            if (dist <= 0f || dist >= radius) return;

            Vector3 normal = delta / dist;
            float push = (radius - dist) * UnityEngine.Random.Range(1f, 4f);
            float d = shield.OnShield(normal * push);

            if (d > 0)
            {
                health.ApplyDamage(new Affector(transform), d);
                AddHitRotation(Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg);
            }
        }
        #endregion

        protected override void ProjectileStart()
        {
            base.ProjectileStart();
            transform.rotation = Quaternion.identity;
            SetSize();
        }

        #region Update
        protected override IEnumerator ProjectileUpdate()
        {
            yield return new WaitUntil(() => health.isDead || !owner.activeInHierarchy);
            Vector3 start = transform.localScale, end = (float3)0.01f;
            yield return new OverTime(0.25f, (t) => transform.localScale = Vector3.Lerp(start, end, t));
        }
        #endregion

        #region Damage VFX
        const int MAX_HITS = 8;
        const int Quadrants = 360 / 4;

        readonly Vector4[] hitPos = new Vector4[MAX_HITS];
        readonly float[] hitTime = new float[MAX_HITS];
        readonly HashSet<int> quadrants = new(Quadrants);

        void SetSize()
        {
            float scale = health.unit * 0.7f + 0.3f; // 0.5 - 1
            transform.localScale = (float3)(diameter * scale);
        }

        void AddHitRotation(float r)
        {
            float angle = (r % 360f + 360f) % 360f;
            quadrants.Add(Mathf.FloorToInt(angle / (360f / Quadrants)));
        }

        void LateUpdate()
        {
            if (!owner) return;
            if (health.isDead) return;

            transform.position = owner.transform.position;
            health.ApplyDamage(null, decay * Time.deltaTime);

            // update only when quadrant is hit (do not overwrite every frame)
            foreach (var q in quadrants)
            {
                int i = q % MAX_HITS; // map quadrant to slot

                float step = 360f / Quadrants, angle = (q + 0.5f) * step; // center of quadrant
                Vector3 dir = Quaternion.Euler(-20, angle, 0) * Vector3.forward;

                // var dir = Quaternion.Euler(-20, q * (360f / Quadrants), 0) * Vector3.forward;
                hitPos[i] = new Vector4(dir.x, dir.y, dir.z, 1);
                hitTime[i] = Time.time; // reset time only on hit
            }

            quadrants.Clear();
            materialInstance.SetFloat("_TimeNow", Time.time);
            materialInstance.SetVectorArray("_HitPos", hitPos);
            materialInstance.SetFloatArray("_HitTime", hitTime);
        }
        #endregion
    }

    public interface IShield
    {
        float OnShield(Vector3 force);
    }
}
