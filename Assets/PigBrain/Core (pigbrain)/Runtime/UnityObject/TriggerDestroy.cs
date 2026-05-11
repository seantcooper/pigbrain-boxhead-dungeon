using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public sealed class TriggerDestroy : MonoBehaviour
    {
        // void OnTriggerEnter(Collider other) => DestroyObject(other);
        void OnTriggerExit(Collider other) => DestroyObject(other);

        void DestroyObject(Collider other)
        {
            if (other.isTrigger) return;
            // Debug.LogError($"Killed Element {other.gameObject.name} {name}");
            Destroy(other.gameObject);

        }
    }
}