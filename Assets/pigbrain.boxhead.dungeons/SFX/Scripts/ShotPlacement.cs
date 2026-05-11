using UnityEngine;
using System.Collections;
using pigbrain.core.Collections;
using UnityEngine.AI;
using System;
using pigbrain.core.Inspector;
using System.Linq;
using pigbrain.core.UnityObject;
using pigbrain.core.AI;
using pigbrain.game.Boxhead.Environment;
using pigbrain.core.Geom;

namespace pigbrain.game.Boxhead
{
    [DefaultExecutionOrder(100)]
    public class ShotPlacement : Shot, ITargetable
    {
        [Header("Placement")]
        [SerializeField] Collider retraction;
        [SerializeField] PickupSwitch retractSwitch;
        [SerializeField] RetractType retractType = RetractType.None;
        [SerializeField][Range(1, 10)] float deployDistance = 4;

        bool ITargetable.isActive => state == State.Active;

        enum RetractType { None, LeaveRoom, NoThreat, }

        [Header("States")]
        [SerializeField][ToggleObject(group: "pm")] Fused fused;
        [SerializeField][ToggleObject(group: "pm")] Turret turret;
        [SerializeField][ToggleObject(group: "pm")] Barrier barrier;
        PlacementState[] states;

        [Header("Debug")]
        [SerializeField][ReadOnly] PickupSwitch retractSwitchInstance;
        [SerializeField][ReadOnly] State state;

        Health health;
        NavMeshAgent agent;
        TransformMotion ownerMotion;
        NavMeshAgent ownerAgent;
        Vector3 ownerLastPosition;
        NavAgentForce force;

        protected override void PoolDestroy()
        {
            base.PoolDestroy();
            retractStarted = false;
        }

        protected override void Awake()
        {
            base.Awake();
            states = new PlacementState[] { fused, turret, barrier };
            health = GetComponent<Health>();

            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;

            this.TryGetComponent(out force);
        }

        bool isDead => !owner || health.isDeadOrWillBe;

        #region Retraction
        void OnTriggerStay(Collider other) => ActivateSwitch(other);

        void ActivateSwitch(Collider pickuper)
        {
            if (!owner || !pickuper) return;
            if (!retractSwitch || retractSwitchInstance) return;
            if (state != State.Active) return;
            if (!owner.transform.IsChildOf(pickuper.transform)) return;
            if (health && health.isDeadOrWillBe) return;
            if (!pickuper) return;
            if (ownerMotion && !ownerMotion.isStationary) return;
            if (!retractSwitchInstance)
            {
                retractSwitchInstance = retractSwitch.CreateInstance(transform, agent.radius,
                    pickuper.transform, pickuper.GetComponent<NavMeshAgent>().radius);
                retractSwitchInstance.OnSwitch += () => state = State.Retract;
            }
        }
        #endregion

        #region Update
        protected override IEnumerator ProjectileUpdate()
        {
            ownerMotion = owner.GetComponentInParent<TransformMotion>();
            ownerAgent = owner.GetComponentInParent<NavMeshAgent>();
            Health ownerHealth = ownerAgent.GetComponent<Health>();

            var placementState = states.FirstOrDefault(s => s.enabled);
            placementState.Start(this);

            // DEPLOY
            state = State.Deploying;
            retraction.enabled = false;
            yield return Deploy();
            retraction.enabled = true;

            // ACTIVE
            state = State.Active;
            while (!ownerHealth.isDead && !isDead && state == State.Active && owner.activeInHierarchy)
            {
                yield return placementState.Update().RunImmediate();
                Pushed();
                RetractUpdate();
                yield return new WaitForNextUpdate();
            }

            if (ownerHealth.isDead) yield break;

            // RETRACT
            if (!isDead && state == State.Retract)
            {
                yield return Retract();
                // Put(); //owner.GetComponentInParent<Weapon>().ReturnShot(this);
            }
        }

