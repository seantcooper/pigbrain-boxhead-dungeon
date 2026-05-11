using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;

public class FullScreenFeature : ScriptableRendererFeature
{
    public string id = "FullScreenFeature";
    public Material material;
    Pass pass;

    class Pass : ScriptableRenderPass
    {
        public string id;
        Material mat;

        public Pass(string id)
        {
            this.id = id;
        }

        public void SetMaterial(Material m) => mat = m;

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!mat) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.isPreviewCamera) return;

            TextureHandle src = resourceData.activeColorTexture;
            if (!src.IsValid()) return;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = id;
            desc.depthBufferBits = 0;

            TextureHandle temp = renderGraph.CreateTexture(desc);
            if (!temp.IsValid()) return;

            // effect pass
            renderGraph.AddBlitPass(new BlitMaterialParameters(src, temp, mat, 0), id);

            // write result back to camera color
            renderGraph.AddBlitPass(new BlitMaterialParameters(temp, src, mat, -1), $"{id}WriteBack");
        }
    }

    public override void Create() =>
        pass = new(id) { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        if (!material) return;
        pass.SetMaterial(material);
        renderer.EnqueuePass(pass);
    }
}