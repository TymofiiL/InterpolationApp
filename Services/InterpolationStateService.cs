using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InterpolationApp.Interpolators;
using InterpolationApp.Models;

namespace InterpolationApp.Services;

public sealed partial class InterpolationStateService : ObservableObject
{
    public ObservableCollection<NodeItem> Nodes { get; } = new();

    [ObservableProperty] private bool _useLagrange = true;
    [ObservableProperty] private bool _useBarycentric = true;
    [ObservableProperty] private bool _useAitken = true;

    public LagrangeInterpolator Lagrange { get; } = new();
    public BarycentricInterpolator Barycentric { get; } = new();
    public AitkenInterpolator Aitken { get; } = new();

    public InterpolatorBase[] AllInterpolators => new InterpolatorBase[] { Lagrange, Barycentric, Aitken };

    public IEnumerable<InterpolatorBase> SelectedInterpolators
    {
        get
        {
            if (UseLagrange)
            {
                yield return Lagrange;
            }
            if (UseBarycentric)
            {
                yield return Barycentric;
            }
            if (UseAitken)
            {
                yield return Aitken;
            }
        }
    }

    public bool TryGetData(out InterpolationData data, out string error)
    {
        error = string.Empty;
        var xs = new List<double>(Nodes.Count);
        var ys = new List<double>(Nodes.Count);

        foreach (var node in Nodes)
        {
            if (!node.TryGetValues(out double x, out double y))
            {
                error = node.TryGetRawValues(out _, out _)
                    ? "введене значення вузла виходить за межі допустимого діапазону (~±1.8·10³⁰⁸). " +
                      "Введіть менше число."
                    : "деякі поля вузлів містять некоректні символи — введіть числові значення.";
                data = new InterpolationData();
                return false;
            }
            xs.Add(x);
            ys.Add(y);
        }

        data = new InterpolationData { Xs = xs.ToArray(), Ys = ys.ToArray() };
        return true;
    }

    public void InvalidateCaches()
    {
        foreach (var interp in AllInterpolators)
        {
            interp.OnNodesChanged();
        }
    }
}
