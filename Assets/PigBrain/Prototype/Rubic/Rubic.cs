using System;
using System.Linq;
using pigbrain.core.Inspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[InlineButton(nameof(Create))]
public class Rubic : MonoBehaviour
{
    [SerializeField] Cube cube;

    void Create()
    {
        cube.Create();
    }

    void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (!Camera.main) return;
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit)) return;
        cube.Rotate(Vector3Int.RoundToInt(hit.normal));
    }

    [Serializable]
    class Cube
    {
        [SerializeField][ReadOnly] GameObject instance;

        const string ShaderName = "Universal Render Pipeline/Simple Lit";
        [SerializeField] float stickScale = 0.05f;
        [SerializeField] Color[] colors;
        [SerializeField] Material blackMaterial;
        [SerializeField] Material[] colorMaterials;

        public void Create()
        {
            if (instance) DestroyImmediate(instance);

            GameObject cube = new("cube");
            blackMaterial = new(Shader.Find(ShaderName)) { color = Color.black };
            colorMaterials = colors.Select(c => new Material(Shader.Find(ShaderName)) { color = c }).ToArray();

            for (int z = -1; z <= 1; z++) for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0 && z == 0) continue;

                var part = CreateCube(x, y, z);
                part.transform.parent = cube.transform;

                if (x != 0) CreateSticker(new(x, 0, 0), part);
                if (y != 0) CreateSticker(new(0, y, 0), part);
                if (z != 0) CreateSticker(new(0, 0, z), part);

            }
            instance = cube;
        }

        GameObject CreateCube(float x, float y, float z)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = $"cube {x},{y},{z}";
            result.transform.localPosition = new(x, y, z);
            result.GetComponent<Renderer>().sharedMaterial = blackMaterial;
            return result;
        }

        GameObject CreateSticker(int3 axis, GameObject parent)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = $"sticker";
            result.transform.parent = parent.transform;
            result.transform.localPosition = (float3)axis * 0.5f;
            DestroyImmediate(result.GetComponent<Collider>());


            if (axis.x != 0) result.transform.localScale = new(stickScale, 1 - stickScale, 1 - stickScale);
            if (axis.y != 0) result.transform.localScale = new(1 - stickScale, stickScale, 1 - stickScale);
            if (axis.z != 0) result.transform.localScale = new(1 - stickScale, 1 - stickScale, stickScale);

            int index = math.abs(axis.y) * 2 + math.abs(axis.z) * 4 + (axis.x + axis.y + axis.z < 0 ? 1 : 0);
            result.GetComponent<Renderer>().sharedMaterial = colorMaterials[index];
            return result;
        }

        // Update is called once per frame
        public void Rotate(Vector3Int axis) => Rotate(new int3(axis.x, axis.y, axis.z));
        public void Rotate(int3 axis)
        {
            int i = axis.x != 0 ? 0 : axis.y != 0 ? 1 : 2;
            foreach (Transform child in instance.transform)
                if (axis[i] != 0 && Mathf.Abs(child.localPosition[i]) > 0.5f && Mathf.Sign(child.localPosition[i]) == Mathf.Sign(axis[i]))
                    child.RotateAround(instance.transform.position, (float3)axis, 90);
        }
    }
}
