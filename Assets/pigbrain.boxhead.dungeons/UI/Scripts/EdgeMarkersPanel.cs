using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.UI
{
    public class EdgeMarkerPanel : MonoBehaviourSingleton<EdgeMarkerPanel>
    {
        [SerializeField] RectTransform container;
        [SerializeField] RectTransform prefab;
        [SerializeField] public Transform target;

        Camera cam;

        readonly HashSet<EdgeMarker> trackingMarker = new();

        protected override void Awake()
        {
            base.Awake();
            cam = Camera.main;
            for (int i = 0; i <= EdgeMarker.MaxLayer; i++)
                containers.Add(i == 0 ? container : container.Instantiate(container.parent));
        }

        void OnEnable() => StartCoroutine(UpdateMarkers());
        void OnDisable()
        {
            StopAllCoroutines();
            trackingMarker.ForEach(m => PutPool(m));
            trackingMarker.Clear();
        }

        #region Containers / Layers
        readonly List<RectTransform> containers = new();
        RectTransform GetContainer(int layer) =>
            containers[Math.Clamp(layer, 0, containers.Count - 1)];
        #endregion

        #region Marker Add/Remove
        internal void AddMarker(EdgeMarker marker) => trackingMarker.Add(marker);
        internal void RemoveMarker(EdgeMarker marker)
        {
            PutPool(marker);
            trackingMarker.Remove(marker);
        }
        #endregion

        #region Update Markers
        static Matrix4x4 VP;
        static Vector3 CP;
        const int SliceCount = 100, MaxGroups = 3;
        const float M = 0.00f, MP = 0f;

        IEnumerator UpdateMarkers()
        {
            Vector2 half = new(0.5f, 0.5f);
            Transform camT = cam.transform;

            while (enabled)
            {
                // Vector3 camFwd = camT.forward;
                CP = camT.position;
                VP = cam.projectionMatrix * cam.worldToCameraMatrix;

                foreach (var marker in trackingMarker)
                {
                    if (!marker.IsVisible())
                    {
                        PutPool(marker);
                        continue;
                    }
                    if (GetPosition(marker, out Vector3 vp))
                    {
                        Vector2 clamped = new(Mathf.Clamp(vp.x, M, 1 - M), Mathf.Clamp(vp.y, M, 1 - M));
                        Vector2 local = (clamped - half) * container.rect.size;

                        if (!marker.instance)
                        {
                            GetPool(marker);
                            marker.instance.SetColor(marker.fullColor);
                        }
                        marker.instance.transform.anchoredPosition = local;
                        if (marker.directional) marker.instance.SetRotation(target, clamped);
                    }
                    else if (marker.instance) PutPool(marker);
                }
                yield return null;
            }
        }

        bool GetPosition(EdgeMarker marker, out Vector3 position)
        {
            Vector3 pos = marker.transform.position, to = pos - CP;
            Vector4 v = VP * new Vector4(pos.x, pos.y, pos.z, 1f);
            float invW = 1f / v.w;
            float vx = v.x * invW, vy = v.y * invW, vz = v.z * invW;
            Vector3 vp = position = new(vx * 0.5f + 0.5f, vy * 0.5f + 0.5f, vz);
            bool inside = vp.x >= MP && vp.x <= 1 - MP && vp.y >= MP && vp.y <= 1 - MP;
            return !inside;
        }
        #endregion

        #region Pool
        readonly Dictionary<RectTransform, Stack<PoolInstance>> stack = new();
        void GetPool(EdgeMarker marker)
        {
            var prefab = marker.GetPrefab() ? marker.GetPrefab() : this.prefab;

            if (!this.stack.TryGetValue(prefab, out Stack<PoolInstance> stack))
                stack = this.stack[prefab] = new();

            if (stack.Count > 0)
            {
                marker.instance = stack.Pop();
                marker.instance.gameObject.SetActive(true);
            }
            else (marker.instance = new PoolInstance(prefab, GetContainer(marker.GetLayer()))).gameObject.name = marker.name;
            marker.instance.marker = marker;
        }

        void PutPool(EdgeMarker marker)
        {
            if (!marker.instance) return;
            marker.instance.gameObject.SetActive(false);
            stack[marker.instance.prefab].Push(marker.instance);
            marker.instance = null;
        }

        public class PoolInstance
        {
            public RectTransform transform;
            public Image[] images;
            public GameObject gameObject => transform.gameObject;
            public RectTransform prefab;
            public EdgeMarker marker;

            internal void SetColor(Color color) { foreach (var image in images) image.color = color; }

            internal void SetRotation(Transform target, Vector2 markerVP)
            {
                if (!target) return;
                Vector4 tv = VP * new Vector4(target.position.x, target.position.y, target.position.z, 1f);
                float tinvW = 1f / tv.w;
                float tx = tv.x * tinvW, ty = tv.y * tinvW;

                Vector2 targetVP = new(tx * 0.5f + 0.5f, ty * 0.5f + 0.5f);
                Vector2 dir = (targetVP - markerVP).normalized;

                float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            public PoolInstance(RectTransform prefab, RectTransform container)
            {
                this.prefab = prefab;
                this.transform = prefab.Instantiate(container);
                this.images = transform.GetComponentsInChildren<Image>();
            }
            public static implicit operator bool(PoolInstance empty) => empty != null;
        }

        #endregion
    }
}