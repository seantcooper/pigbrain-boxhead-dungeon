using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Map;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    [DefaultExecutionOrder(-100)]
    public class CullingGroupManager : MonoBehaviourSingleton<CullingGroupManager>
    {
        [SerializeField] CullingGroupZone[] zones;

        [Header("Debug")]
        [SerializeField][ReadOnly] int visibleCount;

        CullingGroup group;

        List<BoundingSphere> spheres;
        List<Bounds> bounds;
        List<CullingGroupZone> remapping;

        BoundsPartition partition;

        void Start()
        {
            zones = gameObject.GetComponentsInChildren<CullingGroupZone>(true);
            group = new() { targetCamera = Camera.main };

            spheres = new();
            bounds = new();
            remapping = new();

            foreach (var zone in zones)
            {
                var culling = zone.CreateCullingGroup(group, remapping.Count);
                spheres.AddRange(culling.spheres);
                bounds.AddRange(culling.bounds);
                for (int j = culling.spheres.Length; j > 0; remapping.Add(zone), --j) ;
            }

            partition = new(4, bounds.ToArray());

            group.SetBoundingSpheres(spheres.ToArray());
            group.SetBoundingSphereCount(spheres.Count);
            group.onStateChanged = OnCullingStateChanged;

            zones.ForEach(zones => zones.StartCullingVisibility());
            // StartCoroutine(UpdateItems());
        }


        void Update()
        {
            visibleCount = zones.Count(z => z.isVisibleInScene);
            UpdateZones();
        }
        void UpdateZones()
        {
            if (!isUpdating) StartCoroutine(UpdateZonesRoutine());
        }

        bool isUpdating;
        IEnumerator UpdateZonesRoutine()
        {
            const int ItemUpdateCount = 100;
            using var updating = new ScopeState(() => isUpdating = true, () => isUpdating = false);
            items.Flush();
            for (int index = 0; items.Count > 0;)
            {
                foreach (var item in items)
                {
                    if (item) item.SetZone(GetZone(item.transform.position));
                    if ((++index) % ItemUpdateCount == 0)
                        yield return null;
                }
                items.Flush();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            group?.Dispose();
        }

        void OnCullingStateChanged(CullingGroupEvent e) =>
            remapping[e.index].UpdateCullingVisibility();

        #region Items
        readonly IterateList<CullingGroupItem> items = new();
        internal void RegisterItem(CullingGroupItem item) => items.Add(item);
        internal void UnregisterItem(CullingGroupItem item) => items.Remove(item);

        public CullingGroupZone GetZone(Vector3 position)
        {
            int i = partition.GetIndex(position);
            return i < 0 ? null : remapping[i];
        }
        #endregion

        #region  Gizmos
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;

        void OnDrawGizmosSelected()
        {
            if (!showGizmos || spheres == null) return;
            foreach (var sphere in spheres) Gizmos.DrawWireSphere(sphere.position, sphere.radius);
            if (bounds == null) return;
            foreach (var b in bounds) GizmosUtility.DrawWireCube(b.center, b.size, 0.25f);
        }
        #endregion
    }
}
