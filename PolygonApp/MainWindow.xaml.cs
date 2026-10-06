using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PolygonApp.Geometry;
using PolygonApp.Models;
using PolygonApp.Transforms;

namespace PolygonApp;

public enum ToolMode
{
    None,
    CreatePolygon,
    SelectObject,
    EdgeIntersection,
    PointInConvexPolygon,
    PointInPolygon,
    PointSideOfEdge
}

public partial class MainWindow : Window
{
    private const double HitDistance = 8;

    private readonly List<PolygonModel> _polygons = [];
    private readonly List<(Point2D Point, string Text, Brush Brush)> _marks = [];
    private readonly List<Point2D> _pendingPoints = [];
    private readonly List<Edge> _testedIntersectionEdges = [];

    private PolygonModel? _buildingPolygon;
    private PolygonModel? _selectedPolygon;
    private Edge? _firstIntersectionEdge;
    private Edge? _sideEdge;
    private ToolMode _mode;

    public MainWindow()
    {
        InitializeComponent();
        SetMode(ToolMode.None, "Готово к работе.");
    }

    private void CreatePolygon_Click(object sender, RoutedEventArgs e)
    {
        FinishPolygon(false);
        _buildingPolygon = new PolygonModel();
        SetMode(ToolMode.CreatePolygon, "Кликайте по Canvas, затем нажмите «Завершить».");
        Redraw();
    }

    private void FinishPolygon_Click(object sender, RoutedEventArgs e)
    {
        FinishPolygon(true);
    }

    private void FinishPolygon(bool showMessage)
    {
        if (_buildingPolygon is null)
        {
            if (showMessage)
            {
                SetStatus("Нет строящегося объекта.");
            }

            return;
        }

        if (_buildingPolygon.Vertices.Count == 0)
        {
            _buildingPolygon = null;
            SetStatus("Нет вершин.");
            return;
        }

        _polygons.Add(_buildingPolygon);
        _selectedPolygon = _buildingPolygon;
        int vertexCount = _buildingPolygon.Vertices.Count;
        _buildingPolygon = null;

        SetMode(ToolMode.None, $"Создан и выбран объект: {vertexCount} вершин.");
        Redraw();
    }

    private void ClearScene_Click(object sender, RoutedEventArgs e)
    {
        _polygons.Clear();
        _marks.Clear();
        _pendingPoints.Clear();
        _testedIntersectionEdges.Clear();
        _buildingPolygon = null;
        _selectedPolygon = null;
        _firstIntersectionEdge = null;
        _sideEdge = null;

        SetMode(ToolMode.None, "Сцена очищена.");
        Redraw();
    }

    private void SelectObject_Click(object sender, RoutedEventArgs e)
    {
        SetMode(ToolMode.SelectObject, "Кликните по объекту для выбора.");
    }

    private void EdgeIntersection_Click(object sender, RoutedEventArgs e)
    {
        _pendingPoints.Clear();
        _testedIntersectionEdges.Clear();
        _firstIntersectionEdge = null;

        SetMode(ToolMode.EdgeIntersection, "Задайте первое ребро двумя кликами.");
        Redraw();
    }

    private void ConvexPoint_Click(object sender, RoutedEventArgs e)
    {
        if (CanCheckSelectedPolygon())
        {
            SetMode(ToolMode.PointInConvexPolygon, "Кликайте точки: проверка выпуклого полигона.");
        }
    }

    private void PolygonPoint_Click(object sender, RoutedEventArgs e)
    {
        if (CanCheckSelectedPolygon())
        {
            SetMode(ToolMode.PointInPolygon, "Кликайте точки: Even-Odd rule.");
        }
    }

    private void PointSide_Click(object sender, RoutedEventArgs e)
    {
        _pendingPoints.Clear();
        _sideEdge = null;

        SetMode(ToolMode.PointSideOfEdge, "Задайте ребро двумя кликами, затем проверяйте точки.");
        Redraw();
    }

    private bool CanCheckSelectedPolygon()
    {
        if (_selectedPolygon?.Vertices.Count >= 3)
        {
            return true;
        }

        SetStatus("Выберите полигон с 3+ вершинами в режиме «Выбор объекта».");
        return false;
    }