        #region └Push
        void Pushed()
        {
            if (!ownerMotion || !ownerAgent) return;

            float totalRadius = agent.radius + ownerAgent.radius;
            float pushRadius = totalRadius + 0.2f;
            Vector3 ownerPosition = ownerAgent.transform.position;
            Vector3 delta = ownerPosition - transform.position;
            float distance = delta.magnitude;

            var vel = (ownerPosition - ownerLastPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            ownerLastPosition = ownerPosition;

            if (vel.sqrMagnitude < 0.0001f) return;
            if (distance > pushRadius) return;

            // only push if moving toward object
            Vector3 toObject = (transform.position - ownerPosition).normalized;
            float alignment = Vector3.Dot(vel.normalized, toObject);
            if (alignment < 0.5f) return;

            var dir = vel.normalized;
            // keep object slightly in front of player
            Vector3 targetPos = ownerAgent.transform.position + dir * pushRadius;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, vel.magnitude * Time.deltaTime);
        }
        #endregion

        #region └Deploy
        IEnumerator Deploy()
        {
            Vector3 startPosition = transform.position;
            if (NavMesh.SamplePosition(startPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                startPosition = hit.position;

            Vector3 dir = transform.forward;
            float duration = 0.5f;
            yield return new OverTime(duration, (t) =>
            {
                float f = 1f - Mathf.Pow(1f - t, 3);
                Vector3 p = Vector3.Lerp(startPosition, startPosition + dir * deployDistance, f);
                if (NavMesh.SamplePosition(p, out var hit, 0.5f, agent.areaMask))
                    agent.Warp(hit.position);
            });
        }
        #endregion

        #region └Retract
        bool retractStarted;
        IRoomTracker roomTracker;
        Room currentRoom;

        void RetractUpdate()
        {
            if (retractType == RetractType.LeaveRoom)
            {
                if (!retractStarted)
                {
                    retractStarted = true;
                    roomTracker = owner.GetComponentInParent<IRoomTracker>();
                    if (!roomTracker.isTracker) roomTracker = null;
                    currentRoom = roomTracker.currentRoom;
                }

                if (roomTracker != null && currentRoom != roomTracker.currentRoom)
                    state = State.Retract;
            }
        }

        static NavMeshPath Path;
        IEnumerator Retract()
        {
            const float Time1 = 0.4f / 2;
            Weapon weapon = owner.GetComponentInParent<Weapon>();
            Vector3 start = transform.position;

            float time = Mathf.Min(1, Time1 * (start - weapon.transform.position).magnitude);

            Vector3[] path = null;
            if (agent.CalculatePath(weapon.transform.position, Path ??= new()))
                path = Path.corners.Append(default).ToArray();
            else path = new Vector3[] { start, default };

            yield return new OverTime(time, (t) =>
            {
                path[^1] = weapon.transform.position;
                // Vector3 p = Vector3.Lerp(start, weapon.transform.position, t);
                Vector3 p = path.PositionAlong(t);
                if (NavMesh.SamplePosition(p, out var hit, 0.5f, agent.areaMask))
                    agent.Warp(hit.position);
                else transform.position = p;
            });
            Put(); //weapon.ReturnShot(this);
        }
        #endregion
        #endregion

        #region States
        class PlacementState
        {
            public bool enabled;
            [HideInInspector] protected ShotPlacement placement;
            public Transform transform => placement.transform;
            public NavMeshAgent agent => placement.agent;

            public virtual void Start(ShotPlacement placement) => this.placement = placement;
            public virtual IEnumerator Update() { yield break; }
        }

        [Serializable]
        class Barrier : PlacementState
        {
            [SerializeField] int barrierCount = 10;
            [SerializeField] GameObject node;
            [SerializeField] float spread = 10;

            public override IEnumerator Update()
            {
                // placement.health.ApplyDamage(null, (placement.health.maxDamage / fuse) * Time.deltaTime);
                yield break;
            }
        }

        [Serializable]
        class Fused : PlacementState
        {
            [SerializeField] float fuse = 5;
            public override IEnumerator Update()
            {
                placement.health.ApplyDamage(null, (placement.health.maxDamage / fuse) * Time.deltaTime);
                yield break;
            }
        }

        [Serializable]
        class Turret : PlacementState { }
        #endregion

        enum State { Deploying, Active, Retract, Dead, Other }
    }
}