using System;
using UnityEngine;
using pigbrain.game.Boxhead.Navigation;
using pigbrain.core.Geom;
using System.Collections.Generic;
using UnityEngine.Pool;
using pigbrain.core.Statistics;
using pigbrain.core.Collections;
using System.Collections;
using static pigbrain.game.Boxhead.Target;
using pigbrain.core.Analysis;

namespace pigbrain.game.Boxhead
{
    [DefaultExecutionOrder(-100)]
    public class Targeting : MonoBehaviour
    {
        [SerializeField] internal LayerMask sightMask;
        [SerializeField] internal LayerMask targetMask;

        [SerializeField] internal float baseOffset;
        [SerializeField][Range(0, 20)] internal float maxDistance = 10;
        [SerializeField] WeaponCache weaponsCache;

        [Header("Debug")]
        [SerializeField] internal int trackingCount;

        internal Vector3 position => transform.position.AddY(baseOffset);
        internal CacheValueByTime<float> actualMaxDistance;

        void OnEnable() => StartCoroutine(Run());
        void OnDisable() => StopAllCoroutines();

        IEnumerator Run()
        {
            actualMaxDistance = new(maxDistance, 0.25f);
            while (enabled)
            {
                if (!actualMaxDistance.isValid)
                {
                    if (!(weaponsCache && weaponsCache.GetMaxRange(out float r))) r = maxDistance;
                    actualMaxDistance.Validate(r);
                    maxDistancesq = actualMaxDistance * actualMaxDistance;
                    UpdateTargets();
                }
                yield return new WaitForNextUpdate();
            }
        }

        #region Update Target
        readonly static Collider[] TargetColliders = new Collider[250];
        readonly Dictionary<Collider, Target> targetCache = new(250);
        internal float maxDistancesq;

        void UpdateTargets()
        {
            int count = Physics.OverlapSphereNonAlloc(position, actualMaxDistance,
                TargetColliders, (int)targetMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider collider = TargetColliders[i];
                //if ((targetMask.value & (1 << collider.gameObject.layer)) == 0) continue;
                if (!collider.TryGetComponent<ITargetable>(out var t)) continue;
                if (!targetCache.TryGetValue(collider, out var target))
                    targetCache[collider] = target = new Target(this, collider);
                target.isActiveInTargeting = true;
            }

            // Remove the dead or not touch?
            var toRemove = ListPool<Collider>.Get();
            foreach (var (c, t) in targetCache)
                if (!c || t.isActiveInTargeting || c.transform.isDeadOrWillBe())
                    toRemove.Add(c);

            foreach (var c in toRemove) targetCache.Remove(c);
            trackingCount = targetCache.Count;

            ListPool<Collider>.Release(toRemove);
        }
        #endregion


        #region Interface
        public bool GetBestTarget(out Target result, Score scoreFlags = ScoreDefault) =>
            GetBestTarget(0, actualMaxDistance, out result, scoreFlags);
        public bool GetBestTarget(float min, float max, out Target result, Score scoreFlags = ScoreDefault)
        {
            if (!enabled) { result = null; return false; }
            float minsq = min * min, maxsq = max * max;
            (Target target, float score) best = (default, float.MinValue);
            foreach (Target target in targetCache.Values)
            {
                float distsq = target.GetDistanceSQ(); if (distsq < minsq || distsq > maxsq) continue;
                float score = target.GetScore(scoreFlags); if (score <= best.score) continue;
                if (!target.GetLOS()) continue;
                best.score = score;
                best.target = target;
            }
            result = best.target;
            return result != null;
        }

        public bool GetAimDirection(out Vector3 direction) =>
            GetAimDirection(0, actualMaxDistance, out direction);
        public bool GetAimDirection(float min, float max, out Vector3 direction)
        {
            direction = default;
            if (GetBestTarget(min, max, out Target target))
            {
                direction = target.GetDirection();
                return true;
            }
            return false;
        }
        #endregion
    }

    public interface ITargetable
    {
        bool isActive { get; }
    }

}

#region Target
namespace pigbrain.game.Boxhead
{
    using System;
    using pigbrain.core.AI;
    using pigbrain.core.Geom;
    using pigbrain.core.Statistics;
    using UnityEngine;
    using UnityEngine.AI;

    [Serializable]
    public class Target
    {
        [SerializeField] Targeting targeting;
        [SerializeField] Component registerObject;
        readonly Func<Bounds> getWorldBounds;
        Vector3 lastPosition, lastCenter;
        public string name => registerObject ? registerObject.name : "null";
        public Transform transform => registerObject ? registerObject.transform : null;
        public Vector3 center => registerObject ? lastCenter = getWorldBounds().center : lastCenter;
        public Vector3 position => registerObject ? lastPosition = registerObject.transform.position : lastPosition;

        Collider ccollider;
        public Collider collider => ccollider ? ccollider :
            ccollider = registerObject is Collider c ? c : registerObject.GetComponent<Collider>();

        // Target Stats
        #region └Validation
        int frameIndex;
        public bool isActiveInTargeting { get => frameIndex != Time.frameCount; set => frameIndex = value ? Time.frameCount : 0; }
        #endregion

