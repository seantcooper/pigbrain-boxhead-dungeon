using System.Collections;
using System.Collections.Generic;

namespace PigBrain.LegacyCore.Collections
{
    public class FixedList<T> : IEnumerable<T>
    {
        private readonly T[] items;
        public int Count { get; private set; }
        public int Capacity => items.Length;

        public void Reset() => Count = 0;
        public void Clear() => Count = 0;

        public FixedList(int capacity) => items = new T[capacity];
        public void Add(T item) => items[Count++] = item;
        public T this[int index] => items[index];

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return items[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}