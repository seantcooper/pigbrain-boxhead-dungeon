using pigbrain.core.AI;
using pigbrain.core.Analysis;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.FiniteStateMachine;
using pigbrain.game.Boxhead.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(NavMeshAgentState))]
    [InlineButton(nameof(TryAgain))]
    public class Enemy : FSM, IEnemyType, IShield, ISetValue, ITargetable, IExplodeModel
    {
        [Header("Enemy")]
        [SerializeField] Type type;
        public LayerMask enemyMask;
        public int prize = 999;
        public int exp = 0;

        [ReadOnly] public Target target;
        public Type GetEnemyType() => type;
        bool ITargetable.isActive => true;

        #region States
        [Header("States")]
        [ToggleObject] public EnemyMove move;
        [ToggleObject] public EnemyAttack attack;
        [ToggleObject] public EnemyDead dead;
        // EXTRA STATES
        [ToggleObject] public EnemyEnrage enrage;
        [ToggleObject] public EnemyExplode explode;
        [ToggleObject] public EnemyRainOfFire rainOfFire;
        [ToggleObject] public EnemyGrow grow;

        [Preserve]
        void SetAttackDamage(float damage) => attack.SetAttackDamage(damage);
        [Preserve]
        void SetGrow(float scale) => grow.SetScale(scale);
        [Preserve]
        void SetMinRange(float range) { rainOfFire.minRange = range; }
        [Preserve]
        void SetMaxRange(float range) { rainOfFire.maxRange = enrage.range = range; }
        [Preserve]
        public override void SetTimeScale(float timeScale)
        {
            base.SetTimeScale(timeScale);
            if (nmaState.animator) nmaState.animator.speed = timeScale;
        }
        #endregion

        GameObject IExplodeModel.GetPrefab() => dead.explodeModel;

        [HideInInspector] public NavMeshAgentState nmaState;
        [HideInInspector] public NavAgentForce force;
        [HideInInspector] public Health health;

        public NavMeshAgent agent => nmaState.agent;

        protected override void OnValidate()
        {
            base.OnValidate();
            nmaState = GetComponent<NavMeshAgentState>();
            force = GetComponent<NavAgentForce>();
        }

        void Start()
        {
            if (TryGetComponent(out health)) health.onDeath += Death;
            if (enabled) StartFSM(move);
        }

        void TryAgain()
        {
            this.LogMessage($"Try again");
            StartFSM(move);
        }

        public void Death()
        {
            markedAsDying = true;
            Interrupt(dead);
        }
        void OnDestroy() => NavMap.TryGetLayer<NavMapLayerFear>().Remove(transform);

        #region Set Value
        [SetValue] float applyDamage { get => 0; set => GetComponent<Health>().ApplyDamage(null, value); }

        #endregion

        #region Target
        public float targetDistance => target ? transform.DistanceTo(target.transform) - target.GetRadius() : 0;
        public Vector3 targetDirection => target ? transform.DirectionTo(target?.transform) : Vector3.forward;
        public bool isFacingTarget(float threshold = 0.95f) => Vector3.Dot(transform.forward, targetDirection) >= threshold;
        public Vector3 targetPosition => target ? target.transform.position : transform.position;

        NavMapLayerMove moveLayer;
        public Target FindTarget()
        {
            if (!moveLayer) moveLayer = NavMap.TryGetLayer<NavMapLayerMove>();
            var agentTarget = moveLayer.GetTarget(agent);

            target = agentTarget ? new Target(agentTarget) : null;

            if (target && gameObject.TryGetComponent(out HeadTracking track))
                track.SetTarget(target.transform);


            return target;
        }

        float IShield.OnShield(Vector3 force)
        {
            const float strength = 3f;
            Vector3 f = force * strength;
            float max = agent.speed * 4;
            if (f.magnitude > max) f = f.normalized * max;
            agent.Move(f * Time.deltaTime);
            return attack.GetAttackDamage() * Time.deltaTime;
        }

        #endregion

        public enum Type { Zombie, Runner, Devil, Terror, Ghost }
    }

    public interface IEnemyType
    {
        Enemy.Type GetEnemyType();
    }
}