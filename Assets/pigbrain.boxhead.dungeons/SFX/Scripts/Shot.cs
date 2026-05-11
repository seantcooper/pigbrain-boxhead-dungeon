using UnityEngine;
using System.Collections;
using pigbrain.core.UnityObject;
using pigbrain.core.Geom;
using pigbrain.core.AI;
using pigbrain.core.Audio;
using pigbrain.core.Inspector;
using System;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.core.Collections;

namespace pigbrain.game.Boxhead
{
    public class Shot : PoolBehaviour<Shot>
    {
        [Header("Shot")]
        [SerializeField][Range(0, 100)] protected float speed = 10f;
        [SerializeField][Range(0, 60)] protected float maxLife = 20f;

        [SerializeField] protected LayerMask damageMask;
        [SerializeField][Range(0, 100)] protected float damage = 0;
        [SerializeField][Range(0, 100)] protected float registerDamage = 0;

        [SerializeField] protected GameObject effect;
        [SerializeField] protected GameObject[] effectLevels;
        [SerializeField] protected MassForce massForce;
        [SerializeField] protected ClipLink fireSound;

        [Header("Debug")]
        [SerializeField][ReadOnly] protected Target target;
        [SerializeField][ReadOnly] protected GameObject owner;
        [SerializeField][ReadOnly] protected float duration;
        Vector3 lastTargetPosition;

        protected override void OnValidate()
        {
            base.OnValidate();
            massForce = GetComponent<MassForce>();
        }

        #region Create Instance
        public virtual Shot CreateInstance(GameObject owner, Target target, Vector3 position, Quaternion rotation)
        {
            var inst = Get((inst) =>
            {
                inst.transform.SetPositionAndRotation(position, rotation);
                inst.target = target;
                inst.gameObject.layer = owner.layer;
                inst.owner = owner;
            });
            inst.GetTargetPosition();
            inst.ProjectileStart();
            return inst;
        }
        #endregion

        #region Target
        protected Vector3 targetPosition => GetTargetPosition();
        protected Vector3 GetTargetPosition() =>
            target ? lastTargetPosition = target.center : lastTargetPosition;
        protected Vector3 targetDirection => GetTargetDirection();
        protected Vector3 GetTargetDirection() =>
            target ? (targetPosition - transform.position).normalized : transform.forward;
        // protected IAffector GetAffector() => owner ? owner.GetComponent<IAffector>() : null;
        #endregion

        #region Update
        protected void FireSound() => fireSound.Play(transform.position);
        protected virtual void ProjectileStart()
        {
            StartCoroutine(Run());
        }

        protected virtual IEnumerator ProjectileUpdate() { yield break; }

        protected IEnumerator Run()
        {
            if (maxLife > 0) Put(maxLife);
            FireSound();
            if (registerDamage > 0 && target)
            {
                duration = (transform.position - targetPosition).TimeToTarget(speed);
                Affector affector = new(transform, massForce ? massForce.mass : 0, speed * transform.forward);
                target.transform.TryRegisterDamage(affector, registerDamage, duration);
            }
            yield return ProjectileUpdate();
            if (!gameObject.activeInHierarchy) yield break;
            CreateEffect();
            Put();
        }
        #endregion

        #region Effect
        protected GameObject CreateEffect()
        {
            if (!effect && effectLevels.IsNullOrEmpty()) return null;

            GameObject effectprefab = effect;
            StatsController stats = null;

            if (owner)
            {
                stats = owner.GetComponentInParent<StatsController>();
                if (!effectLevels.IsNullOrEmpty())
                    effectprefab = effectLevels[Math.Min(effectLevels.Length - 1, stats.GetLevelIndex())];
            }

            var inst = effectprefab.Instantiate(transform.position, transform.rotation);
            inst.transform.localScale = transform.lossyScale;

            if (owner) stats.TryApplyIndexTo(inst);

            foreach (var owners in inst.GetComponentsInChildren<IOwner>())
                owners.owner = owner;

            inst.layer = gameObject.layer;
            return inst;
        }
        #endregion
    }
}
