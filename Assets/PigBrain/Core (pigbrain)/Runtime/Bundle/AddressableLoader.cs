using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace pigbrain.core.Bundle
{
    public class AddressableLoader : MonoBehaviour
    {
        public string key = "";
    }

    public abstract class AddressableLoader<T> : AddressableLoader where T : Object
    {
        // [HelpBox("To get excluded from build, it must be an addressable in a prefab.")]
        // [Header("Loader")]
        AsyncOperationHandle<T> handle;
        static readonly Dictionary<string, AsyncOperationHandle> Cache = new();

        IEnumerator Start()
        {
            yield return Addressables.InitializeAsync();

            if (Cache.TryGetValue(key, out var cached))
            {
                handle = cached.Convert<T>();
                OnStartLoad(handle);
                if (handle.IsDone) OnCompleted(handle);
                else handle.Completed += OnCompleted;
            }
            else
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                OnStartLoad(handle);
                yield return new WaitForSeconds(0.5f);
                Cache[key] = handle;
                handle.Completed += OnCompleted;
            }
        }

        void OnCompleted(AsyncOperationHandle<T> h)
        {
            // Console.Print($"LOAD {key} → {h.Status} ({typeof(T)})");
            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Failed to load '{key}'!");
                OnError(h);
                return;
            }
            OnLoaded(h);
        }

        protected virtual void OnStartLoad(AsyncOperationHandle<T> handle) { }
        protected abstract void OnLoaded(AsyncOperationHandle<T> handle);
        protected virtual void OnError(AsyncOperationHandle<T> handle) { }

        void OnDestroy()
        {
            if (handle.IsValid() && !Cache.ContainsKey(key))
                Addressables.Release(handle);
        }
    }
}


// var loc = Addressables.LoadResourceLocationsAsync(key);
// loc.Completed += h => Debug.Log($"{key} locations: {h.Result.Count}");
