using System;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    public class CullingGroupItem : MonoBehaviour
    {
        [SerializeField] internal Setting settings;
        [SerializeField] internal string staticContainerName;

        // All objects: Animator, Renderers, Lights
        [SerializeField][ReadOnly] internal CullingGroupZone zone;
        [SerializeField][HideInInspector] Renderer[] renderers;
        [SerializeField][HideInInspector] bool visible = true;

        public event Action<CullingGroupZone> OnAddedToZone, OnRemovedFromZone;

        void OnValidate() => renderers = GetComponentsInChildren<Renderer>();

        internal void SetZone(CullingGroupZone zone)
        {
            if (zone == null || this.zone == zone) return;
            if (!zone.isVisibleInScene) return;
            if (this.zone) this.zone.RemoveItem(this);
            this.zone = zone;
            if (this.zone) this.zone.AddItem(this);
        }

        IEnumerator Start()
        {
            if (renderers.IsNullOrEmpty()) renderers = GetComponentsInChildren<Renderer>();
            SetVisibility(visible = true);
            if (settings.HasFlag(Setting.Static))
            {
                yield return new WaitUntil(() => CullingGroupManager.Instance.enabled);
                yield return null;
                var zone = CullingGroupManager.Instance.GetZone(transform.position);
                if (zone)
                {
                    var container = zone.transform.Find(staticContainerName);
                    transform.parent = container ? container : zone.transform;
                }
                Destroy(this); // destroy the Cull Item behaviour only
            }
            else CullingGroupManager.Instance.RegisterItem(this);
        }

        void OnDestroy()
        {
            if (settings.HasFlag(Setting.Static)) return;
            if (!CullingGroupManager.Instance) return;
            CullingGroupManager.Instance.UnregisterItem(this);
            if (zone) zone.RemoveItem(this);
        }

        internal void AddedToZone(CullingGroupZone zone) => OnAddedToZone?.Invoke(zone);
        internal void RemovedFromZone(CullingGroupZone zone) => OnRemovedFromZone?.Invoke(zone);

        public bool isVisible => visible;
        public bool isVisibleInScene => !settings.HasFlag(Setting.Hidden) && visible;

        internal void SetVisibility(bool visible)
        {
            this.visible = visible;
            bool setVisible = isVisibleInScene;
            foreach (var renderer in renderers)
                if (renderer)
                    renderer.enabled = setVisible;
        }

        [Flags]
        internal enum Setting
        {
            UseZoneEvents = 1 << 0,
            Hidden = 1 << 1,
            Static = 1 << 2,
        }
    }
}