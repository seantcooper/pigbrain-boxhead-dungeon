#pragma warning disable UDR0005
using System;
using UnityEngine;
using UnityEngine.Events;

namespace PigBrain.LegacyCore.Utility
{
    public class BehaviourEvents : MonoBehaviour
    {
        public UnityEvent onStartUE;
        public UnityEvent onDestroyUE;
        void Start() => onStartUE?.Invoke();
        void OnDestroy() => onDestroyUE?.Invoke();
    }
}
