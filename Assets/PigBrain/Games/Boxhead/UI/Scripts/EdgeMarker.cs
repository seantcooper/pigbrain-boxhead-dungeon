using System;
using pigbrain.core.Geom;
using pigbrain.game.Boxhead.Environment;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.UI
{
    public class EdgeMarker : MonoBehaviour
    {
        public const int MaxLayer = 10;

        [SerializeField] Traits traits;
        [SerializeField] RectTransform prefab;
        [SerializeField] Color color = Color.white;
        [SerializeField] float alpha = 0.5f;

        [SerializeField][Range(0, MaxLayer)] int layer = 0;

        public Color fullColor => color.WithA(alpha);
        public bool directional => traits.HasFlag(Traits.Directional);
        public RectTransform GetPrefab() => prefab;
        public int GetLayer() => layer;
        internal Room room;
        internal EdgeMarkerPanel.PoolInstance instance;

        void Start() => room = traits.HasFlag(Traits.RoomOnly) ? GetComponentInParent<Room>(true) : null;
        void OnEnable() => Add();
        void OnDisable() => Remove();
        void Add() { if (EdgeMarkerPanel.Instance) EdgeMarkerPanel.Instance.AddMarker(this); }
        void Remove() { if (EdgeMarkerPanel.Instance) EdgeMarkerPanel.Instance.RemoveMarker(this); }

        [Flags]
        enum Traits
        {
            None = 0,
            Directional = 1 << 0,
            RoomOnly = 1 << 1,
            Another = 1 << 2,
        }
    }

    public static class EdgeMarkerX
    {
        public static bool IsVisible(this EdgeMarker marker) =>
            marker && marker.isActiveAndEnabled && (!marker.room || marker.room == ActivePlayer.Instance.room);
    }
}