namespace InterpolationApp.Models;

/// <summary>
/// Immutable container for a set of interpolation nodes (x_i, y_i).
/// </summary>
public sealed class InterpolationData
{
    /// <summary>Node arguments x_0, x_1, …, x_{n-1}. Must be pairwise distinct.</summary>
    public double[] Xs { get; init; } = Array.Empty<double>();

    /// <summary>Function values y_i = f(x_i) at the nodes.</summary>
    public double[] Ys { get; init; } = Array.Empty<double>();

    /// <summary>Number of interpolation nodes.</summary>
    public int Count => Xs.Length;
}
