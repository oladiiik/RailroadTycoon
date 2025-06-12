using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Render.Raster
{
    public static class RasterHeightRenderer
    {
        private static readonly Rgba32[] BaseControlColors =
        {
            new Rgba32(0, 0, 0, 0),
            Rgba32.ParseHex("#D8C9A5")  
        };
        private static readonly Rgba32[] LastColorOptions =
        {
            Rgba32.ParseHex("#615c4b"),
            Rgba32.ParseHex("#756f58"),
            Rgba32.ParseHex("#a49a76")
        };

        public static byte[] RenderPng(
            float[,] map,
            float[] levels,
            int pxPerCell = 1,
            int seed      = 42,
            float gamma   = 1f)
        {
            int wCells = map.GetLength(0), hCells = map.GetLength(1);
            int wPx    = wCells * pxPerCell, hPx = hCells * pxPerCell;

            Rgba32[] bandColors;
            if (levels.Length == 2)
            {
                bandColors = GetRandomControlColors(seed);
            }
            else
            {
                int bandCount = levels.Length + 1;
                bandColors = GenerateBandColors(bandCount, seed, gamma);
            }

            using var img = new Image<Rgba32>(wPx, hPx);
            for (int y = 0; y < hCells; y++)
            for (int x = 0; x < wCells; x++)
            {
                int bi;
                if (levels.Length == 2)
                {
                    float v = map[x, y];
                    bi = v < levels[0] ? 0 : (v < levels[1] ? 1 : 2);
                }
                else
                {
                    bi = BandIndex(map[x, y], levels);
                }
                var col = bandColors[bi];
                for (int dy = 0; dy < pxPerCell; dy++)
                    for (int dx = 0; dx < pxPerCell; dx++)
                        img[x * pxPerCell + dx, y * pxPerCell + dy] = col;
            }

            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }
        
        private static Rgba32[] GetRandomControlColors(int seed)
        {
           int idx = ((seed % LastColorOptions.Length) + LastColorOptions.Length) % LastColorOptions.Length;
           var last = LastColorOptions[idx];
           return new[] { BaseControlColors[0], BaseControlColors[1], last };
        }
        
        private static Rgba32[] GenerateBandColors(int bandCount, int seed, float gamma = 1f)
        {
            if (bandCount < 1) throw new ArgumentException("bandCount must be >= 1", nameof(bandCount));
            var controlColors = GetRandomControlColors(seed);
            int segments = controlColors.Length - 1;
            var result = new Rgba32[bandCount];
            for (int i = 0; i < bandCount; i++)
            {
                float t = bandCount > 1 ? (float)i / (bandCount - 1) : 0f;
                t = MathF.Pow(t, 1f / gamma);
                float pos = t * segments;
                int idx = Math.Min((int)pos, segments - 1);
                float localT = pos - idx;
                var c0 = controlColors[idx];
                var c1 = controlColors[idx + 1];
                byte R = (byte)(c0.R + (c1.R - c0.R) * localT);
                byte G = (byte)(c0.G + (c1.G - c0.G) * localT);
                byte B = (byte)(c0.B + (c1.B - c0.B) * localT);
                byte A = (byte)(c0.A + (c1.A - c0.A) * localT);
                result[i] = new Rgba32(R, G, B, A);
            }
            return result;
        }

        private static int BandIndex(float heightValue, float[] levels)
        {
            for (int i = 0; i < levels.Length; i++)
                if (heightValue < levels[i]) return i;
            return levels.Length;
        }
    }
}