using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class PoolGroup : MonoBehaviour
    {
        [SerializeField] internal string id;
        void OnValidate() { if (string.IsNullOrEmpty(id)) id = name; }

        void Start()
        {
            if (string.IsNullOrEmpty(id)) id = name;
            AddToGroup(this);
        }

        static PoolingContainer RootContainer => PoolingContainer.TryCreateInstance();

        public static Transform GetGroupContainer(string id) =>
            RootContainer.groups.TryGetValue(id, out Transform container) && container ? container
                : RootContainer.groups[id] = CreateContainer(id);

        static Transform CreateContainer(string id) => new GameObject($"GROUP: {id}")
        { transform = { parent = PoolingContainer.TryCreateInstance().transform } }.transform;

        internal static void AddToGroup(PoolGroup group) =>
            group.transform.parent = GetGroupContainer(group.id);
    }
}