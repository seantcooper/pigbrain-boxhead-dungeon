// using pigbrain.Core.Inspector;
// using Unity.Mathematics;
// using UnityEngine;
// using UnityEngine.Rendering.Universal;

// namespace pigbrain.Core.Graphics
// {
//     public class DynamicScaling : MonoBehaviour
//     {
//         [SerializeField] int maxPixels = 1000000;
//         [SerializeField][ReadOnly] float scale = 0;
//         [SerializeField][ReadOnly] int2 size;

//         public float currentScale => scale;
//         void Start()
//         {
//             UpdateScale();
//         }

//         float GetRenderScale(int2 size) => Mathf.Clamp01(maxPixels / (float)(size.x * size.y));
//         void Update()
//         {
//             if (scale != GetRenderScale(new(Screen.width, Screen.height)))
//                 UpdateScale();
//         }

//         void UpdateScale()
//         {
//             size = new(Screen.width, Screen.height);
//             scale = Mathf.Clamp01(maxPixels / (float)(size.x * size.y));
//             UniversalRenderPipeline.asset.renderScale = scale;
//         }
//     }
// }