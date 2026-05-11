using System;
using System.Collections;
using System.Text;
using pigbrain.core.UnityObject;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Profiling.Profiler;

namespace pigbrain.core.Analysis
{
    public class RuntimePerformance : MonoBehaviourSingleton<RuntimePerformance>
    {
        [SerializeField] DynamicScaling dynamicScaling;
        [SerializeField] TMP_Text textField;
        [SerializeField] bool lightning = false;
        [SerializeField] RenderPipelineAsset urpLightning;
        [SerializeField] RenderPipelineAsset urpOriginal;
        [SerializeField][Range(0.001f, 1)] float updateRate = 0.25f;

        float frameTime;

        public static void SetFPS(int fps)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }

        public static void SetDynamicScale(float scale) =>
            Instance.dynamicScaling.SetScale(scale);

        public void SetLightning(bool state)
        {
            if (!urpLightning || !urpOriginal) return;
            lightning = state;
            var urp = state ? urpLightning : urpOriginal;
            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
            SetFPS(lightning ? 30 : -1);
            Camera.main.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = !lightning;
        }

        protected override void Awake()
        {
            base.Awake();
            if (!urpOriginal) urpOriginal = UniversalRenderPipeline.asset;
        }

        void OnEnable() => StartCoroutine(Run());
        void OnDisable() => StopAllCoroutines();
        void OnApplicationQuit() => GraphicsSettings.defaultRenderPipeline = urpOriginal;

        IEnumerator Run()
        {
            while (enabled)
            {
                if (textField && textField.isActiveAndEnabled) UpdateProfile();
                yield return new WaitForSecondsRealtime(updateRate);
            }
        }

        void Update()
        {
            dynamicScaling.Update();
            frameTime = Mathf.Lerp(frameTime, Time.unscaledDeltaTime, 0.1f);
        }

        readonly StringBuilder sb = new(128);
        void UpdateProfile()
        {
            GetReport().Build(sb);
            if (textField) textField.text = sb.ToString().ToUpper();
        }

        Report lastReport;
        public Report GetReport() => lastReport = new(frameTime, dynamicScaling.currentScale);

        [Serializable]
        public struct Report
        {
            const string Div = " | ";
            public long reservedMemory, allocatedMemory, monoUsedSize, monoHeapSize, gcTotalMemory;
            public int targetFrameRate;
            public float dynamicScale, frameTime, frameTimePeak;
            public string productName;
            public string version;

            static (float ft, float ts) FrameTimePeak;
            const float PeakDuration = 5;
            public Report(float frameTime, float dynamicScale)
            {
                this.frameTime = frameTime;
                if (frameTime < FrameTimePeak.ft || FrameTimePeak.ts == 0 || Time.time >= FrameTimePeak.ts + PeakDuration)
                    FrameTimePeak = (frameTime, Time.time);
                frameTimePeak = FrameTimePeak.ft;

                this.dynamicScale = dynamicScale;
                targetFrameRate = Application.targetFrameRate;
                reservedMemory = GetTotalReservedMemoryLong();
                allocatedMemory = GetTotalAllocatedMemoryLong();
                monoUsedSize = GetMonoUsedSizeLong();
                monoHeapSize = GetMonoHeapSizeLong();
                gcTotalMemory = GC.GetTotalMemory(false);
                productName = Application.productName;
                version = Application.version;
            }

            public void Build(StringBuilder sb)
            {
                sb.Clear();

                // VERSION
                sb.Append(productName).Append(" prototype v").Append(version).Append(Div);

                // FPS
                int GetFPS(float ft) => Mathf.RoundToInt(ft > 0f ? 1f / ft : 0f);
                sb.Append(GetFPS(frameTime));
                if (Application.targetFrameRate != -1) sb.Append(':').Append(Application.targetFrameRate);
                sb.Append($" ({GetFPS(frameTimePeak)})");
                sb.Append(Div);

                // SCALE
                sb.Append((dynamicScale * 100f).ToString("0")).Append('%').Append(Div);

                // MEMORY
                static void AppendMB(System.Text.StringBuilder sb, string label, long memory) =>
                    sb.Append(label).Append(':').Append(MemoryProfiler.FormatBytes(memory));

                AppendMB(sb, "T", allocatedMemory); sb.Append(Div);
                AppendMB(sb, "R", reservedMemory); sb.Append(Div);
                AppendMB(sb, "M", monoUsedSize); sb.Append(Div);
                AppendMB(sb, "H", monoHeapSize); sb.Append(Div);
                AppendMB(sb, "G", gcTotalMemory); sb.Append(Div);
                sb.Append(JS.URL);
            }
        }
    }

    [Serializable]
    class DynamicScaling
    {
        [SerializeField] int maxPixels = 1000000;
        [SerializeField] bool inUnityEditor = false;
        float scale = 0;

        int2 screenSize => new(Screen.width, Screen.height);
        int screenPixels => Screen.width * Screen.height;
        public float currentScale => scale;
        float GetRenderScale(int2 size) => Mathf.Clamp01(maxPixels / (float)(size.x * size.y));

        internal void Update()
        {
#if UNITY_EDITOR
            if (!inUnityEditor)
            {
                SetRenderScale(1);
                return;
            }
#endif
            if (scale == GetRenderScale(screenSize)) return;
            SetRenderScale(GetRenderScale(screenSize));
        }

        void SetRenderScale(float scale)
        {
            this.scale = Mathf.Min(1, scale);
            UniversalRenderPipeline.asset.renderScale = this.scale;
        }

        public void SetScale(float scale)
        {
            maxPixels = Mathf.RoundToInt(screenPixels / scale);
            pigbrain.game.Boxhead.UI.Console.Print($"MaxPixels: {maxPixels}/{screenPixels}");
        }
    }
}
