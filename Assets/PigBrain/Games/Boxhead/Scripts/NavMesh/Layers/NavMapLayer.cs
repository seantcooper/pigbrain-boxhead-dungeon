#pragma warning disable UDR0001
using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;
using System.Collections.Generic;
using pigbrain.core.Collections;
using static pigbrain.game.Boxhead.Navigation.NavMapData;
using System;
using pigbrain.core.Analysis;
using System.Collections;

namespace pigbrain.game.Boxhead.Navigation
{
    #region Base Layer
    public class NavMapLayer : MonoBehaviour
    {
        [SerializeField][ReadOnly] protected int sessionID = 0;
        [SerializeField][ReadOnly] protected string time;

        protected NavMap navMap;
        protected NavMapData data => navMap ? navMap.GetData() : null;
        internal virtual void OnAddedToMap(NavMap navMap) => this.navMap = navMap;

        long tick;
        protected void StartProfile() => tick = Profiler.Start();
        protected void StopProfile() => time = $"{Profiler.Stop(tick).TotalMilliseconds}ms";
    }
    #endregion

    #region Layer Fill
    public abstract class NavMapLayerFill<T> : NavMapLayer where T : NavMapLayerCell<T>
    {
        [SerializeField][Range(0.01f, 1)] protected float fillRatio = 1 / 60f;
        [SerializeField][ReadOnly] protected int fillIndex = 0;
        [SerializeField][ReadOnly] protected int fillCount = 0;

        protected Dictionary<Node, T> map;
        protected T GetCell(Vector3 p) => map[data.GetNode(p)];

        readonly T[] buffer = new T[6];
        protected ArraySegment<T> GetConnectors(T cell)
        {
            int count = cell.node.connectors.Count;
            for (int i = 0; i < count; buffer[i] = map[cell.node.connectors[i]], i++) ;
            return new ArraySegment<T>(buffer, 0, count);
        }

        protected void StartFill()
        {
            fillCount = Mathf.RoundToInt(navMap.GetData().map.Count * fillRatio);
            sessionID++;
            fillIndex = 0;
        }
        protected void StopFill() { }

        IEnumerator Fill()
        {
            while (true)
            {
                StartFill();
                yield return FillSession();
                StopFill();
                yield return new WaitForNextUpdate();
            }
        }

        protected bool FillExceeded() => (++fillIndex) % fillCount == 0;

        protected IEnumerator FillWait()
        {
            StopProfile();
            yield return new WaitForNextUpdate();
            StartProfile();
        }

        protected virtual IEnumerator FillSession() { yield break; }

        internal override void OnAddedToMap(NavMap navMap)
        {
            base.OnAddedToMap(navMap);
            CreateCellMap();
            StartCoroutine(Fill());
        }

        protected abstract void CreateCellMap();
    }
    #endregion

    #region Layer Cell
    public class NavMapLayerCell
    {
        public Node node;
        public int sessionID;
    }

    public class NavMapLayerCell<T> : NavMapLayerCell
    {
        protected NavMapLayerCell() { }
        public NavMapLayerCell(Node node) { this.node = node; }
    }
    #endregion
}
