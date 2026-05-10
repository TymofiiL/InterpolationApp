namespace InterpolationApp.Interpolators;

/// <summary>
/// Aitken's recurrence scheme for computing the interpolation polynomial value
/// at a single point without explicitly constructing the polynomial.
///
/// Algorithm (in-place, O(n) space):
///   Initialise: table[i] = y_i
///   For k = 1 to n-1 (recursion level):
///       For i = n-1 downto k (reverse order preserves previous level):
///           table[i] = [(x−x_{i-k})·table[i] − (x−x_i)·table[i-1]] / (x_i − x_{i-k})
///   Return: table[n-1]  (= P(x))
///
/// Complexity: O(n²) time, O(n) space.
/// Advantage: incremental — adding one new node requires only one extra column.
/// </summary>
public sealed class AitkenInterpolator : InterpolatorBase
{
    public override string Name => "Схема Ейткена";
    public override string ShortName => "Ейткен";
    public override Color ChartColor => Color.FromArgb("#2E7D32");

    // ── Core computation ───────────────────────────────────────────────
    /// <inheritdoc/>
    public override double Compute(double[] xs, double[] ys, double x) =>
        Evaluate(xs, ys, x);

    private static double Evaluate(double[] xs, double[] ys, double x)
    {
        int n = xs.Length;
        if (n == 1)
            return ys[0];

        // Work array, initially a copy of y values (level 0 of Aitken table)
        double[] table = (double[])ys.Clone();

        // Fill Aitken table column by column
        for (int k = 1; k < n; k++)
        {
            // Traverse in reverse so table[i-1] still holds the previous-level value
            for (int i = n - 1; i >= k; i--)
            {
                double denom = xs[i] - xs[i - k];
                table[i] = ((x - xs[i - k]) * table[i]
                          - (x - xs[i]) * table[i - 1])
                          / denom;
            }
        }

        // table[n-1] now holds P(x)
        return table[n - 1];
    }
}
