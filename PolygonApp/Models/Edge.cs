namespace PolygonApp.Models;

public class Edge
{
    public Point2D Start { get; }
    public Point2D End { get; }
    public Edge(Point2D start, Point2D end) { Start = start; End = end; }
}
