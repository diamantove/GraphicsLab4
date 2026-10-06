namespace PolygonApp.Models;

public enum PointLocation { Inside, Outside, Boundary }
public enum IntersectionKind { Point, None, Parallel, Coincident }
public record IntersectionResult(IntersectionKind Kind, Point2D? Point = null);
