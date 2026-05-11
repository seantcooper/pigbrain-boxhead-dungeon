public static class UnityObjectExtensions
{
    public static class RendererUtility
    {
    }

    // public class RendererFlash
    // {
    //     readonly Renderer renderer;
    //     readonly Coroutine flash;
    //     readonly float duration;

    //     public RendererFlash(MonoBehaviour control, Renderer renderer, float duration = 0.5f)
    //     {
    //         this.renderer = renderer;
    //         this.duration = duration;
    //         this.flash = control.StartCoroutine(Flash());
    //     }

    //     Color flashColor;
    //     float flashTime;
    //     public void Flash(Color color)
    //     {
    //         flashColor = color;
    //         flashTime = Time.time;
    //     }

    //     IEnumerator Flash()
    //     {
    //         MaterialPropertyBlock block = new();
    //         renderer.GetPropertyBlock(block);
    //         int prop = Shader.PropertyToID("_BaseColor");
    //         Color materialColor = block.GetColor(prop);

    //         while (true)
    //         {
    //             float t = Mathf.Clamp01((Time.time - flashTime) / duration);
    //             block.SetColor(prop, flashColor = Color.Lerp(flashColor, materialColor, t));
    //             renderer.SetPropertyBlock(block);
    //             yield return new WaitForNextUpdate();
    //         }
    //     }
    // }
}