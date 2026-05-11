using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using Unity.Mathematics;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    [ExecuteAlways]
    public class QuadRenderer : MonoBehaviour
    {
        public float scale = 1;
        public Material material;
        public List<Quad> quads = new();

        public void Clear() => quads.Clear();

        public void Add(Vector3 position, Quaternion rotation, Color32 color) =>
            quads.Add(new Quad(position, rotation, color));

        public void Add(Vector3 position, Vector3 normal, Color32 color) =>
            Add(position, normal.GetRotation(), color);

        [Serializable]
        public class Quad
        {
            public Vector3 position;
            public Quaternion rotation;
            public Color32 color;
            public Quad(Vector3 position, Quaternion rotation, Color32 color)
            {
                this.position = position;
                this.rotation = rotation;
                this.color = color;
            }
        }

        Mesh mesh;
        void Update()
        {
            if (!mesh) mesh = MeshPrimitive.GetQuadMesh();
            Vector3 scale = (float3)this.scale;
            var m = transform.localToWorldMatrix;
            quads.GroupBy(q => q.color).ForEach(g => UnityEngine.Graphics.DrawMeshInstanced(mesh, 0, GetMaterial(g.Key),
                g.Select(q => m * Matrix4x4.TRS(q.position, q.rotation, scale)).ToArray()));
        }

        Dictionary<Color32, Material> materials = new();
        Material GetMaterial(Color32 color)
        {
            if (!materials.ContainsKey(color))
                materials.Add(color, new Material(material.shader)
                {
                    enableInstancing = true,
                    color = color,
                });
            return materials[color];
        }
    }
}