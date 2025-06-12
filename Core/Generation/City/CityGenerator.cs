using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Algorithms.Graph;

namespace Core.Generation.City;
public static class CityGenerator
{
    public static (List<Point> cities, List<string> names) Generate(
        float[,] hmap,
        int    seed,
        int    count,
        float  threshold)
    {
        int w = hmap.GetLength(0);
        int h = hmap.GetLength(1);
        
        var bag = new ConcurrentBag<Point>();

        Parallel.For(0, w, x =>
        {
            var local = new List<Point>(128);
            for (int y = 0; y < h; y++)
                if (hmap[x, y] >= threshold)
                    local.Add(new Point(x, y));

            if (local.Count > 0)
                foreach (var p in local) bag.Add(p);
        });
        
        var candidates = bag
            .ToArray()
            .OrderBy(p => p.X)
            .ThenBy(p => p.Y)
            .ToList();
        
        var rnd = new Random(seed);
        var selected = candidates
            .OrderBy(_ => rnd.Next())
            .Take(count)
            .ToList();
        
        var names = new string[selected.Count];
        Parallel.For(0, selected.Count, i =>
        {
            var p = selected[i];
            names[i] = CityNameGenerator.Generate(seed, p.X, p.Y);
        });

        return (selected, names.ToList());
    }
}