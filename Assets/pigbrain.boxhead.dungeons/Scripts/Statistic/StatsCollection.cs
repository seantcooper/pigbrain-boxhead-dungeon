using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    public class StatsCollection : core.UnityObject.IdentityScriptableObject
    {
        [SerializeField][InlineScriptableObject] Stats[] stats;

        public int count => stats.Length;

        public Stats[] GetStats() => stats;
        readonly Dictionary<string, Stats> statsByName = new();
        public StatsCollection CreateRuntimeInstance()
        {
            var inst = CreateInstance<StatsCollection>();
            inst.name = $"{name} (Runtime)";
            inst.stats = new Stats[stats.Length];
            for (int i = 0; i < stats.Length; i++)
            {
                inst.stats[i] = stats[i].GetRuntimeStats();
                inst.statsByName.Add(stats[i].name, stats[i]);
            }
            return inst;
        }
        public Stats this[string id] => statsByName[id];
        public Stats this[int index] => stats[index];
    }
}

#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Statistic
{
    using UnityEditor;
    using System.Linq;

    [CustomEditor(typeof(StatsCollection)), CanEditMultipleObjects]
    sealed class StatsCollection_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (var _ = new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find All Stats"))
                {
                    FindAllMulti();
                    return;
                }

                if (GUILayout.Button("Find Local Stats"))
                {
                    FindLocalMulti();
                    return;
                }
            }

            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        void FindLocalMulti()
        {
            Undo.RecordObjects(targets, "Populate Local Stats");
            foreach (var t in targets)
            {
                var so = new SerializedObject(t);
                so.Update();

                var path = AssetDatabase.GetAssetPath(t);
                var folder = System.IO.Path.GetDirectoryName(path);

                var guids = AssetDatabase.FindAssets($"t:{nameof(Stats)}", new[] { folder });
                var assets = guids
                    .Select(g => AssetDatabase.LoadAssetAtPath<Stats>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(s => s != null)
                    .OrderBy(s => s.name)
                    .ToArray();

                var prop = so.FindProperty("stats");
                prop.arraySize = assets.Length;
                for (int i = 0; i < assets.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(t);
                Debug.Log($"Local Stats populated with {assets.Length} Stats assets from {folder} on {t.name}");
            }
        }

        void FindAllMulti()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(Stats)}");
            var assets = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<Stats>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => s.name)
                .ToArray();

            Undo.RecordObjects(targets, "Populate All Stats");
            foreach (var t in targets)
            {
                var so = new SerializedObject(t);
                so.Update();

                var prop = so.FindProperty("stats");
                prop.arraySize = assets.Length;
                for (int i = 0; i < assets.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(t);
                Debug.Log($"GameStats populated with {assets.Length} Stats assets on {t.name}");
            }
        }
    }
}
#endif