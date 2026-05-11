#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    public class StatsRuntime : Stats
    {
        readonly Dictionary<Stat, Value> lookupStat = new();
        readonly Dictionary<Stat, ControlValue> lookupControl = new();
        readonly Dictionary<Stat, Vector3> positions = new();
        readonly List<Upgrade> consumables = new();
        readonly Queue<IValidationCommand> validationQueue = new();
        bool validationLock;
        internal Stats original;

        internal static StatsRuntime CreateInstance(Stats editorStats)
        {
            string json = JsonUtility.ToJson(editorStats);
            var runtimeStats = CreateInstance<StatsRuntime>();
            JsonUtility.FromJsonOverwrite(json, runtimeStats);
            runtimeStats.name = $"{editorStats.name} (Runtime)";
            runtimeStats.original = editorStats;
            return runtimeStats;
        }

        public override StatsRuntime GetRuntimeStats() => this;

        public override void OnStart()
        {
            lookupStat.Clear();
            consumables.Clear();
            validationQueue.Clear();
            validationLock = false;

            foreach (var stat in stats)
            {
                if (!lookupStat.ContainsKey(stat.stat))
                {
                    lookupStat.Add(stat.stat, stat);
                    stat.Start();
                }
            }

            if (this.upgrades.Count > 0)
            {
                var addUpgrades = upgrades.ToArray();
                upgrades.Clear();
                AddUpgrades(addUpgrades);
            }
        }

        #region Validation
        interface IValidationCommand { void Execute(StatsRuntime runtime); }
        void Validate(IValidationCommand command)
        {
            validationQueue.Enqueue(command);

            if (validationLock) return;
            validationLock = true;

            while (validationQueue.Count > 0)
            {
                validationQueue.Dequeue().Execute(this);

                foreach (var value in stats)
                {
                    if (value.validated) continue;

                    value.ResetValue();
                    if (!consumables.IsNullOrEmpty())
                    {
                        foreach (var u in consumables) foreach (var ustat in u.ustats)
                            if (ustat.stat == value.stat)
                                value.SetBaseValue(value.value = ustat.Apply(value.value));
                        consumables.Clear();
                    }

                    if (!upgrades.IsNullOrEmpty())
                        foreach (var u in upgrades) foreach (var ustat in u.ustats)
                            if (ustat.stat == value.stat)
                                value.value = ustat.Apply(value.value);

                    value.Validate();
                }
            }

            foreach (var value in stats)
                value.RaiseChangeEvent(this);

            validationLock = false;
        }
        #endregion

        #region Value
        public override float this[Stat stat] => lookupStat.TryGetValue(stat, out Value v) ? v.value : 0;

        struct VCSetValue : IValidationCommand
        {
            public Stat stat;
            public float value;
            void IValidationCommand.Execute(StatsRuntime runtime) =>
                runtime.TryGetValue(stat).SetBaseValue(value);
        }

        public override void SetValue(Stat stat, float value) =>
            Validate(new VCSetValue() { stat = stat, value = value });
        public override float GetValue(Stat stat) =>
            lookupStat.TryGetValue(stat, out Value v) ? v.value : 0;

        public override void SetPosition(Stat stat, Vector3 position) =>
            positions[stat] = position;
        public override bool GetPosition(Stat stat, out Vector3 position) =>
            positions.TryGetValue(stat, out position);

        internal override Value TryGetValue(Stat stat)
        {
            if (!lookupStat.TryGetValue(stat, out Value value))
                stats.Add(value = lookupStat[stat] = new Value(stat));
            return value;
        }
        internal override ControlValue TryGetControl(Stat stat, float min = float.MinValue, float max = float.MaxValue)
        {
            if (!lookupStat.TryGetValue(stat, out Value value))
                stats.Add(value = lookupStat[stat] = new Value(stat));
            if (!lookupControl.TryGetValue(stat, out ControlValue control))
                control = lookupControl[stat] = new ControlValue(this, value, min, max);
            return control;
        }
        #endregion

        #region Upgrades
        struct VCAddUpgrades : IValidationCommand
        {
            public Upgrade[] upgrades;
            readonly void IValidationCommand.Execute(StatsRuntime runtime)
            {
                foreach (var upgrade in upgrades)
                {
                    if (!upgrade) continue;
                    (upgrade.consume ? runtime.consumables : runtime.upgrades).Add(upgrade);
                    foreach (var ustat in upgrade.ustats)
                        if (ustat != null && runtime.TryGetValue(ustat.stat) is Value v)
                            v.Invalidate();
                }
            }
        }

        public override void AddUpgrades(Upgrade[] upgrades) =>
            Validate(new VCAddUpgrades { upgrades = upgrades });
        // {
        //     void AddUpgradesAction()
        // {
        //     foreach (var upgrade in upgrades)
        //     {
        //         if (!upgrade) continue;
        //         (upgrade.consume ? this.consumables : this.upgrades).Add(upgrade);
        //         foreach (var ustat in upgrade.ustats)
        //             if (ustat != null && TryGetValue(ustat.stat) is Value v)
        //                 v.Invalidate();
        //     }
        // }
        // Validate(AddUpgradesAction);
        // }
        // public override void AddUpgrades(Upgrade[] upgrades)
        // => Validate(() =>
        // {
        //     // foreach (var upgrade in upgrades)
        //     // {
        //     //     if (!upgrade) continue;
        //     //     (upgrade.consume ? this.consumables : this.upgrades).Add(upgrade);
        //     //     foreach (var ustat in upgrade.ustats)
        //     //         if (ustat != null && TryGetValue(ustat.stat) is Value v)
        //     //             v.Invalidate();
        //     // }
        // });

        struct VCRemoveUpgrade : IValidationCommand
        {
            public Upgrade upgrade;
            void IValidationCommand.Execute(StatsRuntime runtime)
            {
                foreach (var ustat in upgrade.ustats)
                    if (ustat != null && runtime.TryGetValue(ustat.stat) is Value v)
                        v.Invalidate();
                runtime.upgrades.Remove(upgrade);
            }
        }

        public override void RemoveUpgrade(Upgrade upgrade) =>
            Validate(new VCRemoveUpgrade { upgrade = upgrade });
        // {
        //     foreach (var ustat in upgrade.ustats)
        //         if (ustat != null && TryGetValue(ustat.stat) is Value v)
        //             v.Invalidate();
        //     upgrades.Remove(upgrade);
        // });

        struct VCClearUpgrades : IValidationCommand
        {
            public Upgrade upgrade;
            void IValidationCommand.Execute(StatsRuntime runtime)
            {
                foreach (var u in runtime.upgrades) foreach (var ustat in u.ustats)
                    if (ustat != null && runtime.TryGetValue(ustat.stat) is Value v)
                        v.Invalidate();
                runtime.upgrades.Clear();
            }
        }

        public override void ClearUpgrades() => Validate(new VCClearUpgrades());
        // Validate(() =>
        // {
        //     foreach (var u in upgrades) foreach (var ustat in u.ustats)
        //         if (ustat != null && TryGetValue(ustat.stat) is Value v)
        //             v.Invalidate();
        //     upgrades.Clear();
        // });
        #endregion

        #region Events
        public override void AddChangeListener(Stat stat, Action<ChangeEvent> cb)
        {
            var value = TryGetValue(stat);
            value.onChange += cb;
            value.RaiseChangeEvent(this, true);
        }

        public override void RemoveChangeListener(Stat stat, Action<ChangeEvent> cb)
        {
            if (lookupStat != null && lookupStat.ContainsKey(stat)) // do not add
                lookupStat[stat].onChange -= cb;
        }
        #endregion
    }
}