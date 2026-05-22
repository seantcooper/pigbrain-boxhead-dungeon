using System;
using System.Collections.Generic;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
#if UNITY_EDITOR
    [InlineButton(nameof(Rebuild))]
#endif
    public class Configuration : MonoBehaviour
    {
        public Row rowPrefab;
        public Cell cellPrefab;
        public List<Row> rows;

        public Data data;

#if UNITY_EDITOR
        bool dirty;

        void Rebuild()
        {
            if (!dirty || !this) return; else dirty = false;

            RemoveRows();
            rows.Add(rowPrefab.CreateInstance(transform, "ENEMIES", "LEVEL + QUANTITY"));
            for (int i = 0; i < 5; i++)
                cellPrefab.CreateInstance(rows[^1], 2);

            rows.Add(rowPrefab.CreateInstance(transform, "RAMP", "DIFFICULTY"));
            for (int i = 0; i < 5; i++)
                cellPrefab.CreateInstance(rows[^1], 1);

            rows.Add(rowPrefab.CreateInstance(transform, "ROOMS", "COUNT + SIZE + SHAPE"));
            for (int i = 0; i < 6; i++)
                cellPrefab.CreateInstance(rows[^1], 1);

            rows.Add(rowPrefab.CreateInstance(transform, "WEAPONS", "START + LEVEL"));
            for (int i = 0; i < 4; i++)
                cellPrefab.CreateInstance(rows[^1], 1);

            rows.Add(rowPrefab.CreateInstance(transform, "SOLDIERS", "START + COUNT"));
            for (int i = 0; i < 1; i++)
                cellPrefab.CreateInstance(rows[^1], 1);

            rows.ForEach(r => r.Resize());
        }

        void RemoveRows()
        {
            if (rows.IsNullOrEmpty()) return;
            rows.ForEach(r => DestroyImmediate(r.gameObject));
            rows.Clear();
        }
#endif

        [Serializable]
        public class Data
        {
            [Serializable]
            public class Node { }

            public EnemyNode[] enemies = new EnemyNode[]
            {
                new() {type=Enemy.Type.Zombie,count=100, countMax=200,level=EnemyLevel.Level_1,ramp=Ramp.Linear},
                new() {type=Enemy.Type.Runner,count=50, countMax=100,level=EnemyLevel.Level_1,ramp=Ramp.Linear},
                new() {type=Enemy.Type.Ghost,count=5, countMax=10,level=EnemyLevel.Level_1,ramp=Ramp.Linear},
                new() {type=Enemy.Type.Terror,count=50, countMax=100,level=EnemyLevel.Level_1,ramp=Ramp.Linear},
                new() {type=Enemy.Type.Devil,count=2, countMax=10,level=EnemyLevel.Level_1,ramp=Ramp.Linear},
            };

            [Serializable]
            public class EnemyNode : Node
            {
                public Enemy.Type type;
                public EnemyLevel level;
                public int count;
                public int countMax;
                public Ramp ramp;
            }

            public enum EnemyCount
            {
                Low = 0,
                Medium = 1,
                Height = 2,
            }

            [Serializable]
            public class Weapon : Node
            {
                public string name;
                public int level;
            }

            [Serializable]
            public class RoomShape : Node
            {
                public string name;
            }

            [Serializable]
            public class RoomSize : Node
            {
                public int size;
            }

            [Serializable]
            public class Soldier : Node
            {
                public int number;
            }
        }

        [System.Flags]
        // All present 
        // cycle level
        public enum Enemies : uint
        {
            Zombie = 1 << 0,
            Runner = 1 << 1,
            Ghost = 1 << 2,
            Terror = 1 << 3,
            Devil = 1 << 4,
        }

        // Radio buttons
        public enum EnemyLevel : byte
        {
            Level_1 = 1,
            Level_2 = 2,
            Level_3 = 3,
        }

        // Radio buttons
        public enum Soldiers : byte
        {
            _0 = 0,
            _1 = 1,
            _2 = 2,
            _5 = 5,
            _10 = 10,
        }

        [System.Flags]
        // Radio buttons
        public enum Ramp : byte
        {
            Linear = 1 << 0,
            Ramp_End = 1 << 1,
            Ramp_Start = 1 << 2,
            Flat = 1 << 3,
        }

        // total 4 
        // assign base level 1
        // cycle level
        public enum Weapons : ulong
        {
            None = 0,
            AK47 = 1,
            Barrel = 2,
            Barrier = 3,
            Grenade = 4,
            Railgun = 5,
            Rockets = 6,
            Shield = 7,
            Shotgun = 8,
            Turret = 9,
            Uzi = 10,
        }

        // Radio buttons
        public enum WeaponLevel : byte
        {
            Level_1 = 1,
            Level_2 = 2,
            Level_3 = 3,
            Level_4 = 4,
            Level_5 = 5,
        }


        [System.Flags]
        // all present
        // toggle on/off
        // need at least one
        public enum RoomShapes : byte
        {
            Rectangle = 1 << 0,
            CShape = 1 << 1,
            LShape = 1 << 2,
            TShape = 1 << 3,
            Island = 1 << 4,
            Castle = 1 << 5,
            Cross = 1 << 6,
        }

        // Radio buttons
        public enum RoomCount : byte
        {
            _2 = 2,
            _5 = 5,
            _10 = 10,
            _25 = 25,
        }

        [System.Flags]
        // Radio buttons
        public enum RoomSizeStart : byte
        {
            Small = 6,
            Medium = 10,
            Large = 16,
        }

        [System.Flags]
        // Radio buttons
        public enum RoomSizeEnd : byte
        {
            Small = 6,
            Medium = 10,
            Large = 16,
        }
    }
}