    private void SceneCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Point clickPosition = e.GetPosition(SceneCanvas);
        var point = new Point2D(clickPosition.X, clickPosition.Y);

        switch (_mode)
        {
            case ToolMode.CreatePolygon:
                AddPolygonVertex(point);
                break;
            case ToolMode.SelectObject:
                SelectPolygon(point);
                break;
            case ToolMode.EdgeIntersection:
                HandleIntersectionClick(point);
                break;
            case ToolMode.PointSideOfEdge:
                HandleSideClick(point);
                break;
            case ToolMode.PointInConvexPolygon:
                ShowPointResult(point, PointInPolygon.CheckConvex(_selectedPolygon!, point));
                break;
            case ToolMode.PointInPolygon:
                ShowPointResult(point, PointInPolygon.CheckAny(_selectedPolygon!, point));
                break;
        }
    }

    private void AddPolygonVertex(Point2D point)
    {
        _buildingPolygon!.Vertices.Add(point);
        SetStatus("Вершина добавлена.");
        Redraw();
    }

    private void SelectPolygon(Point2D point)
    {
        _selectedPolygon = _polygons.LastOrDefault(polygon => IsHit(polygon, point));
        SetStatus(_selectedPolygon is null
            ? "Объект не найден."
            : $"Выбрано вершин: {_selectedPolygon.Vertices.Count}.");
        Redraw();
    }

    private static bool IsHit(PolygonModel polygon, Point2D point)
    {
        if (polygon.Vertices.Count == 1)
        {
            Point2D vertex = polygon.Vertices[0];
            return GeometryHelper.Length(point.X - vertex.X, point.Y - vertex.Y) <= HitDistance;
        }

        if (polygon.Vertices.Count == 2)
        {
            return GeometryHelper.DistanceToSegment(point, new Edge(polygon.Vertices[0], polygon.Vertices[1])) <= HitDistance;
        }

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            Point2D start = polygon.Vertices[i];
            Point2D end = polygon.Vertices[(i + 1) % polygon.Vertices.Count];

            if (GeometryHelper.DistanceToSegment(point, new Edge(start, end)) <= HitDistance)
            {
                return true;
            }
        }

        return PointInPolygon.CheckAny(polygon, point) != PointLocation.Outside;
    }

    private void HandleIntersectionClick(Point2D point)
    {
        _pendingPoints.Add(point);

        if (_pendingPoints.Count < 2)
        {
            SetStatus("Первая точка ребра задана.");
            Redraw();
            return;
        }

        var edge = new Edge(_pendingPoints[0], _pendingPoints[1]);
        _pendingPoints.Clear();

        if (_firstIntersectionEdge is null)
        {
            _firstIntersectionEdge = edge;
            SetStatus("Первое ребро задано. Следующие 2 клика — второе ребро.");
        }
        else
        {
            _testedIntersectionEdges.Add(edge);
            IntersectionResult result = EdgeIntersection.Find(_firstIntersectionEdge, edge);

            if (result.Point is not null)
            {
                _marks.Add((result.Point, "Пересечение", Brushes.Red));
            }

            SetStatus(result.Kind switch
            {
                IntersectionKind.Point => "Ребра пересекаются.",
                IntersectionKind.None => "Отрезки не пересекаются.",
                IntersectionKind.Parallel => "Ребра параллельны.",
                _ => "Ребра совпадают по прямой."
            });
        }

        Redraw();
    }

    private void HandleSideClick(Point2D point)
    {
        if (_sideEdge is null)
        {
            _pendingPoints.Add(point);

            if (_pendingPoints.Count == 1)
            {
                SetStatus("Первая точка ребра задана.");
                Redraw();
                return;
            }

            _sideEdge = new Edge(_pendingPoints[0], _pendingPoints[1]);
            _pendingPoints.Clear();
            SetStatus("Ребро задано. Следующие клики — проверка.");
            Redraw();
            return;
        }

        SideOfEdge result = PointSide.Find(_sideEdge, point);
        _marks.Add((point, result.ToString(), Brushes.DarkMagenta));
        SetStatus($"Результат: {result}. Инструмент остается активным.");
        Redraw();
    }

    private void ShowPointResult(Point2D point, PointLocation result)
    {
        Brush brush = result switch
        {
            PointLocation.Inside => Brushes.ForestGreen,
            PointLocation.Boundary => Brushes.DarkOrange,
            _ => Brushes.Crimson
        };

        _marks.Add((point, result.ToString(), brush));
        SetStatus($"Результат: {result}. Инструмент остается активным.");
        Redraw();
    }

    private void Translate_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out PolygonModel polygon) || !TryReadNumber(DxBox, out double dx) || !TryReadNumber(DyBox, out double dy))
        {
            return;
        }

        AffineTransform.Apply(polygon, Matrix3x3.Translation(dx, dy));
        SetStatus("Выполнено матрицей переноса.");
        Redraw();
    }

    private void RotatePoint_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out PolygonModel polygon) || !TryReadNumber(AngleBox, out double angle) || !TryReadCenter(out Point2D center))
        {
            return;
        }

        ApplyTransform(polygon, AffineTransform.AroundPoint(Matrix3x3.Rotation(angle), center), "T(C) × R × T(-C)");
    }

    private void RotateCenter_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out PolygonModel polygon) || !TryReadNumber(AngleBox, out double angle))
        {
            return;
        }

        ApplyTransform(polygon, AffineTransform.AroundPoint(Matrix3x3.Rotation(angle), GetCenter(polygon)), "поворот вокруг центра");
    }

    private void ScalePoint_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out PolygonModel polygon) || !TryReadScale(out double sx, out double sy) || !TryReadCenter(out Point2D center))
        {
            return;
        }

        ApplyTransform(polygon, AffineTransform.AroundPoint(Matrix3x3.Scaling(sx, sy), center), "T(C) × S × T(-C)");
    }

    private void ScaleCenter_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out PolygonModel polygon) || !TryReadScale(out double sx, out double sy))
        {
            return;
        }

        ApplyTransform(polygon, AffineTransform.AroundPoint(Matrix3x3.Scaling(sx, sy), GetCenter(polygon)), "масштабирование вокруг центра");
    }

    private void ApplyTransform(PolygonModel polygon, Matrix3x3 matrix, string description)
    {
        AffineTransform.Apply(polygon, matrix);
        SetStatus($"Выполнено: {description}.");
        Redraw();
    }

    private bool TryGetSelected(out PolygonModel polygon)
    {
        polygon = _selectedPolygon!;

        if (_selectedPolygon is not null)
        {
            return true;
        }

        SetStatus("Сначала выберите объект.");
        return false;
    }

    private bool TryReadNumber(TextBox textBox, out double value)
    {
        if (double.TryParse(textBox.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        SetStatus($"Некорректное число: {textBox.Text}");
        return false;
    }

    private bool TryReadCenter(out Point2D center)
    {
        if (TryReadNumber(CenterXBox, out double x) && TryReadNumber(CenterYBox, out double y))
        {
            center = new Point2D(x, y);
            return true;
        }

        center = null!;
        return false;
    }

    private bool TryReadScale(out double sx, out double sy)
    {
        sx = 0;
        sy = 0;
        return TryReadNumber(ScaleXBox, out sx) && TryReadNumber(ScaleYBox, out sy);
    }

    private static Point2D GetCenter(PolygonModel polygon)
    {
        if (polygon.Vertices.Count == 1)
        {
            return polygon.Vertices[0].Copy();
        }

        if (polygon.Vertices.Count == 2)
        {
            return new Point2D((polygon.Vertices[0].X + polygon.Vertices[1].X) / 2, (polygon.Vertices[0].Y + polygon.Vertices[1].Y) / 2);
        }

        double area = 0;
        double centerX = 0;
        double centerY = 0;

        for (int i = 0; i < polygon.Vertices.Count; i++)
        {
            Point2D start = polygon.Vertices[i];
            Point2D end = polygon.Vertices[(i + 1) % polygon.Vertices.Count];
            double cross = start.X * end.Y - end.X * start.Y;
            area += cross;
            centerX += (start.X + end.X) * cross;
            centerY += (start.Y + end.Y) * cross;
        }

        if (Math.Abs(area) < GeometryHelper.Epsilon)
        {
            return new Point2D(polygon.Vertices.Average(point => point.X), polygon.Vertices.Average(point => point.Y));
        }

        return new Point2D(centerX / (3 * area), centerY / (3 * area));
    }

    private void Redraw()
    {
        SceneCanvas.Children.Clear();

        foreach (PolygonModel polygon in _polygons)
        {
            DrawPolygon(polygon, polygon == _selectedPolygon);
        }

        if (_buildingPolygon is not null)
        {
            DrawPolygon(_buildingPolygon, true);
        }

        if (_firstIntersectionEdge is not null)
        {
            DrawEdge(_firstIntersectionEdge, Brushes.OrangeRed, 3);
        }

        foreach (Edge edge in _testedIntersectionEdges)
        {
            DrawEdge(edge, Brushes.SteelBlue, 3);
        }

        if (_sideEdge is not null)
        {
            DrawEdge(_sideEdge, Brushes.DarkMagenta, 3);
        }

        if (_pendingPoints.Count == 1)
        {
            DrawMark(_pendingPoints[0], string.Empty, Brushes.Gray);
        }

        foreach ((Point2D point, string text, Brush brush) in _marks)
        {
            DrawMark(point, text, brush);
        }
    }

    private void DrawPolygon(PolygonModel polygon, bool isSelected)
    {
        Brush brush = isSelected ? Brushes.DodgerBlue : Brushes.Black;

        if (polygon.Vertices.Count == 1)
        {
            DrawMark(polygon.Vertices[0], string.Empty, brush);
        }
        else if (polygon.Vertices.Count == 2)
        {
            DrawEdge(new Edge(polygon.Vertices[0], polygon.Vertices[1]), brush, isSelected ? 3 : 2);
        }
        else
        {
            var visual = new Polygon
            {
                Stroke = brush,
                StrokeThickness = isSelected ? 3 : 2,
                Fill = isSelected ? new SolidColorBrush(Color.FromArgb(25, 30, 144, 255)) : Brushes.Transparent
            };

            foreach (Point2D point in polygon.Vertices)
            {
                visual.Points.Add(new Point(point.X, point.Y));
            }

            SceneCanvas.Children.Add(visual);
        }

        foreach (Point2D point in polygon.Vertices)
        {
            DrawVertex(point, brush);
        }
    }

    private void DrawEdge(Edge edge, Brush brush, double thickness)
    {
        SceneCanvas.Children.Add(new Line
        {
            X1 = edge.Start.X,
            Y1 = edge.Start.Y,
            X2 = edge.End.X,
            Y2 = edge.End.Y,
            Stroke = brush,
            StrokeThickness = thickness
        });
    }

    private void DrawVertex(Point2D point, Brush brush)
    {
        var ellipse = new Ellipse { Width = 8, Height = 8, Fill = brush, Stroke = Brushes.White };
        Canvas.SetLeft(ellipse, point.X - 4);
        Canvas.SetTop(ellipse, point.Y - 4);
        SceneCanvas.Children.Add(ellipse);
    }

    private void DrawMark(Point2D point, string text, Brush brush)
    {
        var ellipse = new Ellipse { Width = 12, Height = 12, Fill = Brushes.White, Stroke = brush, StrokeThickness = 3 };
        Canvas.SetLeft(ellipse, point.X - 6);
        Canvas.SetTop(ellipse, point.Y - 6);
        SceneCanvas.Children.Add(ellipse);

        if (text == string.Empty)
        {
            return;
        }

        var label = new TextBlock { Text = text, Foreground = brush, FontWeight = FontWeights.SemiBold };
        Canvas.SetLeft(label, point.X + 8);
        Canvas.SetTop(label, point.Y - 9);
        SceneCanvas.Children.Add(label);
    }

    private void SetMode(ToolMode mode, string status)
    {
        _mode = mode;
        ModeText.Text = mode.ToString();
        SetStatus(status);
    }

    private void SetStatus(string status)
    {
        StatusText.Text = status;
    }
}
