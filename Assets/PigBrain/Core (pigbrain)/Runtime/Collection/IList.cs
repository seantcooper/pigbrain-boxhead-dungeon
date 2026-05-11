using System.Collections.Generic;
using UnityEngine;

namespace pigbrain.core.Collections
{
    public class IterateList<T> : List<T>
    {
        readonly List<T> add = new(), remove = new();
        readonly HashSet<T> addSet = new(), removeSet = new();

        public void Flush()
        {
            if (add.Count > 0)
            {
                base.AddRange(add);
                add.Clear(); addSet.Clear();
            }

            if (remove.Count > 0)
            {
                remove.ForEach(e => base.Remove(e));
                remove.Clear(); removeSet.Clear();
            }
        }

        public new void Add(T item)
        {
            if (removeSet.Remove(item)) remove.Remove(item);
            else if (addSet.Add(item)) add.Add(item);
        }

        public new bool Remove(T item)
        {
            if (addSet.Remove(item)) add.Remove(item);
            else if (removeSet.Add(item)) remove.Add(item);
            return true;
        }

        public new bool RemoveAt(int index) => throw new System.Exception("Not supported");
        public new void Clear() => throw new System.Exception("Not supported");
        public new void Insert(int index, T item) => throw new System.Exception("Not supported");
        public new void AddRange(IEnumerable<T> collection) => throw new System.Exception("Not supported");
        public new void InsertRange(int index, IEnumerable<T> collection) => throw new System.Exception("Not supported");
        public new void RemoveRange(int index, int count) => throw new System.Exception("Not supported");
        public new void Reverse() => throw new System.Exception("Not supported");
        public new void Sort() => throw new System.Exception("Not supported");
        public new void Sort(System.Comparison<T> comparison) => throw new System.Exception("Not supported");
        public new void Sort(System.Collections.Generic.IComparer<T> comparer) => throw new System.Exception("Not supported");
    }
}