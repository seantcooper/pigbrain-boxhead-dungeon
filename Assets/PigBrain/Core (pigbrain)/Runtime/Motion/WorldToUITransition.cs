using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace pigbrain.core.Motion
{
    public class WorldToUITransition : MonoBehaviour
    {
        [SerializeField] Transform worldSource;   // quad
        [SerializeField] Image uiPrefab;          // UI Image prefab
        [SerializeField] RectTransform uiTarget; // target slot
        [SerializeField] Canvas canvas;
        [SerializeField] float duration = 0.4f;

        public void Play()
        {
            if (!worldSource || !uiPrefab || !uiTarget || !canvas) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // world → screen
            var screen = Camera.main.WorldToScreenPoint(worldSource.position);
            if (screen.z < 0) yield break;

            // spawn UI
            var ui = Instantiate(uiPrefab, canvas.transform);
            var rt = ui.rectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screen,
                cam,
                out var startPos);

            rt.anchoredPosition = startPos;

            // match sprite (if quad has texture)
            var r = worldSource.GetComponent<Renderer>();
            if (r && r.sharedMaterial && r.sharedMaterial.mainTexture)
            {
                var tex = r.sharedMaterial.mainTexture as Texture2D;
                if (tex) ui.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            // hide world
            worldSource.gameObject.SetActive(false);

            // animate
            var endPos = uiTarget.anchoredPosition;

            float t = 0;
            var startScale = Vector3.one;
            var endScale = Vector3.one * 0.6f;

            while (t < 1)
            {
                t += Time.deltaTime / duration;
                float e = 1 - Mathf.Pow(1 - t, 3); // ease out

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
                rt.localScale = Vector3.Lerp(startScale, endScale, e);

                yield return null;
            }

            rt.anchoredPosition = endPos;
            rt.localScale = endScale;
        }
    }
}