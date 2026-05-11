#pragma warning disable UDR0001
using System;
using System.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    public static class MeshPrimitive
    {
        static Mesh QuadMesh;
        public static Mesh GetQuadMesh() => QuadMesh ? QuadMesh : QuadMesh =
            Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        static Mesh CubeMesh;
        public static Mesh GetCubeMesh() => CubeMesh ? CubeMesh : CubeMesh =
            Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }
}