using UnityEngine;
using UnityEngine.Rendering;

namespace pigbrain.core.Graphics
{
    [ExecuteAlways]
    public class BackfaceObject : MonoBehaviour
    {
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering -= OnCamera;
            RenderPipelineManager.beginCameraRendering += OnCamera;
        }

        // void OnDisable()
        // {
        //     RenderPipelineManager.beginCameraRendering -= OnCamera;
        // }

        void OnDestroy() => RenderPipelineManager.beginCameraRendering -= OnCamera;

        void OnCamera(ScriptableRenderContext ctx, Camera camera)
        {
            if (!camera) return;

            bool active = true;

            if (camera.orthographic)
            {
                active = Vector3.Dot(transform.forward, -camera.transform.forward) > 0;
            }
            else
            {
                Vector3 toCam = (camera.transform.position - transform.position).normalized;
                active = Vector3.Dot(transform.forward, toCam) > 0f;
            }

            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
    }
}