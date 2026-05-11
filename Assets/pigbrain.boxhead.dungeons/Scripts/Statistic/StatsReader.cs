using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Inspector;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    [Serializable]
    [InlineButton(nameof(Read), nameof(Clear))]
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Stats Reader")]
    [ProjectInterface.Control(ProjectInterface.Filter.Project)]
    public class StatsReader : ScriptableObject
    {
        [SerializeField] string prefix = "ai";
        [Header("Google Sheets")]
        [SerializeField] GoogleSheetReader reader;
        [SerializeField][InlineScriptableObject] Stats[] stats;

        [ProjectInterface.Button("Read")]
        public void Read() => reader.Read(OnRead, OnError);

        public void Clear()
        {
            foreach (var target in this.stats)
            {
                target.stats?.Clear();
                target.upgrades?.Clear();
                Write(target);
            }
        }

        void OnRead()
        {
#if UNITY_EDITOR
            string GetKey(string name) => $"{prefix}_{name}";

            Clear();

            var lines = reader.lines;
            var statsByName = Enum.GetNames(typeof(Stat))
                .Zip(Enum.GetValues(typeof(Stat)).Cast<Stat>(), (k, v) => (k, v))
                .ToDictionary(t => t.k.ToLower(), t => t.v);

            string commonPath = "Assets";
            if (stats != null && stats.Length > 0)
                commonPath = System.IO.Path.GetDirectoryName(
                    UnityEditor.AssetDatabase.GetAssetPath(stats.FirstOrDefault(s => s)));

            List<Stats> newStats = new();
            foreach (var line in lines)
            {
                Stats target = null;
                GoogleSheetReader.Line.Group gn = line.groups.FirstOrDefault(kv => kv.key == "name");

                var name = gn?.GetValue() ?? "";
                if (string.IsNullOrEmpty(name) || name.StartsWith("{")) continue;

                if (!(target = this.stats.FirstOrDefault(s => s.name == name)))
                {
                    var assetPath = $"{commonPath}/{name}.asset";
                    var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Stats>(assetPath);
                    if (existing) target = existing;
                    else
                    {
                        newStats.Add(target = Stats.CreateInstance(name));
                        UnityEditor.AssetDatabase.CreateAsset(target, assetPath);
                    }
                }

                foreach (var g in line.groups)
                {
                    if (statsByName.TryGetValue(GetKey(g.key).ToLower(), out Stat stat))
                        target.stats.Add(new Stats.Value(stat,
                            float.TryParse(g.values.First(), out float v) ? v : 0));
                }
                Write(target);
            }

            stats = (stats ?? Array.Empty<Stats>())
                .Concat(newStats)
                .OrderBy(s => s.name)
                .ToArray();

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        void Write(Stats target)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(target);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        void OnError()
        {

        }
    }
}