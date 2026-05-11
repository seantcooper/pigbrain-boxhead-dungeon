using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Map;
using Unity.Mathematics;

namespace pigbrain.core.Collections
{
    public class Array3<T> : IEnumerable<T>
    {
        int3 size;
        readonly T[] items;

        public int3 Size => size;
        public int Length => items.Length;

        public T this[int3 i]
        {
            get => items[ToItemIndex(i)];
            set => items[ToItemIndex(i)] = value;
        }

        public T this[float3 i]
        {
            get => this[(int3)i];
            set => this[(int3)i] = value;
        }

        public T this[int x, int y, int z]
        {
            get => items[ToItemIndex(new(x, y, z))];
            set => items[ToItemIndex(new(x, y, z))] = value;
        }

        public Array3(int3 size)
        {
            this.size = size;
            items = new T[size.x * size.y * size.z];
        }

        public Array3(int3 size, T value) : this(size)
        {
            for (int i = 0; i < items.Length; items[i] = value, i++) ;
        }

        public Array3(int3 size, IList<T> values) : this(size)
        {
            for (int i = 0; i < items.Length; items[i] = values[i], i++) ;
        }

        public bool IsOOB(int3 i) => i.x < 0 || i.y < 0 || i.z < 0
            || i.x >= size.x || i.y >= size.y || i.z >= size.z;
        public bool IsBoundary(int3 i) => i.x == 0 || i.y == 0 || i.z == 0
            || i.x == size.x - 1 || i.y == size.y - 1 || i.z == size.z - 1;

        int ToItemIndex(int3 i)
        {
            if (IsOOB(i)) throw new Exception($"OOB {i}/{size}");
            return (i.z * size.y + i.y) * size.x + i.x;
        }

        public IEnumerable<int3> Range => MapUtility.Range3d(size);
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < items.Length; i++)
                yield return items[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

// public IEnumerable<(T value, int3 i)> Items
// {
//     get
//     {
//         int i = 0;
//         for (int3 z = default; z.z < size.z; z.z++)
//             for (int3 y = z; y.y < size.y; y.y++)
//                 for (int3 x = y; x.x < size.x; x.x++, i++)
//                     yield return (items[i], x);

//     }
// }