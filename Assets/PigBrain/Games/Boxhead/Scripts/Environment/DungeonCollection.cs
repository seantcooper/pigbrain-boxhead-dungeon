using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    [InlineButton(nameof(Read))]
    [ProjectInterface.Control(ProjectInterface.Filter.Project)]
    public class DungeonCollection : ScriptableObject
    {
        public GoogleSheetReader reader;
        [SerializeField][InlineScriptableObject] internal LevelData[] levelDatas;

        public LevelData this[int index] =>
            levelDatas[Mathf.Clamp(index, 0, levelDatas.Length - 1)];

        [ProjectInterface.Button("Read")]
        public void Read() => reader.Read(OnRead, OnError);

        public void ReadRuntime(Action<LevelData> onRead)
        {
            reader.ReadRuntime(() => OnReadRuntime(onRead), OnError);
        }

        public void OnError() { }
        void OnRead()
        {
#if UNITY_EDITOR
            var all = LevelDataX.GetAllLevelData();
            var levelDatas = all.ToDictionary(d => d.name, d => d);
            List<LevelData> result = GetLevelDatas(false, (ld) => ld.Read(), all.ToDictionary(d => d.name, d => d));
            this.levelDatas = result.ToArray();
#endif
        }

        void OnReadRuntime(Action<LevelData> onRead)
        {
            List<LevelData> result = GetLevelDatas(true, (ld) => ld.ReadRuntime(() => onRead(ld)), null);
            this.levelDatas = result.ToArray();
        }

        List<LevelData> GetLevelDatas(bool runtime, Action<LevelData> read, Dictionary<string, LevelData> levelDatas = null)
        {
            List<LevelData> result = new();
            foreach (GoogleSheetReader.Line line in reader.lines)
            {
                string name = line.GetValue("name", "NONAME");

                if (levelDatas != null && levelDatas.TryGetValue(name, out LevelData ld)) result.Add(ld);
#if UNITY_EDITOR
                else if (!runtime)
                {
                    result.Add(ld = LevelDataX.Create(this, name));
                }
#endif
                else ld = LevelDataX.CreateRuntime(this, name);

                ld.title = line["title"].values.First();
                ld.reader.id = line.GetValue("id", reader.sheet); ;
                ld.reader.sheet = line.GetValue("sheet", reader.sheet); ;
                ld.seed = (uint)line.GetValue<int>("sheet", 12345);
                ld.money = line.GetValue<int>("money", 10);
                ld.soldiers = line.GetValue<int>("soldiers", 0);
                ld.startWeapons = line.GetValues("weapon", new string[0]);
                ld.roomSize = new MinMaxInt(line.GetValue<int>("minroom", 6), line.GetValue<int>("maxroom", 20));
                ld.roomCount = line.GetValue<int>("roomcount", 0);
                read?.Invoke(ld);
            }
            return result;
        }

    }
}

namespace pigbrain.game.Boxhead.Environment
{
    public static class LevelDataX
    {
#if UNITY_EDITOR
        public static LevelData[] GetAllLevelData()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(LevelData)}");
            return guids
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(o => o)
                .ToArray();
        }

        public static LevelData Create(DungeonCollection collection, string name)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(collection);
            var dir = System.IO.Path.GetDirectoryName(path);

            var asset = ScriptableObject.CreateInstance<LevelData>();
            asset.name = name;

            var assetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{dir}/{name}.asset");
            UnityEditor.AssetDatabase.CreateAsset(asset, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            return asset;
        }
#endif

        public static LevelData CreateRuntime(DungeonCollection collection, string name)
        {
            LevelData asset = ScriptableObject.CreateInstance<LevelData>();
            asset.name = name;
            asset.reader = new();
            return asset;
        }
    }
}
