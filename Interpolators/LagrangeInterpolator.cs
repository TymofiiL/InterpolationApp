using System.Text;

namespace InterpolationApp.Interpolators;

public sealed class LagrangeInterpolator : InterpolatorBase
{
    private const string ZeroPolynomial = "P(x) = 0";

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

        return firstTerm ? ZeroPolynomial : sb.ToString();
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

        return PolyToString(poly, n);
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

    private static string PolyToString(double[] poly, int n)
    {
        var sb = new StringBuilder("P(x) = ");
        bool first = true;

        for (int k = n - 1; k >= 0; k--)
        {
            double c = poly[k];
            if (Math.Abs(c) < 1e-9)
            {
                continue;
            }

            if (!first)
                sb.Append(c >= 0 ? " + " : " - ");
            else if (c < 0)
                sb.Append('-');
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

        return first ? ZeroPolynomial : sb.ToString();
    }
}
