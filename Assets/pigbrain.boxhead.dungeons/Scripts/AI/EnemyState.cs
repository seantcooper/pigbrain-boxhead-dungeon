using UnityEngine;
using UnityEngine.AI;
using pigbrain.game.Boxhead.FiniteStateMachine;
using pigbrain.core.Collections;
using System.Collections;
using System;
using pigbrain.game.Boxhead.Navigation;
using pigbrain.core.Geom;
using pigbrain.core.UnityObject;
using pigbrain.core.Audio;
using pigbrain.generated;

namespace pigbrain.game.Boxhead
{
    #region State
    public abstract class EnemyState : FSM.State<Enemy>
    {
        public NavMeshAgentState nmaState => fsm.nmaState;
        // public Animator animator => nmaState.animator;
        public NavMeshAgent agent => nmaState.agent;
        Collider collider;

        public void SetTrigger(string name) => nmaState.animator.SetTrigger(name);

        const int DefaultSightMask = (int)(GameLayerFlags.Wall | GameLayerFlags.Furniture | GameLayerFlags.Terrain);
        public bool HasLOS()
        {
            if (!fsm.target) return false;
            if (!collider) collider = fsm.GetComponent<Collider>();
            return !Physics.Linecast(collider.bounds.center, fsm.target.center, DefaultSightMask);
        }

        public bool HasLOS(float radius)
        {
            if (!fsm.target) return false;
            if (!collider) collider = fsm.GetComponent<Collider>();

            Vector3 delta = collider.bounds.center - fsm.target.center;
            bool result = Physics.SphereCast(fsm.target.center, radius, delta, out RaycastHit hit, delta.magnitude,
                DefaultSightMask | (1 << gameObject.layer), QueryTriggerInteraction.Ignore)
                && hit.collider == collider;

            Debug.DrawLine(fsm.target.center, collider.bounds.center, result ? Color.green : Color.red, 0.1f);
            return result;
        }

        protected void InterruptState(Health.Damage damage)
        {
            if (fsm.health.isDamageDead) return;
            fsm.Interrupt(fsm.move);
        }
    }
    #endregion

    #region Move
    [Serializable]
    public sealed class EnemyMove : EnemyState
    {
        // public override void Start()
        // {
        //     base.Start();
        // }

        public override void Enter()
        {
            SetTrigger("Move");
            nmaState.StartMovement();
        }

        public override void Exit()
        {
            nmaState.StopMovement();
        }

        public override IEnumerator Run()
        {
            DamageDead();
            if (!fsm.FindTarget()) yield break;
            nmaState.SetDestination(fsm.targetPosition);
            if (!TrySetState(fsm.grow))
                if (!TrySetState(fsm.rainOfFire))
                    if (!TrySetState(fsm.attack))
                        if (!TrySetState(fsm.enrage))
                            yield break;
        }

        int damageDeadCount;
        bool DamageDead()
        {
            if (damageDeadCount > 0)
                Debug.LogError($"Damage Dead count: {damageDeadCount} {fsm.name} marked: {fsm.markedAsDying} hasDied: {fsm.health.hasDied} damage: {fsm.health.damage} maxDamage: {fsm.health.maxDamage}");
            if (fsm.health.isDamageDead)
            {
                damageDeadCount++;
                Debug.LogError($"IS DAMAGE DEAD! Died {fsm.health.hasDied}");
                return true;
            }
            return false;
        }
    }
    #endregion

    #region Grow
    [Serializable]
    public sealed class EnemyGrow : EnemyState
    {
        [SerializeField][Range(0.1f, 60)] float activationTime = 5;
        [SerializeField][Range(0.1f, 10)] float scale = 2;
        bool grown;

        internal void SetScale(float scale)
        { if (enabled = scale > 1) this.scale = scale; }

