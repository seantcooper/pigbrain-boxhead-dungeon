using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace pigbrain.game.Boxhead.Environment
{
    public sealed class AssetLoader : MonoBehaviourSingleton<AssetLoader>
    {
        // [SerializeField] string[] autoload;
        readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> scenes = new();

#if UNITY_EDITOR
        void Start() => UnloadScenes();
#endif

        public static void Load(string key, Action<float> progress = null, Action<Scene> completed = null) =>
            Instance.LoadInternal(key, progress, completed);

        public static Task Unload(string key) =>
            Instance.UnloadInternal(key);

        async void LoadInternal(string key, Action<float> progress, Action<Scene> completed)
        {
            if (scenes.ContainsKey(key)) return;

            var locations = await Addressables.LoadResourceLocationsAsync(key).Task;
            if (locations == null || locations.Count == 0)
            {
                Debug.LogError($"AssetLoader: Invalid Addressable key '{key}'");
                return;
            }

            var handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive, activateOnLoad: true);

            scenes[key] = handle;

            // Report loading progress while the scene loads
            while (!handle.IsDone)
            {
                progress?.Invoke(handle.PercentComplete);
                await Task.Yield();
            }

            // Ensure completion
            await handle.Task;
            progress?.Invoke(1f);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var scene = handle.Result.Scene;

                // // Ensure the loaded scene becomes the active scene immediately
                // if (scene.IsValid() && scene.isLoaded)
                //     SceneManager.SetActiveScene(scene);

                completed?.Invoke(scene);
            }
            else scenes.Remove(key);
        }

        async Task UnloadInternal(string key)
        {
            if (!scenes.TryGetValue(key, out var handle)) return;

            await Addressables.UnloadSceneAsync(handle).Task;
            scenes.Remove(key);
        }

        void UnloadScenes()
        {
#if UNITY_EDITOR
            var bootstrap = gameObject.scene;
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded || s == bootstrap) continue;
                // bool keep = false;
                // foreach (var key in autoload)
                //     if (!string.IsNullOrEmpty(key) &&
                //         s.name.Equals(key, StringComparison.OrdinalIgnoreCase))
                //     { keep = true; break; }
                // if (!keep) SceneManager.UnloadSceneAsync(s);
                SceneManager.UnloadSceneAsync(s);
            }
#endif
        }
    }
}