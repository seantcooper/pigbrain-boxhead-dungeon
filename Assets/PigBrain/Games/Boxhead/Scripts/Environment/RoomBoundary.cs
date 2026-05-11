using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using pigbrain.core.Analysis;
using pigbrain.core.Collections;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead.Environment
{
    [Serializable]
    public class RoomBoundary
    {
        public Marker[] exterior;
        public Marker[] interior;
        public int2[] floor;
        public int2[] holes;

        public RoomBoundary() { }

        #region Marker
        [Serializable]
        public class Marker
        {
            public int2 index;
            public Type type;
            public Cell.Type cellType; // only for striaghts
            public int direction;
            public Marker(int2 index, Type type, Cell.Type cellType, int direction)
            {
                this.index = index;
                this.type = type;
                this.cellType = cellType;
                this.direction = direction;
            }

            public enum Type { None = 0, Straight = 1, Corner = 2 }
        }
        #endregion

        #region Build

        internal static RoomBoundary BuildBoundary(RoomData data)
        {
            byte[] StraightMasks =
            {
                0b00000111, 0b00011100, 0b01110000, 0b11000001,
                0b00000110, 0b00011000, 0b01100000, 0b10000001,
                0b00000011, 0b00001100, 0b00110000, 0b11000000,
            };
            // 7, 28, 112, 193,
            // 6, 24, 96, 129,
            // 3, 12, 48, 192

            RoomBoundary boundary = new();
            boundary.floor = data.FindIndices(Cell.Type.Floor).ToArray();
            boundary.holes = data.FindIndices(Cell.Type.Interior).ToArray();

            HashSet<int2> interior = new(), exterior = new();

            // expand floor by +1 to get empty cells
            foreach (int2 i in boundary.floor) foreach (int2 d in Array2.Neighbor8)
            {
                int2 di = i + d;
                var type = data.GetCellType(di);
                if (type.HasFlag(Cell.Type.Exterior)) exterior.Add(di);
                else if (type.HasFlag(Cell.Type.Interior)) interior.Add(di);
            }

            // detect all straights, i.e. not corners
            var floor = boundary.floor.ToHashSet();
            IEnumerable<Marker> Trace(HashSet<int2> indices)
            {
                foreach (var i in indices)
                {
                    byte mask = (byte)Enumerable.Range(0, Array2.Neighbor8.Length)
                        .Where(n => floor.Contains(i + Array2.Neighbor8[n])).Aggregate(0, (m, n) => m | (1 << n));
                    int mi = Array.IndexOf(StraightMasks, mask);
                    if (mi != -1)
                    {
                        var d = Array2.Neighbor4.First(d => data.GetCellType(i + d).HasFlag(Cell.Type.Floor));
                        var cellType = data.GetCellType(i + d);
                        yield return new Marker(i, Marker.Type.Straight, cellType, mi % 4);
                    }
                    else yield return new Marker(i, Marker.Type.Corner, Cell.Type.Empty, mi);
                }
            }

            boundary.exterior = Trace(exterior).ToArray();
            boundary.interior = Trace(interior).ToArray();

            return boundary;
        }
        #endregion
    }
}

namespace pigbrain.game.Boxhead.Environment
{
    using static pigbrain.game.Boxhead.Environment.RoomBoundary;
    public static class RoomBoundaryX
    {
        public static IEnumerable<(int2 start, int2 end)> WallToWall(
            this RoomBoundary rb, int axis, Func<Marker, bool> predicate)
        {
            int otherAxis = (axis + 1) % 2; // x & y
            var all = rb.exterior.Where(m => predicate(m)).Select(m => m.index).Concat(rb.holes);
            var col = all.GroupBy(i => i[axis]).ToDictionary(g => g.Key, g => g.ToArray());
            return col.Values.Where(c => c.Length == 2)
                .Select(c => c[0][otherAxis] < c[1][otherAxis] ? (c[0], c[1]) : (c[1], c[0]));
        }

        public static List<int2[]> WallToWallLoops(
            this RoomBoundary rb, int turns, int minTurnDistance, Func<Marker, bool> predicate)
        {
            // var p = Profiler.Start();
            IEnumerable<(int2 p0, int2 p1)> GetLines(int axis) => rb.WallToWall(axis, predicate);

            var vlines = GetLines(0).ToDictionary(l => l.p0.x, l => l);
            var hlines = GetLines(1).ToDictionary(l => l.p0.y, l => l);

            List<int2[]> paths = new();

            foreach (var line in vlines.Values) FindPaths(0, line, (hlines, vlines));
            foreach (var line in hlines.Values) FindPaths(0, line, (vlines, hlines));

            void FindPaths(int count, (int2 p0, int2 p1) line,
                (Dictionary<int, (int2 p0, int2 p1)>, Dictionary<int, (int2 p0, int2 p1)>) lines, int2[] path = null)
            {
                int2 d = math.sign(line.p1 - line.p0);
                int2 a = d.x == 0 ? new int2(1, 0) : new int2(0, 1);

                if (count == 0) paths.Add(new int2[] { line.p0, line.p1 });

                // for (int2 i = line.p0 + d * minTurnDistance; d[a.x] > 0 ? i[a.x] <= line.p1[a.x] : i[a.x] >= line.p1[a.x]; i += d)
                foreach (var coord in lines.Item1.Keys)
                {
                    int delta = coord - line.p0[a.x];
                    if (delta * d[a.x] < minTurnDistance || (coord - line.p1[a.x]) * d[a.x] > 0) continue;
                    int2 i = line.p0;
                    i[a.x] = coord;

                    var other = lines.Item1[coord];

                    // if (lines.Item1.TryGetValue(i[a.x], out var other))
                    {
                        for (int e = 0; e < 2; e++)
                        {
                            int2 end = e == 0 ? other.p0 : other.p1;
                            if (math.abs(end[a.y] - i[a.y]) >= minTurnDistance)
                            {
                                var basePath = path == null ? new List<int2> { line.p0 } : new List<int2>(path);
                                basePath.Add(i);
                                paths.Add(basePath.Append(end).ToArray());
                                if (count + 1 < turns)
                                    FindPaths(count + 1, (i, end), (lines.Item2, lines.Item1), basePath.ToArray());
                            }
                        }
                    }
                }
            }

            // Profiler.StopAndLog(p);
            // Debug.Log(paths.Count);
            return paths;
        }

        // static Dictionary<int2, ((int2 p0, int2 p1) h, (int2 p0, int2 p1) v)> WallToWallIntersections(
        //    IEnumerable<(int2 p0, int2 p1)> vert, IEnumerable<(int2 p0, int2 p1)> hori)
        // {
        //     Dictionary<int2, ((int2 p0, int2 p1) h, (int2 p0, int2 p1) v)> intersections = new();
        //     var hlookup = hori.ToDictionary(l => l.p0.y, l => l);
        //     foreach (var v in vert)
        //         for (int2 i = v.p0, d = math.sign(v.p1 - v.p0); i.y <= v.p1.y; i += d)
        //             if (hlookup.TryGetValue(i.y, out var h))
        //                 intersections[i] = (h, v);
        //     return intersections;
        // }
    }
}