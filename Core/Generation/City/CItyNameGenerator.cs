using System;

namespace Core.Generation.City;

public static class CityNameGenerator
{
    private static readonly string[] Prefixes =
    {
        "Novi", "Green", "River", "Stone", "High",
        "Lake", "Silver", "Red", "Wind", "Bright",
        "Oak", "Wolf", "Sun", "Moon", "Star"
    };
    private static readonly string[] Suffixes =
    {
        "dale", "ford", "ville", "burg", "port",
        "ton", "haven", "crest", "wood", "helm",
        "mouth", "field", "bridge", "peak", "grove"
    };

    public static string Generate(int seed, int x, int y)
    {
        int h = ((seed * 31) ^ (x * 17) ^ (y * 13)) & 0x7FFFFFFF;
        var rnd2 = new Random(h);
        return Prefixes[rnd2.Next(Prefixes.Length)] + Suffixes[rnd2.Next(Suffixes.Length)];
    }
}