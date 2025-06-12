namespace Core.Noise;

public sealed class FbmNoise3D
{
    private readonly PerlinNoise3D _base;
    private readonly int   _octaves;
    private readonly float _lacunarity;
    private readonly float _gain;

    public FbmNoise3D(int seed = 42,
        int octaves = 5,
        float lacunarity = 2.0f,
        float gain = 0.5f)
    {
        _base       = new PerlinNoise3D(seed);
        _octaves    = octaves;
        _lacunarity = lacunarity;
        _gain       = gain;
    }

    public float Sample(float x, float y, float z)
    {
        float freq = 1f;
        float amp  = 1f;
        float sum  = 0f;
        float norm = 0f;

        for (int i = 0; i < _octaves; ++i)
        {
            sum  += _base.Sample(x * freq, y * freq, z * freq) * amp;
            norm += amp;

            freq *= _lacunarity;
            amp  *= _gain;
        }
        return sum / norm;          
    }
}