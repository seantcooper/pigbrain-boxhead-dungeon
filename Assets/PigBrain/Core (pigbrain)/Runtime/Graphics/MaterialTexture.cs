using System;
using System.Collections;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    [RequireComponent(typeof(Renderer))]
    public class MaterialTexture : MonoBehaviour
    {
        [SerializeField] Texture texture;
        [SerializeField] string key = "_BaseColorMap";
        MaterialPropertyBlock block;

        Renderer Renderer => GetComponent<Renderer>();

        public void SetTexture(Texture texture)
        {
            this.texture = texture;
            if (block == null) block = new MaterialPropertyBlock();
            Renderer.GetPropertyBlock(block);
            block.SetTexture(key, texture);
            Renderer.SetPropertyBlock(block);
        }
        void OnValidate() => SetTexture(texture);
    }
}