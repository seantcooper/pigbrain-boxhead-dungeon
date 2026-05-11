using System;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public sealed class DestroyedEvent : MonoBehaviour
    {
        public event Action<DestroyedEvent> onDestroyed;
        void OnDestroy() => onDestroyed?.Invoke(this);
    }
}