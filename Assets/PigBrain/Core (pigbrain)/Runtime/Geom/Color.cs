using System.Collections.Generic;
using UnityEngine;

namespace pigbrain.core.Geom
{
    public static class ColorX
    {
        public static Color ToColor(this string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        public static Color WithR(this Color c, float r) => new(r, c.g, c.b, c.a);
        public static Color WithG(this Color c, float g) => new(c.r, g, c.b, c.a);
        public static Color WithB(this Color c, float b) => new(c.r, c.g, b, c.a);
        public static Color WithA(this Color c, float a) => new(c.r, c.g, c.b, a);

        public static Color32 WithR(this Color32 c, byte r) => new(r, c.g, c.b, c.a);
        public static Color32 WithG(this Color32 c, byte g) => new(c.r, g, c.b, c.a);
        public static Color32 WithB(this Color32 c, byte b) => new(c.r, c.g, b, c.a);
        public static Color32 WithA(this Color32 c, byte a) => new(c.r, c.g, c.b, a);

        public static uint ARGB(this Color32 c) => ((uint)c.a << 24) | ((uint)c.r << 16) | ((uint)c.g << 8) | (uint)c.b;
        public static uint RGB(this Color32 c) => ((uint)c.r << 16) | ((uint)c.g << 8) | (uint)c.b;

        // public static Color Quantize(this Color c, int steps = 15)
        // {
        //     if (steps <= 0) return c;
        //     float Q(float v) => Mathf.Round(v * steps) / steps;
        //     Color result = new(Q(c.r), Q(c.g), Q(c.b), c.a);
        //     Debug.Log("Quantize: ")
        //     return result;
        // }
        // public static Color32 Quantize(this Color32 c, int levels = 15) =>
        //     ((Color)c).Quantize(levels);

        public static float DistanceSq(this Color c) => new Vector3(c.r, c.g, c.b).sqrMagnitude;
    }

    public class Palette
    {
        readonly List<Color> palette = new();
        readonly int[] remap;

        public Palette(Color[] colors, float threshold)
        {
            remap = new int[colors.Length];
            foreach (var c in colors)
            {
                bool merged = false;
                for (int i = 0; i < palette.Count; i++)
                {
                    if ((palette[i] - c).DistanceSq() <= threshold * threshold)
                    {
                        palette[i] = Color.Lerp(palette[i], c, 0.5f);
                        merged = true;
                        break;
                    }
                }
                if (!merged) palette.Add(c);
            }
        }

        public static Color SnapToPalette(Color c, Color[] palette)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < palette.Length; i++)
            {
                float d = (palette[i] - c).DistanceSq();
                if (d < bestD) { bestD = d; best = i; }
            }
            return palette[best];
        }

        public static void ApplyPalette(Color[] data, Color[] palette)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = SnapToPalette(data[i], palette);
        }
    }
}
