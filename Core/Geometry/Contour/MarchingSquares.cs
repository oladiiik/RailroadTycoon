using System;
using System.Collections.Generic;

namespace Core.Geometry.Contour;

public static class MarchingSquares
{
    private static readonly (int A, int B)[][] EdgeTable =
    {
        Array.Empty<(int,int)>(),                 // 0
        new[]{(3,0)},
        new[]{(0,1)},
        new[]{(3,1)},
        new[]{(1,2)},
        new[]{(3,0),(1,2)},
        new[]{(0,2)},
        new[]{(3,2)},
        new[]{(2,3)},
        new[]{(2,0)},
        new[]{(1,3),(0,2)},
        new[]{(1,2)},
        new[]{(3,1)},
        new[]{(0,1)},
        new[]{(3,0)},
        Array.Empty<(int,int)>() //15
    };
    private static readonly (float X, float Y)[] EdgeMid =
    {
        (0.5f,0f), (1f,0.5f), (0.5f,1f), (0f,0.5f)
    };
    
    public static IEnumerable<Polyline> Extract(float[,] map, IReadOnlyList<float> levels)
    {
        int w = map.GetLength(0) - 1;
        int h = map.GetLength(1) - 1;

        foreach (float lvl in levels)
        {
            var lines = new List<Polyline>();

            for (int y = 0; y < h; ++y)
            for (int x = 0; x < w; ++x)
            {
                int mask =
                    (map[x,     y]     >= lvl ? 1 : 0) |
                    (map[x + 1, y]     >= lvl ? 2 : 0) |
                    (map[x + 1, y + 1] >= lvl ? 4 : 0) |
                    (map[x,     y + 1] >= lvl ? 8 : 0);

                foreach (var (ea,eb) in EdgeTable[mask])
                {
                    var p1 = (EdgeMid[ea].X + x, EdgeMid[ea].Y + y);
                    var p2 = (EdgeMid[eb].X + x, EdgeMid[eb].Y + y);
                    lines.Add(new Polyline(new() { p1, p2 }));
                }
            }
            foreach (var ln in lines) yield return ln;
        }
    }
}