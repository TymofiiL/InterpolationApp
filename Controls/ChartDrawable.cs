using InterpolationApp.Models;

namespace InterpolationApp.Controls;

public sealed class ChartDrawable : IDrawable
{
    public List<PlotSeries> Series { get; set; } = new();
    public List<(double X, double Y)> NodePoints { get; set; } = new();

    private double _viewMinX, _viewMaxX, _viewMinY, _viewMaxY;
    private bool _autoScale = true;

    public void ResetView()
    {
        _autoScale = true;
    }

    public void Zoom(double factor)
    {
        _autoScale = false;
        double cx = (_viewMinX + _viewMaxX) / 2;
        double cy = (_viewMinY + _viewMaxY) / 2;
        double hw = (_viewMaxX - _viewMinX) / 2 / factor;
        double hh = (_viewMaxY - _viewMinY) / 2 / factor;
        _viewMinX = cx - hw;
        _viewMaxX = cx + hw;
        _viewMinY = cy - hh;
        _viewMaxY = cy + hh;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float PadL = 72f, PadR = 24f, PadT = 24f, PadB = 56f;
        const int TickCount = 6;

        float plotW = dirtyRect.Width - PadL - PadR;
        float plotH = dirtyRect.Height - PadT - PadB;

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        if (Series.Count == 0 && NodePoints.Count == 0)
        {
            canvas.FontSize = 14;
            canvas.FontColor = Colors.Gray;
            canvas.DrawString("Немає даних. Введіть вузли та натисніть «Побудувати».",
                dirtyRect.Center.X, dirtyRect.Center.Y, HorizontalAlignment.Center);
            return;
        }

        if (_autoScale)
        {
            AutoScaleBounds();
        }

        double rangeX = _viewMaxX - _viewMinX;
        double rangeY = _viewMaxY - _viewMinY;
        if (rangeX < 1e-12)
        {
            _viewMinX -= 1;
            _viewMaxX += 1;
            rangeX = 2;
        }
        if (rangeY < 1e-12)
        {
            _viewMinY -= 1;
            _viewMaxY += 1;
            rangeY = 2;
        }

        float ToSx(double x) => PadL + (float)((x - _viewMinX) / rangeX * plotW);
        float ToSy(double y) => PadT + plotH - (float)((y - _viewMinY) / rangeY * plotH);

        DrawGrid(canvas, PadL, PadT, plotW, plotH, TickCount);
        DrawAxes(canvas, PadL, PadT, plotW, plotH);
        DrawTickLabels(canvas, PadL, PadT, plotH, TickCount, _viewMinX, _viewMinY, rangeX, rangeY, ToSx, ToSy);
        DrawSeries(canvas, ToSx, ToSy, PadT, plotH);
        DrawNodes(canvas, ToSx, ToSy);
        DrawLegend(canvas, PadL + 6, PadT + 6);
    }

    private static void DrawGrid(ICanvas canvas, float padL, float padT, float plotW, float plotH, int tickCount)
    {
        canvas.StrokeColor = Color.FromArgb("#E0E0E0");
        canvas.StrokeSize = 1;
        for (int t = 0; t <= tickCount; t++)
        {
            float gx = padL + t * plotW / tickCount;
            float gy = padT + t * plotH / tickCount;
            canvas.DrawLine(gx, padT, gx, padT + plotH);
            canvas.DrawLine(padL, gy, padL + plotW, gy);
        }
    }

    private static void DrawAxes(ICanvas canvas, float padL, float padT, float plotW, float plotH)
    {
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(padL, padT, padL, padT + plotH);
        canvas.DrawLine(padL, padT + plotH, padL + plotW, padT + plotH);
        canvas.FontSize = 12;
        canvas.DrawString("x", padL + plotW + 10, padT + plotH, HorizontalAlignment.Left);
        canvas.DrawString("P(x)", padL - 10, padT - 14, HorizontalAlignment.Center);
    }

