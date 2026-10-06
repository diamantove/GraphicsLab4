using PolygonApp.Models;

namespace PolygonApp.Geometry;

public static class EdgeIntersection
{
    public static IntersectionResult Find(Edge first, Edge second)
    {
        double rx = first.End.X - first.Start.X, ry = first.End.Y - first.Start.Y;
        double sx = second.End.X - second.Start.X, sy = second.End.Y - second.Start.Y;
        double denominator = rx * sy - ry * sx;
        double qpx = second.Start.X - first.Start.X, qpy = second.Start.Y - first.Start.Y;
        if (Math.Abs(denominator) < GeometryHelper.Epsilon)
            return Math.Abs(qpx * ry - qpy * rx) < GeometryHelper.Epsilon
                ? new(IntersectionKind.Coincident) : new(IntersectionKind.Parallel);
        double t = (qpx * sy - qpy * sx) / denominator;
        double u = (qpx * ry - qpy * rx) / denominator;
        if (t < -GeometryHelper.Epsilon || t > 1 + GeometryHelper.Epsilon || u < -GeometryHelper.Epsilon || u > 1 + GeometryHelper.Epsilon)
            return new(IntersectionKind.None);
        return new(IntersectionKind.Point, new(first.Start.X + t * rx, first.Start.Y + t * ry));
    }
}
