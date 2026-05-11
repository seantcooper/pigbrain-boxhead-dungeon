using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TextureToMaterial
{
    [MenuItem("Assets/Create/Material from Texture", priority = -10000)]
    private static void CreateUnderSelection() =>
        GetTextureAssets().ToList().ForEach(t =>
            CreateMaterialForTexture(t.texture, Path.GetDirectoryName(t.path)));

    [MenuItem("Assets/Create/Material from Texture", validate = true)]
    private static bool CreateUnderSelection_Validate() => GetTextureAssets().Count() > 0;

    static IEnumerable<(Texture2D texture, string guid, string path)> GetTextureAssets()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex) yield return (tex, guid, path);
        }
    }

    private static void CreateMaterialForTexture(Texture2D tex, string folder)
    {
        string matPath = Path.Combine(folder, tex.name + ".mat");

        // Prevent accidental overwrite
        matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);

        // Choose your shader
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");

        var material = new Material(shader) { name = tex.name };

        // Assign main texture
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", tex);
        else if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", tex);

        AssetDatabase.CreateAsset(material, matPath); // write file
        Debug.Log($"Created material: {matPath}", material);
    }
}