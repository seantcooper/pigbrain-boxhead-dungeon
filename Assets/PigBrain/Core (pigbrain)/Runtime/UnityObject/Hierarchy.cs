using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;


namespace pigbrain.core.UnityObject
{
    public class Hierarchy
    {
        public readonly Transform root;
        public readonly List<Transform> transforms = new();
        public readonly List<string> paths = new();

        public Hierarchy(Transform root)
        {
            this.root = root;
            Fill();
        }

        void Fill()
        {
            void Recursive(Transform transform, string path)
            {
                transforms.Add(transform);
                paths.Add(path);
                foreach (Transform child in transform)
                    Recursive(child, $"{path}/{child.name}");
            }
            Recursive(root, root.name);
        }

        public int GetIndex(Transform transform) => transforms.IndexOf(transform);
        public string GetPath(Transform transform) => paths[GetIndex(transform)];
        public Transform GetTransform(int i) => transforms[i];
        public Transform GetTransform(string path) => transforms[paths.IndexOf(path)];

        public static T[] Remap<T>(Transform from, Transform to, T[] components) where T : Component
        {
            var fromHierarchy = new Hierarchy(from.transform);
            var toHierarchy = new Hierarchy(to.transform);
            T[] result = new T[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                var index = fromHierarchy.GetIndex(components[i].transform);
                result[i] = toHierarchy.GetTransform(index).GetComponent<T>();
            }
            return result;
        }
    }
}