using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Core.Geometry.Contour;


namespace Core.Render.Vector
{
    public static class SvgRenderer
    {
        private const double SimplifyEpsilon = 1.0;  

        public static string ToSvg(
            IEnumerable<Polyline> lines,
            int gridX, int gridY,
            int pxPerCell = 1,
            int strokeWidth = 1,
            string strokeColor = "#423b35")
        {
            int w = gridX * pxPerCell, h = gridY * pxPerCell;
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {w} {h}\" style=\"position:absolute;pointer-events:none;\">");
            sb.Append("  <path d=\"");

            foreach (var pl in lines)
            {
                var ptsRaw = pl.Points;
                if (ptsRaw.Count < 2) continue;
                
                var pts = Simplify(ptsRaw, SimplifyEpsilon)
                          .Select(p => (X: p.X * pxPerCell, Y: p.Y * pxPerCell))
                          .ToList();

                sb.AppendFormat(CultureInfo.InvariantCulture, "M {0:F1},{1:F1}", pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Count; i++)
                    sb.AppendFormat(CultureInfo.InvariantCulture, " L {0:F1},{1:F1}", pts[i].X, pts[i].Y);
            }

            sb.Append($"\" fill=\"none\" stroke=\"{strokeColor}\" stroke-width=\"{strokeWidth}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>\n</svg>");
            return sb.ToString();
        }
        
        private static List<(float X, float Y)> Simplify(IList<(float X, float Y)> points, double eps)
        {
            if (points.Count < 3)
                return new List<(float, float)>(points);
            
            double maxDist = 0;
            int    index   = -1;
            var    A       = points[0];
            var    B       = points[^1];

            for (int i = 1; i < points.Count - 1; i++)
            {
                double d = PerpDistance(points[i], A, B);
                if (d > maxDist)
                {
                    maxDist = d;
                    index   = i;
                }
            }

            if (maxDist <= eps)
            {
                return new List<(float, float)> { A, B };
            }
            
            var left  = Simplify(points.Take(index + 1).ToList(), eps);
            var right = Simplify(points.Skip(index).ToList(),       eps);
            
            return left.Take(left.Count - 1)
                       .Concat(right)
                       .ToList();
        }
        
        private static double PerpDistance(
            (float X, float Y) P,
            (float X, float Y) A,
            (float X, float Y) B)
        {
            double dx  = B.X - A.X;
            double dy  = B.Y - A.Y;
            double num = Math.Abs(dy * P.X - dx * P.Y + B.X * A.Y - B.Y * A.X);
            double den = Math.Sqrt(dx * dx + dy * dy);
            return den == 0 ? 0 : num / den;
        }
    }
}
