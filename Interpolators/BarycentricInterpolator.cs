namespace InterpolationApp.Interpolators;

/// <summary>
/// Barycentric form of the Lagrange interpolation polynomial.
///
/// Mathematical formulation (second barycentric form):
///   w_i = 1 / Π_{j≠i}(x_i − x_j)         
///   P(x) = [Σ w_i·y_i/(x−x_i)] / [Σ w_i/(x−x_i)]   
///
/// When x ≈ x_k (node coincidence), P(x_k) = y_k directly (special case handled).
///
/// Numerically more stable than the classical form; ideal for repeated evaluation
/// (e.g., plotting) after the one-time O(n²) weight computation.
/// </summary>
public sealed class BarycentricInterpolator : InterpolatorBase
{
    public override string Name => "Барицентричний Лагранж";
    public override string ShortName => "Барицентрич.";
    public override Color ChartColor => Color.FromArgb("#D32F2F");

    // ── Cached barycentric weights ─────────────────────────────────────
    private double[]? _weights;
    private double[]? _cachedXs;

    // ── Cache invalidation ─────────────────────────────────────────────
    /// <inheritdoc/>
    public override void OnNodesChanged()
    {
        _weights = null;
        _cachedXs = null;
    }

    // ── Core computation ───────────────────────────────────────────────
    /// <inheritdoc/>
    public override double Compute(double[] xs, double[] ys, double x)
    {
        // Recompute weights if nodes changed
        if (_cachedXs is null || !xs.SequenceEqual(_cachedXs))
        {
            ComputeWeights(xs);
        }

        int n = xs.Length;
        const double Eps = 1e-14;

        // Special case: x coincides with a node → return that node's value exactly
        for (int i = 0; i < n; i++)
        {
            if (Math.Abs(x - xs[i]) < Eps)
            {
                return ys[i];
            }
        }

        // Second barycentric formula
        double num = 0.0, denom = 0.0;
        for (int i = 0; i < n; i++)
        {
            double t = _weights![i] / (x - xs[i]);
            num += t * ys[i];
            denom += t;
        }
        // + 0.0 normalizes IEEE 754 -0.0 to +0.0 (e.g. when all y_i = 0)
        return num / denom + 0.0;
    }

    // ── Weight computation O(n²) ───────────────────────────────────────
    /// <summary>
    /// Computes barycentric weights w_i = 1/Π_{j≠i}(x_i−x_j).
    /// Called automatically when xs changes; may also be called explicitly.
    /// </summary>
    public void ComputeWeights(double[] xs)
    {
        int n = xs.Length;
        _weights = new double[n];

        for (int i = 0; i < n; i++)
        {
            double prod = 1.0;
            for (int j = 0; j < n; j++)
            {
                if (j != i)
                {
                    prod *= xs[i] - xs[j];
                }
            }
            _weights[i] = 1.0 / prod;
        }

        _cachedXs = (double[])xs.Clone();
    }
}
