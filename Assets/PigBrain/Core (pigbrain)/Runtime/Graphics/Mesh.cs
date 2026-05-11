using System.Linq;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    public static class MeshX
    {
        public static Mesh BakeMesh(this Mesh mesh, Transform transform, bool recalculate = true)
        {
            mesh = mesh.Instantiate();
            mesh.vertices = mesh.vertices.Select(v => transform.TransformPoint(v)).ToArray();
            if (recalculate)
            {
                mesh.RecalculateTangents();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }
            return mesh;
        }

        public static bool TryGetMesh(this Renderer renderer, out Mesh mesh) =>
            mesh = renderer.GetMesh();

        public static Mesh GetMesh(this Renderer renderer)
        {
            if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                return filter.sharedMesh;
            return null;
            // throw new System.NotImplementedException();
        }
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Graphics
{
    using System.Collections.Generic;
    using pigbrain.core.Collections;
    using UnityEditor;
    public static class MeshEditorX
    {
        [MenuItem("Tools/pigbrain/Mesh/Get Dimensions", true)]
        static bool GetMeshDimensions_Validate() => GetAllSelectedMeshRenderers().Count() > 0;

        [MenuItem("Tools/pigbrain/Mesh/Get Dimensions")]
        static void GetMeshDimensions_Action() => GetAllSelectedMeshRenderers().ForEach(r =>
        {
            var mesh = r.GetMesh();
            if (!mesh) Debug.Log($"{mesh.name} is not readable");
            else Debug.Log($"{mesh.name} = mesh/renderer bounds {mesh.bounds} / {r.bounds}");
        });

        static IEnumerable<MeshRenderer> GetAllSelectedMeshRenderers() => Selection.gameObjects
            .SelectMany(g => g.GetComponentsInChildren<MeshRenderer>())
            .Where(r => r.GetMesh());
    }
}
#endif
