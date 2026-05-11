#pragma warning disable UDR0001
using UnityEngine;
using System;
using static pigbrain.game.Boxhead.Navigation.NavMapLayerFear;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapLayerFearItem : MonoBehaviour, IThreat
    {
        [SerializeField][Range(0, 1)] float threat = 1;
        float IThreat.threat => threat;

        NavMapLayerFear layerCache;
        public NavMapLayerFear layer => layerCache ? layerCache : layerCache = NavMap.TryGetLayer<NavMapLayerFear>();
        public float GetThreat() => threat;

        void OnEnable() => layer.Add(transform, this);
        void OnDisable() => layer.Remove(transform);

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            var node = FindAnyObjectByType<NavMap>().GetData().GetNode(transform.position);
            Gizmos.DrawCube(node.position, Vector3.one);
        }
    }
}
