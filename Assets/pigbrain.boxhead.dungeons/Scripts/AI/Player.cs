using UnityEngine;
using pigbrain.core.Inspector;
using UnityEngine.AI;
using pigbrain.game.Boxhead.FiniteStateMachine;
using pigbrain.game.Boxhead.Navigation;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Geom;
using pigbrain.game.Boxhead.Environment;
using pigbrain.core.UnityObject;
using pigbrain.core.Graphics;
using UnityEngine.Scripting;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.core.Events;

namespace pigbrain.game.Boxhead
{
    [RequireComponent(typeof(NavMeshAgentState))]
    [RequireComponent(typeof(Targeting))]
    public class Player : FSM, IPickup, IRoomTracker, IPadTrigger, ISetValue, ITargetable, ILevelIndex, IExplodeModel
    {
        [Header("Player")]
        [SerializeField] internal Player leader;

        [Header("States")]
        [ToggleObject] public PlayerDemo demo;
        [ToggleObject] public PlayerControl control;
        [ToggleObject] public PlayerRage rage;
        [ToggleObject] public PlayerFind find;
        [ToggleObject] public PlayerFollow follow;
        [ToggleObject] public PlayerLeave leave;
        [ToggleObject] public PlayerDead dead;

        [Header("Components")]
        [ReadOnly] public NavMeshAgentController nmaController;
        [ReadOnly] public Targeting targeting;
        [ReadOnly] public NavMeshAgentState nmaState;
        [ReadOnly] public Health health;
        public NavMeshAgent agent => nmaState.agent;

        GameObject IExplodeModel.GetPrefab() => dead.explodeModel;

        bool ITargetable.isActive => true;

        public bool aiControl => follow.enabled;
        bool IRoomTracker.isTracker => !aiControl;
        Room IRoomTracker.currentRoom => this && TryGetComponent(out CullingGroupItem item) && item.zone
            ? item.zone.GetComponent<Room>() : null;

        bool IPadTrigger.canTrigger => !aiControl;
        bool IPickup.OnPickup(Pickup pickup) => !aiControl;

        [SetValue]
        [Preserve]
        float totalDamage { get => health.damage; set => health.damage = value; }
        [Preserve]
        public override void SetTimeScale(float timeScale)
        {
            base.SetTimeScale(timeScale);
            if (nmaState.animator) nmaState.animator.speed = timeScale;
        }

        StatsController statsController => GetComponent<StatsController>();
        public bool SetIndex(int index) => statsController.SetLevelIndex(index);
        public int GetIndex() => statsController.GetLevelIndex();

        public void StartDemo() => Interrupt(demo);

        protected override void OnValidate()
        {
            base.OnValidate();
            nmaController = GetComponent<NavMeshAgentController>();
            nmaState = GetComponent<NavMeshAgentState>();
            targeting = GetComponent<Targeting>();
            health = GetComponent<Health>();
        }

        void Start()
        {
            // NavMap.TryGetLayer<NavMapLayerMove>().Add(agent);
            if (TryGetComponent(out Health health)) health.onDeath += Death;
            if (enabled) StartFSM(new PlayerState[] { control, find, follow, demo }.FirstOrDefault(s => s.enabled));
        }

        public void Death()
        {
            SetLeader(null);
            targeting.enabled = false;
            Interrupt(dead);
        }

        public float Rage(bool active)
        {
            if (active)
            {
                Interrupt(rage);
                return Time.time + rage.duration;
            }
            Interrupt(control);
            return 0;
        }

        void Update()
        {
            if (TimeScale.IsPaused) return;
            if (targeting.GetBestTarget(out Target target))
                nmaState.SetLookDirection(target.position - transform.position);
            else nmaState.ClearLookDirection();
        }

        #region Leader
        readonly Dictionary<Player, int> rank = new();

        public void SetLeader(Player leader)
        {
            if (this.leader)
            {
                this.leader.GetComponent<StatsController>()
                    .RemoveListener<IndexChangeEvent>(OnLeaderIndexChanged);
                this.leader.RemoveFollower(this);
            }
            this.leader = leader;
            if (this.leader)
            {
                this.leader.GetComponent<StatsController>()
                    .AddListener(OnLeaderIndexChanged);
                this.leader.AddFollower(this);
            }
        }

        void OnLeaderIndexChanged(IndexChangeEvent evt)
        {
            if (this) GetComponent<StatsController>().SetLevelIndex(evt.index);
        }

        void RemoveFollower(Player player) => rank.Remove(player);
        void AddFollower(Player player) => rank[player] = GetRankIndex();
        int GetRankIndex()
        {
            var set = new HashSet<int>(rank.Values);
            for (int i = 0; ; i++) if (!set.Contains(i)) return i;
        }

        readonly static float[] RankRotations = new float[] { 90, -90, 30, -30, 150, 210 };
        readonly static Vector3[] RankPositions = RankRotations
            .Select(r => Quaternion.Euler(0, r, 0) * Vector3.forward).ToArray();

        // called from AI
        public Vector3 GetRankPosition(float distance)
        {
            if (leader)
            {
                if (leader.rank.TryGetValue(this, out int rankIndex))
                {
                    rankIndex %= RankPositions.Length;
                    Rnd rnd = new(Rnd.GetIntervalSeed(this.GetUniqueID(), 5));
                    var off = RankPositions[rankIndex] * distance + rnd.NextVector3FlatDirection() * distance / 4;
                    Vector3 position = leader.transform.position + leader.transform.TransformDirection(off);
                    if (NavMesh.SamplePosition(position, out var hit, 5, agent.areaMask))
                        return hit.position;
                    return position;
                }
            }
            return transform.position;
        }
        #endregion
    }
}