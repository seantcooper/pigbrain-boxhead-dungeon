#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Utility;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    [CreateAssetMenu(menuName = "PigBrain/Stats")]
    public class Stats : ScriptableObject, IScriptableObjectRuntime
    {
        [SerializeField] internal List<Value> stats;
        [SerializeField] internal List<Upgrade> upgrades;
        internal StatsRuntime runtime;
        public bool IsRuntime() => this is StatsRuntime;

        public static Stats CreateInstance(string name, IList<Value> stats = null, IList<Upgrade> upgrades = null)
        {
            var inst = CreateInstance<Stats>();
            inst.stats = stats?.ToList() ?? new();
            inst.upgrades = upgrades?.ToList() ?? new();
            return inst;
        }

        public virtual StatsRuntime GetRuntimeStats()
        {
            if (!runtime)
            {
                runtime = StatsRuntime.CreateInstance(this);
                runtime.OnStart();
            }
            return runtime;
        }

        void IScriptableObjectRuntime.OnBackup() { }
        void IScriptableObjectRuntime.OnQuit() => runtime = null;
        public virtual void OnStart() { }

        #region Value
        public virtual float this[Stat stat] => runtime[stat];
        public virtual void SetValue(Stat stat, float value) =>
            runtime.SetValue(stat, value);
        public virtual float GetValue(Stat stat) =>
            runtime.GetValue(stat);

        public virtual void SetPosition(Stat stat, Vector3 position) =>
            runtime.SetPosition(stat, position);
        public virtual bool GetPosition(Stat stat, out Vector3 position) =>
            runtime.GetPosition(stat, out position);

        internal virtual Value TryGetValue(Stat stat) =>
            runtime.TryGetValue(stat);
        internal virtual ControlValue TryGetControl(Stat stat, float min = float.MinValue, float max = float.MaxValue) =>
            runtime ? runtime.TryGetControl(stat, min, max) : null;
        #endregion

        #region Upgrades
        public virtual void AddUpgrades(params Upgrade[] upgrades) =>
            runtime.AddUpgrades(upgrades);
        public virtual void RemoveUpgrade(Upgrade upgrade) =>
            runtime.RemoveUpgrade(upgrade);
        public virtual void ClearUpgrades() =>
            runtime.ClearUpgrades();
        #endregion

        #region Events
        public virtual void AddChangeListener(Stat stat, Action<ChangeEvent> cb) =>
            runtime.AddChangeListener(stat, cb);
        public virtual void RemoveChangeListener(Stat stat, Action<ChangeEvent> cb)
        { if (runtime) runtime.RemoveChangeListener(stat, cb); }

        internal object Select(Func<object, object> value)
        {
            throw new NotImplementedException();
        }

        public struct ChangeEvent
        {
            public StatsRuntime stats;
            public Stat stat;
            public float oldValue, newValue;
            public float delta => newValue - oldValue;
        }
        #endregion

        #region  Value
        [Serializable]
        public class Value
        {
            public Stat stat;
            public float value;

            // Runtime
            float baseValue, lastValue;
            internal bool validated = true;
            public event Action<ChangeEvent> onChange;

            public Value(Stat stat) => this.stat = stat;
            public Value(Stat stat, float value)
            {
                this.stat = stat;
                this.value = value;
            }

            public void Start()
            {
                baseValue = lastValue = value;
                validated = true;
                onChange = null;
            }

            internal void RaiseChangeEvent(StatsRuntime stats, bool force = false)
            {
                if (value == lastValue && !force) return;
                if (onChange == null) return;
                // Debug.Log($"Change Event {stat} {value}");
                onChange.Invoke(new ChangeEvent
                { stats = stats, stat = stat, oldValue = lastValue, newValue = value });
                lastValue = value;
            }

            // Invalidating Methods
            internal void SetBaseValue(float baseValue)
            { this.baseValue = baseValue; Invalidate(); }

            internal void ResetValue()
            { value = baseValue; Invalidate(); }

            internal void Invalidate() => validated = false;
            internal void Validate() => validated = true;
        }
        #endregion

        #region Value Control
        public class ControlValue
        {
            readonly Stats stats;
            readonly Value value;
            readonly float min, max;
            internal ControlValue(Stats stats, Value value, float min = float.MinValue, float max = float.MaxValue)
            {
                this.stats = stats;
                this.value = value;
                this.min = min;
                this.max = max;
            }

            public float Get() =>
                stats.GetValue(value.stat);
            public void Set(float v) =>
                stats.SetValue(value.stat, Mathf.Clamp(v, min, max));
            public void AddUpgrades(Upgrade[] upgrades) =>
                stats.AddUpgrades(upgrades);
            public void AddChangeListener(Action<ChangeEvent> cb) =>
                stats.AddChangeListener(value.stat, cb);
            public void RemoveChangeListener(Action<ChangeEvent> cb) =>
                stats.RemoveChangeListener(value.stat, cb);

            public static implicit operator float(ControlValue c) => c.value.value;
        }
        #endregion
    }

    public interface IStats { Stats GetStats(); }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using UnityEditor;

    [CustomEditor(typeof(Stats), true)]
    public class StatsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var stats = (Stats)target;

            if (!Application.isPlaying || stats.runtime == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);

            foreach (var field in typeof(StatsRuntime).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType != typeof(List<Stats.Value>)) continue;

                var list = field.GetValue(stats.runtime) as List<Stats.Value>;
                if (list == null) continue;

                foreach (var v in list)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(v.stat.ToString(), GUILayout.Width(120));
                    EditorGUILayout.LabelField(v.value.ToString("0.##"));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
#endif
#endregion