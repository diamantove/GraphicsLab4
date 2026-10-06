namespace PolygonApp.Models;

public class PolygonModel
{
    public List<Point2D> Vertices { get; } = [];
    public PolygonModel() { }
    public PolygonModel(IEnumerable<Point2D> vertices) { Vertices.AddRange(vertices); }
}
