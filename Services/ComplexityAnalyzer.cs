using System.Diagnostics;
using InterpolationApp.Interpolators;

namespace InterpolationApp.Services;

public sealed class ComplexityAnalyzer
{
    private ComplexityAnalyzer() { }

    private const int WarmupRuns = 5;
    private const int TimedRuns = 100;

    public static double MeasureTimeUs(
        InterpolatorBase interpolator,
        double[] xs, double[] ys, double x)
    {
        for (int k = 0; k < WarmupRuns; k++)
        {
            interpolator.Compute(xs, ys, x);
        }

        var sw = Stopwatch.StartNew();
        for (int k = 0; k < TimedRuns; k++)
        {
            interpolator.Compute(xs, ys, x);
        }
        sw.Stop();

        return sw.Elapsed.TotalMicroseconds / TimedRuns;
    }

    public static async Task<double[][]> RunSweepAsync(
        InterpolatorBase[] interpolators,
        int[] nodeCounts,
        IProgress<double>? progress = null,
        CancellationToken cancellation = default)
    {
        int interpolatorCount = interpolators.Length;
        int nodeCount = nodeCounts.Length;
        var times = new double[interpolatorCount][];
        for (int m = 0; m < interpolatorCount; m++)
        {
            times[m] = new double[nodeCount];
        }

        int total = interpolatorCount * nodeCount;
        int done = 0;

        await Task.Run(() =>
        {
            for (int ni = 0; ni < nodeCount; ni++)
            {
                cancellation.ThrowIfCancellationRequested();

                int n = nodeCounts[ni];
                double[] xs = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
                double[] ys = xs.Select(Math.Sin).ToArray();
                double x = xs[n / 2];

                foreach (var interp in interpolators)
                {
                    interp.OnNodesChanged();
                }

                for (int m = 0; m < interpolatorCount; m++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    times[m][ni] = MeasureTimeUs(interpolators[m], xs, ys, x);
                    progress?.Report(++done / (double)total);
                }
            }
        }, cancellation);

        return times;
    }
}
