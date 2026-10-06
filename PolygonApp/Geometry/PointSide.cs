using PolygonApp.Models;

namespace PolygonApp.Geometry;

public enum SideOfEdge { Left, Right, OnEdge }

public static class PointSide
{
    public static SideOfEdge Find(Edge edge, Point2D point)
    {
        // Negate the screen cross to obtain conventional mathematical left/right.
        double cross = -GeometryHelper.Cross(edge.Start, edge.End, point);
        if (Math.Abs(cross) < GeometryHelper.Epsilon) return SideOfEdge.OnEdge;
        return cross > 0 ? SideOfEdge.Left : SideOfEdge.Right;
    }
}
