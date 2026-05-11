using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    public class CullingGroupZone : MonoBehaviour
    {
        [SerializeField][ReadOnly] bool visible = true;
        [SerializeField][ReadOnly] bool hidden = false;

        [Header("Activation")]
        [SerializeField] public List<GameObject> deactivationObjects = new();
        [SerializeField] Component[] deactivationComponents;

        [Header("Visibles")]
        [SerializeField] Renderer[] renderers;
        [SerializeField] Light[] lights;
        [SerializeField] Bounds fullBounds;

        [Header("Debug")]
        [SerializeField][ReadOnly] bool lastVisibleState = true;

        public event Action<bool> OnVisibilityChange;
        public event Action<CullingGroupItem> OnItemAdded, OnItemRemoved;

        readonly HashSet<CullingGroupItem> items = new();

        void OnEnable() => SetVisibility(isVisible);

        #region  Visibility
        public IEnumerable<GameObject> visibleObjects =>
            renderers.Cast<Component>().Concat(lights).Where(c => c)
                .Select(c => c.gameObject).Distinct();

        public bool isVisible => visible;
        public bool isVisibleInScene => hidden == false && visible;

        public void Hide() { hidden = true; SetVisibility(isVisible); }
        public void Show() { hidden = false; SetVisibility(isVisible); }

        internal void SetVisibility(bool visible)
        {
            if (!CullingGroupManager.Instance.enabled) return;

            // Cache missing local items
            if (lights.IsNullOrEmpty()) lights = GetComponentsInChildren<Light>();
            if (renderers.IsNullOrEmpty()) renderers = GetComponentsInChildren<Renderer>();

            this.visible = visible;
            bool setVisible = isVisibleInScene;

            if (lastVisibleState == isVisibleInScene) return;
            lastVisibleState = isVisibleInScene;

            // Debug.Log($"Zone '{name}' Set Visible actual/set{this.visible}/{setVisible} gameObjects: {!deactivationObjects.IsNullOrEmpty()}");
            if (!deactivationObjects.IsNullOrEmpty())
                foreach (var gameObject in deactivationObjects) if (gameObject) gameObject.SetActive(setVisible);
            if (!deactivationComponents.IsNullOrEmpty())
                foreach (var component in deactivationComponents) if (component) component.SetEnabled(setVisible);
            if (!renderers.IsNullOrEmpty())
                foreach (var render in renderers) if (render) render.enabled = setVisible;
            if (!lights.IsNullOrEmpty())
                foreach (var light in lights) if (light) light.enabled = setVisible;
            if (!items.IsNullOrEmpty())
                foreach (var item in items) if (item) item.SetVisibility(setVisible);
            OnVisibilityChange?.Invoke(setVisible);
        }
        #endregion

        #region Item Manage
        internal void AddItem(CullingGroupItem item)
        {
            if (items.Add(item))
            {
                item.SetVisibility(isVisibleInScene);
                if (isVisibleInScene && item.settings.HasFlag(CullingGroupItem.Setting.UseZoneEvents))
                {
                    item.AddedToZone(this);
                    OnItemAdded?.Invoke(item);
                }
            }
        }

        internal void RemoveItem(CullingGroupItem item)
        {
            if (items.Remove(item))
            {
                if (isVisibleInScene && item.settings.HasFlag(CullingGroupItem.Setting.UseZoneEvents))
                {
                    item.RemovedFromZone(this);
                    OnItemRemoved?.Invoke(item);
                }
            }
        }
        #endregion

        #region Build
        public void UpdateVisibilityObjects()
        {
            renderers = GetComponentsInChildren<Renderer>();
            lights = GetComponentsInChildren<Light>();
        }
        #endregion

        #region Bounds
        public void SetFullBounds(Bounds b) => fullBounds = b;

        internal Culling culling;
        internal class Culling
        {
            public const float HideDelay = 0.2f;
            public CullingGroup group;
            public int minIndex, maxIndex;
            public BoundingSphere[] spheres;
            public Bounds[] bounds;
            public float lastChangeTime;

            public bool IsVisible()
            {
                for (int i = minIndex; i <= maxIndex; i++)
                    if (group.IsVisible(i)) return true;
                return false;
            }
        }

        public void StartCullingVisibility() =>
            SetVisibility(culling.IsVisible());

        public void UpdateCullingVisibility()
        {
            bool anyVisible = culling.IsVisible();

            if (anyVisible)
            {
                culling.lastChangeTime = Time.time;

                if (!isVisibleInScene)
                    SetVisibility(true);
            }
            else
            {
                if (isVisibleInScene && Time.time - culling.lastChangeTime > Culling.HideDelay)
                    SetVisibility(false);
            }
        }

        internal Culling CreateCullingGroup(CullingGroup group, int index)
        {
            culling = new();
            culling.group = group;
            culling.bounds = CreateBounds();
            culling.spheres = new BoundingSphere[culling.bounds.Length];

            for (int i = 0; i < culling.bounds.Length; i++)
                culling.spheres[i] = new(culling.bounds[i].center, culling.bounds[i].extents.magnitude);
            culling.minIndex = index; culling.maxIndex = index + culling.bounds.Length - 1;
            return culling;
        }

        public Bounds GetFullBounds() => fullBounds;
        Bounds[] CreateBounds()
        {
            if (fullBounds.size.sqrMagnitude <= 0.00001f)
            {
                fullBounds = renderers
                    .Where(r => r.gameObject.layer != 0)
                    .Select(c => c.bounds).Encapsulate();
            }

            if (this.TryGetComponent(out IBounds ib) && ib.GetBounds(fullBounds, out Bounds[] bounds))
                return bounds;

            return new Bounds[] { fullBounds };
        }

        public interface IBounds
        {
            bool GetBounds(Bounds fullBounds, out Bounds[] bounds);
        }
        #endregion

    }
}