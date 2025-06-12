using System.Collections.Generic;

namespace Core.Geometry.Contour;
public sealed record Polyline(List<(float X, float Y)> Points);