using PolygonApp.Models;

namespace PolygonApp.Transforms;

public static class AffineTransform
{
    public static void Apply(PolygonModel polygon, Matrix3x3 matrix)
    {
        for (int i = 0; i < polygon.Vertices.Count; i++) polygon.Vertices[i] = matrix.Apply(polygon.Vertices[i]);
    }
    public static Matrix3x3 AroundPoint(Matrix3x3 operation, Point2D center) =>
        Matrix3x3.Translation(center.X, center.Y) * operation * Matrix3x3.Translation(-center.X, -center.Y);
}
