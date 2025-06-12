using System.Buffers;
using System.Threading.Tasks;
using Core.Noise;

namespace Core.Generation.Map;

public static class CloudMapGenerator
{
    public static float[,] Generate(
        int   width,
        int   height,
        int   seed        = 42,
        float frequency   = 0.005f,
        int   octaves     = 6,
        float lacunarity  = 2.0f,
        float gain        = 0.5f)
    {
        var map = new float[width, height];
        
        var rowMin = ArrayPool<float>.Shared.Rent(height);
        var rowMax = ArrayPool<float>.Shared.Rent(height);

        Parallel.For(0, height, y =>
        {
            var fbm = new FbmNoise3D(seed, octaves, lacunarity, gain);

            float localMin =  float.MaxValue;
            float localMax = -float.MaxValue;

            for (int x = 0; x < width; x++)
            {
                float v = fbm.Sample(x * frequency, y * frequency, 0f);
                map[x, y] = v;
                if (v < localMin) localMin = v;
                if (v > localMax) localMax = v;
            }
            rowMin[y] = localMin;
            rowMax[y] = localMax;
        });

        float min = float.MaxValue, max = -float.MaxValue;
        for (int y = 0; y < height; y++)
        {
            if (rowMin[y] < min) min = rowMin[y];
            if (rowMax[y] > max) max = rowMax[y];
        }
        ArrayPool<float>.Shared.Return(rowMin);
        ArrayPool<float>.Shared.Return(rowMax);

        float rangeInv = 1f / (max - min);

        Parallel.For(0, height, y =>
        {
            for (int x = 0; x < width; x++)
                map[x, y] = (map[x, y] - min) * rangeInv;
        });

        return map;
    }
}