using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Core.Noise;


public sealed class PerlinNoise3D
{
    private readonly int[] _perm;               
    private readonly int   _mask;               

    public PerlinNoise3D(int seed = 42, int tableSize = 256)
    {
        _mask = tableSize - 1;
        _perm = new int[tableSize * 2];

        var rnd  = new Random(seed);
        var src  = Enumerable.Range(0, tableSize).ToArray();
        for (int i = tableSize - 1; i > 0; --i)
        {
            int swap = rnd.Next(i + 1);
            (src[i], src[swap]) = (src[swap], src[i]);
        }
        
        for (int i = 0; i < tableSize * 2; ++i)
            _perm[i] = src[i & _mask];
    }
    
    public float Sample(float x, float y, float z)
    {
        int xi = FastFloor(x) & _mask;
        int yi = FastFloor(y) & _mask;
        int zi = FastFloor(z) & _mask;

        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);
        float zf = z - MathF.Floor(z);

        float u = Fade(xf);
        float v = Fade(yf);
        float w = Fade(zf);
        
        int aaa = _perm[_perm[_perm[ xi ] + yi ] + zi ];
        int aba = _perm[_perm[_perm[ xi ] + yi+1] + zi ];
        int aab = _perm[_perm[_perm[ xi ] + yi ] + zi+1];
        int abb = _perm[_perm[_perm[ xi ] + yi+1] + zi+1];
        int baa = _perm[_perm[_perm[ xi+1] + yi ] + zi ];
        int bba = _perm[_perm[_perm[ xi+1] + yi+1] + zi ];
        int bab = _perm[_perm[_perm[ xi+1] + yi ] + zi+1];
        int bbb = _perm[_perm[_perm[ xi+1] + yi+1] + zi+1];

        // інтерполюємо
        float x1, x2, y1, y2;
        x1 = Lerp(Grad(aaa, xf,     yf,     zf    ),
                  Grad(baa, xf-1,   yf,     zf    ), u);
        x2 = Lerp(Grad(aba, xf,     yf-1,   zf    ),
                  Grad(bba, xf-1,   yf-1,   zf    ), u);
        y1 = Lerp(x1, x2, v);

        x1 = Lerp(Grad(aab, xf,     yf,     zf-1  ),
                  Grad(bab, xf-1,   yf,     zf-1  ), u);
        x2 = Lerp(Grad(abb, xf,     yf-1,   zf-1  ),
                  Grad(bbb, xf-1,   yf-1,   zf-1  ), u);
        y2 = Lerp(x1, x2, v);

        return Lerp(y1, y2, w);                 
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(float f) => (f >= 0 ? (int)f : (int)f - 1);

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float a, float b, float t) => a + t * (b - a);

    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;              // 0..15
        float u = (h < 8) ? x : y;
        float v = (h < 4) ? y : (h == 12 || h == 14 ? x : z);
        return (((h & 1) == 0) ?  u : -u) + (((h & 2) == 0) ?  v : -v);
    }
}
