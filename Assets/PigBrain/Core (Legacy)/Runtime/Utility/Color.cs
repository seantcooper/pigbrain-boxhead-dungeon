// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;

// public static class ColorsUtility
// {
//     public static Color Dark(this Color color, float amount = 0.5f) => Color.Lerp(color, Color.black, amount);
//     public static Color Light(this Color color, float amount = 0.5f) => Color.Lerp(color, Color.white, amount);

//     public static string HTML(this Color color) => ToHTML(color);
//     public static string ToHTML(this Color color) => UnityEngine.ColorUtility.ToHtmlStringRGBA(color);

//     public static Color SetBrightness(this Color color, float brightnessScalar)
//     {
//         Color.RGBToHSV(color, out float h, out float s, out float v);
//         return Color.HSVToRGB(h, s, v * brightnessScalar).WithA(color.a);
//     }

//     public static Color RotateHue(this Color color, float offset)
//     {
//         Color.RGBToHSV(color, out float h, out float s, out float v);
//         return Color.HSVToRGB(Mathf.Repeat(h + offset, 1), s, v).WithA(color.a);
//     }

//     public static Color WithR(this Color c, float r) => new(r, c.g, c.b, c.a);
//     public static Color WithG(this Color c, float g) => new(c.r, g, c.b, c.a);
//     public static Color WithB(this Color c, float b) => new(c.r, c.g, b, c.a);
//     public static Color WithA(this Color c, float a) => new(c.r, c.g, c.b, a);

//     static readonly Dictionary<(Color, int), Texture2D> textures = new();
//     public static Texture2D ToTexture2D(this Color color, int size = 4)
//     {
//         var key = (color, size);
//         if (!textures.TryGetValue(key, out Texture2D texture))
//         {
//             var t = new Texture2D(1, 1);
//             textures[key] = texture = new Texture2D(1, 1);
//             texture.SetPixels(Enumerable.Range(0, texture.width * texture.height).Select(i => color).ToArray());
//             texture.Apply(false, true);
//         }
//         return texture;
//     }
// }

