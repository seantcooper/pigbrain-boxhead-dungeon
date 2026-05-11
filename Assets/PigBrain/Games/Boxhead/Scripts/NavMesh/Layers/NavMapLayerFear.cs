#pragma warning disable UDR0001
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using pigbrain.core.Collections;
using static pigbrain.game.Boxhead.Navigation.NavMapData;
using System.Collections;
using pigbrain.core.Geom;
using System;
using static pigbrain.game.Boxhead.Navigation.NavMapLayerFear;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapLayerFear : NavMapLayerFill<NavMapLayerFear.Cell>
    {
        [Header("Fear")]
        [SerializeField][Range(0, 1)] float decay = 0.95f;

        #region Items
        readonly Dictionary<Transform, IThreat> items = new();
        internal void InternalAdd(Transform item, IThreat threat) => items.Add(item, threat);
        internal void InternalRemove(Transform item) => items.Remove(item);
        #endregion

        #region Cell
        public class Cell : NavMapLayerCell<Cell>
        {
            public float threat;
            public Cell() : base(null) { }
            public Cell(Node node) : base(node) { }
        }
        #endregion

        #region Fill
        readonly Queue<Cell> queue = new(1000);
        readonly HashSet<Cell> track = new(1000);

        protected override IEnumerator FillSession()
        {
            StartProfile();
            foreach (var cell in track) cell.threat = 0;

            track.Clear();
            void Set(Cell cell, float threat)
            {
                if (cell.sessionID == sessionID && cell.threat >= threat) return;
                track.Add(cell);
                queue.Enqueue(cell);
                cell.threat = threat;
                cell.sessionID = sessionID;
            }

            foreach (var kv in items)
            {
                var cell = GetCell(kv.Key.position);
                // if (kv.Value.threat <= cell.threat) continue;
                Set(cell, kv.Value.threat);
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                float next = cell.threat * decay;
                if (next < 0.001f) continue;
                foreach (var n in GetConnectors(cell))
                    Set(n, next);
                if (FillExceeded()) yield return FillWait();
            }
            StopProfile();
            yield break;
        }
        #endregion

        #region Search
        public float GetThreat(Vector3 position) => map[data.GetNode(position)].threat;

        public Vector3 GetDirection(Vector3 position, float threshold = 0.01f, int steps = 1) =>
            GetPosition(position, out Vector3 destination, threshold, steps)
                ? (destination - position).normalized : Vector3.zero;

        public bool GetPosition(Vector3 position, out Vector3 destination, float threshold = 0.01f, int steps = 1)
        {
            destination = Vector3.zero;
            Cell cell = map[data.GetNode(position)], acell = cell;
            if (acell.threat < threshold) return false;
            for (int i = steps; i > 0; --i)
                foreach (Cell ncell in GetConnectors(cell))
                    if (ncell.threat < acell.threat) { acell = ncell; break; }
            destination = acell.node.position;
            return cell != acell;
        }
        #endregion

        #region Gizmos
        void OnDrawGizmos() { if (navMap && navMap.showGizmos) DrawGizmos(); }
        void OnDrawGizmosSelected() { if (navMap && !navMap.showGizmos) DrawGizmos(); }
        void DrawGizmos()
        {
            if (!data) return;
            foreach (var kv in map)
            {
                if (kv.Value.threat < 0.002f) continue;
                Gizmos.color = Color.red.WithA(kv.Value.threat);
                var size = Mathf.Lerp(0.25f, 0.5f, Mathf.Clamp01(kv.Value.threat));
                Gizmos.DrawCube(kv.Key.position, Vector3.one * size);
            }
        }

        protected override void CreateCellMap()
        {
            map = new();
            foreach (NavMapData.Cell cell in navMap.GetData().map.Values)
                foreach (NavMapData.Node node in cell.nodes)
                    map.Add(node, new(node));
        }

        #endregion

        public interface IThreat
        {
            float threat { get; }
        }
    }

    static class NavMapLayerFearX
    {
        public static void Add(this NavMapLayerFear fear, Transform item, IThreat threat)
        { if (fear) fear.InternalAdd(item, threat); }

        public static void Remove(this NavMapLayerFear fear, Transform item)
        { if (fear) fear.InternalRemove(item); }
    }
}
