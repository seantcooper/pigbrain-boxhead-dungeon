using System.Collections.Generic;
using System.Xml.Serialization;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.core.Utility;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace pigbrain.game.Boxhead
{
    public class OrthoCamera : MonoBehaviourSingleton<OrthoCamera>
    {
        [SerializeField][MinMaxRange(0, 50)] MinMaxFloat orthoSize = new(6, 13);
        [SerializeField][Range(0, 50)] float distance = 10;
        [SerializeField][Range(0f, 20f)] float boundsSharpness = 6f;
        [SerializeField][Range(0f, 1f)] float listenerRange = 1;

        [Header("Offsets")]
        [SerializeField][Range(-5, 5)] float baseOffset = 0.5f;
        [SerializeField][Range(-5, 5)] float horiOffset = 0;

        [Header("Padding")]
        [SerializeField][Range(-10, 10)] float paddingLeft = 2;
        [SerializeField][Range(-10, 10)] float paddingTop = 2;
        [SerializeField][Range(-10, 10)] float paddingBottom = 2;
        [SerializeField][Range(-10, 10)] float paddingRight = 2;

        [Header("Debug")]
        [SerializeField][ReadOnly] AudioListener audioListener;
        [SerializeField][ReadOnly] Transform target;
        [SerializeField][ReadOnly] int count = 0;
        [SerializeField][ReadOnly] Bounds encapsulatedBounds;
        [SerializeField][ReadOnly] Bounds smoothedBounds;

        public void SetHorizontalOffset(float v) => horiOffset = v;

        Camera cam => Camera.main;

        protected override void Awake()
        {
            base.Awake();
            if (!target) target = transform;
            smoothedBounds = encapsulatedBounds;
            ResetCamera();
            audioListener = cam.GetComponentInChildren<AudioListener>();
        }

        void Start() => ResetCamera();
        void ResetCamera()
        {
            if (!cam || !target) return;
            horiOffset = 0;

            Bounds b = trackingBounds.Count > 0 ? GetBounds() : new Bounds(target.position, Vector3.zero);
            smoothedBounds = b;

            float setOrthoSize = GetOrthoSize(b);

            Vector3 desiredPivot = target.position.AddY(baseOffset);
            Vector3 desiredCamPos = desiredPivot - cam.transform.forward * distance;
            SetCamera(cam, desiredCamPos, setOrthoSize);
        }

        float GetOrthoSize(Bounds b)
        {
            float halfWidth = b.size.x * 0.5f;
            float halfDepth = b.size.z * 0.5f;

            Vector3 camRight = cam.transform.right;
            Vector3 camForward = cam.transform.forward;

            float projectedWidth = Mathf.Abs(camRight.x) * halfWidth + Mathf.Abs(camRight.z) * halfDepth;
            float projectedHeight = Mathf.Abs(camForward.x) * halfWidth + Mathf.Abs(camForward.z) * halfDepth;

            float requiredHalfHeight = Mathf.Max(projectedHeight, projectedWidth / cam.aspect);
            float padH = Mathf.Max(paddingTop, paddingBottom);

            return Mathf.Clamp(requiredHalfHeight + padH, orthoSize.min, orthoSize.max);
        }

        void LateUpdate()
        {
            if (!cam) return;
            if (trackingBounds.Count == 0) return;
            count = trackingBounds.Count;
            Bounds targetBounds = GetBounds();

            if (smoothedBounds.size.sqrMagnitude < 0.00001f)
                smoothedBounds = targetBounds;

            // Smooth bounds change (zone transitions)
            float t = 1f - Mathf.Exp(-boundsSharpness * Time.deltaTime);
            smoothedBounds.center = Vector3.Lerp(smoothedBounds.center, targetBounds.center, t);
            smoothedBounds.size = Vector3.Lerp(smoothedBounds.size, targetBounds.size, t);

            Bounds b = smoothedBounds;

            float setOrthoSize = GetOrthoSize(b);

            // Desired camera pivot (like a 2D scroller)
            Vector3 desiredPivot = target.position.AddY(baseOffset);

            if (b.size.sqrMagnitude < 0.00001f)
            {
                SetCamera(cam, desiredPivot - cam.transform.forward * distance, setOrthoSize);
                return;
            }

            // --- Camera footprint on XZ (orthographic, yaw-safe) ---
            float halfH = setOrthoSize;
            float halfW = halfH * cam.aspect;

            Vector3 right = cam.transform.right * halfW;
            Vector3 up = cam.transform.up * halfH;

            Vector3 extent = new();
            void Acc(Vector3 v) =>
                extent = new(Mathf.Max(extent.x, Mathf.Abs(v.x)), 0, Mathf.Max(extent.z, Mathf.Abs(v.z)));
            Acc(right + up);
            Acc(right - up);
            Acc(-right + up);
            Acc(-right - up);

            // --- Allowed camera pivot range (exactly like 2D) ---
            // Shrink clamp bounds by padding so camera can see beyond edges
            Vector3 min = new(b.min.x + extent.x - paddingLeft, 0, b.min.z + extent.z - paddingBottom);
            Vector3 max = new(b.max.x - extent.x + paddingRight, 0, b.max.z - extent.z + paddingTop);

            // X axis
            if (min.x > max.x) desiredPivot.x = b.center.x;
            else desiredPivot.x = Mathf.Clamp(desiredPivot.x, min.x, max.x);

            // Z axis
            if (min.z > max.z) desiredPivot.z = b.center.z;
            else desiredPivot.z = Mathf.Clamp(desiredPivot.z, min.z, max.z);

            // Final camera position from pivot
            Vector3 desiredCamPos = desiredPivot - cam.transform.forward * distance;

            if (CameraShake.Instance)
                desiredCamPos += CameraShake.Instance.offset;

            desiredCamPos = desiredCamPos.AddX(horiOffset);

            // Smooth AFTER clamping
            SetCamera(cam, Vector3.Lerp(cam.transform.position, desiredCamPos,
                1f - Mathf.Exp(-8f * Time.deltaTime)), setOrthoSize);

            if (audioListener)
                audioListener.transform.position = Vector3.Lerp(desiredPivot, cam.transform.position, listenerRange);
        }

        List<Camera> overlays;
        void SetCamera(Camera cam, Vector3 position, float orthoSize)
        {
            cam.transform.position = position;
            if (cam.orthographicSize == orthoSize) return;
            cam.orthographicSize = orthoSize;
            overlays ??= Camera.main.GetUniversalAdditionalCameraData().cameraStack;
            foreach (var overlay in overlays) overlay.orthographicSize = orthoSize;
        }

        #region Bounds Colliders
        HashSet<Bounds> trackingBounds = new();
        public void AddBounds(Bounds bounds)
        {
            if (!enabled) return;
            trackingBounds.Add(bounds);
            this.encapsulatedBounds = new();
        }

        public void RemoveBounds(Bounds bounds)
        {
            trackingBounds.Remove(bounds);
            this.encapsulatedBounds = new();
        }

        public Bounds GetBounds()
        {
            if (encapsulatedBounds.size.sqrMagnitude < 0.00001f && trackingBounds.Count > 0)
                encapsulatedBounds = trackingBounds.Encapsulate();
            return encapsulatedBounds;
        }
        #endregion
    }
}
