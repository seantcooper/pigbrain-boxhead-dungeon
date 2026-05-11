using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using PigBrain.Generated;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace pigbrain.core.UnityObject
{
    public static class ObjectUtility
    {
        public static void SetEnabled(this Component component, bool state)
        {
            if (component is Light light) light.enabled = state;
            else if (component is Renderer renderer) renderer.enabled = state;
            else if (component is Collider collider) collider.enabled = state;
            else if (component is Behaviour behaviour) behaviour.enabled = state;
            else throw new System.NotImplementedException($"{component.GetType()} not implemented!");
        }

        public static void SetLayer(this GameObject root, GameLayer layer) => root.SetLayer((int)layer);
        public static void SetLayer(this GameObject root, int layer) =>
            root.GetComponentsInChildren<Transform>().ForEach(t => t.gameObject.layer = layer);

        public static void SetStatic(this GameObject root) =>
            root.GetComponentsInChildren<Transform>().ForEach(t => t.gameObject.isStatic = true);

        #region Try Add
        public static bool TryAddComponent<T>(this MonoBehaviour behaviour, out T result) where T : Component =>
            behaviour.transform.TryAddComponent<T>(out result);
        public static bool TryAddComponent<T>(this GameObject gameObject, out T result) where T : Component =>
            gameObject.transform.TryAddComponent<T>(out result);
        public static bool TryAddComponent<T>(this Transform transform, out T result) where T : Component
        {
            if (transform.TryGetComponent(out result)) return false;
            result = transform.gameObject.AddComponent<T>();
            return true;
        }
        #endregion

        #region Try Get Children
        public static bool TryGetComponentInChildren<T>(this Component component, out T result, bool includeInactive = false) where T : Component =>
            component.transform.TryGetComponentInChildren<T>(out result, includeInactive);
        public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T result, bool includeInactive = false) where T : Component =>
            gameObject.transform.TryGetComponentInChildren<T>(out result, includeInactive);
        public static bool TryGetComponentInChildren<T>(this Transform transform, out T result, bool includeInactive = false) where T : Component =>
            result = transform.GetComponentInChildren<T>(includeInactive);
        #endregion

        #region Try Get Parent
        public static bool TryGetComponentInParent<T>(this Component component, out T result, bool includeInactive = false) where T : Component =>
            component.transform.TryGetComponentInParent<T>(out result, includeInactive);
        public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T result, bool includeInactive = false) where T : Component =>
            gameObject.transform.TryGetComponentInParent<T>(out result, includeInactive);
        public static bool TryGetComponentInParent<T>(this Transform transform, out T result, bool includeInactive = false) where T : Component =>
            result = transform.GetComponentInParent<T>(includeInactive);
        #endregion

        public static List<Scene> GetScenes()
        {
            var result = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i) is Scene scene && scene.isLoaded) result.Add(scene);
            return result;
        }

        public static List<T> GetSceneItems<T>()
        {
            var result = new List<T>();
            result.AddRange(GetScenes()
                .SelectMany(s => s.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren<T>(true))));
            return result;
        }

        #region Find
        public static IEnumerable<T> Find<T>(this GameObject gameObject, GameTag tag, bool includeInactive = false) where T : UnityEngine.Component
        {
            string stag = $"{tag}";
            return gameObject.GetComponentsInChildren<T>(includeInactive)
                .Where(t => t.gameObject.CompareTag(stag));
        }
        #endregion

    }
}
