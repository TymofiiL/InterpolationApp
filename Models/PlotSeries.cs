namespace InterpolationApp.Models;

public sealed class PlotSeries
{
    public string Name { get; init; } = string.Empty;
    public Color Color { get; init; } = Colors.Blue;
    public double[] Xs { get; init; } = Array.Empty<double>();
    public double[] Ys { get; init; } = Array.Empty<double>();
}
