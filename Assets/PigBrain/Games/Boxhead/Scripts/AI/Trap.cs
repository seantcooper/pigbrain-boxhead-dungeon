using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.Utility;
using pigbrain.game.Boxhead.Environment;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class Trap : MonoBehaviour
    {
        const string Group = "trap";
        [SerializeField] internal float damage = 10;
        [SerializeField] Placement placement = Placement.Floor;
        [SerializeField][ToggleObject(group: Group)] Spikes spikes;
        [SerializeField][ToggleObject(group: Group)] Activation activate;
        [SerializeField][ToggleObject(group: Group)] Particles particles;
        [SerializeField][ToggleObject(group: Group)] Weapon weapon;
        TrapState[] states => new TrapState[] { spikes, activate, particles, weapon };

        Rnd rnd;
        float startTime;
        // bool doDamageOnEnter = false;

        void Awake()
        {
            rnd = GetComponentInParent<RoomData>().GetRnd();
            startTime = Time.time;
        }

        // void OnValidate() => seed = seed == 0 ? Mathf.FloorToInt(UnityEngine.Random.value * int.MaxValue) : seed;
        void OnDisable() => StopAllCoroutines();
        void OnEnable()
        {
            if (states.FirstOrDefault(s => s.enabled) is TrapState state)
                StartCoroutine(state.Run(this));
        }

        public bool isPlacementWall => placement == Placement.Wall;
        public bool isPlacementFloor => placement == Placement.Floor;

        #region Triggers
        HashSet<Health> healths = new();
        void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out Health health)) return;
            healths.Add(health);
            if (activeDamage) health.ApplyDamage(new(transform), damage);
        }
        void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out Health health)) return;
            healths.Remove(health);
        }
        void DoDamage(float scale = 1)
        {
            foreach (Health health in healths)
                if (health) health.ApplyDamage(new(transform), damage * scale);
        }
        bool activeDamage = false;
        internal void StartDamage()
        {
            activeDamage = true;
            DoDamage();
        }
        internal void StopDamage() => activeDamage = false;

        #endregion

        #region Spikes
        [Serializable]
        class Spikes : TrapState
        {
            [SerializeField][Range(0.1f, 10)] float enterDuration = 0.5f;
            [SerializeField][Range(0.1f, 10)] float stayDuration = 0.5f;
            [SerializeField][Range(0.1f, 10)] float exitDuration = 0.5f;
            [SerializeField] float distance = 1;

            (Vector3 start, Vector3 end) motion;
            protected override void Start(Trap trap) =>
                motion = (target.localPosition, target.localPosition + Vector3.up * distance);

            protected override IEnumerator Activate(Trap trap)
            {
                static float Ease(float t) => Mathf.Pow(Mathf.Clamp01(t), 2);
                trap.StartDamage();
                yield return new OverTime(enterDuration, (t) => target.localPosition = Vector3.Lerp(motion.start, motion.end, Ease(t)));
                yield return new WaitForSeconds(stayDuration);
                trap.StopDamage();
                yield return new OverTime(exitDuration, (t) => target.localPosition = Vector3.Lerp(motion.end, motion.start, Ease(t)));
            }
        }
        #endregion

        #region Activate
        [Serializable]
        class Activation : TrapState
        {
            [SerializeField][Range(0.1f, 10)] float duration = 2f;
            protected override void Deactivate(Trap trap) =>
                target.gameObject.SetActive(false);

            protected override IEnumerator Activate(Trap trap)
            {
                target.gameObject.SetActive(true);
                for (float time = Time.time + duration; Time.time < time;)
                {
                    trap.DoDamage(Time.deltaTime);
                    yield return new WaitForNextUpdate();
                }
            }
        }
        #endregion

        #region Particles
        [Serializable]
        class Particles : TrapState
        {
            [SerializeField][Range(0.1f, 10)] float duration = 2f;

            protected override void Deactivate(Trap trap) =>
                target.GetComponent<ParticleSystem>().Stop();

            protected override IEnumerator Activate(Trap trap)
            {
                target.GetComponent<ParticleSystem>().Play();
                for (float time = Time.time + duration; Time.time < time;)
                {
                    trap.DoDamage(Time.deltaTime);
                    yield return new WaitForNextUpdate();
                }
            }
        }
        #endregion

        #region Weapon
        [Serializable]
        class Weapon : TrapState
        {
            [SerializeField] Shot shot;
            [SerializeField] Shot warningShot;
            [SerializeField][Range(0, 2)] float warningDuration = 0;
            protected override IEnumerator Activate(Trap trap)
            {
                if (warningDuration >= 0)
                {
                    if (warningShot) warningShot.CreateInstance(trap.gameObject, null, target.position, target.rotation);
                    yield return new WaitForSeconds(warningDuration);
                }
                if (shot) shot.CreateInstance(trap.gameObject, null, target.position, target.rotation);
                yield break;
            }
        }
        #endregion

        #region Trap State
        class TrapState
        {
            [SerializeField] protected Transform target;
            [SerializeField][HideInInspector] internal bool enabled = false;
            [SerializeField][Range(1, 10)] protected float frequency = 5;
            FreqTime freq;

            public IEnumerator Run(Trap trap)
            {
                if (freq == null)
                {
                    Start(trap);
                    freq = new FreqTime(trap.rnd.NextFloat(frequency * 0.1f, frequency), frequency);
                }

                while (enabled)
                {
                    Deactivate(trap);
                    yield return freq.WaitForFrequency();
                    yield return Activate(trap);
                }
            }
            protected virtual void Start(Trap trap) { }
            protected virtual void Deactivate(Trap trap) { }
            protected virtual IEnumerator Activate(Trap trap) { yield break; }
        }
        #endregion

        #region Placement
        enum Placement
        {
            Floor,
            Wall,
            Door,
        }
        #endregion

    }
}