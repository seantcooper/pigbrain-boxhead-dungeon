#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Graphics;
using pigbrain.core.Map;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static pigbrain.game.Boxhead.Environment.CellObject;

namespace pigbrain.game.Boxhead.Environment
{
    [Serializable]
    public class RoomData : MonoBehaviour, ISerializationCallbackReceiver
    {
        public uint seed;
        public int gridSize;
        public Room.Type roomType;
        public Array2<Cell> map;
        public int2 worldIndex;
        public RoomPrefabs prefabs;
        public int level;
        public Rnd rnd;
        public List<CellObject> objects = new();
        public int2 size => map.size;
        public RoomBoundary boundary;
        public Transform builder;
        public RoomData parent;
        public List<RoomData> children = new();
        public LevelData.Level levelData;
        public Volume volume;

        public const string CeilingContainerName = "ceiling";

        internal static RoomData CreateInstance(RoomShape shape, int gridSize, uint seed, IEnumerable<RoomPrefabs> prefabGroups)
        {
            string levelName = shape.index == -1 ? "" : $" [{shape.index}]";
            GameObject room = new($"Room ({shape.roomType}){levelName}");

            RoomData roomData = room.AddComponent<RoomData>();
            roomData.rnd = new(roomData.seed = seed);
            roomData.level = shape.index;
            roomData.roomType = shape.roomType;
            roomData.prefabs = roomData.rnd.NextWeighted<RoomPrefabs>(prefabGroups);
            roomData.worldIndex = shape.position;
            roomData.gridSize = gridSize;

            roomData.builder = new GameObject("builder").transform;
            roomData.builder.SetParent(roomData.transform);

            roomData.transform.position = roomData.GetWorldPosition(0);

            roomData.map = new(shape.size, MapUtility.Range2d(shape.size)
                .Select(i => new Cell { type = shape.map[i], index = i }).ToArray());

            return roomData;
        }

        public Vector3 GetSpawnPosition()
        {
            var fearLayer = NavMap.TryGetLayer<NavMapLayerFear>();

            var floor = map.range.Where(i => map[i].type == Cell.Type.Floor).ToArray();
            if (floor.Length == 0) floor = map.range.Where(i => map[i].type.HasFlag(Cell.Type.Floor)).ToArray();

            return floor.Select(i => GetWorldPosition(i))
                .OrderBy(w => fearLayer ? fearLayer.GetThreat(w) : rnd.NextFloat(0, 1))
                .First();
        }

        public Vector3 GetRandomPosition(float space = 0.5f) => GetRandomPosition(rnd, space);
        public Vector3 GetRandomPosition(Rnd rnd, float space = 0.5f)
        {
            var floor = map.range.Where(i => map[i].type == Cell.Type.Floor).ToArray();
            if (floor.Length == 0) return GetWorldPosition(0);

            int2 i = rnd.Next(floor);

            float half = gridSize * 0.5f;
            float range = half - space;

            float x = rnd.NextFloat(-range, range);
            float z = rnd.NextFloat(-range, range);

            return GetWorldCenter(i) + new Vector3(x, 0, z);
        }

        #region Build
        public int2 GetWorldIndex(int2 i) => worldIndex + i;
        public Vector3 GetWorldCenter(int2 i) => GetWorldPosition((float2)i + 0.5f);
        public Vector3 GetWorldPosition(float2 i) => GetLocalPosition((float2)worldIndex + i);
        public Vector3 GetLocalCenter(int2 i) => GetLocalPosition((float2)i + 0.5f);
        public Vector3 GetLocalPosition(float2 i) => new(i.x * gridSize, 0, i.y * gridSize);

        internal CellObject CreateObject(PrefabList prefabs, Vector3 p, Quaternion r, Cell.Type cellType, GeomType geomType) =>
            prefabs ? CreateObject(prefabs.prefabs.items, p, r, cellType, geomType) : null;

        internal CellObject CreateObject(IEnumerable<PrefabList.Info> items, Vector3 p, Quaternion r,
            Cell.Type cellType, GeomType geomType) =>
            prefabs && rnd.NextWeighted<GameObject>(items) is GameObject prefab
                && CreateObject(prefab, p, r, cellType, geomType) is CellObject inst ? inst : null;

        internal CellObject CreateObject(GameObject prefab, Vector3 p, Quaternion r, Cell.Type cellType, GeomType geomType)
        {
            if (!prefab) return null;
            var inst = prefab.PrefabInstantiate(builder);
            inst.transform.SetLocalPositionAndRotation(p, r);
            inst.SetLayer((int)geomType.GetUnityLayer());
            var cellObject = inst.AddComponent<CellObject>();
            cellObject.cellType = cellType;
            cellObject.geomType = geomType;
            cellObject.worldPositionKey = GetWorldPositionKey(inst.transform.position);
            objects.Add(cellObject);
            AddCellObjectToMap(cellObject);
            return cellObject;
        }

        internal void MarkExternalAndInternal()
        {
            Queue<int2> q = new();
            HashSet<int2> visited = new();

            void TryQue(int2 i) { if (!visited.Contains(i) && GetCellType(i) == 0) q.Enqueue(i); }

            for (int x = 0; x < size.x; x++) { TryQue(new(x, 0)); TryQue(new(x, size.y - 1)); }
            for (int y = 0; y < size.y; y++) { TryQue(new(0, y)); TryQue(new(size.x - 1, y)); }

            while (q.Count > 0)
            {
                int2 i = q.Dequeue();
                if (!visited.Add(i) || GetCellType(i) != 0) continue;
                map[i].type |= Cell.Type.Exterior;
                Array2.Neighbor4.ForEach(d => TryQue(i + d));
            }

            map.range.Where(i => GetCellType(i) == 0).ForEach(i => map[i].type |= Cell.Type.Interior);
            boundary = RoomBoundary.BuildBoundary(this);
        }
        #endregion

