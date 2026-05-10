namespace InterpolationApp.Controls;

public sealed class ComplexityDrawable : IDrawable
{
    private int[] _nodeCounts = Array.Empty<int>();
    private double[][] _times = Array.Empty<double[]>();
    private string[] _names = Array.Empty<string>();
    private Color[] _colors = Array.Empty<Color>();

    internal void SetData(int[] nodeCounts, double[][] times, string[] names, Color[] colors)
    {
        _nodeCounts = nodeCounts;
        _times = times;
        _names = names;
        _colors = colors;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float PadL = 78f, PadR = 24f, PadT = 24f, PadB = 56f;

        float plotW = dirtyRect.Width - PadL - PadR;
        float plotH = dirtyRect.Height - PadT - PadB;

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        if (_nodeCounts.Length == 0)
        {
            DrawEmptyMessage(canvas, dirtyRect);
            return;
        }

        int M = _names.Length;
        int K = _nodeCounts.Length;
        double maxT = ComputeMaxTime(M, K);

        double minX = _nodeCounts[0];
        double maxX = _nodeCounts[K - 1];
        double rangeX = maxX - minX;
        if (rangeX < 1e-9)
        {
            rangeX = 1;
        }

        float ToSx(double n) => PadL + (float)((n - minX) / rangeX * plotW);
        float ToSy(double t) => PadT + plotH - (float)(t / maxT * plotH);

        DrawGridLines(canvas, PadL, PadT, plotW, plotH);
        DrawAxes(canvas, PadL, PadT, plotW, plotH);
        DrawXLabels(canvas, PadT, plotH, K, ToSx);
        DrawYLabels(canvas, PadL, maxT, ToSy);
        DrawAxisTitles(canvas, PadL, PadT, plotW, plotH);
        DrawSeries(canvas, M, K, ToSx, ToSy);
        DrawLegend(canvas, PadL, PadT, M);
    }

    private static void DrawEmptyMessage(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontSize = 14;
        canvas.FontColor = Colors.Gray;
        canvas.DrawString("Натисніть «Розпочати аналіз» для побудови графіка.",
            dirtyRect.Center.X, dirtyRect.Center.Y, HorizontalAlignment.Center);
    }

    private double ComputeMaxTime(int M, int K)
    {
        double maxT = 0;
        for (int m = 0; m < M; m++)
        {
            for (int ni = 0; ni < K; ni++)
            {
                if (_times[m][ni] > maxT)
                {
                    maxT = _times[m][ni];
                }
            }
        }
        return maxT < 1e-9 ? 1 : maxT;
    }

    private static void DrawGridLines(ICanvas canvas, float padL, float padT, float plotW, float plotH)
    {
        const int TickCount = 6;
        canvas.StrokeColor = Color.FromArgb("#E0E0E0");
        canvas.StrokeSize = 1;
        for (int t = 0; t <= TickCount; t++)
        {
            float gy = padT + t * plotH / TickCount;
            canvas.DrawLine(padL, gy, padL + plotW, gy);
        }
    }

    private static void DrawAxes(ICanvas canvas, float padL, float padT, float plotW, float plotH)
    {
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(padL, padT, padL, padT + plotH);
        canvas.DrawLine(padL, padT + plotH, padL + plotW, padT + plotH);
    }

    private void DrawXLabels(ICanvas canvas, float padT, float plotH, int K, Func<double, float> toSx)
    {
        canvas.FontSize = 10;
        canvas.FontColor = Colors.Black;
        for (int ki = 0; ki < K; ki++)
        {
            float sx = toSx(_nodeCounts[ki]);
            canvas.DrawLine(sx, padT + plotH, sx, padT + plotH + 4);
            canvas.DrawString(_nodeCounts[ki].ToString(), sx, padT + plotH + 7, HorizontalAlignment.Center);
        }
    }

    private static void DrawYLabels(ICanvas canvas, float padL, double maxT, Func<double, float> toSy)
    {
        const int TickCount = 6;
        canvas.FontSize = 10;
        canvas.FontColor = Colors.Black;
        for (int t = 0; t <= TickCount; t++)
        {
            double tv = t * maxT / TickCount;
            float sy = toSy(tv);
            canvas.DrawLine(padL - 4, sy, padL, sy);
            canvas.DrawString(tv.ToString("G3"), padL - 7, sy, HorizontalAlignment.Right);
        }
    }

    private static void DrawAxisTitles(ICanvas canvas, float padL, float padT, float plotW, float plotH)
    {
        canvas.FontSize = 12;
        canvas.DrawString("n (вузлів)", padL + plotW / 2, padT + plotH + 32, HorizontalAlignment.Center);
        canvas.DrawString("Час, мкс", padL - 58, padT + plotH / 2, HorizontalAlignment.Left);
    }

    private void DrawSeries(ICanvas canvas, int M, int K,
        Func<double, float> toSx, Func<double, float> toSy)
    {
        for (int m = 0; m < M; m++)
        {
            canvas.StrokeColor = _colors[m];
            canvas.StrokeSize = 2f;

            var path = new PathF();
            for (int ki = 0; ki < K; ki++)
            {
                float sx = toSx(_nodeCounts[ki]);
                float sy = toSy(_times[m][ki]);
                if (ki == 0)
                {
                    path.MoveTo(sx, sy);
                }
                else
                {
                    path.LineTo(sx, sy);
                }
            }
            canvas.DrawPath(path);

            canvas.FillColor = _colors[m];
            for (int ki = 0; ki < K; ki++)
            {
                canvas.FillCircle(toSx(_nodeCounts[ki]), toSy(_times[m][ki]), 3);
            }
        }
    }

    private void DrawLegend(ICanvas canvas, float padL, float padT, int M)
    {
        float lx = padL + 8;
        float ly = padT + 8;
        for (int m = 0; m < M; m++)
        {
            canvas.StrokeColor = _colors[m];
            canvas.StrokeSize = 3;
            canvas.DrawLine(lx, ly + 6, lx + 22, ly + 6);
            canvas.FontSize = 11;
            canvas.FontColor = Colors.Black;
            canvas.DrawString(_names[m], lx + 26, ly + 1, HorizontalAlignment.Left);
            ly += 20;
        }
    }
}