        #region └Cache

        [SerializeField] NavMapLayerFearItem cfearitem;
        NavMapLayerFearItem GetFearItem() => cfearitem ? cfearitem
            : registerObject ? cfearitem = registerObject.GetComponent<NavMapLayerFearItem>() : null;

        CacheValueByFrame<Vector3> cdelta;
        public Vector3 GetDelta()
        {
            if (!cdelta.isValid) cdelta.Validate(position - targeting.position);
            return cdelta.value;
        }
        CacheValueByFrame<float> cdistancesq;
        public float GetDistanceSQ()
        {
            if (!cdistancesq.isValid) cdistancesq.Validate(GetDelta().sqrMagnitude);
            return cdistancesq.value;
        }
        CacheValueByFrame<Vector3> cdirection;
        public Vector3 GetDirection()
        {
            if (!cdirection.isValid) cdirection.Validate(GetDelta().normalized);
            return cdirection.value;
        }
        public Quaternion GetRotation() => GetDirection().GetRotation();

        CacheValueByFrame<float> cthreatonmap;
        public float GetThreatOnMap()
        {
            if (!registerObject || !GetFearItem()) return 0;
            if (!cthreatonmap.isValid) cthreatonmap.Validate(GetFearItem().layer.GetThreat(transform.position));
            return cthreatonmap.value;
        }

        public float GetThreat() => registerObject && GetFearItem() ? GetFearItem().GetThreat() : 0;

        #region Score
        [Flags]
        public enum Score
        { None = 0, Furthest = 1 << 1, Threat = 1 << 2, Fear = 1 << 3, Angle = 1 << 4, }
        public const Score ScoreDefault = Score.Fear | Score.Threat | Score.Angle;

        public struct Scoring
        {
            public const float WAngle = 0.15f, WFear = 0.35f, WThreat = 0.5f, WProx = 0.6f;
            public float angle;
            public float threat;
            public float fear;
            public float proximity;

            internal float Get(Score score)
            {
                float value = 0, weight = 0;
                float prox = score.HasFlag(Score.Furthest) ? 1 - proximity : proximity;

                value += prox * prox * prox * WProx;
                weight += WProx;

                if (score.HasFlag(Score.Threat)) { value += threat * WThreat; weight += WThreat; }
                if (score.HasFlag(Score.Fear)) { value += fear * WFear; weight += WFear; }
                if (score.HasFlag(Score.Angle)) { value += angle * WAngle; weight += WAngle; }
                return weight > 0 ? value / weight : 0;
            }
        }

        CacheValueByFrame<Scoring> cscore;
        public float GetScore(Score score = ScoreDefault)
        {
            if (!cscore.isValid)
            {
                float t;
                cscore.Validate(new()
                {
                    // everything behind is 0
                    angle = Mathf.Max(0f, Vector3.Dot(targeting.transform.forward, GetDirection())),
                    // get object threat
                    threat = (t = Mathf.Clamp01(GetThreat())) * t,
                    // get the threat on the map
                    fear = (t = Mathf.Clamp01(GetThreatOnMap())) * t,
                    // near to far, 1 to 0
                    proximity = 1 - Mathf.Clamp01(GetDistanceSQ() / targeting.maxDistancesq),
                });
            }
            return cscore.value.Get(score);
        }
        #endregion

        CacheValueByFrame<bool> clos = new(false, 4);
        public bool GetLOS()
        {
            if (!clos.isValid) clos.Validate(Physics.Linecast(targeting.position, center, out RaycastHit hit,
                 targeting.sightMask, QueryTriggerInteraction.Ignore) && hit.collider == registerObject as Collider);
            return clos.value;
        }
        #endregion

        #region └Constructor
        public Target(Targeting targeting, Component component)
        {
            this.targeting = targeting;
            this.registerObject = component;
            this.lastPosition = registerObject.transform.position;
        }

        Bounds GetAgentBounds() => ((NavMeshAgent)registerObject).GetBounds();
        Bounds GetColliderBounds() => ((Collider)registerObject).bounds;
        public float GetRadius() => registerObject is NavMeshAgent agent ? agent.radius : 0;

        public Target(NavMeshAgent agent) : this(null, agent)
        {
            this.getWorldBounds = GetAgentBounds;
            this.lastCenter = getWorldBounds().center;
        }
        public Target(Targeting targeting, Collider collider) : this(targeting, (Component)collider)
        {
            this.getWorldBounds = GetColliderBounds;
            this.lastCenter = getWorldBounds().center;
        }
        #endregion

        public static implicit operator Component(Target target) => target ? target.registerObject : null;

        public static implicit operator bool(Target empty) => empty != null && empty.registerObject != null
            && empty.registerObject.gameObject.activeInHierarchy;
    }
}
#endregion

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead
{
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(Target), true)]
    public class Target_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            var p2 = prop.FindPropertyRelative("registerObject");
            EditorGUI.PropertyField(pos, p2, label, false);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            LineHeight + VerticalSpacing;
    }
}
#endif
#endregion