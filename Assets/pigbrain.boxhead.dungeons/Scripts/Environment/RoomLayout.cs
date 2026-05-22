#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.Project;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.UI;
using Unity.Mathematics;
using UnityEngine;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead.Environment
{
    [InlineButton(nameof(Build), nameof(Clear))]
    [RequireComponent(typeof(RoomBuilder))]
    [DefaultExecutionOrder(-200)]
    [ProjectInterface.Control(ProjectInterface.Filter.Hierarchy, 50)]
    public class RoomLayout : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] internal uint seed = 10001;
        [SerializeField] internal Setting settings;
        [SerializeField] RoomBuilder builder;
        [SerializeField][Range(0, 10)] internal int shuffle = 2;

        [Header("Room")]
        [SerializeField][Range(1, 100)] internal int roomCount = 5;
        [SerializeField][MinMaxRange(7, 25)] MinMaxInt roomSize = new(7, 25);
        [SerializeField] internal RoomShape.TypeMask shapeMask = 0;
        [SerializeField][Range(1, 8)] internal int roomPad = 4;

        [Header("Loot")]
        [SerializeField][Range(0, 10)] internal int lootSize = 3;
        [SerializeField][MinMaxRange(2, 8)] internal MinMaxInt lootPad = new(2, 3);

        [Header("Misc")]
        [Tooltip("Ratio x Floor tile count")]
        [SerializeField][Range(0, 0.1f)] internal float spawnRatio = 0.5f;
        [Tooltip("Ratio x Floor tile count")]
        [SerializeField][Range(0, 0.1f)] internal float trapRatio = 0.5f;

        // static Rnd Rnd;
        PasteBoard board;

        [ProjectInterface.Button("Clear")]
        void Clear()
        {
            board = null;
            if (!settings.HasFlag(Setting.HoldBuilder))
                builder.Clear();
        }

        [ProjectInterface.Button("Build")]
        public void Build()
        {
            var p = Profiler.Start();

            LevelData levelData = DungeonSelector.GetDungeon();

            if (!levelData) throw new Exception("levelData is null!");

            if (levelData.traits.HasFlag(LevelData.Traits.LootRooms))
                settings |= Setting.LootRooms;
            else settings &= ~Setting.LootRooms;

            roomSize = levelData.roomSize;
            roomCount = levelData.roomCount <= 0 ? levelData.levels.Count : levelData.roomCount;

            seed = levelData.seed;

            builder = GetComponent<RoomBuilder>();
            builder.SetLevelData(levelData);

            board = new PasteBoard(seed);
            PlaceRooms(roomCount, roomSize);
            Profiler.StopAndLog(p, $"Room Layout {roomCount}");

            if (builder && !settings.HasFlag(Setting.HoldBuilder))
                builder.Build(board);
            Profiler.StopAndLog(p, $"Total");
        }

        #region Place Rooms
        void PlaceRooms(int count, MinMaxInt size)
        {
            // from small rooms to large rooms
            RoomShape[] rooms = GetRooms(count).OrderBy(r => r.floorCount).ToArray();

            // First room centered at origin
            int currentDirection = 0;
            RoomShape prev = RoomShape.CreateShape(null, RoomShape.Type.Rectangle, size.min), next = null;
            board.Add(prev);
            prev.position = -prev.size / 2;
            prev.roomType = Room.Type.Start;
            prev.index = 1;
            if (!settings.HasFlag(Setting.FirstRoomPlain))
                AddTrapAndSpawn(prev);

            List<int> lootDirections = new();
            for (int i = 1; i < count; i++)
            {
                if (AddMainRoom(ref currentDirection, prev, rooms[i]))
                {
                    next = rooms[i];
                    next.index = prev.index + 1;
                }

                if (settings.HasFlag(Setting.LootRooms))
                {
                    AddLootRoom(lootDirections, prev);
                    while (lootDirections.Count > 2)
                        lootDirections.RemoveAt(0);
                }

                prev = next;
            }
            next.roomType = Room.Type.Final;
        }

        #region └Main Room
        bool AddMainRoom(ref int currentDirection, RoomShape prev, RoomShape next)
        {
            int d = AddRoom(GetPlacementPositions(prev, next), prev, next, roomPad, (currentDirection + 2) % 4);
            if (d == -1) return false;
            AddTrapAndSpawn(next);
            currentDirection = d;
            return true;
        }
        #endregion

        #region └Trap / Spawn
        void AddTrapAndSpawn(RoomShape room)
        {
            Rnd rnd = new(board.rnd);
            var floor = room.map.range.Where(i => room.map[i] == Cell.Type.Floor).ToHashSet();

            IEnumerable<int2> Mark(int count, bool surround = false)
            {
                bool SurroundingFloor(int2 i) =>
                    Array2.Neighbor8.Count(n => floor.Contains(n + i)) == 8;

                for (int i = 0; i < count && floor.Count > 0; i++)
                {
                    int2 p = 0;
                    if (surround)
                    {
                        if (!rnd.Next(floor.Where(i => SurroundingFloor(i)), out p))
                            break;
                    }
                    else if (!rnd.Next(floor, out p))
                        break;

                    floor.Remove(p);
                    yield return p;
                }
            }

            int2[] traps = null, holes = null;

            if (settings.HasFlag(Setting.Traps))
            {
                traps = Mark(Mathf.CeilToInt(floor.Count * trapRatio), false).ToArray();
                traps.ForEach(i => room.map[i] |= Cell.Type.Trap);
            }
            if (settings.HasFlag(Setting.Holes))
            {
                holes = Mark(Mathf.CeilToInt(floor.Count * spawnRatio), true).ToArray();
                holes.ForEach(i => room.map[i] |= Cell.Type.SpawnHole);
                holes.SelectMany(i => Array2.Neighbor8.Select(n => i + n))
                    .Distinct()
                    .Where(i => room.map[i].HasFlag(Cell.Type.Floor))
                    .ForEach(i => room.map[i] |= Cell.Type.FloorVoid);
            }
        }
        #endregion

        #region └Loot Room
        bool AddLootRoom(List<int> notDirection, RoomShape prev)
        {
            if (lootSize <= 0) return false;

            var next = RoomShape.CreateShape(null, RoomShape.Type.Rectangle, lootSize);
            next.index = prev.index;
            next.roomType = Room.Type.Loot;

            foreach (int pad in new int[] { lootPad.min, lootPad.max })
                if (AddRoom(GetPlacementPositions(prev, next, pad), prev, next, pad, notDirection.ToArray()) != -1)
                    return true;
            return false;
        }
        #endregion

        #region └Add Room
        int AddRoom(List<(int2 p, int d)> positions, RoomShape prev, RoomShape next, int pad, params int[] notDirection)
        {
            foreach (var p in positions)
            {
                if (notDirection.Contains(p.d)) continue;
                if (!board.TryAdd(next, p.p, p.d, shuffle, pad)) continue;
                if (TryAddEdge(prev, next, p.d, pad)) return p.d;
                else board.Remove(next);
            }
            return -1;
        }
        #endregion

        #region └Placement Pos
        List<(int2 p, int d)> GetPlacementPositions(RoomShape prev, RoomShape next, int pad = 4)
        {
            var off = prev.size / 2 - next.size / 2;

            static int IndexOf(int2 d) => Array.IndexOf(Array2.Neighbor4, d);
            List<(int2 p, int d)> positions = new(4)
            {
                (new(prev.position.x + off.x, prev.position.y - next.size.y - pad), IndexOf(new(0, -1))),
                (new(prev.position.x + off.x, prev.position.y + prev.size.y + pad), IndexOf(new(0, +1))),
                (new(prev.position.x - next.size.x - pad, prev.position.y + off.y), IndexOf(new(-1, 0))),
                (new(prev.position.x + prev.size.x + pad, prev.position.y + off.y), IndexOf(new(+1, 0)))
            };

            // Order is closest to origin
            positions.Sort((a, b) => math.lengthsq((float2)a.p).CompareTo(math.lengthsq((float2)b.p)));
            return positions;
        }
        #endregion
        #endregion

        #region Create Rooms
        // Create rooms starting from small to big
        IEnumerable<RoomShape> GetRooms(int count)
        {
            Rnd rnd = new(board.rnd);

            RoomShape CreateRoom(RoomShape.Type type, int2 size)
            {
                var shape = RoomShape.CreateShape(rnd, type, size);
                if (settings.HasFlag(Setting.Rotate))
                    shape.map = shape.map.Rotate(board.rnd.NextInt(0, 4));
                if (settings.HasFlag(Setting.Flip))
                {
                    if (board.rnd.NextFloat() < 0.5f) shape.map = shape.map.FlipX();
                    if (board.rnd.NextFloat() < 0.5f) shape.map = shape.map.FlipY();
                }
                return shape;
            }

            var shapeTypes = Enum.GetValues(typeof(RoomShape.Type)).Cast<RoomShape.Type>()
                .Where(v => shapeMask.HasFlag((RoomShape.TypeMask)(1 << (int)v))).ToList();

            for (int i = 0; i < count; i++)
            {
                int w = rnd.NextInt(roomSize.min, roomSize.max + 1);
                int h = rnd.NextInt(Math.Max(roomSize.min, w * 3 / 4), w + 1);
                for (int t = 0; t < 100; t++)
                {
                    var room = CreateRoom(rnd.Next(shapeTypes), new int2(w, h));
                    room.roomType = Room.Type.Room;
                    if (room.floorCount > room.area / 2)
                    {
                        yield return room;
                        break;
                    }
                }
            }
        }
        #endregion

        #region Room Borders
        int2 GetRoomBorder(RoomShape room, int direction)
        {
            var border = room.GetConnectingBorders(direction, false).ToArray();
            var nonCorners = border.Where(i => !room.CellHasCorner(i)).ToArray();
            int2 p = board.rnd.Next(nonCorners, out int2 i) ? i : board.rnd.Next(border);
            return room.position + p;
        }

        bool HasMatchingRoomBorder(RoomShape room, int2 index, int direction)
        {
            var border = room.GetConnectingBorders(direction, false).ToArray();
            var localIndex = index - room.position;
            return border.Contains(localIndex);
        }

        RoomShape TryAddEdge(RoomShape prev, RoomShape next, int direction, int distance)
        {
            int odirection = (direction + 2) % 4;
            int2 d = Array2.Neighbor4[direction];
            int endDirection = (direction + 2) % 4;

            int2 end = GetRoomBorder(next, endDirection);
            int2 start = end + Array2.Neighbor4[endDirection] * (distance + 1);

            bool lootRoom = distance <= 2 && next.size.x == next.size.y && next.size.x == lootSize;
            if (!(lootRoom && HasMatchingRoomBorder(prev, start, direction)))
            {
                if (distance <= 2) return null;
                start = GetRoomBorder(prev, direction);
            }

            int2 edgeStart = start + d, edgeEnd = end - d;
            var edge = RoomShape.CreateEdge(edgeStart, edgeEnd, direction);
            if (board.Overlaps(edge, -1)) return null;

            edge.map[edgeStart - edge.position] |= Cell.Type.Enter | Cell.GetTypeDirection(odirection);
            edge.map[edgeEnd - edge.position] |= Cell.Type.Exit | Cell.GetTypeDirection(direction);
            prev.map[start - prev.position] |= Cell.Type.Exit | Cell.GetTypeDirection(direction);
            next.map[end - next.position] |= Cell.Type.Enter | Cell.GetTypeDirection(odirection);
            edge.index = prev.index;

            // edge.roomType = Room.Type.Connector;


            prev.children.Add(edge);
            board.AddEdge(edge);
            edge.children.Add(next);
            return edge;
        }
        #endregion

        #region Paste Board
        public class PasteBoard
        {
            public readonly List<RoomShape> rooms = new();
            public readonly Rnd rnd;
            readonly Dictionary<int2, Cell.Type> board = new();

            public PasteBoard(uint seed) =>
                this.rnd = new(seed);

            public Cell.Type Get(int2 i) =>
                board.TryGetValue(i, out var type) ? type : Cell.Type.Empty;

            public bool Overlaps(RoomShape room, int pad = 3) => Overlaps(room, rooms, pad);
            static bool Overlaps(RoomShape room, IEnumerable<RoomShape> rooms, int pad = 3) =>
               rooms.Any(r => room.Overlaps(r, pad));
            // && room.map.range.Any(i => Get(room.position + i) != room.map[i]));

            public void Remove(RoomShape room) =>
                rooms.Remove(room);

            public bool TryAdd(RoomShape room, int2 p, int direction, int move = 4, int pad = 4)
            {
                room.position = p;
                if (Overlaps(room, pad))
                {
                    bool TryMove()
                    {
                        if (move <= 0) return false;
                        int2 d = math.abs(Array2.Neighbor4[(direction + 1) % 4]);
                        for (int i = 1; i <= move; i++)
                        {
                            room.position = p - d * i;
                            if (!Overlaps(room, pad)) return true;
                            room.position = p + d * i;
                            if (!Overlaps(room, pad)) return true;
                        }
                        return false;
                    }
                    if (!TryMove()) return false;
                }
                Add(room);
                return true;
            }

            public bool TryAdd(RoomShape room, int pad = 3)
            {
                if (Overlaps(room, pad)) return false;
                Add(room);
                return true;
            }

            void Paint(RoomShape room) =>
                room.map.range.Where(i => room.map[i] == Cell.Type.Floor)
                    .ForEach(i => board[room.position + i] = room.map[i]);

            public void Add(RoomShape room)
            {
                Paint(room);
                rooms.Add(room);
            }
            public void AddEdge(RoomShape room)
            {
                Paint(room);
                rooms.Add(room);
            }
            public static implicit operator bool(PasteBoard empty) => empty != null;
        }
        #endregion

        #region Gizmos
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;
        void OnDrawGizmos()
        {
            if (!showGizmos || board == null) return;
            foreach (RoomShape shape in board.rooms)
            {
                switch (shape.roomType)
                {
                    case Room.Type.Room: Gizmos.color = Color.skyBlue.WithA(0.5f); break;
                    case Room.Type.Start: Gizmos.color = Color.softBlue.WithA(0.5f); break;
                    case Room.Type.Loot: Gizmos.color = Color.green.WithA(0.5f); break;
                    case Room.Type.Connector: Gizmos.color = Color.orange.WithA(0.5f); break;
                }

                DrawShapeCube(shape, (t) => t != Cell.Type.Empty, Gizmos.color);
                DrawShapeCube(shape, (t) => t.HasFlag(Cell.Type.SpawnHole), Color.black.WithA(0.5f));
                DrawShapeCube(shape, (t) => t.HasFlag(Cell.Type.Trap), Color.white.WithA(0.5f));
                DrawShapeCube(shape, (t) => t.HasFlag(Cell.Type.Enter), Color.green.WithA(0.5f));
                DrawShapeCube(shape, (t) => t.HasFlag(Cell.Type.Exit), Color.red.WithA(0.5f));
            }

            void DrawShapeCube(RoomShape shape, Func<Cell.Type, bool> cellType, Color color)
            {
                Gizmos.color = color;
                Vector3 s = new Vector3(1, 0.1f, 1) * 0.9f;
                shape.map.range
                    .Where(i => cellType(shape.map[i]))
                    .Select(i => i + shape.position)
                    .ForEach(i => Gizmos.DrawCube(new(i.x, 0, i.y), s));
            }
        }
        #endregion

        #region Settings
        [Flags]
        internal enum Setting
        {
            Flip = 1 << 0,
            Rotate = 1 << 1,
            HoldBuilder = 1 << 2,
            Holes = 1 << 3,
            Traps = 1 << 4,
            FirstRoomPlain = 1 << 5,
            LootRooms = 1 << 6,
            [InspectorName(" ")] Another = 1 << 10,
        }
        #endregion
    }
}