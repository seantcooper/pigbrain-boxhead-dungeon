using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Map
{
    public static class MapUtility
    {
        #region Range 2D
        public static IEnumerable<int2> Range2d(this int2 count) => Range2d(int2.zero, count - 1);
        // public static IEnumerable<int2> Range2d(RectInt rect) => Range2d(new(rect.min.x, rect.min.y), new(rect.max.x, rect.max.y));
        public static IEnumerable<int2> Range2d(int2 min, int2 max)
        {
            for (int2 y = min; y.y <= max.y; y.y++)
                for (int2 x = y; x.x <= max.x; x.x++)
                    yield return x;
        }
        #endregion

        #region Range 3D
        public static IEnumerable<int3> Range3d(int3 count) => Range3d(int3.zero, count - 1);
        public static IEnumerable<int3> Range3d(int3 min, int3 max)
        {
            for (int3 z = min; z.z <= max.z; z.z++)
                for (int3 y = z; y.y <= max.y; y.y++)
                    for (int3 x = y; x.x <= max.x; x.x++)
                        yield return x;
        }
        #endregion
    }
}