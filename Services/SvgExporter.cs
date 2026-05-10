using System.Text;
using InterpolationApp.Models;

namespace InterpolationApp.Services;

public static class SvgExporter
{
    private const int W = 900, H = 560;
    private const int PadL = 80, PadR = 30, PadT = 30, PadB = 60;
    private const int PlotW = W - PadL - PadR;
    private const int PlotH = H - PadT - PadB;
    private const int Ticks = 6;

    public static string Export(List<PlotSeries> series, List<(double X, double Y)> nodes)
    {
        if (series.Count == 0 && nodes.Count == 0)
        {
            return "<svg xmlns='http://www.w3.org/2000/svg'><text x='10' y='20'>No data</text></svg>";
        }

        ComputeBounds(series, nodes,
            out double minX, out double minY,
            out double rx, out double ry);

        double ToSx(double x) => PadL + (x - minX) / rx * PlotW;
        double ToSy(double y) => PadT + PlotH - (y - minY) / ry * PlotH;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{W}' height='{H}' font-family='Arial' font-size='11'>");
        sb.AppendLine($"<rect width='{W}' height='{H}' fill='white' stroke='#E0E0E0'/>");

        AppendGrid(sb);
        AppendAxes(sb);
        AppendTickLabels(sb, minX, minY, rx, ry, ToSx, ToSy);
        AppendAxisTitles(sb);
        AppendSeries(sb, series, ToSx, ToSy);
        AppendNodes(sb, nodes, ToSx, ToSy);
        AppendLegend(sb, series);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void ComputeBounds(
        List<PlotSeries> series, List<(double X, double Y)> nodes,
        out double minX, out double minY,
        out double rx, out double ry)
    {
        var allX = series.SelectMany(s => s.Xs).Concat(nodes.Select(n => n.X)).ToList();
        var allY = series.SelectMany(s => s.Ys.Where(y => !double.IsNaN(y) && !double.IsInfinity(y)))
                         .Concat(nodes.Select(n => n.Y)).ToList();

        minX = allX.Min();
        double maxX = allX.Max();
        minY = allY.Min();
        double maxY = allY.Max();

        double px = (maxX - minX) * 0.05;
        double py = (maxY - minY) * 0.05;
        minX -= px;
        maxX += px;
        minY -= py;
        maxY += py;

        rx = maxX - minX;
        ry = maxY - minY;
        if (rx < 1e-12)
        {
            minX--;
            rx = 2;
        }
        if (ry < 1e-12)
        {
            minY--;
            ry = 2;
        }
    }

    private static void AppendGrid(StringBuilder sb)
    {
        for (int t = 0; t <= Ticks; t++)
        {
            double gx = PadL + t * PlotW / Ticks;
            double gy = PadT + t * PlotH / Ticks;
            sb.AppendLine($"<line x1='{gx:F1}' y1='{PadT}' x2='{gx:F1}' y2='{PadT + PlotH}' stroke='#E0E0E0'/>");
            sb.AppendLine($"<line x1='{PadL}' y1='{gy:F1}' x2='{PadL + PlotW}' y2='{gy:F1}' stroke='#E0E0E0'/>");
        }
    }

    private static void AppendAxes(StringBuilder sb)
    {
        sb.AppendLine($"<line x1='{PadL}' y1='{PadT}' x2='{PadL}' y2='{PadT + PlotH}' stroke='black' stroke-width='1.5'/>");
        sb.AppendLine($"<line x1='{PadL}' y1='{PadT + PlotH}' x2='{PadL + PlotW}' y2='{PadT + PlotH}' stroke='black' stroke-width='1.5'/>");
    }

    private static void AppendTickLabels(StringBuilder sb,
        double minX, double minY, double rx, double ry,
        Func<double, double> toSx, Func<double, double> toSy)
    {
        for (int t = 0; t <= Ticks; t++)
        {
            double xv = minX + t * rx / Ticks;
            double yv = minY + t * ry / Ticks;
            double sx = toSx(xv);
            double sy = toSy(yv);
            sb.AppendLine($"<text x='{sx:F1}' y='{PadT + PlotH + 16}' text-anchor='middle'>{xv:G4}</text>");
            sb.AppendLine($"<text x='{PadL - 6}' y='{sy + 4:F1}' text-anchor='end'>{yv:G4}</text>");
        }
    }

    private static void AppendAxisTitles(StringBuilder sb)
    {
        sb.AppendLine($"<text x='{PadL + PlotW / 2}' y='{H - 10}' text-anchor='middle' font-weight='bold'>x</text>");
        sb.AppendLine($"<text x='12' y='{PadT + PlotH / 2}' text-anchor='middle' font-weight='bold' transform='rotate(-90,12,{PadT + PlotH / 2})'>P(x)</text>");
    }

    private static void AppendSeries(StringBuilder sb, List<PlotSeries> series,
        Func<double, double> toSx, Func<double, double> toSy)
    {
        foreach (var s in series)
        {
            if (s.Xs.Length < 2)
            {
                continue;
            }
            string hex = ColorToHex(s.Color);
            var pts = new StringBuilder();
            bool first = true;
            for (int i = 0; i < s.Xs.Length; i++)
            {
                if (double.IsNaN(s.Ys[i]) || double.IsInfinity(s.Ys[i]))
                {
                    first = true;
                    continue;
                }
                double sx = toSx(s.Xs[i]);
                double sy = Math.Clamp(toSy(s.Ys[i]), PadT - 10, PadT + PlotH + 10);
                pts.Append(first ? $"M{sx:F1},{sy:F1}" : $" L{sx:F1},{sy:F1}");
                first = false;
            }
            sb.AppendLine($"<path d='{pts}' fill='none' stroke='{hex}' stroke-width='2.5'/>");
        }
    }

    private static void AppendNodes(StringBuilder sb, List<(double X, double Y)> nodes,
        Func<double, double> toSx, Func<double, double> toSy)
    {
        foreach (var (nx, ny) in nodes)
        {
            double sx = toSx(nx);
            double sy = toSy(ny);
            sb.AppendLine($"<circle cx='{sx:F1}' cy='{sy:F1}' r='5' fill='#FF6F00' stroke='#212121' stroke-width='1.5'/>");
        }
    }

    private static void AppendLegend(StringBuilder sb, List<PlotSeries> series)
    {
        double lx = PadL + 10;
        double ly = PadT + 10;
        sb.AppendLine($"<circle cx='{lx + 5}' cy='{ly + 5}' r='5' fill='#FF6F00'/>");
        sb.AppendLine($"<text x='{lx + 14}' y='{ly + 10}'>Вузли</text>");
        ly += 22;
        foreach (var s in series)
        {
            string hex = ColorToHex(s.Color);
            sb.AppendLine($"<line x1='{lx}' y1='{ly + 6}' x2='{lx + 22}' y2='{ly + 6}' stroke='{hex}' stroke-width='3'/>");
            sb.AppendLine($"<text x='{lx + 26}' y='{ly + 10}'>{s.Name}</text>");
            ly += 22;
        }
    }

    public static string ExportComplexity(int[] nodeCounts, IReadOnlyList<double[]> times, string[] names, Color[] colors)
    {
        if (nodeCounts.Length == 0)
        {
            return "<svg xmlns='http://www.w3.org/2000/svg'><text x='10' y='20'>No data</text></svg>";
        }

        int K = nodeCounts.Length;
        int M = names.Length;

        double maxT = ComputeMaxTime(times, M, K);

        double minX = nodeCounts[0];
        double rangeX = nodeCounts[K - 1] - minX;
        if (rangeX < 1e-9)
        {
            rangeX = 1;
        }

        double ToSx(double n) => PadL + (n - minX) / rangeX * PlotW;
        double ToSy(double t) => PadT + PlotH - t / maxT * PlotH;

        var sb = new StringBuilder();
        sb.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{W}' height='{H}' font-family='Arial' font-size='11'>");
        sb.AppendLine($"<rect width='{W}' height='{H}' fill='white' stroke='#E0E0E0'/>");

        for (int t = 0; t <= Ticks; t++)
        {
            double gy = PadT + t * PlotH / Ticks;
            sb.AppendLine($"<line x1='{PadL}' y1='{gy:F1}' x2='{PadL + PlotW}' y2='{gy:F1}' stroke='#E0E0E0'/>");
        }

        sb.AppendLine($"<line x1='{PadL}' y1='{PadT}' x2='{PadL}' y2='{PadT + PlotH}' stroke='black' stroke-width='1.5'/>");
        sb.AppendLine($"<line x1='{PadL}' y1='{PadT + PlotH}' x2='{PadL + PlotW}' y2='{PadT + PlotH}' stroke='black' stroke-width='1.5'/>");

        for (int ki = 0; ki < K; ki++)
        {
            double sx = ToSx(nodeCounts[ki]);
            sb.AppendLine($"<line x1='{sx:F1}' y1='{PadT + PlotH}' x2='{sx:F1}' y2='{PadT + PlotH + 4}' stroke='black'/>");
            sb.AppendLine($"<text x='{sx:F1}' y='{PadT + PlotH + 16}' text-anchor='middle'>{nodeCounts[ki]}</text>");
        }

        for (int t = 0; t <= Ticks; t++)
        {
            double tv = t * maxT / Ticks;
            double sy = ToSy(tv);
            sb.AppendLine($"<line x1='{PadL - 4}' y1='{sy:F1}' x2='{PadL}' y2='{sy:F1}' stroke='black'/>");
            sb.AppendLine($"<text x='{PadL - 7}' y='{sy + 4:F1}' text-anchor='end'>{tv:G3}</text>");
        }

        sb.AppendLine($"<text x='{PadL + PlotW / 2}' y='{H - 10}' text-anchor='middle' font-weight='bold'>n (вузлів)</text>");
        sb.AppendLine($"<text x='12' y='{PadT + PlotH / 2}' text-anchor='middle' font-weight='bold' transform='rotate(-90,12,{PadT + PlotH / 2})'>Час, мкс</text>");

        for (int m = 0; m < M; m++)
        {
            string hex = ColorToHex(colors[m]);
            var pts = new StringBuilder();
            for (int ki = 0; ki < K; ki++)
            {
                double sx = ToSx(nodeCounts[ki]);
                double sy = ToSy(times[m][ki]);
                pts.Append(ki == 0 ? $"M{sx:F1},{sy:F1}" : $" L{sx:F1},{sy:F1}");
            }
            sb.AppendLine($"<path d='{pts}' fill='none' stroke='{hex}' stroke-width='2'/>");
            for (int ki = 0; ki < K; ki++)
            {
                sb.AppendLine($"<circle cx='{ToSx(nodeCounts[ki]):F1}' cy='{ToSy(times[m][ki]):F1}' r='3' fill='{hex}'/>");
            }
        }

        double lx = PadL + 10;
        double ly = PadT + 10;
        for (int m = 0; m < M; m++)
        {
            string hex = ColorToHex(colors[m]);
            sb.AppendLine($"<line x1='{lx}' y1='{ly + 6}' x2='{lx + 22}' y2='{ly + 6}' stroke='{hex}' stroke-width='3'/>");
            sb.AppendLine($"<text x='{lx + 26}' y='{ly + 10}'>{names[m]}</text>");
            ly += 22;
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static double ComputeMaxTime(IReadOnlyList<double[]> times, int M, int K)
    {
        double maxT = 0;
        for (int m = 0; m < M; m++)
        {
            for (int ki = 0; ki < K; ki++)
            {
                if (times[m][ki] > maxT)
                {
                    maxT = times[m][ki];
                }
            }
        }
        return maxT < 1e-9 ? 1 : maxT;
    }

    private static string ColorToHex(Color c) =>
        $"#{(int)(c.Red * 255):X2}{(int)(c.Green * 255):X2}{(int)(c.Blue * 255):X2}";
}
