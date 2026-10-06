namespace PolygonApp.Models;

public class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
    public Point2D(double x, double y) { X = x; Y = y; }
    public Point2D Copy() => new(X, Y);
}
