using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Map;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Map
{
    public class RectPartition
    {
        readonly Rect[] rects;
        readonly float gridSize;
        readonly Rect fullRect;

        readonly float2 min;
        readonly float invGridSize;

        readonly Dictionary<int2, List<int>> grid = new();

        public RectPartition(float gridSize, Rect[] rects)
        {
            this.rects = rects;
            this.gridSize = gridSize;
            this.fullRect = rects.Encapsulate().Expand(1);
            this.min = this.fullRect.min;
            this.invGridSize = 1f / gridSize;
            BuildPartition();
        }

        int2 Cell(float2 pos) => (int2)((pos - min) * invGridSize);
        float2 Pos(int2 cell) => (float2)cell * gridSize + min;

        void BuildPartition()
        {
            for (int i = 0; i < rects.Length; i++)
            {
                Rect b = rects[i];
                int2 min = Cell(b.min), max = Cell(b.max);
                MapUtility.Range2d(min, max + 1).ForEach(key =>
                {
                    if (!grid.TryGetValue(key, out var list))
                        grid[key] = list = new List<int>(4);
                    list.Add(i);
                });
            }
        }

        public int GetIndex(float x, float y) => GetIndex(new(x, y));
        public int GetIndex(float2 position)
        {
            int2 cell = Cell(position);
            if (!grid.TryGetValue(cell, out var list)) return -1;
            if (list.Count <= 1)
                foreach (var i in list)
                    if (rects[i].Contains(position))
                        return i;
            return list[0];
        }
        public List<int> GetIndices(float2 position)
        {
            int2 cell = Cell(position);
            return grid.TryGetValue(cell, out var list) ? list : null;
        }
    }
}
