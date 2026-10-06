using PolygonApp.Models;

namespace PolygonApp.Geometry;

public static class GeometryHelper
{
    public const double Epsilon = 1e-8;

    // Cross(A->B, A->P). Its sign is inverted relative to math coordinates because WPF Y grows down.
    public static double Cross(Point2D a, Point2D b, Point2D p) =>
        (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    public static bool IsPointOnSegment(Point2D point, Edge edge)
    {
        if (Math.Abs(Cross(edge.Start, edge.End, point)) > Epsilon) return false;
        return point.X >= Math.Min(edge.Start.X, edge.End.X) - Epsilon && point.X <= Math.Max(edge.Start.X, edge.End.X) + Epsilon &&
               point.Y >= Math.Min(edge.Start.Y, edge.End.Y) - Epsilon && point.Y <= Math.Max(edge.Start.Y, edge.End.Y) + Epsilon;
    }

    public static double DistanceToSegment(Point2D point, Edge edge)
    {
        double dx = edge.End.X - edge.Start.X, dy = edge.End.Y - edge.Start.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared < Epsilon) return Length(point.X - edge.Start.X, point.Y - edge.Start.Y);
        double t = Math.Clamp(((point.X - edge.Start.X) * dx + (point.Y - edge.Start.Y) * dy) / lengthSquared, 0, 1);
        return Length(point.X - edge.Start.X - t * dx, point.Y - edge.Start.Y - t * dy);
    }

    public static double Length(double x, double y) => Math.Sqrt(x * x + y * y);
}
