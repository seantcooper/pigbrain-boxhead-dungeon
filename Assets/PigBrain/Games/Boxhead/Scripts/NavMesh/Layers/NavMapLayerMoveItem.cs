#pragma warning disable UDR0001
using UnityEngine;
using System;
using pigbrain.core.AI;
using UnityEngine.AI;

namespace pigbrain.game.Boxhead.Navigation
{
    [DisallowMultipleComponent]
    public class NavMapLayerMoveItem : MonoBehaviour
    {
        public NavMeshAgent agent;
        public int distance = 100;

        void OnValidate() => agent = GetComponent<NavMeshAgent>();

        void OnEnable()
        {
            if (!agent) agent = GetComponent<NavMeshAgent>();
            if (agent) NavMap.TryGetLayer<NavMapLayerMove>().Add(this);
        }

        void OnDisable()
        {
            if (agent) NavMap.TryGetLayer<NavMapLayerMove>().Remove(this);
        }

    }
}