        public override bool CanSet() => enabled && !grown && fsm.time >= activationTime;
        public override void Enter() => grown = true;
        public override IEnumerator Run()
        {
            Vector3 start = transform.localScale, end = Vector3.one * scale;
            yield return new OverTime(1.5f, (t) =>
            {
                // Overshoot growth
                const float s = 1.70158f;
                float p = t == 1 ? 1 : 1 + (--t) * t * ((s + 1) * t + s);

                // Create staged/cartoon growth (bigger → bigger → bigger)
                const float stages = 4f;
                float stepped = Mathf.Floor(p * stages) / stages;
                p = Mathf.Lerp(stepped, p, 0.6f);

                transform.localScale = Vector3.LerpUnclamped(start, end, p);
            });
            TrySetState(fsm.move);
        }
    }
    #endregion

    #region Enrage
    [Serializable]
    public sealed class EnemyEnrage : EnemyState
    {
        public float frequency = 5;
        public float range = 6;
        public float speedMultiplier = 3;

        float originalSpeed;
        float frequencyTime;

        public override void Enter()
        {
            originalSpeed = agent.speed;
            nmaState.StartMovement();
            SetTrigger($"Enrage");
            fsm.health.onDamage += InterruptState;
        }
        public override void Exit()
        {
            agent.speed = originalSpeed;
            nmaState.StopMovement();
            frequencyTime = Time.time;
            fsm.health.onDamage -= InterruptState;
        }

        public override bool CanSet()
        {
            bool canset = enabled && Time.time > frequencyTime + frequency &&
                fsm.targetDistance < range && HasLOS();
            return canset;
        }

        public override IEnumerator Run()
        {
            agent.speed = originalSpeed * speedMultiplier;
            nmaState.SetDestination(fsm.targetPosition);
            if (!TrySetState(fsm.explode))
                if (!TrySetState(fsm.attack))
                    if (!fsm.target || fsm.targetDistance >= range)
                        TrySetState(fsm.move);
            yield break;
        }
    }
    #endregion

    #region Explode
    [Serializable]
    public sealed class EnemyExplode : EnemyState
    {
        const float DamageDuration = 0.5f, AnimationLength = 1f;

        [SerializeField][Range(0.1f, 10)] float triggerRange = 2f;
        [SerializeField][Range(0.1f, 10)] float explodeDelay = 1f;
        [SerializeField] GameObject explosionPrefab;

        public override bool CanSet() => enabled && inRange;
        public override void Enter() => nmaState.SetLookDirection(fsm.targetDirection);
        public override void Exit() => nmaState.ClearLookDirection();

        public bool inRange => fsm.targetDistance < triggerRange;
        public Vector3 attackPosition => transform.position.AddY(agent.height / 2) + transform.forward;
        public float attackRadius => 0.45f;

        public override IEnumerator Run()
        {
            yield return new WaitForSeconds(explodeDelay);
            fsm.Death();
        }

        public void Explode()
        {
            ExplodeModel.CreateInstance(fsm.name, transform, transform.position, 20, 2);
            if (explosionPrefab) explosionPrefab.Instantiate(transform.position);
        }
    }
    #endregion

    #region Attack
    [Serializable]
    public sealed class EnemyAttack : EnemyState
    {
        const float DamageDuration = 0.5f, AnimationLength = 1f;

        [SerializeField][Range(1, 50)] float attackDamage = 1;
        [SerializeField][Range(0.1f, 10)] float attackRange = 1f;
        [SerializeField] ClipLink attackSound;

        public float SetAttackDamage(float damage) => attackDamage = damage;
        public float GetAttackDamage() => attackDamage;
        public override bool CanSet() => enabled && fsm.target && inRange;
        public override void Enter()
        {
            nmaState.SetLookDirection(fsm.targetDirection);
            fsm.health.onDamage += InterruptState;
        }
        public override void Exit()
        {
            nmaState.ClearLookDirection();
            fsm.health.onDamage -= InterruptState;
        }
        public bool inRange => fsm.targetDistance < attackRange;

        // TODO - fix distance for DEVIL - i think it is missing radius
        public Vector3 attackPosition => transform.position.AddY(agent.height / 2) + transform.forward;
        public float attackRadius => 0.45f;

