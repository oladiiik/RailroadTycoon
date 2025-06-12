using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Algorithms.Graph
{
    #region простий Point
    public class Point
    {
        public int X { get; }
        public int Y { get; }

        public Point(int x, int y) { X = x; Y = y; }

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public double DistanceTo(Point o)
        {
            int dx = X - o.X, dy = Y - o.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
    #endregion

    public static class EconomicAnalyzer
    {
        public static (int startIdx, List<(int from, int to)> connections)
            GetBestEconomicTree(
                IList<Point> cities,
                float[,]     hmap,
                float        threshold,
                double       initialBudget,
                double       costPerKm,
                double       rewardPerCity)
        {
            int n = cities.Count;
            var graph = BuildGraph(cities, hmap, threshold);   

            int   bestStart = -1;
            int   bestScore = -1;
            List<(int,int)> bestTree = null;

            object sync = new();

            Parallel.For(0, n, start =>
            {
                var tree = BuildTreeFromStart(
                    start, graph, cities, threshold,
                    initialBudget, costPerKm, rewardPerCity);

                int connected = tree
                    .SelectMany(e => new[] { e.from, e.to })
                    .Append(start)
                    .Distinct()
                    .Count();
                
                lock (sync)
                {
                    if (connected > bestScore)
                    {
                        bestScore = connected;
                        bestStart = start;
                        bestTree  = tree;
                    }
                }
            });

            return (bestStart, bestTree ?? new List<(int,int)>());
        }
        
        private static List<(int from, int to)> BuildTreeFromStart(
            int           start,
            Dictionary<int, List<(int to, double len)>> graph,
            IList<Point>  cities,
            float         threshold,
            double        initialBudget,
            double        costPerKm,
            double        rewardPerCity)
        {
            var connected = new HashSet<int> { start };
            double budget = initialBudget;
            var result    = new List<(int from, int to)>();
            
            var pq = new SortedSet<(double cost, int from, int to)>(
                Comparer<(double cost, int from, int to)>.Create((a, b) =>
                    a.cost != b.cost ? a.cost.CompareTo(b.cost)
                    : a.from != b.from ? a.from.CompareTo(b.from)
                    : a.to.CompareTo(b.to)));
            
            foreach (var (to, len) in graph[start])
                pq.Add((Math.Max(0, len * costPerKm - rewardPerCity), start, to));

            while (pq.Count > 0)
            {
                var (edgeCost, from, to) = pq.Min;
                pq.Remove(pq.Min);

                if (connected.Contains(to)) continue;
                if (budget < edgeCost)     continue;

                budget -= edgeCost;
                connected.Add(to);
                result.Add((from, to));

                foreach (var (nei, len) in graph[to])
                    if (!connected.Contains(nei))
                        pq.Add((Math.Max(0, len * costPerKm - rewardPerCity), to, nei));
            }
            return result;
        }
        
        private static Dictionary<int, List<(int to, double length)>> BuildGraph(
            IList<Point> cities, float[,] hmap, float threshold)
        {
            int n = cities.Count;
            
            var edges = new ConcurrentBag<(int i, int j, double len)>();

            Parallel.For(0, n, i =>
            {
                var local = new List<(int, int, double)>();
                for (int j = i + 1; j < n; j++)
                    if (HasDirectPath(cities[i], cities[j], hmap, threshold))
                        local.Add((i, j, cities[i].DistanceTo(cities[j])));
                
                foreach (var e in local) edges.Add(e);
            });
            
            var graph = Enumerable.Range(0, n)
                                  .ToDictionary(k => k, _ => new List<(int,double)>());

            foreach (var (i, j, len) in edges)
            {
                graph[i].Add((j, len));
                graph[j].Add((i, len));
            }
            return graph;
        }
        
        public static bool HasDirectPath(Point a, Point b, float[,] hmap, float thr)
        {
            int x0 = a.X, y0 = a.Y,
                x1 = b.X, y1 = b.Y,
                dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1,
                dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1,
                err = dx + dy;

            while (true)
            {
                if (hmap[x0, y0] < thr) return false;
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
            return true;
        }
    }
}
