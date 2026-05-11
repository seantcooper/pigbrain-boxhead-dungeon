using System;
using System.Linq;
using System.Reflection;
using pigbrain.core.Collections;
using pigbrain.core.Events;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.UI;
using UnityEngine;
using static pigbrain.core.Utility.ReflectionUtility;
using static pigbrain.game.Boxhead.Statistic.Stats;
using static pigbrain.game.Boxhead.UI.Console;

namespace pigbrain.game.Boxhead.Statistic
{
    public interface ILevelIndex
    {
        bool SetIndex(int index);
        int GetIndex();
    }

    [Serializable]
    public sealed class StatsController : MonoBehaviour, IStats, ILevelIndex
    {
        [SerializeField] int index = 0;
        [SerializeField][InlineScriptableObject] Stats stats;
        [SerializeField][InlineScriptableObject] StatsCollection levels;
        [SerializeField] internal StatProperty[] properties;

        public string id => levels ? levels.GetGuid() : stats.name;

        void OnValidate()
        {
            if (levels && !stats) stats = levels[this.index = Math.Clamp(index, 0, levels.count - 1)];
        }

        void Awake() { SetLevelIndex(index, true); }
        void OnDestroy() => properties.ForEach(p => p.Detach());

        #region Events
        readonly EventDispatcher events = new();
        public void AddListener(Action<IndexChangeEvent> cb) =>
            events.AddListener(cb, new IndexChangeEvent(this, index));

        public void RemoveListener<T>(Action<T> cb) where T : EventBase =>
            events.RemoveListener(cb);
        #endregion

        bool ILevelIndex.SetIndex(int index) => SetLevelIndex(index);
        int ILevelIndex.GetIndex() => GetLevelIndex();

        public bool SetLevelIndex(int index, bool force = false)
        {
            if (force == false && this.index == index) return false;
            int lastIndex = this.index;
            this.index = levels ? Math.Clamp(index, 0, levels.count - 1) : 0;
            if (lastIndex != this.index)
                events.Invoke(new IndexChangeEvent(this, this.index));
            UpdateStats();
            return lastIndex != this.index;
        }
        public int GetLevelIndex() => index;
        public StatsCollection GetLevels() => levels;

        void UpdateStats()
        {
            if (levels && levels.count > 0) stats = levels[index];
            if (stats)
            {
                stats = StatsLink.GetRuntime(stats);
                foreach (var p in properties) p.Attach(stats);
            }
        }

        #region Stats
        public Stats GetStats() => stats;
        public float this[Stat stat] => stats[stat];

        public void AddUpgrade(Upgrade upgrade) => stats.AddUpgrades(upgrade);
        public void AddUpgrades(params Upgrade[] upgrades) => stats.AddUpgrades(upgrades);
        public void RemoveUpgrade(Upgrade upgrade) => stats.RemoveUpgrade(upgrade);
        public void RemoveAllUpgrades() => stats.ClearUpgrades();

        public void AddChangeListener(Stat stat, Action<ChangeEvent> cb) =>
            stats.AddChangeListener(stat, cb);
        public void RemoveChangeListener(Stat stat, Action<ChangeEvent> cb) =>
            stats.AddChangeListener(stat, cb);
        #endregion

        #region Property
        [Serializable]
        internal class StatProperty
        {
            public Stat stat;
            public Component component;
            public string memberName;
            ComponentMember member;
            Stats attachedStats;

            internal void SetValueInEditor(Stats stats)
            {
                if (GetMemberInfo() is MemberInfo info)
                {
                    member = new ComponentMember(info);
                    Value actualValue = stats.stats.FirstOrDefault(s => s.stat == stat);
                    if (actualValue != null)
                        SetValue(actualValue.value);
                }
            }

            internal void Attach(Stats stats)
            {
                Detach();
                if (GetMemberInfo() is MemberInfo info)
                {
                    member = new ComponentMember(info);
                    attachedStats = stats;
                    attachedStats.AddChangeListener(stat, SetValue);
                }
            }
            internal void Detach()
            {
                if (!attachedStats) return;
                attachedStats.RemoveChangeListener(stat, SetValue);
                attachedStats = null;
                member = null;
            }

            void SetValue(ChangeEvent ev) => SetValue(ev.newValue);
            void SetValue(float value)
            {
                if (!component) Detach();
                else member?.SetValue(component, value);
            }

