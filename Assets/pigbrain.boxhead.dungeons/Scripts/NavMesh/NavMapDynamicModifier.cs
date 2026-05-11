#pragma warning disable UDR0001
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace pigbrain.game.Boxhead.Navigation
{
    public class NavMapDynamicModifier : MonoBehaviour
    {
        [SerializeField] NavMeshModifier modifier;

        public readonly List<Collider> colliders = new();
        void OnValidate()
        {
            if (!modifier) modifier = GetComponentInParent<NavMeshModifier>();
        }
    }
}
