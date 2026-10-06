using PolygonApp.Models;

namespace PolygonApp.Geometry;

public static class PointInPolygon
{
    public static PointLocation CheckConvex(PolygonModel polygon, Point2D point)
    {
        if (polygon.Vertices.Count < 3) return PointLocation.Outside;
        int sign = 0;
        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            var edge = new Edge(polygon.Vertices[i], polygon.Vertices[(i + 1) % polygon.Vertices.Count]);
            if (GeometryHelper.IsPointOnSegment(point, edge)) return PointLocation.Boundary;
            int currentSign = GeometryHelper.Cross(edge.Start, edge.End, point) > 0 ? 1 : -1;
            if (sign == 0) sign = currentSign;
            else if (sign != currentSign) return PointLocation.Outside;
        }
        return PointLocation.Inside;
    }

    public static PointLocation CheckAny(PolygonModel polygon, Point2D point)
    {
        if (polygon.Vertices.Count < 3) return PointLocation.Outside;
        bool inside = false;
        for (int i = 0, j = polygon.Vertices.Count - 1; i < polygon.Vertices.Count; j = i++)
        {
            var a = polygon.Vertices[j]; var b = polygon.Vertices[i];
            if (GeometryHelper.IsPointOnSegment(point, new Edge(a, b))) return PointLocation.Boundary;
            if ((a.Y > point.Y) != (b.Y > point.Y))
            {
                double crossingX = (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X;
                if (point.X < crossingX) inside = !inside;
            }
        }
        return inside ? PointLocation.Inside : PointLocation.Outside;
    }
}
