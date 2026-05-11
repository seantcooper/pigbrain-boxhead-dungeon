using System;
using System.Collections;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.Audio;
using UnityEngine;
using static pigbrain.core.UnityPhysics.PhysicsUtility;
using pigbrain.core.AI;
using pigbrain.core.Analysis;
using pigbrain.core.UnityObject;

namespace pigbrain.game.Boxhead
{
    public class Health : MonoBehaviour, ISetValue, IPooling
    {
        [SerializeField] float maxHitPoints = 10;
        [SerializeField] DamageMaterial material = DamageMaterial.Normal;
        [SerializeField] ClipLink damageSound, deathSound;
        [SerializeField] public NavAgentForce force;

        public enum DamageMaterial { Normal, Shield }

        public DamageMaterial GetDamageMaterial() => material;

#if UNITY_EDITOR
        [SerializeField, ReadOnly]
#endif
        float totalDamage = 0, registeredAffectsDamage;
        Damage lastDamage;

        public bool TryGetLastDamage(out Damage damage) => damage = lastDamage;

        public event Action onDeath;
        public event Action<Damage> onDamage;
        public event Action<Damage> onDamageApplied;

        #region Interface
        public float unit => 1 - Mathf.Clamp01(totalDamage / maxHitPoints);
        public bool isDead => hasDied; //totalDamage >= maxHitPoints;
        public bool isDamageDead => totalDamage >= maxHitPoints;
        public bool isDeadOrWillBe => totalDamage + registeredAffectsDamage >= maxHitPoints;
        public bool hasDied { get; private set; }

        [SetValue]
        internal float damage { get => totalDamage; set => totalDamage = value; }
        internal float maxDamage { get => maxHitPoints; set => maxHitPoints = value; }

        void OnValidate() => AddForce();
        void Awake() => AddForce();
        void AddForce() { if (!force) force = GetComponent<NavAgentForce>(); }

        void IPooling.Reset()
        {
            totalDamage = 0;
            registeredAffectsDamage = 0;
            hasDied = false;
            StopAllCoroutines();
        }

        public void ForceDeath() => Death();
        void Death()
        {
            hasDied = true;
            totalDamage = maxHitPoints;
            deathSound.Play(transform.position);
            StopAllCoroutines();
            onDeath?.Invoke();
        }
        #endregion

        #region Apply Damage
        public bool ApplyDamage(Affector affector, float damage)
        {
            if (hasDied || !enabled) return false;
            Damage ev = lastDamage = new(affector, damage);
            onDamage?.Invoke(ev);
            if (ev.cancelled) return false;

            totalDamage += damage;
            if (damage > 0)
            {
                damageSound.Play(transform.position);
                ApplyForce(affector, damage);
            }
            onDamageApplied?.Invoke(ev);
            if (totalDamage >= maxHitPoints) Death();
            return true;
        }

        public class Damage
        {
            public readonly Affector affector;
            public readonly float amount;
            public readonly float time;
            internal bool cancelled;

            public Damage(Affector affector, float amount)
            {
                this.affector = affector;
                this.amount = amount;
                this.time = Time.time;
                this.cancelled = false;
            }

            public void Cancel() => cancelled = true;
            public static implicit operator bool(Damage empty) => empty != null;
        }

        void ApplyForce(Affector affector, float damage)
        {
            if (!affector) return;
            if (!force) return;
            if (affector.mass < 0.01f) return;
            if (affector.velocity.sqrMagnitude < 0.0001f) return;
            force.ApplyForce(affector.mass, affector.velocity);
        }

        public static bool ApplyDamage(Affector affector, Transform affected, float damage) =>
            affected.TryGetComponent(out Health health) && health.ApplyDamage(affector, damage);

        static Health AffectCollider(Affector affector, Collider collider, float damage) =>
            collider.TryGetComponent(out Health h) && h.ApplyDamage(affector, damage) ? h : null;

        readonly static Health[] healths = new Health[1000];
        public static ArraySegment<Health> ApplyDamageSphere(Affector affector, Vector3 position,
            float radius, float damage, LayerMask mask)
        {
            int n = 0;
            foreach (var collider in OverlapSphere(position, radius, mask))
                if (AffectCollider(affector, collider, damage) is Health h)
                    healths[n++] = h;
            return new ArraySegment<Health>(healths, 0, n);
        }

        public static ArraySegment<Health> ApplyDamageRay(Affector affector, Vector3 p1, Vector3 p2,
            float damage, LayerMask mask)
        {
            int n = 0;
            foreach (var hit in LineCast(p1, p2, mask))
                if (AffectCollider(affector, hit.collider, damage) is Health h)
                    healths[n++] = h;
            return new ArraySegment<Health>(healths, 0, n);
        }
        #endregion

        #region Register Damage
        // register damage before it happens
        internal IEnumerator RegisterDamage(Affector affector, float damage, float duration)
        {
            registeredAffectsDamage += damage;
            yield return new WaitForSeconds(duration);
            registeredAffectsDamage -= damage;
            ApplyDamage(affector, damage);
        }
        #endregion
    }

    public static class HealthX
    {
        public static bool IsDead(this Transform target)
        {
            if (target.TryGetComponent(out Health health))
                return health.isDead;
            return false;
        }
        public static bool isDeadOrWillBe(this Transform target)
        {
            if (target.TryGetComponent(out Health health))
                return health.isDeadOrWillBe;
            return false;
        }


        public static bool TryRegisterDamage(this Transform affected, Affector affector, float damage, float duration)
        {
            if (affected.TryGetComponent(out Health health))
            {
                health.StartCoroutine(health.RegisterDamage(affector, damage, duration));
                return true;
            }
            return false;
        }

        public static bool TryApplyDamage(this Target affected, Affector affector, float damage) =>
            affected.transform.TryApplyDamage(affector, damage);
        public static bool TryApplyDamage(this Transform affected, Affector affector, float damage) =>
            affected.TryApplyDamage(affector, damage, out Health health);

        public static bool TryApplyDamage(this Transform affected, Affector affector, float damage, out Health health)
        {
            if (affected.TryGetComponent(out health))
            {
                health.ApplyDamage(affector, damage);
                return true;
            }
            return false;
        }
    }

    // public interface IOnDeath { void OnDeath(); }
    // public interface IOnDamage { void OnDamage(); }
    public interface IDamage { float damage { get; set; } }

    #region Affector
    // public interface IAffector { }

    public class Affector
    {
        public Transform transform;
        public Vector3 velocity;
        public float mass = 1;
        public Affector(Transform transform, float mass = 1, Vector3 velocity = default)
        {
            this.transform = transform;
            this.velocity = velocity;
            this.mass = mass;
        }
        public static implicit operator bool(Affector empty) => empty != null;
    }
    #endregion
}