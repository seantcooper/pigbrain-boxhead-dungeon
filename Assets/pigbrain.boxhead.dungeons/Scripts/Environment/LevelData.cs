using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    [InlineButton(nameof(Read))]
    // [ProjectInterface.Control(ProjectInterface.Filter.Project)]
    public class LevelData : ScriptableObject
    {
        // [SerializeField][InlineScriptableObject(true)] internal PrefabList enemies;
        public GoogleSheetReader reader;
        public string title;
        public uint seed;
        public int money;
        public float exp = 0;
        public string[] startWeapons;
        public int soldiers;
        public MinMaxInt roomSize;
        public int roomCount;
        public Traits traits = Traits.LootRooms;
        public Type type = Type.Curated;

        [Flags]
        public enum Traits
        {
            None = 0,
            LootRooms = 1 << 0,
            Other = 1 << 16
        }

        public enum Type
        {
            Curated = 0,
            Creator = 1,
        }


        [SerializeField] internal List<Level> levels;

        #region Level
        public Level this[int levelid] => levels[Math.Min(levels.Count - 1, levelid - 1)];
        [Serializable]
        public class Level
        {
#if UNITY_EDITOR
            [SerializeField] internal string name;
#endif
            public int levelid;
            public string title, description;
            public Item[] items;
            public string[] loot;
            public bool lockDoor;

            [Serializable]
            public class Item
            {
                public Enemy.Type type;
                public int count, total, level;
                public float interval;
                public float burstRate = 0.1f;
            }
        }
        #endregion

        #region Read
        [ProjectInterface.Button("Read")]
        public void Read() => reader.Read(OnRead, OnError);
        public void ReadRuntime(Action onComplete) => reader.ReadRuntime(() => OnReadRuntime(onComplete), OnError);
        public void OnError() { }

        void OnReadRuntime(Action onComplete)
        {
            OnRead();
            onComplete?.Invoke();
        }

        void OnRead()
        {
            levels = new();
            foreach (var line in reader.lines)
            {
                var level = new Level { items = new Level.Item[line["type"].values.Length] };
                var keys = line.groups.Select(g => g.key).ToArray();

                var reflector = new ObjectReflector(level);
                foreach (var key in keys) reflector[key] = line[key][0];

                level.loot = line["loot"] != null ? line["loot"].values : new string[0];
                for (int i = 0; i < level.items.Length; i++)
                {
                    reflector = new ObjectReflector(level.items[i] = new());
                    foreach (var key in keys) reflector[key] = line[key][i];
                }

                if (level.levelid > 0)
                {
#if UNITY_EDITOR
                    level.name = $"{level.title} [{level.levelid}]";
#endif
                    levels.Add(level);
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        #endregion
    }
}