        #region Search        
        public IEnumerable<CellObject> Find(Cell.Type cellType, GeomType geomType) =>
            GetComponentsInChildren<CellObject>(true).Where(c =>
                (cellType == 0 || c.cellType.HasFlag(cellType)) && (geomType == 0 || c.geomType == geomType));

        readonly static Array2<bool> CBool = new(2);
        public bool IsCorner(int2 i)
        {
            int count = 0;
            foreach (int2 c in Array2.Corners)
                if (CBool[c] = map.TryGetValue(i + c - 1)) count++;
            if (count == 0 || count == 4) return false;
            return (count & 1) == 1 || (CBool[0, 0] && CBool[1, 1]) || (CBool[1, 0] && CBool[0, 1]);
        }

        public bool IsDoor(int2 i, int2 d) =>
            IsCellTypeAny(i, Cell.Type.Enter | Cell.Type.Exit)
                && map[i].GetTypeDirection() == Array.IndexOf(Array2.Neighbor4, d);

        public bool IsWall(int2 i, int2 d) =>
            map.TryGetValue(i) && !map.TryGetValue(i + d) && !IsDoor(i, d);

        public IEnumerable<int2> GetWallDirections(int2 i) =>
            Array2.Neighbor4.Where(d => IsWall(i, d));

        public bool IsWallOrDoor(int2 i, int2 d) =>
            IsDoor(i, d) || IsWall(i, d);

        public Cell.Type GetCellType(int2 i) => map.IsOOB(i) ? Cell.Type.Exterior : map[i].type;
        public bool IsCellType(int2 i, Cell.Type type) => GetCellType(i).HasFlag(type);
        public bool IsCellTypeAny(int2 i, Cell.Type type) => ((int)GetCellType(i) & (int)type) > 0;

        public int WallCount(int2 i) => Array2.Neighbor4.Count(d => IsWall(i, d));
        public int FloorCount(int2 i) =>
            Array2.Neighbor8.Count(d => map.TryGetValue(i + d) is Cell c
                && c.type.HasFlag(Cell.Type.Floor));

        public IEnumerable<int2> FindIndices(Cell.Type has, Cell.Type not = 0) =>
            map.range.Where(i => (map[i].type & has) == has && (not == 0 || (map[i].type & not) == 0));

        #endregion

        #region Object Map
        Dictionary<int2, List<CellObject>> objectMap = new();
        readonly static List<CellObject> EmptyCellObject = new();
        public List<CellObject> GetCellObjectsAt(Vector3 p)
        {
            int2 key = new(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z));
            return objectMap.TryGetValue(key, out var list) ? list : EmptyCellObject;
        }

        public void AddCellObjectToMap(CellObject cellObject) =>
            (objectMap.TryGetValue(cellObject.worldPositionKey, out var list)
                ? list : objectMap[cellObject.worldPositionKey] = new()).Add(cellObject);

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            objectMap = new Dictionary<int2, List<CellObject>>();
            for (int i = 0; i < objects.Count; i++)
                AddCellObjectToMap(objects[i]);
        }
        #endregion

        #region Cell
        public class Cell
        {
            public int2 index, worldIndex;
            public Type type;

            [Flags]
            public enum Type
            {
                Empty = 0,
                Floor = 1 << 0,
                FloorVoid = 1 << 1,
                SpawnHole = 1 << 2,
                Exit = 1 << 3,
                Enter = 1 << 4,
                Furniture = 1 << 5,
                Trap = 1 << 6,

                Exterior = 1 << 14,
                Interior = 1 << 15,

                Direction0 = 1 << 16,
                Direction1 = 1 << 17,
                Direction2 = 1 << 19,
                Direction3 = 1 << 20,

                HiddenFloor = 1 << 24,
            }

            public static Type GetTypeDirection(int d) => d switch
            {
                1 => Type.Direction1,
                2 => Type.Direction2,
                3 => Type.Direction3,
                _ => Type.Direction0,
            };
            public int GetTypeDirection() =>
                type.HasFlag(Type.Direction0) ? 0
                    : (type.HasFlag(Type.Direction1) ? 1
                        : (type.HasFlag(Type.Direction2) ? 2
                            : (type.HasFlag(Type.Direction3) ? 3
                                : -1)));

            public static implicit operator bool(Cell empty) =>
                empty != null && empty.type.HasFlag(Type.Floor);
        }
        #endregion

        #region Gizmos
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;
        void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // using (new GizmosUtility.MatrixScope(transform.localToWorldMatrix))
            // {
            //     Gizmos.color = Color.blue.WithA(0.5f);
            //     Vector3 cellSize = (Vector3.one * gridSize).WithY(0.5f);
            //     boundary.exterior.ForEach(e => Gizmos.DrawCube(GetLocalCenter(e.index), cellSize));

            //     Gizmos.color = Color.red.WithA(0.5f);
            //     boundary.interior.ForEach(e => Gizmos.DrawCube(GetLocalCenter(e.index), cellSize));
            //     // boundary.holes.ForEach(i => Gizmos.DrawCube(GetLocalCenter(i), cellSize));
            // }
        }
        #endregion
    }

    public static class RoomDataX
    {
        public static Rnd GetRnd(this RoomData data)
        {
            return data
                ? new Rnd(data.rnd.NextUInt())
                : new Rnd(0);
        }
    }

}
