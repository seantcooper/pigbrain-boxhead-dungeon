using System;
using System.IO;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Graphics
{
    public static class Texture2DX
    {
        #region Write
        public static void WriteAsPNG(this Texture2D texture, string path) =>
            WriteTexture(texture, path, WriteAsPNG_Readable);

        public static void WriteAsJPG(this Texture2D texture, string path) =>
            WriteTexture(texture, path, WriteAsJPG_Readable);

        static void WriteTexture(this Texture2D texture, string path, Action<Texture2D, string> write)
        {
            if (texture.isReadable)
            {
                using var textureScope = new Scoped<Texture2D>(texture.MakeReadable());
                write?.Invoke(textureScope, path);
            }
            else write?.Invoke(texture, path);
        }
        static void WriteAsPNG_Readable(this Texture2D texture, string path) =>
            File.WriteAllBytes(path, texture.EncodeToPNG());

        static void WriteAsJPG_Readable(this Texture2D texture, string path) =>
            File.WriteAllBytes(path, texture.EncodeToJPG());
        #endregion

        #region Make Readable
        public static Texture2D MakeReadable(this Texture2D texture)
        {
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32,
                texture.isDataSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);

            UnityEngine.Graphics.Blit(texture, rt);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            copy.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }
        #endregion 
    }
}
