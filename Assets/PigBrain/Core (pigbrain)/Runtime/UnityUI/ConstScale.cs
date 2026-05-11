using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.core.UnityUI
{
    [ExecuteAlways]
    public class ConstScale : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [SerializeField] CanvasScaler scaler;
        Vector2 baseSize;
        bool initialized;
        void LateUpdate()
        {
            if (!canvas && !this.TryGetComponentInParent(out canvas)) return;
            if (!scaler && !canvas.TryGetComponent(out scaler)) return;

            var rt = (RectTransform)transform;

            if (!initialized)
            {
                baseSize = rt.sizeDelta;
                initialized = true;
            }

            float scale = scaler.scaleFactor;
            if (scale <= 0f) return;

            rt.localScale = Vector3.one;
            rt.sizeDelta = baseSize / scale;
        }
    }
}