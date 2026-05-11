using System;
using UnityEngine;
using static pigbrain.game.Boxhead.Statistic.Stats;

namespace pigbrain.game.Boxhead.Statistic
{
    [Serializable]
    public class StatsSingle
    {
        [SerializeField] Stats stats;
        [SerializeField] Stat stat;

        Stats runtimeStats;
        Stats runtime => runtimeStats ? runtimeStats : runtimeStats = StatsLink.GetRuntime(stats);

        public void AddChangeListener(Action<ChangeEvent> cb) =>
            runtime.AddChangeListener(stat, cb);

        public void RemoveChangeListener(Action<ChangeEvent> cb) =>
            runtime.AddChangeListener(stat, cb);
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

    [CustomPropertyDrawer(typeof(StatsSingle), true)]
    public class SingleStat_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            // these should be guard but private?
            var stats = prop.FindPropertyRelative("stats");
            var stat = prop.FindPropertyRelative("stat");

            Rect p = EditorGUI.PrefixLabel(pos, label);
            var areas = p.DivideAreaRatio(Padding, 0.5f, 0.5f);
            new[] { stats, stat }.DrawPropertyFields(areas);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
#endregion
