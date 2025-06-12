using System.Collections.Generic;
using System.Text;
using Core.Algorithms.Graph;
using Core.Generation.City;

namespace Core.Render.Vector
{
    public static class CityRenderer
    {
        public static string RenderCities(
            float[,] hmap,
            int seed,
            int count,
            int cellSize,
            float threshold,
            double initialBudget,
            double costPerKm,
            double rewardPerCity)
        {
            var (selected, names) = CityGenerator.Generate(hmap, seed, count, threshold);

            var (startIdx, connections) = EconomicAnalyzer.GetBestEconomicTree(
                selected, hmap, threshold, initialBudget, costPerKm, rewardPerCity);

            return RenderSvg(selected, names, connections, startIdx, cellSize);
        }
        public static string RenderCities(
            List<Point> selected,
            List<string> names,
            float[,] hmap,
            float threshold,
            double initialBudget,
            double costPerKm,
            double rewardPerCity,
            int cellSize)
        {
            var (startIdx, links) = EconomicAnalyzer.GetBestEconomicTree(
                selected, hmap, threshold, initialBudget, costPerKm, rewardPerCity);

            return RenderSvg(selected, names, links, startIdx, cellSize);
        }

        
        private static string RenderSvg(
            List<Point> selected,
            List<string> names,
            List<(int from, int to)> connections,
            int startIdx,
            int cellSize)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<svg xmlns='http://www.w3.org/2000/svg'>");
            
            sb.AppendLine("<g class='cities' font-family='sans-serif' font-size='6'>");
            for (int i = 0; i < selected.Count; i++)
            {
                var p = selected[i];
                double cx = p.X * cellSize + cellSize * 0.5;
                double cy = p.Y * cellSize + cellSize * 0.5;
                string fill = (i == startIdx) ? "#FFD700" : "green";
                string name = names[i];
                sb.AppendLine($"  <g class='city' data-name='{name}' data-index='{i}'>");
                sb.AppendLine($"    <circle cx='{cx}' cy='{cy}' r='{cellSize / 2.0}' fill='{fill}' />");
                sb.AppendLine($"    <text x='{cx}' y='{cy + 7}' text-anchor='middle' fill='black'>{name}</text>");
                sb.AppendLine("  </g>");
            }
            sb.AppendLine("</g>");

            sb.AppendLine("</svg>");
            return sb.ToString();
        }
    }
}