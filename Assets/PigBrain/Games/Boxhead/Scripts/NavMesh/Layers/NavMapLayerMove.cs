#pragma warning disable UDR0001
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using static pigbrain.game.Boxhead.Navigation.NavMapData;
using System.Collections;
using System;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapLayerMove : NavMapLayerFill<NavMapLayerMove.Cell>
    {
        public class Cell : NavMapLayerCell<Cell>
        {
            public short distance, fillDistance;
            public NavMeshAgent agent;
            public Cell() : base(null) { }
            public Cell(Node node) : base(node) { }
        }

        #region Fill
        readonly Queue<Cell> queue = new(1000);
        protected override IEnumerator FillSession()
        {
            StartProfile();
            void Set(Cell cell, NavMeshAgent agent, int distance = 0, int fillDistance = 100)
            {
                if (cell.sessionID == sessionID && cell.distance <= distance) return;
                queue.Enqueue(cell);
                cell.agent = agent;
                cell.distance = (short)distance;
                cell.fillDistance = (short)fillDistance;
                cell.sessionID = sessionID;
            }

            foreach (var item in items)
                Set(GetCell(item.transform.position), item.agent, 0, (short)item.distance);

            while (queue.Count > 0)
            {
                Cell cell = queue.Dequeue();
                if (cell.fillDistance <= 0) continue;

                foreach (var c in GetConnectors(cell))
                    Set(c, cell.agent, cell.distance + 1, cell.fillDistance - 1);

                if (FillExceeded()) yield return FillWait();
            }
            StopProfile();
        }

        #endregion

        #region Interface
        readonly HashSet<NavMapLayerMoveItem> items = new();
        internal void InternalAdd(NavMapLayerMoveItem item) => items.Add(item);
        internal void InternalRemove(NavMapLayerMoveItem item) => items.Remove(item);

        internal NavMeshAgent InternalGetTarget(NavMeshAgent agent) =>
            map[data.GetNode(agent.transform.position)].agent;

        internal Vector3 InternalGetDirection(NavMeshAgent agent, int steps = 1) =>
            (InternalGetPosition(agent, steps) - agent.transform.position).normalized;

        internal Vector3 InternalGetPosition(NavMeshAgent agent, int steps = 1)
        {
            Cell cell = map[data.GetNode(agent.transform.position)];
            for (int i = steps; i > 0; --i)
                foreach (Cell c in GetConnectors(cell))
                    if (c.distance == 0 || c.distance < cell.distance)
                    {
                        cell = c;
                        break;
                    }
            return cell.node.position;
        }
        #endregion

        protected override void CreateCellMap()
        {
            map = new();
            foreach (NavMapData.Cell cell in navMap.GetData().map.Values)
                foreach (NavMapData.Node node in cell.nodes)
                    map.Add(node, new(node));
        }
    }

    static class NavMapLayerMoveX
    {
        public static void Add(this NavMapLayerMove move, NavMapLayerMoveItem item)
        { if (move) move.InternalAdd(item); }
        public static void Remove(this NavMapLayerMove move, NavMapLayerMoveItem item)
        { if (move) move.InternalRemove(item); }
        public static NavMeshAgent GetTarget(this NavMapLayerMove move, NavMeshAgent agent) => move ? move.InternalGetTarget(agent) : default;
        public static Vector3 GetDirection(this NavMapLayerMove move, NavMeshAgent agent, int steps = 1) => move ? move.InternalGetDirection(agent, steps) : default;
        public static Vector3 GetPosition(this NavMapLayerMove move, NavMeshAgent agent, int steps = 1) => move ? move.InternalGetPosition(agent, steps) : default;
    }
}