            MemberInfo GetMemberInfo() => !component || string.IsNullOrEmpty(memberName) ? null
                : component.GetType().GetMember(memberName, DefaultBindings).FirstOrDefault();
        }
        #endregion

        #region Gizmos
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;
        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            GizmosUtility.DrawText(transform.position, index);
        }
        #endregion
    }

    #region Member
    [Serializable]
    public class ComponentMember
    {
        static readonly object[] Parameter = new object[1];

        readonly MemberInfo memberInfo;
        readonly Func<float, object> validateValue;

        public ComponentMember(MemberInfo memberInfo)
        {
            this.memberInfo = memberInfo;
            var valueType = GetValueType();
            if (valueType == typeof(float)) validateValue = (value) => value;
            else if (valueType == typeof(int)) validateValue = (value) => Mathf.RoundToInt(value);
            else throw new InvalidOperationException($"Unsupported stat target type: {valueType}");
        }

        public void SetValue(Component component, float value)
        {
            var v = validateValue(value);
            switch (memberInfo)
            {
                case FieldInfo f: f.SetValue(component, v); break;
                case PropertyInfo p: p.SetValue(component, v); break;
                case MethodInfo m: Parameter[0] = v; m.Invoke(component, Parameter); break;
            }
        }

        Type GetValueType() => memberInfo switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            MethodInfo m => m.GetParameters()[0].ParameterType,
            _ => throw new NotSupportedException()
        };
    }
    #endregion

    #region Extensions
    public static class StatsControllerX
    {
        public static bool TryApplyIndexTo(this StatsController statsController, GameObject gameObject)
        {
            if (!statsController || !gameObject.TryGetComponent(out StatsController other)) return false;
            // Debug.Log($"Set Index ({statsController.GetLevelIndex()}) from {statsController.name} to {gameObject.name} ");
            other.SetLevelIndex(statsController.GetLevelIndex());
            return true;
        }
    }
    #endregion
}

