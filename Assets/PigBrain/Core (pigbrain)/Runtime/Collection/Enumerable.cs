using System;
using System.Collections.Generic;
using System.Linq;

namespace pigbrain.core.Collections
{
    public static class EnumerableX
    {
        public static bool IsNullOrEmpty<T>(this IList<T> self) => self == null || self.Count == 0;
        public static bool IsNullOrEmpty<T>(this HashSet<T> self) => self == null || self.Count == 0;

        public static bool Compare<T>(this IList<T> self, IList<T> other, out List<T> add, out List<T> stay, out List<T> remove)
        {
            self.Compare(other, out add, out remove);
            stay = self.Except(add.Concat(remove)).ToList();
            return add.Count > 0 || remove.Count > 0;
        }
        public static bool Compare<T>(this IList<T> self, IList<T> other, out List<T> add, out List<T> remove)
        {
            add = other.Except(self).ToList();
            remove = self.Except(other).ToList();
            return add.Count > 0 || remove.Count > 0;
        }

        public static IEnumerable<T> ForEach<T>(this Array self, Action<T> action)
        {
            foreach (T item in self.Cast<T>()) action(item);
            return self.Cast<T>();
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (T item in self) action(item);
            return self;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T, int> action)
        {
            int counter = 0;
            foreach (T item in self) action(item, counter++);
            return self;
        }

        public static IEnumerable<T> Iterate<T>(this T seed, Func<T, T> next) where T : class
        {
            for (var cur = seed; cur != null; cur = next(cur)) yield return cur;
        }

        public static int IndexOf<T>(this IEnumerable<T> self, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (var item in self)
                if (predicate(item)) return i; else i++;
            return -1;
        }

        public static Dictionary<TKey, TSource[]> ToGroupDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            source.GroupBy(keySelector).ToDictionary(g => g.Key, g => g.ToArray());
    }
}