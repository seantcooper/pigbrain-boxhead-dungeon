using System.IO;
using UnityEditor;
using UnityEngine;
using pigbrain.core.Graphics;
using System;

namespace pigbrain.core.Project
{
    public static class ExportAs
    {
        const string ExporAssetPathKey = "pigbrain.LastExportPath";

        #region Export Texture
        [MenuItem("Assets/Export/As PNG", true)]
        [MenuItem("Assets/Export/As JPG", true)]
        static bool ExportAsPNG_Validate() => Selection.activeObject is Texture2D;

        [MenuItem("Assets/Export/As PNG")]
        static void ExportAsPNG() =>
            ExportAsTexture(Selection.activeObject as Texture2D, "png", Texture2DX.WriteAsPNG);

        [MenuItem("Assets/Export/As JPG")]
        static void ExportAsJPG() =>
            ExportAsTexture(Selection.activeObject as Texture2D, "jpg", Texture2DX.WriteAsJPG);

        static void ExportAsTexture(Texture2D tex, string ext, Action<Texture2D, string> write)
        {
            if (!tex) return;
            string start = EditorPrefs.GetString(ExporAssetPathKey, Application.dataPath);
            string path = EditorUtility.SaveFilePanel("Select Export File", start, tex.name, ext);
            if (!string.IsNullOrEmpty(path))
            {
                write?.Invoke(tex, path);
                EditorPrefs.SetString(ExporAssetPathKey, Path.GetDirectoryName(path));
            }
        }
        #endregion
    }
}

