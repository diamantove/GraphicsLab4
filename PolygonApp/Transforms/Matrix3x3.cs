using PolygonApp.Models;

namespace PolygonApp.Transforms;

public class Matrix3x3
{
    private readonly double[,] _values;
    private Matrix3x3(double[,] values) { _values = values; }

    public static Matrix3x3 Identity() => new(new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } });
    public static Matrix3x3 Translation(double dx, double dy) => new(new double[,] { { 1, 0, dx }, { 0, 1, dy }, { 0, 0, 1 } });
    public static Matrix3x3 Scaling(double sx, double sy) => new(new double[,] { { sx, 0, 0 }, { 0, sy, 0 }, { 0, 0, 1 } });
    public static Matrix3x3 Rotation(double degrees)
    {
        // With screen Y going down, positive angle looks clockwise.
        double radians = degrees * Math.PI / 180.0, cos = Math.Cos(radians), sin = Math.Sin(radians);
        return new(new double[,] { { cos, -sin, 0 }, { sin, cos, 0 }, { 0, 0, 1 } });
    }
    public static Matrix3x3 operator *(Matrix3x3 left, Matrix3x3 right)
    {
        var result = new double[3, 3];
        for (int row = 0; row < 3; row++) for (int column = 0; column < 3; column++) for (int i = 0; i < 3; i++) result[row, column] += left._values[row, i] * right._values[i, column];
        return new(result);
    }
    public Point2D Apply(Point2D point)
    {
        double x = _values[0, 0] * point.X + _values[0, 1] * point.Y + _values[0, 2];
        double y = _values[1, 0] * point.X + _values[1, 1] * point.Y + _values[1, 2];
        double w = _values[2, 0] * point.X + _values[2, 1] * point.Y + _values[2, 2];
        return new(x / w, y / w);
    }
}
