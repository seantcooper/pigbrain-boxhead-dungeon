using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.UI
{
    public class MeshGraphic : MaskableGraphic
    {
        public Mesh mesh;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (mesh == null) return;

            var verts = mesh.vertices;
            var tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                vh.AddVert(verts[tris[i]], color, Vector2.zero);
                vh.AddVert(verts[tris[i + 1]], color, Vector2.zero);
                vh.AddVert(verts[tris[i + 2]], color, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2);
            }
        }
    }
}