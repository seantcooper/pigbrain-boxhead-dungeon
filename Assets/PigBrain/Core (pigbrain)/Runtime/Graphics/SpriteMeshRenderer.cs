using UnityEngine;

namespace pigbrain.core.Graphics
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SpriteMeshRenderer : MonoBehaviour
    {
        [SerializeField] Sprite sprite;
        [SerializeField] Material material; // must support _MainTex + _MainTex_ST

        void OnEnable() => Apply();
        void OnValidate()
        {
            Apply();
            Hide();
            GetComponent<MeshRenderer>().enabled = enabled;
        }

        void Apply()
        {
            if (!sprite) return;

            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            mf.sharedMesh = MeshPrimitive.GetQuadMesh();
            mr.sharedMaterial = material;

            var tex = sprite.texture;
            var r = sprite.textureRect;

            Vector4 st = new(r.width / tex.width, r.height / tex.height, r.x / tex.width, r.y / tex.height);

            var block = new MaterialPropertyBlock();
            block.SetTexture("_BaseMap", tex);
            block.SetVector("_BaseMap_ST", st);
            mr.SetPropertyBlock(block);
            // var size = sprite.bounds.size;
            // transform.localScale = new Vector3(size.x, size.y, 1);
        }

        void Reset() => Hide();
        void Hide()
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();

            if (mf) mf.hideFlags = 0;//HideFlags.HideInInspector;
            if (mr) mr.hideFlags = 0;//HideFlags.HideInInspector;
        }
    }
}