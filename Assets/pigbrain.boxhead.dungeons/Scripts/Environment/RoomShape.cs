#pragma warning disable UDR0001
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.Mathematics;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead.Environment
{
    public class RoomShape
    {
        const int MinCorridor = 3;
        public Array2<Cell.Type> map;
        public Room.Type roomType;
        public int2 position;
        public List<RoomShape> children = new();
        public int index;
        public uint seed;

        public int2 size => map.size;
        public int2 center => (position * 2 + size) / 2;
        public int area => size.x * size.y;
        public int floorCount => map.Count(c => c == Cell.Type.Floor);
        public IEnumerable<int2> range => map.range.Select(i => i + position);

        public RoomShape(Array2<Cell.Type> map) =>
            this.map = map;

        #region Corners
        readonly static Array2<bool> CBool = new(2);
        public bool IsCorner(int2 i)
        {
            int count = 0;
            foreach (int2 c in Array2.Corners)
                if (CBool[c] = map.TryGetValue(i + c - 1) == Cell.Type.Empty)
                    count++;

            return (count & 1) == 1
                || (CBool[new int2(0, 0)] && CBool[new int2(1, 1)])
                || (CBool[new int2(1, 0)] && CBool[new int2(0, 1)]);
        }

        public bool CellHasCorner(int2 i)
        {
            if (map.TryGetValue(i) == Cell.Type.Empty) return false;
            return Array2.Corners.Any(c => IsCorner(i + c));
        }
        #endregion

        #region Borders
        public IEnumerable<int2> GetConnectingBorders(int direction)
        {
            int2 d = Array2.Neighbor4[direction];
            if (math.abs(d.x) > 0)
            {
                for (int2 i = new(d.x < 0 ? 0 : size.x - 1, 0); i.y < size.y; i.y++)
                    if (map[i] != Cell.Type.Empty) yield return i;
            }
            else
            {
                for (int2 i = new(0, d.y < 0 ? 0 : size.y - 1); i.x < size.x; i.x++)
                    if (map[i] != Cell.Type.Empty) yield return i;
            }
        }

        public IEnumerable<int2> GetConnectingBorders(int direction, bool includeCorners)
        {
            var border = GetConnectingBorders(direction).ToArray();
            if (includeCorners) return border;
            var nonCorners = border.Where(i => !CellHasCorner(i)).ToArray();
            return nonCorners.Length > 0 ? nonCorners : border;
        }

        #endregion

        #region Overlaps
        public static bool Overlaps(RoomShape room, int2 position, int2 size) =>
            Overlaps(position, size, room.position, room.size);

        public bool Overlaps(RoomShape other, int pad = 0) =>
            Overlaps(position - pad, size + pad * 2, other.position, other.size);

        static bool Overlaps(int2 p1, int2 s1, int2 p2, int2 s2) =>
            p1.x < (p2 + s2).x && (p1 + s1).x > p2.x && p1.y < (p2 + s2).y && (p1 + s1).y > p2.y;

        #endregion

        #region Create
        public enum Type
        {
            Rectangle = 0,
            LShape = 1,
            TShape = 2,
            CShape = 3,
            Cross = 4,
            Island = 5,
            Castle = 6
        }

        [Flags]
        public enum TypeMask
        {
            Rectangle = 1 << Type.Rectangle,
            LShape = 1 << Type.LShape,
            TShape = 1 << Type.TShape,
            CShape = 1 << Type.CShape,
            Cross = 1 << Type.Cross,
            Island = 1 << Type.Island,
            Castle = 1 << Type.Castle
        }

        public static RoomShape CreateShape(Rnd rnd, Type type, int2 size) => type switch
        {
            Type.Rectangle => CreateRectangle(rnd, size),
            Type.LShape => CreateLShape(rnd, size),
            Type.TShape => CreateTShape(rnd, size),
            Type.CShape => CreateCShape(rnd, size),
            Type.Cross => CreateCross(rnd, size),
            Type.Island => CreateIsland(rnd, size),
            Type.Castle => CreateCastle(rnd, size),
            _ => throw new Exception($"Invalid shape type '{type}'"),
        };

        static int RndSize(Rnd rnd, int size, int padMin, int padMax) =>
            rnd.NextInt(padMin, size - padMax + 1);

        #region Edge
        // Connects roomShapes together and loot rooms
        public static RoomShape CreateEdge(int2 start, int2 end, int direction)
        {
            int2 d = Array2.Neighbor4[direction];
            int2 min = math.min(start, end), max = math.max(start, end), size = max - min + 1;
            RoomShape shape = new(new(size, Cell.Type.Empty))
            {
                position = min,
                roomType = Room.Type.Connector,
            };
            // shape.index = -1;

            if (d.x != 0)
            {
                for (int dx = d.x, x = dx < 0 ? size.x - 1 : 0, hs = size.x / 2, i = 0; i < size.x; i++, x += dx)
                    if (i == hs) for (int y = 0; y < size.y; shape.map[x, y] = Cell.Type.Floor, y++) ;
                    else shape.map[x, (i < hs ? start.y : end.y) - min.y] = Cell.Type.Floor;
            }
            else // if (d.y != 0)
            {
                for (int dy = d.y, y = dy < 0 ? size.y - 1 : 0, hs = size.y / 2, i = 0; i < size.y; i++, y += dy)
                    if (i == hs) for (int x = 0; x < size.x; shape.map[x, y] = Cell.Type.Floor, x++) ;
                    else shape.map[(i < hs ? start.x : end.x) - min.x, y] = Cell.Type.Floor;
            }
            return shape;
        }

        #endregion

        #region Rect
        static RoomShape CreateRectangle(Rnd rnd, int2 size) =>
           new(new(size, Cell.Type.Floor));
        #endregion

        #region LShape
        static RoomShape CreateLShape(Rnd rnd, int2 size)
        {
            int w = RndSize(rnd, size.x, MinCorridor, MinCorridor);
            int h = RndSize(rnd, size.y, MinCorridor, MinCorridor);

            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if (x < w && y < h)
                    shape.map[x, y] = Cell.Type.Empty;
            return shape;
        }
        #endregion

        #region TShape
        static RoomShape CreateTShape(Rnd rnd, int2 size)
        {
            int x1 = RndSize(rnd, size.x, MinCorridor, MinCorridor * 2);
            int x2 = size.x - RndSize(rnd, size.x, MinCorridor, x1 + MinCorridor);
            int h = RndSize(rnd, size.y, MinCorridor, MinCorridor);
            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if ((x < x1 || x >= x2) && y >= h)
                    shape.map[x, y] = Cell.Type.Empty;
            return shape;
        }
        #endregion

        #region CShape
        static RoomShape CreateCShape(Rnd rnd, int2 size)
        {
            int x1 = RndSize(rnd, size.x, MinCorridor, MinCorridor * 2);
            int x2 = size.x - RndSize(rnd, size.x, MinCorridor, x1 + MinCorridor);
            int h = RndSize(rnd, size.y, MinCorridor, MinCorridor);
            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if (x >= x1 && x < x2 && y < h)
                    shape.map[x, y] = Cell.Type.Empty;
            return shape;
        }
        #endregion

        #region Cross
        static RoomShape CreateCross(Rnd rnd, int2 size)
        {
            int x1 = RndSize(rnd, size.x, MinCorridor, MinCorridor * 2);
            int x2 = size.x - RndSize(rnd, size.x, MinCorridor, x1 + MinCorridor);
            int y1 = RndSize(rnd, size.y, MinCorridor, MinCorridor * 2);
            int y2 = size.y - RndSize(rnd, size.y, MinCorridor, y1 + MinCorridor);

            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if (!((x >= x1 && x < x2) || (y >= y1 && y < y2)))
                    shape.map[x, y] = Cell.Type.Empty;
            return shape;
        }
        #endregion

        #region Island
        static RoomShape CreateIsland(Rnd rnd, int2 size)
        {
            int x1 = RndSize(rnd, size.x, MinCorridor, MinCorridor * 2);
            int x2 = size.x - RndSize(rnd, size.x, MinCorridor, x1 + MinCorridor);
            int y1 = RndSize(rnd, size.y, MinCorridor, MinCorridor * 2);
            int y2 = size.y - RndSize(rnd, size.y, MinCorridor, y1 + MinCorridor);

            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if (x >= x1 && x < x2 && y >= y1 && y < y2)
                    shape.map[x, y] = Cell.Type.Empty;
            return shape;
        }
        #endregion

        #region Castle
        static RoomShape CreateCastle(Rnd rnd, int2 size)
        {
            const int MinIsland = 4, MinGap = 2;
            if (size.x < MinIsland * 2 + MinGap || size.y < MinIsland * 2 + MinGap) //???
                return CreateRectangle(rnd, size);

            int x1 = RndSize(rnd, size.x, MinIsland, MinGap + MinIsland);
            int x2 = size.x - RndSize(rnd, size.x, MinIsland, x1 + MinCorridor);
            int y1 = RndSize(rnd, size.y, MinIsland, MinGap + MinIsland);
            int y2 = size.y - RndSize(rnd, size.y, MinIsland, y1 + MinCorridor);

            int gx1 = rnd.NextInt(MinGap, x1 - MinGap * 2 + 1);
            int gx2 = rnd.NextInt(x2 + MinGap, size.x - MinGap * 2 + 1);
            int gy1 = rnd.NextInt(MinGap, y1 - MinGap * 2 + 1);
            int gy2 = rnd.NextInt(y2 + MinGap, size.y - MinGap * 2 + 1);

            RoomShape shape = new(new(size, Cell.Type.Floor));
            for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
                if (!(x == gx1 || x == gx2 || y == gy1 || y == gy2))
                    if ((x >= x1 && x < x2) || (y >= y1 && y < y2))
                        shape.map[x, y] = Cell.Type.Empty;

            return shape;
        }
        #endregion
        #endregion

        public static implicit operator bool(RoomShape empty) => empty != null;
    }
}
