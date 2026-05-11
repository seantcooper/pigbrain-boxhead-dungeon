using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Map
{
    public class BoundsPartition
    {
        readonly Bounds[] bounds;
        readonly float gridSize;
        readonly Bounds fullBounds;

        readonly float3 min;
        readonly float invGridSize;

        readonly Dictionary<int3, List<int>> grid = new();

        public BoundsPartition(float gridSize, Bounds[] bounds)
        {
            this.bounds = bounds;
            this.gridSize = gridSize;
            this.fullBounds = bounds.Encapsulate().Inflate(1);

            this.min = this.fullBounds.min;
            this.invGridSize = 1f / gridSize;
            BuildPartition();
        }

        int3 Cell(float3 pos) => (int3)((pos - min) * invGridSize);
        float3 Pos(int3 cell) => (float3)cell * gridSize + min;

        void BuildPartition()
        {
            for (int i = 0; i < bounds.Length; i++)
            {
                Bounds b = bounds[i];
                int3 min = Cell(b.min), max = Cell(b.max);
                MapUtility.Range3d(min, max + 1).ForEach(key =>
                {
                    if (!grid.TryGetValue(key, out var list))
                        grid[key] = list = new List<int>(4);
                    list.Add(i);
                });
            }
        }

        public int GetIndex(Vector3 position)
        {
            int3 cell = Cell(position);
            if (!grid.TryGetValue(cell, out var list)) return -1;
            if (list.Count > 1)
                foreach (var i in list)
                    if (bounds[i].Contains(position))
                        return i;
            return list[0];
        }
    }
}