#region Property Drawer
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using System.Collections.Generic;
    using System.IO;
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    using static pigbrain.game.Boxhead.Statistic.StatsController;

    [CustomEditor(typeof(StatsController)), CanEditMultipleObjects]
    sealed class StatsController_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Populate Stats"))
                {
                    Undo.RecordObjects(targets, "Populate Stats Collection");
                    PopulateMulti();
                }

                if (GUILayout.Button("Populate Properties"))
                {
                    Undo.RecordObjects(targets, "Populate Stat Properties");
                    foreach (var t in targets)
                    {
                        if (t is not StatsController c || !c.GetStats()) continue;
                        c.properties = c.GetStats().stats.Select(s => new StatProperty() { stat = s.stat }).ToArray();
                        EditorUtility.SetDirty(c);
                    }
                }

                if (GUILayout.Button("Set Values"))
                {
                    Undo.RecordObjects(targets, "Set Values");
                    foreach (var t in targets)
                    {
                        if (t is not StatsController c || !c.GetStats()) continue;
                        c.properties.ForEach(p => p.SetValueInEditor(c.GetStats()));
                        EditorUtility.SetDirty(c.gameObject);
                    }
                }
            }

            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        void PopulateMulti()
        {
            (bool success, string message) Action(UnityEngine.Object t)
            {
                StatsController controller = t as StatsController;
                GameObject go = controller.gameObject;

                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                if (string.IsNullOrEmpty(prefabPath)) return (false, $"No prefab path for {go.name}");

                string dir = Path.GetDirectoryName(prefabPath);
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
                string dataFolder = Path.Combine(dir, prefabName);
                bool hasContentFolder = AssetDatabase.IsValidFolder(dataFolder);

                var searchFolders = hasContentFolder ? new[] { dataFolder, dir } : new[] { dir };
                var guids = AssetDatabase.FindAssets($"t:{nameof(StatsCollection)}", searchFolders);
                if (guids == null || guids.Length == 0)
                    return (false, hasContentFolder
                        ? $"No StatsCollection found in {dataFolder} or prefab folder {dir} for {go.name}"
                        : $"No StatsCollection found in prefab folder {dir} for {go.name}");

                var path = guids.Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .OrderByDescending(p => Path.GetDirectoryName(p) == dataFolder)
                    .FirstOrDefault(p => Path.GetDirectoryName(p) == dataFolder || Path.GetDirectoryName(p) == dir);

                if (string.IsNullOrEmpty(path)) return (false, $"No StatsCollection directly in {dataFolder} or {dir} for {go.name}");

                var collection = AssetDatabase.LoadAssetAtPath<StatsCollection>(path);
                if (!collection) return (false, $"Invalid StatsCollection at {path} for {go.name}");
                if (collection.count == 0) return (false, $"Empty StatsCollection '{collection.name}' for {go.name}");

                var so = new SerializedObject(controller);
                so.Update();
                so.FindProperty("levels").objectReferenceValue = collection;
                if (collection.count > 0) so.FindProperty("stats").objectReferenceValue = collection[0];
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(controller);
                return (true, $"Assigned StatsCollection '{collection.name}' to {go.name}");
            }

            foreach (var t in targets)
            {
                var (success, message) = Action(t);
                if (success) Debug.Log(message);
                else Debug.LogWarning(message);
            }
        }
    }

    [CustomPropertyDrawer(typeof(StatsController.StatProperty))]
    public class StatsController_StatProperty_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var area = position.WithH(LineHeight).DivideArea(Padding, 0, 0, 0).ToArray();
            var stat = property.FindPropertyRelative(nameof(StatsController.StatProperty.stat));
            var component = property.FindPropertyRelative(nameof(StatsController.StatProperty.component));
            var member = property.FindPropertyRelative(nameof(StatsController.StatProperty.memberName));
            EditorGUI.PropertyField(area[0], stat, GUIContent.none);
            EditorGUI.PropertyField(area[1], component, GUIContent.none);
            if (!component.objectReferenceValue)
            { using (new EditorGUI.DisabledScope(true)) EditorGUI.Popup(area[2], -1, new string[0]); }
            else
            {
                CacheMembers(component.objectReferenceValue);
                int index = EditorGUI.Popup(area[2], Array.IndexOf(cache.names, member.stringValue), cache.displayNames);
                member.stringValue = index == -1 ? "" : cache.names[index];
            }
            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }

        (string[] names, string[] displayNames, MemberInfo[] members, UnityEngine.Object objectValue) cache;
        void CacheMembers(UnityEngine.Object behaviour)
        {
            cache.objectValue = null;
            cache.members = FindMembers(behaviour.GetType(), DefaultBindings).ToArray();
            cache.names = cache.members.Select(m => m.Name).ToArray();
            cache.displayNames = cache.members.Select((m, i) => GetMemberDisplayName(m)).ToArray();
            cache.objectValue = behaviour;
        }

        static string GetValueName(MemberInfo member) => GetMemberValueType(member).ToString().ToLower();
        public static string GetMemberDisplayName(MemberInfo member) => member.MemberType switch
        {
            MemberTypes.Method => $"{member.Name} ({GetValueName(member)})",
            MemberTypes.Property => $"{GetValueName(member)} {member.Name}",
            MemberTypes.Field => $"{GetValueName(member)} {member.Name}",
            _ => throw new Exception($"{member.MemberType} not implmented!"),
        };

        // Find all qualifying members; field, property, method with Float/Int value type
        public static IEnumerable<MemberInfo> FindMembers(Type type, BindingFlags flags)
        {
            var fields = type.GetFields(flags).Where(f => IsNumercal(f.FieldType));
            var props = type.GetProperties(flags).Where(p => IsNumercal(p.PropertyType) && p.GetIndexParameters().Length == 0);
            var methods = type.GetMethods(flags).Where(m => !m.IsSpecialName && m.GetParameters().Length == 1
                && IsNumercal(m.GetParameters()[0].ParameterType));
            return fields.Cast<MemberInfo>().Concat(props).Concat(methods);
        }

        public enum ValueType { Float, Int }
        readonly static Type[] ValueTypes = new[] { typeof(float), typeof(int) };
        static bool IsNumercal(Type type) => ValueTypes.Contains(type);
        static ValueType ToValueType(Type type) => type == typeof(float) ? ValueType.Float : ValueType.Int;
        public static ValueType GetMemberValueType(MemberInfo member) => member.MemberType switch
        {
            MemberTypes.Method => ToValueType((member as MethodInfo).GetParameters()[0].ParameterType),
            MemberTypes.Field => ToValueType((member as FieldInfo).FieldType),
            MemberTypes.Property => ToValueType((member as PropertyInfo).PropertyType),
            _ => throw new Exception($"{member.MemberType} not implmented!"),
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => FullLineHeight;
    }
}
#endif
#endregion
