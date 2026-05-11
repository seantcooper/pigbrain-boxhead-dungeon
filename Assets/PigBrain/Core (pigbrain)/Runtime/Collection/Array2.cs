using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Map;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Collections
{
    [Serializable]
    public class Array2
    {
        //        0          0   1    2
        //      ┌────┐         ┌────┐
        //    3 │    │ 1     7 │    │ 3 
        //      └────┘         └────┘
        //        2          6   5    4

        public readonly static int2[] Diagonal4 = new int2[]
        { new(-1, -1), new(1, -1), new(1, 1), new(-1, 1) };
        public readonly static int2[] Neighbor4 = new int2[]
        { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
        public readonly static int2[] Neighbor8 = new int2[]
        { new(-1,-1), new(0, -1),new(1, -1), new(1, 0),new(1,1), new(0, 1), new(-1,1), new(-1, 0) };
        public readonly static int2[] Corners = new int2[]
        { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
        public readonly static int2[] RadialSearch = Radial.Offsets;
    }

    [Serializable]
    public class Array2<T> : IEnumerable<T>
    {
        [SerializeField] int2 isize;
        [SerializeField] T[] items;

        public int2 size => isize;
        public int length => items.Length;

        public void SetValues(IList<T> values) => items = values.ToArray();
        public T[] GetValues() => items.ToArray();

        #region Constructor
        public Array2() : this(new()) { }

        public Array2(int2 size)
        { this.isize = size; items = new T[size.x * size.y]; }

        public Array2(int2 size, T value) : this(size)
        { for (int i = 0; i < items.Length; items[i] = value, i++) ; }

        public Array2(int2 size, IList<T> values) : this(size)
        { for (int i = 0; i < items.Length; items[i] = values[i], i++) ; }
        #endregion

        #region Value
        public T this[int i] { get => items[i]; set => items[i] = value; }

        public T this[int2 i]
        {
            get => items[ToItemIndex(i)];
            set => items[ToItemIndex(i)] = value;
        }

        public T this[int x, int y]
        {
            get => items[ToItemIndex(new(x, y))];
            set => items[ToItemIndex(new(x, y))] = value;
        }

        public T TryGetValue(int2 i) => IsOOB(i) ? default : items[ToItemIndex(i, false)];
        public bool TryGetValue(int2 i, out T value)
        {
            if (IsOOB(i))
            {
                value = default;
                return false;
            }
            value = items[ToItemIndex(i, false)];
            return true;
        }

        int ToItemIndex(int2 i, bool oob = true)
        {
#if UNITY_EDITOR
            if (oob && IsOOB(i)) throw new Exception($"OOB {i}/{isize}");
#endif
            return i.y * isize.x + i.x;
        }
        #endregion

        #region Search
        public IEnumerable<T> TryGetValues(IEnumerable<int2> indices) =>
            indices.Select(i => TryGetValue(i));
        public IEnumerable<T> TryGetValues(int2 i, IEnumerable<int2> offsets) =>
            offsets.Select(o => TryGetValue(o + i));

        public IEnumerable<int2> GetExteriorIndices() =>
            Enumerable.Range(-1, isize.x + 2).Select(x => new int2(x, -1))
                .Concat(Enumerable.Range(-1, isize.x + 2).Select(x => new int2(x, isize.y)))
                .Concat(Enumerable.Range(-1, isize.y + 2).Select(y => new int2(-1, y)))
                .Concat(Enumerable.Range(-1, isize.y + 2).Select(y => new int2(isize.x, y)))
                .Distinct();

        public bool IsOOB(int2 i) => i.x < 0 || i.y < 0 || i.x >= isize.x || i.y >= isize.y;
        public bool IsBoundary(int2 i) => i.x == 0 || i.y == 0 || i.x == isize.x - 1 || i.y == isize.y - 1;
        #endregion

        #region Edit
        public Array2<T> Rotate(int cw)
        {
            if ((cw &= 3) == 0) return new Array2<T>(isize, items);
            Array2<T> result = new((cw % 2 == 0) ? isize : new(isize.y, isize.x));
            for (int y = 0; y < isize.y; y++) for (int x = 0; x < isize.x; x++)
                switch (cw)
                {
                    case 1: result[result.isize.x - 1 - y, x] = this[x, y]; break;
                    case 2: result[result.isize.x - 1 - x, result.isize.y - 1 - y] = this[x, y]; break;
                    case 3: result[y, result.isize.y - 1 - x] = this[x, y]; break;
                }
            return result;
        }

        public Array2<T> FlipX()
        {
            var result = new Array2<T>(isize);
            for (int y = 0; y < isize.y; y++) for (int x = 0; x < isize.x; x++)
                result[isize.x - 1 - x, y] = this[x, y];
            return result;
        }

        public Array2<T> FlipY()
        {
            var result = new Array2<T>(isize);
            for (int y = 0; y < isize.y; y++) for (int x = 0; x < isize.x; x++)
                result[x, isize.y - 1 - y] = this[x, y];
            return result;
        }
        #endregion

        #region Enumerator
        public IEnumerable<int2> range => MapUtility.Range2d(isize);
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < items.Length; i++)
                yield return items[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + isize.x;
                hash = hash * 31 + isize.y;
                for (int i = 0; i < items.Length; i++)
                    hash = hash * 31 + (items[i]?.GetHashCode() ?? 0);
                return hash;
            }
        }
        #endregion
    }

    public static class Array2X
    {
        public static Array2<T> ToArray2<T>(this IEnumerable<T> items, int2 size) => new(size, items.ToArray());
    }

    public static class Radial
    {
        public static readonly int2[] Offsets = {
            new (1,0), new (0,1), new (0,-1), new (-1,0), new (1,1), new (1,-1), new (-1,1), new (-1,-1), new (2,0), new (0,2), new (0,-2), new (-2,0), new (2,1), new (2,-1), new (1,2), new (1,-2),
            new (-1,2), new (-1,-2), new (-2,1), new (-2,-1), new (2,2), new (2,-2), new (-2,2), new (-2,-2), new (3,0), new (0,3), new (0,-3), new (-3,0), new (3,1), new (3,-1), new (1,3), new (1,-3),
            new (-1,3), new (-1,-3), new (-3,1), new (-3,-1), new (3,2), new (3,-2), new (2,3), new (2,-3), new (-2,3), new (-2,-3), new (-3,2), new (-3,-2), new (4,0), new (0,4), new (0,-4), new (-4,0),
            new (4,1), new (4,-1), new (1,4), new (1,-4), new (-1,4), new (-1,-4), new (-4,1), new (-4,-1), new (3,3), new (3,-3), new (-3,3), new (-3,-3), new (4,2), new (4,-2), new (2,4), new (2,-4),
            new (-2,4), new (-2,-4), new (-4,2), new (-4,-2), new (5,0), new (4,3), new (4,-3), new (3,4), new (3,-4), new (0,5), new (0,-5), new (-3,4), new (-3,-4), new (-4,3), new (-4,-3), new (-5,0),
            new (5,1), new (5,-1), new (1,5), new (1,-5), new (-1,5), new (-1,-5), new (-5,1), new (-5,-1), new (5,2), new (5,-2), new (2,5), new (2,-5), new (-2,5), new (-2,-5), new (-5,2), new (-5,-2),
            new (4,4), new (4,-4), new (-4,4), new (-4,-4), new (5,3), new (5,-3), new (3,5), new (3,-5), new (-3,5), new (-3,-5), new (-5,3), new (-5,-3), new (6,0), new (0,6), new (0,-6), new (-6,0),
            new (6,1), new (6,-1), new (1,6), new (1,-6), new (-1,6), new (-1,-6), new (-6,1), new (-6,-1), new (6,2), new (6,-2), new (2,6), new (2,-6), new (-2,6), new (-2,-6), new (-6,2), new (-6,-2),
            new (5,4), new (5,-4), new (4,5), new (4,-5), new (-4,5), new (-4,-5), new (-5,4), new (-5,-4), new (6,3), new (6,-3), new (3,6), new (3,-6), new (-3,6), new (-3,-6), new (-6,3), new (-6,-3),
            new (7,0), new (0,7), new (0,-7), new (-7,0), new (7,1), new (7,-1), new (5,5), new (5,-5), new (1,7), new (1,-7), new (-1,7), new (-1,-7), new (-5,5), new (-5,-5), new (-7,1), new (-7,-1),
            new (6,4), new (6,-4), new (4,6), new (4,-6), new (-4,6), new (-4,-6), new (-6,4), new (-6,-4), new (7,2), new (7,-2), new (2,7), new (2,-7), new (-2,7), new (-2,-7), new (-7,2), new (-7,-2),
            new (7,3), new (7,-3), new (3,7), new (3,-7), new (-3,7), new (-3,-7), new (-7,3), new (-7,-3), new (6,5), new (6,-5), new (5,6), new (5,-6), new (-5,6), new (-5,-6), new (-6,5), new (-6,-5),
            new (8,0), new (0,8), new (0,-8), new (-8,0), new (8,1), new (8,-1), new (7,4), new (7,-4), new (4,7), new (4,-7), new (1,8), new (1,-8), new (-1,8), new (-1,-8), new (-4,7), new (-4,-7),
            new (-7,4), new (-7,-4), new (-8,1), new (-8,-1), new (8,2), new (8,-2), new (2,8), new (2,-8), new (-2,8), new (-2,-8), new (-8,2), new (-8,-2), new (6,6), new (6,-6), new (-6,6), new (-6,-6),
            new (8,3), new (8,-3), new (3,8), new (3,-8), new (-3,8), new (-3,-8), new (-8,3), new (-8,-3), new (7,5), new (7,-5), new (5,7), new (5,-7), new (-5,7), new (-5,-7), new (-7,5), new (-7,-5),
            new (8,4), new (8,-4), new (4,8), new (4,-8), new (-4,8), new (-4,-8), new (-8,4), new (-8,-4), new (9,0), new (0,9), new (0,-9), new (-9,0), new (9,1), new (9,-1), new (1,9), new (1,-9),
            new (-1,9), new (-1,-9), new (-9,1), new (-9,-1), new (9,2), new (9,-2), new (7,6), new (7,-6), new (6,7), new (6,-7), new (2,9), new (2,-9), new (-2,9), new (-2,-9), new (-6,7), new (-6,-7),
            new (-7,6), new (-7,-6), new (-9,2), new (-9,-2), new (8,5), new (8,-5), new (5,8), new (5,-8), new (-5,8), new (-5,-8), new (-8,5), new (-8,-5), new (9,3), new (9,-3), new (3,9), new (3,-9),
            new (-3,9), new (-3,-9), new (-9,3), new (-9,-3), new (9,4), new (9,-4), new (4,9), new (4,-9), new (-4,9), new (-4,-9), new (-9,4), new (-9,-4), new (7,7), new (7,-7), new (-7,7), new (-7,-7),
            new (10,0), new (8,6), new (8,-6), new (6,8), new (6,-8), new (0,10), new (0,-10), new (-6,8), new (-6,-8), new (-8,6), new (-8,-6), new (-10,0), new (10,1), new (10,-1), new (1,10), new (1,-10),
            new (-1,10), new (-1,-10), new (-10,1), new (-10,-1), new (10,2), new (10,-2), new (2,10), new (2,-10), new (-2,10), new (-2,-10), new (-10,2), new (-10,-2), new (9,5), new (9,-5), new (5,9), new (5,-9),
            new (-5,9), new (-5,-9), new (-9,5), new (-9,-5), new (10,3), new (10,-3), new (3,10), new (3,-10), new (-3,10), new (-3,-10), new (-10,3), new (-10,-3), new (8,7), new (8,-7), new (7,8), new (7,-8),
            new (-7,8), new (-7,-8), new (-8,7), new (-8,-7), new (10,4), new (10,-4), new (4,10), new (4,-10), new (-4,10), new (-4,-10), new (-10,4), new (-10,-4), new (9,6), new (9,-6), new (6,9), new (6,-9),
            new (-6,9), new (-6,-9), new (-9,6), new (-9,-6), new (10,5), new (10,-5), new (5,10), new (5,-10), new (-5,10), new (-5,-10), new (-10,5), new (-10,-5), new (8,8), new (8,-8), new (-8,8), new (-8,-8),
            new (9,7), new (9,-7), new (7,9), new (7,-9), new (-7,9), new (-7,-9), new (-9,7), new (-9,-7), new (10,6), new (10,-6), new (6,10), new (6,-10), new (-6,10), new (-6,-10), new (-10,6), new (-10,-6),
            new (9,8), new (9,-8), new (8,9), new (8,-9), new (-8,9), new (-8,-9), new (-9,8), new (-9,-8), new (10,7), new (10,-7), new (7,10), new (7,-10), new (-7,10), new (-7,-10), new (-10,7), new (-10,-7),
            new (9,9), new (9,-9), new (-9,9), new (-9,-9), new (10,8), new (10,-8), new (8,10), new (8,-10), new (-8,10), new (-8,-10), new (-10,8), new (-10,-8), new (10,9), new (10,-9), new (9,10), new (9,-10),
            new (-9,10), new (-9,-10), new (-10,9), new (-10,-9), new (10,10), new (10,-10), new (-10,10), new (-10,-10),};
    }
}

static class Temp
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Gen Offsets")]
    static void Gen()
    {
        const int r = 10;
        var list = new List<int2>();

        for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++)
            if (x != 0 || y != 0) list.Add(new(x, y));

        list.Sort((a, b) =>
        {
            int da = a.x * a.x + a.y * a.y;
            int db = b.x * b.x + b.y * b.y;
            if (da != db) return da - db;
            if (a.x != b.x) return b.x - a.x;
            return b.y - a.y;
        });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("static readonly int2[] Offsets = {");

        int i = 0, n = 16;
        foreach (var o in list)
        {
            if (i++ == 0)
                sb.Append($"    new ({o.x},{o.y}),");
            else
                sb.Append($" new ({o.x},{o.y}),");

            if (i >= n)
            {
                sb.AppendLine();
                i = 0;
            }
        }

        sb.AppendLine("};");

        Debug.Log(sb.ToString());
    }
#endif
}

