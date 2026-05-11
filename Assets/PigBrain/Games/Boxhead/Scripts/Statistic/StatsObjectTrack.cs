using System;
using UnityEngine;
using static pigbrain.game.Boxhead.Statistic.Upgrade;

namespace pigbrain.game.Boxhead.Statistic
{
    [Serializable]
    public class StatsObjectTrack : MonoBehaviour
    {
        [SerializeField] Tracker[] trackers;
        void Awake()
        {
            foreach (var tracker in trackers)
                if (tracker.trigger == Tracker.Trigger.OnDead && TryGetComponent(out Health health))
                {
                    health.onDeath += OnDead;
                    break;
                }
            Add(Tracker.Trigger.Awake);
        }

        void Start() => Add(Tracker.Trigger.Start);
        void OnDestroy() => Add(Tracker.Trigger.OnDestroy);
        void OnDead() => Add(Tracker.Trigger.OnDead);

        void Add(Tracker.Trigger trigger)
        {
            foreach (var tracker in trackers)
                tracker.TryApply(trigger, transform);
        }

        [Serializable]
        class Tracker
        {
            [SerializeField] internal StatsLink stats;
            [SerializeField] internal Trigger trigger;
            [SerializeField] internal UStat[] actions;

            internal bool isValid => actions.Length > 0 && trigger != Trigger.None && stats;

            Stats runtime => stats.GetRuntime();
            Upgrade upgrade => CreateInstance("test", true, actions);

            internal void TryApply(Trigger trigger, Transform owner)
            {
                if (trigger == this.trigger && isValid)
                {
                    foreach (var ustat in actions)
                        runtime.SetPosition(ustat.stat, owner.position);
                    runtime.AddUpgrades(upgrade);
                }
            }
            internal enum Trigger { None, Awake, Start, OnDestroy, OnDead }
        }
    }
}
