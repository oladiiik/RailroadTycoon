using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Render.Raster
{
    public static class RasterWaterRenderer
    {
        public static byte[] RenderPng(
            float[,] waterMask,
            int     pxPerCell = 8,
            float   threshold = 0.5f,
            Rgba32  fillColor = default)
        {
            int width  = waterMask.GetLength(0);
            int height = waterMask.GetLength(1);
            int wPx     = width  * pxPerCell;
            int hPx     = height * pxPerCell;

            if (fillColor.Equals(default))
                fillColor = new Rgba32(255,255,255,255);

            using var img = new Image<Rgba32>(wPx, hPx);

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                if (waterMask[x,y] < threshold)
                    continue;

                int baseX = x * pxPerCell;
                int baseY = y * pxPerCell;

                for (int dy = 0; dy < pxPerCell; dy++)
                for (int dx = 0; dx < pxPerCell; dx++)
                    img[baseX + dx, baseY + dy] = fillColor;
            }

            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}