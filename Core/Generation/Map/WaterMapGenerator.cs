using System;
using System.Threading.Tasks;
using Core.Noise;

namespace Core.Generation.Map;
public static class WaterMapGenerator
{
    public static float[,] Generate(
        int   width,
        int   height,
        int   seed          = 42,
        float frequency     = 0.03f,
        float warpAmplitude = 0.1f,
        float threshold     = 0.4f)
    {
        var map    = new float[width, height];
        
        Parallel.For(0, height, y =>
        {
            var perlin = new PerlinNoise3D(seed);

            for (int x = 0; x < width; x++)
            {
                float fx = x * frequency;
                float fy = y * frequency;

                float wx = perlin.Sample(fx, fy, 0f) * warpAmplitude;
                float wy = perlin.Sample(fx, fy, 1f) * warpAmplitude;

                float sx = fx + wx;
                float sy = fy + wy;

                float d = WorleyDistance(sx, sy, seed);
                map[x, y] = d < threshold ? 0f : 1f;
            }
        });

        return map;
    }
    

    private static float WorleyDistance(float x, float y, int seed)
    {
        int xi = (int)MathF.Floor(x);
        int yi = (int)MathF.Floor(y);
        float minDistSq = float.MaxValue;

        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int cx = xi + dx;
            int cy = yi + dy;

            float px = cx + Hash(cx, cy, seed);
            float py = cy + Hash(cx, cy, seed ^ 0xABCDEF);

            float dx0 = x - px;
            float dy0 = y - py;
            float distSq = dx0 * dx0 + dy0 * dy0;
            if (distSq < minDistSq) minDistSq = distSq;
        }

        return MathF.Sqrt(minDistSq);
    }

    private static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263) ^ (uint)seed;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= (h >> 16);
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
