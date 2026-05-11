#pragma warning disable UDR0001
using UnityEngine;
using System;

namespace pigbrain.core.UnityObject
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] bool doNotDestroy = false;

        public static T Instance;
        public static T Self => Instance;
        public static event Action<T> OnInstanceCreated;

        protected bool isQuitting;

        protected virtual void Awake()
        {
            if (Instance && Instance != this)
                Debug.LogError("Singleton already exists, replacing in Awake!");
            Instance = this as T;
            OnInstanceCreated?.Invoke(Instance);
        }

        protected virtual void OnDestroy()
        {
            isQuitting = true;
            if (Instance == this) Instance = null;
        }
    }
}