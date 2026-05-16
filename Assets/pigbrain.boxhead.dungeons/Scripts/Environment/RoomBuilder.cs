#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.Map;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.generated;
using TMPro;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using static pigbrain.core.Geom.Rnd;
using static pigbrain.game.Boxhead.Environment.CellObject;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead.Environment
{
    public class RoomBuilder : MonoBehaviour
    {
        [SerializeField] internal uint seed = 10001;
        [SerializeField] internal int gridSize = 4;
        [SerializeField] PrefabSet[] prefabSets;
        [SerializeField] Settings settings;
        [SerializeField][InlineScriptableObject] internal LevelData levelData;

        [Header("Fog")]
        [SerializeField] Material[] fogMaterials;
        [SerializeField][Range(0.01f, 4)] float fogHeight = 0.05f;

        [SerializeField][ReadOnly] internal RoomData startRoom;
        [SerializeField][ReadOnly] internal RoomData finalRoom;

        [SerializeField] Events events;
        [Serializable]
        class Events { public UnityEvent onClear, onBuild; }

        // LevelData levelData;

        internal void SetLevelData(LevelData levelData)
        {
            this.levelData = levelData;
            seed = levelData.seed;
        }

        [Flags]
        enum Settings
        {
            None = 0,
            HideEntrances = 1 << 0,
            Another = 1 << 1,
        }

        #region Prefab Sets
        [Serializable]
        public class PrefabSet : IWeightedObject
        {
            [SerializeField] string name;
            [SerializeField] int weight;
            [SerializeField] internal VolumeProfile volume;
            [SerializeField][InlineScriptableObject(true)] RoomPrefabs[] roomPrefabs;
            [SerializeField][InlineScriptableObject(true)] RoomPrefabs[] connectorPrefabs;
            [SerializeField][InlineScriptableObject(true)] RoomPrefabs[] lootPrefabs;

            public RoomPrefabs[] GetTypePrefabs(Room.Type roomType) => roomType switch
            {
                Room.Type.Loot => lootPrefabs,
                Room.Type.Connector => connectorPrefabs,
                _ => roomPrefabs,
            };

            object IWeightedObject.GetValue() => this;
            int IWeightedObject.GetWeight() => weight;
        }
        #endregion

        #region Clear
        public void Clear()
        {
            transform.Cast<Transform>().ToArray()
                .ForEach(t => DestroyImmediate(t.gameObject));
            if (!Application.isPlaying)
                events?.onClear?.Invoke();
        }
        #endregion

        #region Build
        Catalog.CatalogQuery catalogQuery;
        public void Build(RoomLayout.PasteBoard board)
        {
            catalogQuery = Catalog.Query;

            var p1 = Profiler.Start();
            Clear();

            var p2 = Profiler.Start();
            RoomShape[] buildList = board.rooms
                .OrderBy(s => s.index)
                .ThenBy(s => (int)s.roomType)
                .ToArray();

            Rnd setrnd = new(seed);
            Dictionary<int, PrefabSet> sets = new() { { 1, prefabSets[0] } };
            Dictionary<RoomShape, RoomData> shapeToData = new();

            // Create Data

            Rnd rnd = new(seed);
            foreach (var shape in buildList)
            {
                if (!sets.TryGetValue(shape.index, out var set))
                    sets.Add(shape.index, set = setrnd.NextWeighted<PrefabSet>(prefabSets));
                shapeToData[shape] = CreateRoom(shape, set, rnd);
            }

            // Connect rooms
            void AddChild(RoomData parent, RoomData child)
            {
                parent.children.Add(child);
                child.parent = parent;
            }
            foreach (var shape in buildList)
                shape.children.ForEach(s => AddChild(shapeToData[shape], shapeToData[s]));

            startRoom = shapeToData.Values.FirstOrDefault(d => d.roomType == Room.Type.Start);
            finalRoom = shapeToData.Values.FirstOrDefault(d => d.roomType == Room.Type.Final);

            Profiler.StopAndLog(p2, $"Create Rooms");

            BuildFinal(board);

            gameObject.SetStatic();

            Profiler.StopAndLog(p1, $"Room Builder");
            if (!Application.isPlaying)
                events?.onBuild?.Invoke();
        }
        #endregion

        #region Build Final
        void BuildFinal(RoomLayout.PasteBoard board)
        {
            var p = Profiler.Start();
            var roomDatas = GetComponentsInChildren<RoomData>();
            CreateLabels(roomDatas);
            CreateCeiling(roomDatas);
            CreateVoid(roomDatas);
            CreateFog(roomDatas);

            GetComponentsInChildren<Room>()
                .Where(r => r.data.roomType == Room.Type.Loot)
                .SelectMany(r => r.GetComponentsInChildren<Collider>())
                .ForEach(r => AddNMModifier(r.gameObject));

            GetComponentsInChildren<PoolGroup>().ForEach(pg => Destroy(pg));

            Profiler.StopAndLog(p, "Final");
        }

        void AddNMModifier(GameObject gameObject, string area = "Player Only")
        {
            var modifier = gameObject.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Player Only");
        }
        #endregion

        #region Create Labels
        void CreateLabels(RoomData[] datas)
        {
            foreach (var data in datas)
            {
                if (data.roomType == Room.Type.Connector) continue;
                if (data.roomType == Room.Type.Loot) continue;

                data.gameObject.name = $"{data.gameObject.name} ({data.levelData.title})";

                if (data.prefabs.doorLabelExit)
                {
                    foreach (CellObject exit in data.Find(Cell.Type.Exit, GeomType.Door))
                    {
                        RoomData IsLootRoom()
                        {
                            foreach (var child in data.children)
                            {
                                var entrance = child.Find(Cell.Type.Enter, GeomType.Door).FirstOrDefault();
                                if (child.Find(Cell.Type.Enter, GeomType.Door).FirstOrDefault()
                                    && (entrance.transform.position - exit.transform.position).sqrMagnitude < 1
                                    && child.children.First().roomType == Room.Type.Loot)
                                    return child.children.First();
                            }
                            return null;
                        }

                        RoomData lootRoom = IsLootRoom();

                        GameObject label = null;
                        if (lootRoom)
                        {
                            label = data.prefabs.doorLabelLoot.PrefabInstantiate(exit.transform);
                            label.GetComponentInChildren<TMP_Text>(true).text = "LOOT";
                        }
                        else
                        {
                            label = data.prefabs.doorLabelExit.PrefabInstantiate(exit.transform);
                            label.GetComponentInChildren<TMP_Text>(true).text = levelData[data.level + 1].title;
                        }
                        label.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));

                        CellObject cellObject = label.AddComponent<CellObject>();
                        cellObject.cellType = exit.cellType;
                        cellObject.worldPositionKey = GetWorldPositionKey(label.transform.position);
                        cellObject.geomType = GeomType.Label;

                        void LootEmpty(GameObject label)
                        {
                            void DestroyLabel(GameObject label) => Destroy(label);
                            lootRoom.GetComponent<Room>().OnLootRoomEmpty += (room) => DestroyLabel(label);
                        }

                        if (lootRoom) LootEmpty(label);
                    }
                }

                if (data.prefabs.doorLabelEntrance)
                {
                    foreach (CellObject entrance in data.Find(Cell.Type.Enter, GeomType.Door))
                    {
                        var label = data.prefabs.doorLabelEntrance.PrefabInstantiate(entrance.transform);
                        label.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));

                        label.GetComponentInChildren<TMP_Text>(true).text = levelData[data.level - 1].title;

                        var cellObject = label.AddComponent<CellObject>();
                        cellObject.cellType = entrance.cellType;
                        cellObject.worldPositionKey = GetWorldPositionKey(label.transform.position);
                        cellObject.geomType = GeomType.Label;
                    }
                }
            }
        }
        #endregion

        #region Create Room
        public RoomData CreateRoom(RoomShape shape, PrefabSet set, Rnd rnd)
        {
            IEnumerable<RoomPrefabs> prefabGroups = set.GetTypePrefabs(shape.roomType);
            if (prefabGroups.Count() == 0) throw new Exception($"Room prefabs is empty for {shape.roomType}");

            RoomData data = CreateInstance(shape, gridSize, rnd.NextUInt(), prefabGroups);

            data.transform.parent = transform;

            Room room = data.gameObject.AddComponent<Room>();
            room.data = data;

            // Don't create stuff on the invisble wall;
            int2 d = new(0, -1);
            data.map.range
                .Where(i => data.GetCellType(i).HasFlag(Cell.Type.Floor))
                .Where(i => !data.GetCellType(i + d).HasFlag(Cell.Type.Floor))
                .ForEach(i => data.map[i].type |= Cell.Type.HiddenFloor);

            CreateFloor(data);
            CreateWallsAndDoors(data);
            CreateFurniture(data);
            CreateLoot(data);
            CreatePath(data);
            data.MarkExternalAndInternal();

            // Objects
            room.spawner = CreateSpawner(data);
            data.volume = CreateVolume(data, set.volume);

            // For runtime objects
            var gameContainer = new GameObject("runtime");
            gameContainer.transform.parent = data.transform;

            // Culling Zone
            var zone = room.zone = data.gameObject.AddComponent<CullingGroupZone>();
            Vector3 min = data.GetWorldPosition(0).WithY(-gridSize),
                max = data.GetWorldPosition(data.size).WithY(gridSize);
            zone.SetFullBounds(new((max + min) / 2, max - min));

            zone.deactivationObjects.Add(data.builder.gameObject);
            zone.deactivationObjects.Add(gameContainer);
            // if (room.spawner) zone.deactivationObjects.Add(room.spawner.gameObject);
            if (data.volume) zone.deactivationObjects.Add(data.volume.gameObject);
            return data;
        }
        #endregion

        #region Create Volume
        Volume CreateVolume(RoomData data, VolumeProfile profile)
        {
            if (!profile) return null;
            var volumeObject = new GameObject("volume");
            volumeObject.transform.parent = data.transform;
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.weight = 0;
            volume.sharedProfile = profile;
            volume.gameObject.layer = (int)GameLayer.WorldPP;
            return volume;
        }
        #endregion

        #region Create Spawner
        Spawner CreateSpawner(RoomData data)
        {
            if (levelData.levels.Count == 0) return null;
            if (data.roomType == Room.Type.Connector) return null;
            if (data.roomType == Room.Type.Loot) return null;
            if (data.prefabs.doorSpawner)
                foreach (CellObject exit in data.Find(Cell.Type.Exit, GeomType.Door))
                    data.prefabs.doorSpawner.PrefabInstantiate(exit.transform)
                        .transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            return CreateSpawner(data.transform, levelData[data.level]);
        }

        Spawner CreateSpawner(Transform parent, LevelData.Level level)
        {
            var shapes = parent.GetComponentsInChildren<SpawnerShape>();
            if (shapes.Length == 0) return null;

            GameObject spawnerObject = new("spawner");
            spawnerObject.transform.parent = parent;

            var spawner = spawnerObject.AddComponent<Spawner>();
            spawner.shapes = shapes;
            AddBursts(spawner, level);
            return spawner;
        }

        void AddBursts(Spawner spawner, LevelData.Level level)
        {
            if (!spawner) return;

            spawner.GetComponentInParent<RoomData>().levelData = level;
            spawner.GetComponentsInParent<SpawnerBurst>().ForEach(b => DestroyImmediate(b));

            spawner.bursts = level.items
                .Where(item => item.total > 0 && item.count > 0)
                .Select(item =>
                {
                    var burst = spawner.gameObject.AddComponent<SpawnerBurst>();
                    burst.prefab = catalogQuery.Get<GameObject>($"{item.type}");
                    burst.interval = item.interval;
                    burst.count = item.count;
                    burst.level = item.level;
                    burst.total = item.total;

                    // HACK HARD CODED for leve;
                    burst.ignoreClearCondition = item.type == Enemy.Type.Ghost;
                    return burst;
                }).ToArray();
        }
        #endregion

        #region Create Loot
        void CreateLoot(RoomData data)
        {
            if (!levelData) return;
            if (data.roomType != Room.Type.Loot) return;
            // if (data.level < 0) return;
            if (data.prefabs.loot is not PrefabList loot) return;

            Physics.SyncTransforms();
            HashSet<int2> floor = data.map.range
                .Where(i => data.map[i].type == Cell.Type.Floor).ToHashSet();

            void Create(string id)
            {
                var prefab = loot.Get(id);
                if (!prefab) return;
                int2 i = data.rnd.Next(floor);
                Vector3 p = data.GetLocalCenter(i);
                var wp = data.transform.TransformPoint(p);

                if (Physics.Raycast(wp + Vector3.up, Vector3.down, out var hit, 2f,
                    (int)GameLayerFlags.Terrain, QueryTriggerInteraction.Ignore))
                {
                    wp = wp.WithY(hit.point.y);
                    p = data.transform.InverseTransformPoint(wp).AddY(0.01f);
                }

                prefab.SetActive(false);
                var inst = data.CreateObject(prefab, p, Quaternion.identity, data.map[i].type, GeomType.Loot);

                string key = $"{id}.{data.rnd.seed}";
                inst.name = key;
                inst.tracking = true;

                inst.SetActive(true);
                prefab.SetActive(true);

                floor.Remove(i);
            }
            levelData[data.level].loot.ForEach(id => Create(id));
        }
        #endregion

        #region Create Path
        void CreatePath(RoomData data)
        {
            // List<Cell> doors = new();
            // List<Cell> floors = new();
            // data.map.range.ForEach(i =>
            // {
            //     Cell cell = data.map[i];
            //     if (!(cell.type.HasFlag(Cell.Type.Floor)
            //         && !cell.type.HasFlag(Cell.Type.Furniture)
            //         && !cell.type.HasFlag(Cell.Type.Spawn)))
            //         return;

            //     if (cell.type.HasFlag(Cell.Type.Enter | Cell.Type.Exit))
            //         doors.Add(cell);

            //     floors.Add(cell);
            // });


            // var paths = new GameObject("paths") { transform = { parent = data.transform } };

            // foreach (var p in floors)
            // {
            //     var node = new GameObject("node") { transform = { parent = paths.transform } };
            //     node.AddComponent<MeshFilter>().sharedMesh = MeshPrimitive.GetCubeMesh();
            //     node.AddComponent<MeshRenderer>();
            //     node.transform.position = data.GetWorldCenter(p.index);
            // }

        }
        #endregion

        #region Create Floor
        void CreateFloor(RoomData data)
        {
            var prefabs = data.prefabs.floor;
            var root = data.transform;

            data.map.range.ForEach(i =>
            {
                Cell cell = data.map[i];
                cell.worldIndex = data.worldIndex + cell.index;

                if (cell.type == Cell.Type.Empty) return;
                // if (cell.type.HasFlag(Cell.Type.HiddenFloor)) return;

                CellObject inst = null;
                if (!inst) inst = CreateSpawn(data, cell);
                if (!inst) inst = CreateTrap(data, cell);
                if (!inst) inst = data.CreateObject(prefabs, data.GetLocalCenter(i),
                    Quaternion.Euler(0, data.rnd.NextInt(4) * 90, 0), cell.type, GeomType.Floor);
            });
        }
        #endregion

        #region Create Trap
        CellObject CreateTrap(RoomData data, Cell cell)
        {
            if (!cell.type.HasFlag(Cell.Type.Trap)) return null;
            if (!data.prefabs.trap) return null;

            // wall traps
            if (data.WallCount(cell.index) == 1)
            {
                int2 d = data.GetWallDirections(cell.index).First();
                var wallTraps = data.prefabs.trap
                    .Where(t => t.prefab.TryGetComponent(out Trap trap) && trap.isPlacementWall);
                return data.CreateObject(wallTraps, data.GetLocalCenter(cell.index), GetRotation(d),
                    cell.type, GeomType.Floor);
            }

            // floor traps
            var floorTraps = data.prefabs.trap
                .Where(t => t.prefab.TryGetComponent(out Trap trap) && trap.isPlacementFloor);
            return data.CreateObject(floorTraps, data.GetLocalCenter(cell.index),
                Quaternion.identity, cell.type, GeomType.Floor);
        }
        #endregion

        #region Create Spawn
        CellObject CreateSpawn(RoomData data, Cell cell)
        {
            if (!cell.type.HasFlag(Cell.Type.SpawnHole)) return null;
            if (!data.prefabs.spawn) return null;

            return data.CreateObject(data.prefabs.spawn, data.GetLocalCenter(cell.index),
                Quaternion.identity, cell.type, GeomType.Floor);
        }
        #endregion

        #region Create Walls / Doors
        static Quaternion GetRotation(int directionIndex) => Quaternion.Euler(0, 360 - directionIndex * 90, 0);
        static Quaternion GetRotation(int2 direction) => GetRotation(Array.IndexOf(Array2.Neighbor4, direction));
        void CreateWallsAndDoors(RoomData data)
        {
            CellObject CreateWall(int2 i, int2 d)
            {
                var p = data.GetLocalPosition((float2)i + 0.5f + (float2)d / 2);
                var r = GetRotation(d);
                CellObject inst;
                if (data.IsDoor(i, d))
                {
                    var cellType = data.GetCellType(i);
                    inst = data.CreateObject(data.prefabs.door, p, r, cellType, GeomType.Door);
                    if (!inst) Debug.LogError($"No doors defined in {data.prefabs}");

                    // Hide the door, if
                    if (data.roomType == Room.Type.Connector) inst.SetActive(false);
                    else if (settings.HasFlag(Settings.HideEntrances))
                    {
                        // if (data.roomType == Room.Type.Loot) inst.SetActive(false);
                        if (cellType.HasFlag(Cell.Type.Enter)) inst.SetActive(false);
                    }
                }
                else inst = data.CreateObject(data.prefabs.wall, p, r, data.GetCellType(i), GeomType.Wall);
                return inst;
            }

            for (int y = 0; y < data.map.size.y; y++)
                for (int2 x1 = new(-1, y), x2 = new(0, y); x1.x < data.map.size.x; x1 = x2, x2.x++)
                {
                    if (data.IsWallOrDoor(x1, new(1, 0))) CreateWall(x1, new(1, 0));
                    else if (data.IsWallOrDoor(x2, new(-1, 0))) CreateWall(x2, new(-1, 0));
                }

            for (int x = 0; x < data.map.size.x; x++)
                for (int2 y1 = new(x, -1), y2 = new(x, 0); y1.y < data.map.size.y; y1 = y2, y2.y++)
                {
                    if (data.IsWallOrDoor(y1, new(0, 1))) CreateWall(y1, new(0, 1));
                    else if (data.IsWallOrDoor(y2, new(0, -1))) CreateWall(y2, new(0, -1));
                }

            MapUtility.Range2d(data.size + 1).Where(i => data.IsCorner(i)).ForEach(i =>
            {
                var inst = data.CreateObject(data.prefabs.corner, data.GetLocalPosition(i), Quaternion.identity,
                    Cell.Type.Floor, GeomType.Corner);
                // if (inst) inst.AddComponent<NavMapExclude>();
            });

        }
        #endregion

        #region Create Furniture
        void CreateFurniture(RoomData data)
        {
            foreach (Cell cell in data.map)
            {
                if (cell.type != Cell.Type.Floor) continue;

                Vector3 center = data.GetLocalCenter(cell.index);

                int wallCount = data.WallCount(cell.index);
                if (wallCount == 0)
                {
                    if (data.FloorCount(cell.index) == 8)
                        data.CreateObject(data.prefabs.floorFurniture, center,
                            data.rnd.NextQuaternion(0, 360, 0),
                            cell.type | Cell.Type.Furniture, GeomType.Furniture);
                }

                else if (wallCount == 1)
                {
                    int2 d = Array2.Neighbor4.FirstOrDefault(d => data.IsWall(cell.index, d));
                    var inst = data.CreateObject(data.prefabs.wallFurniture, center,
                        GetRotation(d), cell.type | Cell.Type.Furniture, GeomType.Furniture);

                    if (inst)
                    {
                        var o = data.GetCellObjectsAt(data.GetWorldPosition((float2)cell.index + 0.5f + (float2)d / 2))
                            .FirstOrDefault(c => c.geomType == GeomType.Wall);
                        MoveToWall(inst.gameObject, o is CellObject c ? c.gameObject : null);
                    }
                }

                if (wallCount >= 1)
                {
                    Array2.Neighbor4.Where(d => data.IsWall(cell.index, d)).ForEach(d =>
                    {
                        var inst = data.CreateObject(data.prefabs.wallDecoration, center,
                            GetRotation(d), cell.type | Cell.Type.Furniture, GeomType.Furniture);

                        if (inst)
                        {
                            var o = data.GetCellObjectsAt(data.GetWorldPosition((float2)cell.index + 0.5f + (float2)d / 2))
                                .FirstOrDefault(c => c.geomType == GeomType.Wall);
                            MoveToWall(inst.gameObject, o is CellObject c ? c.gameObject : null, 0);
                        }
                    });
                }
            }
        }

        void MoveToWall(GameObject furniture, GameObject wall, float gap = 0.05f)
        {
            if (!furniture || !wall) return;
            var wb = wall.GetRendererBounds();
            var inward = wall.transform.forward;
            float wdepth = Mathf.Abs(Vector3.Dot(wb.extents, inward));
            float fdepth = Mathf.Abs(Vector3.Dot(furniture.GetRendererBounds().extents, inward));
            furniture.transform.position = wb.center.WithY(wb.min.y) + inward * (wdepth + fdepth + gap);
        }
        #endregion

        #region Create Fog
        void CreateFog(RoomData[] datas)
        {
            if (fogMaterials.IsNullOrEmpty()) return;
            foreach (var data in datas)
            {
                var fog = new GameObject("fog") { transform = { parent = data.transform } };
                fog.transform.localPosition = Vector3.up * fogHeight;
                fog.layer = (int)GameLayer.Ceiling;

                var rects = ExtractRects(data.boundary.floor.ToHashSet());
                var verts = new List<Vector3>();
                var tris = new List<int>();
                var uvs = new List<Vector2>();
                foreach (var (pos, size) in rects)
                {
                    int start = verts.Count;
                    tris.Add(start + 0); tris.Add(start + 2); tris.Add(start + 1);
                    tris.Add(start + 0); tris.Add(start + 3); tris.Add(start + 2);

                    float2 min = pos, max = pos + size, roomSize = data.size;
                    float2[] vs = new float2[] { new(min.x, min.y), new(max.x, min.y), new(max.x, max.y), new(min.x, max.y) };
                    foreach (var v in vs)
                    {
                        verts.Add(data.GetLocalPosition(v));
                        uvs.Add(v / roomSize);
                    }
                }

                Mesh mesh = new() { name = "fog" };
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                fog.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = fog.AddComponent<MeshRenderer>();
                r.sharedMaterials = fogMaterials;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        #endregion

        #region Create Ceiling
        void CreateCeiling(RoomData[] datas)
        {
            foreach (var data in datas)
            {
                var ceiling = new GameObject(CeilingContainerName) { transform = { parent = data.transform } };
                var indices = data.boundary.floor.Concat(data.boundary.holes);
                var rects = ExtractRects(indices.ToHashSet());
                foreach (var (pos, size) in rects)
                {
                    var inst = new GameObject("ceiling");
                    inst.transform.parent = ceiling.transform;
                    inst.AddComponent<BoxCollider>().size = new(size.x * gridSize, 1, size.y * gridSize);
                    float2 center = (float2)pos + (float2)size / 2f;
                    inst.transform.position = data.GetWorldPosition(center) + Vector3.up * (gridSize + 0.5f);
                }
                ceiling.SetLayer((int)GameLayer.Ceiling);

                data.GetComponent<Room>().zone.deactivationObjects.Add(ceiling);
            }
        }
        #endregion

        #region Create Void
        void CreateVoid(RoomData[] datas)
        {
            HashSet<int2> used = new();

            void MarkFloor(RoomData data) =>
                data.boundary.floor.ForEach(i => used.Add(data.GetWorldIndex(i)));

            void FillBoundary(RoomData data)
            {
                if (!data.prefabs.voidBlock) return;
                var voids = new GameObject("void") //, typeof(NavMapExclude)
                { transform = { parent = data.transform } };

                var indices = data.boundary.exterior.Concat(data.boundary.interior)
                    .Select(m => m.index).Where(i => !used.Contains(data.GetWorldIndex(i)));
                var rects = ExtractRects(indices.ToHashSet());
                foreach (var (pos, size) in rects)
                {
                    var inst = data.prefabs.voidBlock.PrefabInstantiate(voids.transform);
                    inst.transform.localScale = new(size.x * gridSize, gridSize, size.y * gridSize);
                    float2 center = (float2)pos + (float2)size / 2f;
                    inst.transform.position = data.GetWorldPosition(center).WithY(gridSize * 0.5f - 0.1f);
                }
                voids.SetLayer((int)GameLayer.Void);
                data.GetComponent<Room>().zone.deactivationObjects.Add(voids);
            }
            datas.ForEach(MarkFloor);
            datas.ForEach(FillBoundary);
        }

        #endregion

        #region Greedy Rect
        public static List<(int2 pos, int2 size)> ExtractRects(HashSet<int2> cells)
        {
            var result = new List<(int2, int2)>();
            while (cells.Count > 0)
            {
                // Pick top-leftmost cell
                int2 start = cells.OrderBy(c => c.y).ThenBy(c => c.x).First();
                int width = 1;
                while (cells.Contains(new int2(start.x + width, start.y)))
                    width++;

                int height = 1;
                bool expand = true;

                while (expand)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!cells.Contains(new int2(start.x + x, start.y + height)))
                        {
                            expand = false;
                            break;
                        }
                    }
                    if (expand) height++;
                }

                // Remove rectangle cells
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    cells.Remove(new int2(start.x + x, start.y + y));

                result.Add((start, new int2(width, height)));
            }

            return result;
        }
        #endregion
    }
}
