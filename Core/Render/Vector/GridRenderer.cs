using System.Text;

namespace Core.Render.Vector
{
    public static class GridRenderer
    {
        public static string ToSvgGrid(
            int columns,
            int rows,
            int pxPerCell,
            string strokeColor = "#555",
            float strokeWidth = 0.25f,
            float strokeOpacity = 0.1f)
        {
            int widthPx = columns * pxPerCell;
            int heightPx = rows * pxPerCell;
            var sb = new StringBuilder();

            sb.AppendLine($"<svg width=\"{widthPx}\" height=\"{heightPx}\" xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {widthPx} {heightPx}\">");
            
            for (int i = 0; i <= columns; i++)
            {
                float x = i * pxPerCell;
                sb.AppendLine($"  <line x1=\"{x}\" y1=\"0\" x2=\"{x}\" y2=\"{heightPx}\" stroke=\"{strokeColor}\" stroke-width=\"{strokeWidth}\" stroke-opacity=\"{strokeOpacity}\" />");
            }
            
            for (int j = 0; j <= rows; j++)
            {
                float y = j * pxPerCell;
                sb.AppendLine($"  <line x1=\"0\" y1=\"{y}\" x2=\"{widthPx}\" y2=\"{y}\" stroke=\"{strokeColor}\" stroke-width=\"{strokeWidth}\" stroke-opacity=\"{strokeOpacity}\" />");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        
        public static string MergeSvgs(string baseSvg, string overlaySvg)
        {
            int start1 = baseSvg.IndexOf('>') + 1;
            int end1 = baseSvg.LastIndexOf("</svg>");
            string body1 = baseSvg[start1..end1];
            
            string body2;
            if (overlaySvg.TrimStart().StartsWith("<svg"))
            {
                int start2 = overlaySvg.IndexOf('>') + 1;
                int end2 = overlaySvg.LastIndexOf("</svg>");
                body2 = overlaySvg[start2..end2];
            }
            else
            {
                body2 = overlaySvg;
            }
            
            string header = baseSvg[..start1];
            return header
                + "\n" + body1
                + "\n" + body2
                + "\n</svg>";
        }
    }
}
