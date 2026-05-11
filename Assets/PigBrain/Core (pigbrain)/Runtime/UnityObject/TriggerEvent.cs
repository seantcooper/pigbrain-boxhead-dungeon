using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public sealed class TriggerEvent : MonoBehaviour
    {
        [SerializeField] Target target;
        void OnTriggerEnter(Collider other) => GetEvents<ITriggerEnter>().ForEach(t => t.OnTriggerEnter(other));
        void OnTriggerStay(Collider other) => GetEvents<ITriggerStay>().ForEach(t => t.OnTriggerStay(other));
        void OnTriggerExit(Collider other) => GetEvents<ITriggerExit>().ForEach(t => t.OnTriggerExit(other));

        IEnumerable<T> GetEvents<T>()
        {
            var result = Enumerable.Empty<T>();
            if (target.HasFlag(Target.Parents))
                result = result.Concat(GetComponentsInParent<T>());
            if (target.HasFlag(Target.Children))
                result = result.Concat(GetComponentsInChildren<T>());
            if (target == Target.Self)
                result = result.Concat(GetComponents<T>());
            result.Distinct();
            return result;
        }

        [Flags]
        enum Target
        {
            None = 0,
            Self = 1 << 0,
            Children = 1 << 1,
            Parents = 1 << 2,
            // Direct = 1 << 3,
        }
    }

    public interface ITriggerEnter { void OnTriggerEnter(Collider other); }
    public interface ITriggerStay { void OnTriggerStay(Collider other); }
    public interface ITriggerExit { void OnTriggerExit(Collider other); }

}