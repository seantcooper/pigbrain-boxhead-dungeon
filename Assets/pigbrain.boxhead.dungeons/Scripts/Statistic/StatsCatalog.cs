using pigbrain.core.UnityObject;
using UnityEngine;
using System;
using pigbrain.core.Inspector;
using pigbrain.core.Analysis;

namespace pigbrain.game.Boxhead.Statistic
{
    [DefaultExecutionOrder(-100)]
    public class StatsCatalog : MonoBehaviourSingleton<StatsCatalog>
    {
        [SerializeField][InlineScriptableObject] StatsCollection stats;
        [SerializeField][InlineScriptableObject] StatsCollection[] collections;
        [SerializeField][InlineScriptableObject] ScriptableObject[] other;

        StatsCollection runtimeStatsCache;
        internal StatsCollection runtimeStats => runtimeStatsCache ? runtimeStatsCache
            : runtimeStatsCache = stats.CreateRuntimeInstance();

        public Stats this[string name] => runtimeStats[name];

        protected override void Awake()
        {
            base.Awake();
            ResetRuntime();
        }
        protected override void OnDestroy() { } // Do not destroy

        public static Stats Session =>
            Instance ? Instance.GetComponent<StatsController>().GetStats() : null;

        public void ResetRuntime()
        {
            runtimeStatsCache = null;
            var initialize = runtimeStats;
        }
    }

    [Serializable]
    public class StatsLink : AssetLink<Stats>
    {
        public Stats GetRuntime() => StatsCatalog.Instance.runtimeStats[id];
        public static Stats GetRuntime(Stats stats) => StatsCatalog.Instance.runtimeStats[GetID(stats)];
        public static implicit operator Stats(StatsLink link) => link.GetRuntime();
    }

    [Serializable]
    public class StatsLinkSingle : StatsLink
    {
        public Stat stat;
        public void SetValue(float v) => GetRuntime().SetValue(stat, v);
        public float GetValue() => GetRuntime().GetValue(stat);
    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using UnityEngine;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    using pigbrain.core.Inspector;

    [CustomPropertyDrawer(typeof(StatsLinkSingle), true)]
    public class StatsLinkSingle_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            // these should be guard but private?
            var stats = prop.FindPropertyRelative("target");
            if (stats.objectReferenceValue)
            {
                var stat = prop.FindPropertyRelative("stat");
                Rect p = EditorGUI.PrefixLabel(pos, label);
                var areas = p.DivideAreaRatio(Padding, 0.5f, 0.5f);
                new[] { stats, stat }.DrawPropertyFields(areas);
            }
            else EditorGUI.PropertyField(pos, stats, label);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
#endregion