        public override IEnumerator Run()
        {
            attackSound.Play(transform.position);
            SetTrigger("Attack");
            yield return fsm.WaitForSeconds(DamageDuration);
            if (fsm.target)
            {
                if (CanHitTarget(attackRange, attackRadius))
                {
                    var a = new Affector(transform, fsm.force.mass, transform.forward * 5);
                    fsm.target.TryApplyDamage(a, attackDamage);
                }
                yield return fsm.WaitForSeconds(AnimationLength - DamageDuration);
            }
            TrySetState(fsm.move);
        }

        bool CanHitTarget(float range, float radius)
        {
            if (!fsm.target) return false;

            var forward = transform.forward.WithY(0).normalized;
            var toTarget = (fsm.target.center - attackPosition).WithY(0);
            float dist = toTarget.magnitude;

            if (dist > range + radius) return false;
            if (dist <= 0.001f) return true;

            var dir = toTarget / dist;
            float dot = Vector3.Dot(forward, dir);
            if (dot < 0.4f) return false;

            float lateral = Mathf.Sqrt(1 - dot * dot) * dist;
            return lateral <= radius;
        }
    }
    #endregion

    #region RainOfFire
    [Serializable]
    public sealed class EnemyRainOfFire : EnemyState
    {
        const float EmitDuration = 0.5f, AnimationLength = 1f;
        public float minRange = 5;
        public float maxRange = 10;
        [SerializeField] Shot shot;
        [SerializeField] ClipLink attackSound;

        public override bool CanSet() => enabled && inRange && HasLOS(0.25f);
        public override void Enter()
        {
            nmaState.SetLookDirection(fsm.targetDirection);
            fsm.health.onDamage += InterruptState;
        }
        public override void Exit()
        {
            nmaState.ClearLookDirection();
            fsm.health.onDamage -= InterruptState;
        }

        public bool inRange => fsm.targetDistance > minRange && fsm.targetDistance < maxRange;
        public Vector3 emitPosition => sightPosition + transform.forward;
        public Vector3 sightPosition => transform.position.AddY(agent.height / 2);

        public override IEnumerator Run()
        {
            while (fsm.target && Vector3.Dot(transform.forward, fsm.targetDirection) < 0.5f)
            {
                nmaState.SetLookDirection(fsm.targetDirection);
                yield return new WaitForNextUpdate();
            }
            SetTrigger("RainOfFire");
            yield return fsm.WaitForSeconds(EmitDuration);
            if (fsm.target && shot)
            {
                var inst = shot.CreateInstance(gameObject, fsm.target, emitPosition,
                    Quaternion.LookRotation(fsm.targetDirection));
                inst.transform.localScale = transform.lossyScale;
                attackSound.Play(transform.position);
                yield return fsm.WaitForSeconds(Mathf.Min(AnimationLength, AnimationLength - EmitDuration));
            }
            TrySetState(fsm.move);
        }
    }
    #endregion

    #region Dead
    [Serializable]
    public sealed class EnemyDead : EnemyState
    {
        static int DeathIndex = 0;
        [SerializeField][Range(0.1f, 10)] float animationLength = 1f;
        [SerializeField] CommandCreate prize;
        [SerializeField] CommandCreate exp;
        [SerializeField] CommandContainer onDeath;
        [SerializeField] internal GameObject explodeModel;

        public override bool locked => true;
        public override void Enter()
        {
            gameObject.GetComponentsInChildren<Collider>().ForEach(c => c.enabled = false);
            agent.isStopped = true;
            agent.enabled = false;
            SetTrigger($"Dead{(++DeathIndex % 3) + 1}");
            CreatePrize();
            CreateExp();
        }

        public override IEnumerator Run()
        {
            onDeath?.Invoke(transform);

            if (fsm.explode.enabled) fsm.explode.Explode();
            else yield return new WaitForSeconds(animationLength);

            UnityEngine.Object.Destroy(gameObject);
            yield break;
        }

        public void CreatePrize()
        {
            if (!prize && fsm.prize > 0) return;
            prize.SetCount(fsm.prize);
            prize.Invoke(transform);
        }

        public void CreateExp()
        {
            if (!exp && fsm.exp > 0) return;
            exp.SetCount(fsm.exp);
            exp.Invoke(transform);
        }
    }
    #endregion
}