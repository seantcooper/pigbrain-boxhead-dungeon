using UnityEngine;

namespace pigbrain.core.Graphics
{
    [ExecuteAlways]
    public class MaterialColor : MonoBehaviour
    {
        [SerializeField] Color color = Color.red;
        [SerializeField] string key = "_BaseColor";
        MaterialPropertyBlock block;
        Renderer Renderer => GetComponent<Renderer>();

        void Awake() => UpdateColor();
        void Update() => UpdateColor();

        void UpdateColor()
        {
            if (block == null)
                block = new MaterialPropertyBlock();

            Renderer.GetPropertyBlock(block);
            block.SetColor(key, color);
            Renderer.SetPropertyBlock(block);
        }
    }
}