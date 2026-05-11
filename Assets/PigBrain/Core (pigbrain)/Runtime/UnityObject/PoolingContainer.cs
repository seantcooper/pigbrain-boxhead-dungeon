using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace pigbrain.core.UnityObject
{
    public class PoolingContainer : MonoBehaviourSingleton<PoolingContainer>
    {
        internal readonly Dictionary<string, Transform> groups = new();
        internal readonly Dictionary<int, PoolContainer> pooling = new();
        public static PoolingContainer TryCreateInstance() =>
            Instance ? Instance : new GameObject($"POOL").AddComponent<PoolingContainer>();
    }

    #region Container
    internal class PoolContainer
    {
        readonly Transform root;
        readonly Queue<MonoBehaviour> free = new();

        static PoolingContainer RootContainer => PoolingContainer.TryCreateInstance();

        public PoolContainer(string name) =>
            root = new GameObject($"POOL: {name}")
            { transform = { parent = RootContainer.transform } }.transform;

        public bool Get<T>(out T instance) where T : MonoBehaviour =>
            instance = free.Count > 0 ? free.Dequeue() as T : null;

        public void Put<T>(T instance) where T : MonoBehaviour =>
            free.Enqueue(instance);

        public T Add<T>(T instance) where T : MonoBehaviour
        {
            instance.transform.parent = root;
            return instance;
        }
        public static implicit operator bool(PoolContainer p) => p && p.root;
    }
    #endregion
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.core.UnityObject
{
    [CustomEditor(typeof(PoolingContainer))]
    public class PoolingContainer_Editor : Editor
    {
        PoolingContainer pcontainer => target as PoolingContainer;
        readonly Dictionary<Transform, int> Maximums = new();

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
                Repaint();

            serializedObject.Update();
            base.OnInspectorGUI();

            foreach (Transform child in pcontainer.transform)
            {
                using var _ = new EditorGUILayout.HorizontalScope();
                EditorGUILayout.LabelField(child.name);

                if (!Maximums.TryGetValue(child, out var maximum)) Maximums[child] = child.childCount;
                else if (child.childCount > maximum) Maximums[child] = child.childCount;

                EditorGUILayout.IntField(child.Cast<Transform>().Count(t => t.gameObject.activeSelf), GUILayout.Width(50));
                EditorGUILayout.IntField(child.Cast<Transform>().Count(t => !t.gameObject.activeSelf), GUILayout.Width(50));
                EditorGUILayout.IntField(Maximums[child], GUILayout.Width(50));
            }
        }
    }
}
#endif
#endregion