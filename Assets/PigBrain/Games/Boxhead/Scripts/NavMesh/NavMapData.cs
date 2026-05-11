#pragma warning disable UDR0001
using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapData : ScriptableObject
#if UNITY_EDITOR
    , ISerializationCallbackReceiver
#endif
    {
        public const float EPS = 1e-4f;
        [SerializeField] float size;
        [SerializeField] int2 mapSize;
        [SerializeField] Vector3 min;
        internal Dictionary<int2, Cell> map;
        internal NavMapDataTriangleSampler sampler;

        #region Build
        public void Build(NavMap navMap, float radiusScale)
        {
            size = NavMesh.GetSettingsByID(navMap.surface.agentTypeID).agentRadius * radiusScale * 2;
            sampler = new(navMap.surface, size);
            sampler.SampleNavMesh();
            mapSize = WorldToIndex(sampler.bounds.size) + 2;
            min = sampler.bounds.min;
            map = sampler.map;
            Report();
        }
        #endregion

        #region Report
        void Report()
        {
#if UNITY_EDITOR
            Debug.Log("REPORT >>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            Debug.Log($"    Triangulation: indices {sampler.triangles.Length} vertices {sampler.vertices.Length}");

            HashSet<Node> visited = new();
            List<List<Node>> islands = new();

            var nodes = map.Values.SelectMany(cell => cell.nodes).ToArray();

            foreach (var cell in map.Values)
            {
                if (cell.nodes.Count > 2)
                {
                    Debug.LogWarning($"    cell @ '{cell.index}' has {cell.nodes.Count} layers");
                }
            }

            for (int start = 0; start < nodes.Length; start++)
            {
                var node = nodes[start];
                if (visited.Contains(node)) continue;

                List<Node> island = new();
                Stack<Node> stack = new();

                stack.Push(node);
                visited.Add(node);

                while (stack.Count > 0)
                {
                    Node n = stack.Pop();
                    island.Add(n);
                    foreach (var c in n.connectors)
                        if (visited.Add(c)) stack.Push(c);
                }
                islands.Add(island);
            }

            // var gislands = new int[nodes.Length];
            // for (int i = 0; i < islands.Count; i++)
            //     foreach (int ni in islands[i])
            //         gislands[ni] = i;

            if (islands.Count > 1)
            {
                var error = $"    NavMap islands: {islands.Count} [{string.Join(",", islands.Select(i => i.Count))}]";
                Debug.LogWarning($"    {error}");
                // if (islands.Count(i => i.Count > 500) > 1)
                //     Debug.LogError($"Division in nodes, multiple large islands!");
                // for (int i = 0; i < islands.Count; i++)
                // {
                //     var sample = islands[i].Take(10)
                //         .Select(idx => nodes[idx].position.ToString("F2"));
                // }
            }
            else Debug.Log($"    NavMap OK: {islands[0].Count} nodes");
#endif
        }
        #endregion

        public int2 WorldToIndex(Vector3 world)
        {
            int x = Mathf.FloorToInt((world.x - min.x) / size + EPS);
            int y = Mathf.FloorToInt((world.z - min.z) / size + EPS);
            return new(x, y);
        }

        #region Node
        public bool GetCell(int2 index, out Cell cell) => map.TryGetValue(index, out cell);
        public Node GetNode(Vector3 v)
        {
            int2 index = WorldToIndex(v);

            if (!map.TryGetValue(index, out Cell cell))
                foreach (int2 o in Array2.RadialSearch)
                    if (map.TryGetValue(index + o, out cell)) break;

            if (cell.nodes.Count == 1) return cell.nodes[0];

            (Node node, float d) best =
                (cell.nodes[0], Mathf.Abs(cell.nodes[0].position.y - v.y));

            for (int i = 1; i < cell.nodes.Count; i++)
            {
                float d = Mathf.Abs(cell.nodes[i].position.y - v.y);
                if (d < best.d) best = (cell.nodes[i], d);
            }
            return best.node;
        }

#if UNITY_EDITOR
        [SerializeField] List<SerializeNode> snodes = new();
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            snodes.Clear();
            if (map == null) return;

            var nodeIndex = new Dictionary<Node, int>();
            int id = 0;

            foreach (var kv in map) foreach (var n in kv.Value.nodes)
                nodeIndex[n] = id++;

            foreach (var kv in map) foreach (var n in kv.Value.nodes)
            {
                var sn = new SerializeNode
                {
                    index = kv.Key,
                    position = n.position,
                    triangleIndex = n.triangleIndex,
                    connectors = new int[n.connectors.Count]
                };

                for (int i = 0; i < n.connectors.Count; i++)
                    sn.connectors[i] = nodeIndex[n.connectors[i]];

                snodes.Add(sn);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            map = new Dictionary<int2, Cell>();
            var nodes = new List<Node>(snodes.Count);

            for (int i = 0; i < snodes.Count; i++)
            {
                var sn = snodes[i];
                var node = new Node(sn.position, sn.triangleIndex);
                nodes.Add(node);

                if (!map.TryGetValue(sn.index, out var cell))
                    map[sn.index] = cell = new Cell(sn.index);

                cell.nodes.Add(node);
            }

            // reconnect
            for (int i = 0; i < snodes.Count; i++) foreach (var ci in snodes[i].connectors)
                nodes[i].connectors.Add(nodes[ci]);
        }
        [Serializable]
        public class SerializeNode
        {
            public int2 index;
            public Vector3 position;
            public int triangleIndex;
            public int[] connectors;
        }
#endif

        public class Cell
        {
            public int2 index;
            public List<Node> nodes = new();
            public Cell(int2 index) => this.index = index;
            public static implicit operator bool(Cell empty) => empty != null;
        }

        public class Node
        {
            public Vector3 position;
            public int triangleIndex;
            public List<Node> connectors = new();
            public Node(Vector3 position, int triangleIndex)
            {
                this.position = position;
                this.triangleIndex = triangleIndex;
            }
            public static implicit operator bool(Node empty) => empty != null;
        }

        #endregion
    }
}