    private static void DrawTickLabels(ICanvas canvas, float padL, float padT, float plotH,
        int tickCount, double viewMinX, double viewMinY, double rangeX, double rangeY,
        Func<double, float> toSx, Func<double, float> toSy)
    {
        canvas.FontSize = 10;
        canvas.FontColor = Colors.Black;
        for (int t = 0; t <= tickCount; t++)
        {
            double xVal = viewMinX + t * rangeX / tickCount;
            float sx = toSx(xVal);
            canvas.DrawLine(sx, padT + plotH, sx, padT + plotH + 4);
            canvas.DrawString(xVal.ToString("G4"), sx, padT + plotH + 7, HorizontalAlignment.Center);

            double yVal = viewMinY + t * rangeY / tickCount;
            float sy = toSy(yVal);
            canvas.DrawLine(padL - 4, sy, padL, sy);
            canvas.DrawString(yVal.ToString("G4"), padL - 7, sy, HorizontalAlignment.Right);
        }
    }

    private void DrawSeries(ICanvas canvas, Func<double, float> toSx, Func<double, float> toSy, float padT, float plotH)
    {
        foreach (var series in Series)
        {
            if (series.Xs.Length < 2)
            {
                continue;
            }

            canvas.StrokeColor = series.Color;
            canvas.StrokeSize = 2.5f;

            var path = new PathF();
            bool first = true;

            for (int i = 0; i < series.Xs.Length; i++)
            {
                if (double.IsNaN(series.Ys[i]) || double.IsInfinity(series.Ys[i]))
                {
                    first = true;
                    continue;
                }
                float sx = toSx(series.Xs[i]);
                float sy = Math.Clamp(toSy(series.Ys[i]), padT - 10, padT + plotH + 10);

                if (first)
                {
                    path.MoveTo(sx, sy);
                    first = false;
                }
                else
                {
                    path.LineTo(sx, sy);
                }
            }
            canvas.DrawPath(path);
        }
    }

    private void DrawNodes(ICanvas canvas, Func<double, float> toSx, Func<double, float> toSy)
    {
        canvas.StrokeColor = Color.FromArgb("#212121");
        canvas.FillColor = Color.FromArgb("#FF6F00");
        canvas.StrokeSize = 1.5f;
        foreach (var (nx, ny) in NodePoints)
        {
            float sx = toSx(nx);
            float sy = toSy(ny);
            canvas.FillCircle(sx, sy, 5);
            canvas.DrawCircle(sx, sy, 5);
        }
    }

    private void AutoScaleBounds()
    {
        var allX = Series.SelectMany(s => s.Xs)
                         .Concat(NodePoints.Select(p => p.X)).ToList();
        var allY = Series.SelectMany(s => s.Ys.Where(y => !double.IsNaN(y) && !double.IsInfinity(y)))
                         .Concat(NodePoints.Select(p => p.Y)).ToList();

        if (allX.Count == 0)
        {
            return;
        }

        _viewMinX = allX.Min();
        _viewMaxX = allX.Max();
        _viewMinY = allY.Min();
        _viewMaxY = allY.Max();

        double px = (_viewMaxX - _viewMinX) * 0.05;
        double py = (_viewMaxY - _viewMinY) * 0.05;
        _viewMinX -= px;
        _viewMaxX += px;
        _viewMinY -= py;
        _viewMaxY += py;
    }

    private void DrawLegend(ICanvas canvas, float x, float y)
    {
        const float LineH = 20f, SwatchW = 28f, Gap = 4f;
        float rowY = y;

        canvas.FillColor = Color.FromArgb("#FF6F00");
        canvas.FillCircle(x + 5, rowY + 6, 5);
        canvas.FontSize = 11;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("Вузли", x + SwatchW, rowY + 2, HorizontalAlignment.Left);
        rowY += LineH;

        foreach (var s in Series)
        {
            canvas.StrokeColor = s.Color;
            canvas.StrokeSize = 3;
            canvas.DrawLine(x, rowY + 7, x + SwatchW - Gap, rowY + 7);
            canvas.FontSize = 11;
            canvas.FontColor = Colors.Black;
            canvas.DrawString(s.Name, x + SwatchW, rowY + 2, HorizontalAlignment.Left);
            rowY += LineH;
        }
    }
}
