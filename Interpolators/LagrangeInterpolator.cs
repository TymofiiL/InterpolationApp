using System.Text;

namespace InterpolationApp.Interpolators;

public sealed class LagrangeInterpolator : InterpolatorBase
{
    private const string ZeroPolynomial = "P(x) = 0";
    private const string OverflowPolynomial =
        "P(x) = [коефіцієнти не вдалося обчислити — переповнення при великій кількості вузлів]";
    private const int MaxNodesProductForm = 15;

    public override string Name => "Метод Лагранжа";
    public override string ShortName => "Лагранж";
    public override Color ChartColor => Color.FromArgb("#1565C0");

    public override double Compute(double[] xs, double[] ys, double x)
    {
        int n = xs.Length;
        double result = 0.0;

        for (int i = 0; i < n; i++)
        {
            double li = 1.0;
            for (int j = 0; j < n; j++)
            {
                if (j != i)
                {
                    li *= (x - xs[j]) / (xs[i] - xs[j]);
                }
            }
            result += ys[i] * li;
        }
        return result;
    }

    // Lagrange product form: P(x) = Σ yᵢ · Π (x−xⱼ)/(xᵢ−xⱼ)
    public static string BuildPolynomialExpression(double[] xs, double[] ys)
    {
        int n = xs.Length;
        if (n == 0)
        {
            return ZeroPolynomial;
        }
        if (n > MaxNodesProductForm)
        {
            return $"P(x) = [форма Лагранжа: {n} вузлів × {n - 1} множників на член = {n * (n - 1)} множників — " +
                   $"занадто великий для відображення; дивіться розгорнуту форму нижче]";
        }

        var sb = new StringBuilder("P(x) = ");
        bool firstTerm = true;

        for (int i = 0; i < n; i++)
        {
            if (Math.Abs(ys[i]) < 1e-14)
            {
                continue;
            }

            if (!firstTerm)
            {
                sb.Append(" + ");
            }
            firstTerm = false;

            AppendProductTermFactors(sb, xs, ys, i, n);
        }

        return firstTerm ? ZeroPolynomial : sb.ToString();
    }

    private static void AppendProductTermFactors(StringBuilder sb, double[] xs, double[] ys, int i, int n)
    {
        sb.Append($"({ys[i]:G5})");
        for (int j = 0; j < n; j++)
        {
            if (j == i)
            {
                continue;
            }
            double diff = xs[i] - xs[j];
            string sign = xs[j] >= 0 ? $"-{xs[j]:G5}" : $"+{Math.Abs(xs[j]):G5}";
            sb.Append($"·(x{sign})/({diff:G5})");
        }
    }

    // Expanded standard form: P(x) = aₙ·xⁿ + … + a₁·x + a₀
    public static string BuildExpandedPolynomialExpression(double[] xs, double[] ys)
    {
        int n = xs.Length;
        if (n == 0)
        {
            return ZeroPolynomial;
        }

        // poly[k] = coefficient of x^k accumulated across all basis polynomials
        double[] poly = new double[n];

        for (int i = 0; i < n; i++)
        {
            if (Math.Abs(ys[i]) < 1e-14)
            {
                continue;
            }

            AccumulateBasis(xs, ys, i, n, poly);
        }

        // Detect overflow: denom can exceed double.MaxValue for many widely-spaced nodes,
        // making scale=0 so poly stays all-zero, or basis[] can overflow making poly=±Inf/NaN.
        double maxAbsPoly = poly.Take(n).Max(Math.Abs);
        bool hasNonZeroY = ys.Any(y => Math.Abs(y) >= 1e-14);
        if (!double.IsFinite(maxAbsPoly) || (maxAbsPoly < double.Epsilon && hasNonZeroY))
        {
            return OverflowPolynomial;
        }

        return PolyToString(poly, n, xs);
    }

    // Build L_i(x) by successive multiplication by (x − xs[j]) and accumulate into poly
    private static void AccumulateBasis(double[] xs, double[] ys, int i, int n, double[] poly)
    {
        double[] basis = new double[n];
        basis[0] = 1.0;
        int deg = 0;
        double denom = 1.0;

        for (int j = 0; j < n; j++)
        {
            if (j == i)
            {
                continue;
            }
            denom *= xs[i] - xs[j];
            for (int k = deg; k >= 0; k--)
            {
                basis[k + 1] += basis[k];
                basis[k] *= -xs[j];
            }
            deg++;
        }

        double scale = ys[i] / denom;
        for (int k = 0; k <= deg; k++)
        {
            poly[k] += scale * basis[k];
        }
    }

    private static string PolyToString(double[] poly, int n, double[] xs)
    {
        // Scale-aware threshold: a degree-k coefficient c is negligible only if
        // |c| * maxX^k < eps * maxAbsCoeff, i.e. its contribution at the largest
        // node is tiny relative to the dominant term. This prevents dropping
        // coefficients like -4.5e-300 that look tiny but matter at x=1e300.
        double maxAbsX = xs.Length > 0 ? xs.Max(Math.Abs) : 0.0;
        double maxAbsCoeff = poly.Take(n).Max(x => Math.Abs(x));

        if (maxAbsCoeff < double.Epsilon)
        {
            return ZeroPolynomial;
        }

        const double Eps = 1e-9;
        var sb = new StringBuilder("P(x) = ");
        bool first = true;

        for (int k = n - 1; k >= 0; k--)
        {
            double c = poly[k];
            double xScale = Math.Max(1.0, Math.Pow(maxAbsX, k));
            if (Math.Abs(c) < Eps * maxAbsCoeff / xScale)
            {
                continue;
            }

            AppendPolyTerm(sb, c, k, ref first);
        }

        return first ? ZeroPolynomial : sb.ToString();
    }

    private static void AppendPolyTerm(StringBuilder sb, double c, int k, ref bool first)
    {
        if (!first)
        {
            sb.Append(c >= 0 ? " + " : " - ");
        }
        else if (c < 0)
        {
            sb.Append('-');
        }
        first = false;

        sb.Append(Math.Abs(c).ToString("G5"));
        if (k == 1)
        {
            sb.Append("·x");
        }
        else if (k > 1)
        {
            sb.Append($"·x^{k}");
        }
    }
}
