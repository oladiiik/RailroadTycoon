using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Core.Render.Raster
{
    public class RasterCloudRenderer
    {
        public static byte[] RenderPng(float[,] cloudMap, int pxPerCell = 8, float maxOpacity = 0.5f, float threshold = 0.3f)
        {
            int wCells     = cloudMap.GetLength(0);
            int hCells     = cloudMap.GetLength(1);
            int partWidth  = wCells * pxPerCell;
            int wPx        = partWidth * 3;
            int hPx        = hCells   * pxPerCell;

            using var img = new Image<Rgba32>(wPx, hPx);

            for (int y = 0; y < hCells; y++)
            {
                int baseY = y * pxPerCell;
                for (int x = 0; x < wCells; x++)
                {
                    float noiseValue = cloudMap[x, y];
                    if (noiseValue <= threshold) 
                        continue;
                    
                    float normalized = (noiseValue - threshold) / (1f - threshold);
                    byte alpha = (byte)(normalized * maxOpacity * 255);
                    var color = new Rgba32(255, 255, 255, alpha);

                    int x0 = x * pxPerCell;
                    int x1 = partWidth + ((wCells - 1 - x) * pxPerCell);
                    int x2 = partWidth * 2 + x0;

                    for (int dy = 0; dy < pxPerCell; dy++)
                    {
                        int py = baseY + dy;
                        for (int dx = 0; dx < pxPerCell; dx++)
                        {
                            int ox = dx;
                            img[x0 + ox, py] = color;  
                            img[x1 + ox, py] = color;  
                            img[x2 + ox, py] = color;  
                        }
                    }
                }
            }

            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}
