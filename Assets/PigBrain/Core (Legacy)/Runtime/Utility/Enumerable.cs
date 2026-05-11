// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using Unity.Mathematics;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Utility
// {
//     public static class EnumerableUtility
//     {
//         public static IEnumerable<T> Iterate<T>(this T seed, Func<T, T> next) where T : class
//         {
//             for (var cur = seed; cur != null; cur = next(cur)) yield return cur;
//         }

//         public static (IEnumerable<T> add, IEnumerable<T> remove) Presence<T>(this IEnumerable<T> self, IEnumerable<T> other) =>
//             (other.Except(self), self.Except(other));

//         public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
//         {
//             foreach (T item in self) action(item);
//             return self;
//         }

//         public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T, int> action)
//         {
//             int counter = 0;
//             foreach (T item in self) action(item, counter++);
//             return self;
//         }

//         // public static IEnumerable<int2> Range2d(int count) => Range2d(0, 0, count, count);
//         // public static IEnumerable<int2> Range2d(int start, int count) => Range2d(start, start, count, count);
//         // public static IEnumerable<int2> Range2d(int2 start, int2 count) => Range2d(start.x, start.y, count.x, count.y);
//         // public static IEnumerable<int2> Range2d(RectInt rect) => Range2d(rect.x, rect.y, rect.width, rect.height);
//         // public static IEnumerable<int2> Range2d(int xStart, int yStart, int xCount, int yCount)
//         // {
//         //     for (int y = yStart, ny = yStart + yCount; y < ny; y++)
//         //         for (int x = xStart, nx = xStart + xCount; x < nx; x++)
//         //             yield return new int2(x, y);
//         // }

//         // public static IEnumerable<int3> Range3d(int count) => Range3d(int3.zero, new int3(count));
//         // public static IEnumerable<int3> Range3d(int start, int count) => Range3d(new int3(start), new int3(count));
//         // public static IEnumerable<int3> Range3d(int3 start, int3 count)
//         // {
//         //     for (int3 z = start, n = start + count; z.z < n.z; z.z++)
//         //         for (int3 y = z; y.y < n.y; y.y++)
//         //             for (int3 x = y; x.x < n.x; x.x++)
//         //                 yield return x;
//         // }
//     }
// }
