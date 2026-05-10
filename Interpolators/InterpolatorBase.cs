namespace InterpolationApp.Interpolators;

/// <summary>
/// Abstract base class for all polynomial interpolation methods.
/// Concrete subclasses implement <see cref="Compute"/> and, optionally,
/// override <see cref="OnNodesChanged"/> to pre-compute cached values.
/// </summary>
public abstract class InterpolatorBase
{
    // ── Identity ──────────────────────────────────────────────────────
    /// <summary>Display name shown in the UI.</summary>
    public abstract string Name { get; }

    /// <summary>Short abbreviation used in legends and tables.</summary>
    public abstract string ShortName { get; }

    /// <summary>Colour used for this method's curve on charts.</summary>
    public abstract Color ChartColor { get; }

    // ── Core operation ─────────────────────────────────────────────────
    /// <summary>
    /// Computes the value of the interpolation polynomial P(x) at point <paramref name="x"/>.
    /// </summary>
    /// <param name="xs">Interpolation nodes x_0…x_{n-1} (pairwise distinct).</param>
    /// <param name="ys">Function values y_i = f(x_i).</param>
    /// <param name="x">Evaluation point.</param>
    /// <returns>P(x).</returns>
    public abstract double Compute(double[] xs, double[] ys, double x);

    // ── Optional cache invalidation hook ───────────────────────────────
    /// <summary>
    /// Called by the state service when the node set changes.
    /// Override to invalidate cached pre-computations (e.g., barycentric weights).
    /// </summary>
    public virtual void OnNodesChanged() { }
}
