using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InterpolationApp.Controls;
using InterpolationApp.Messages;
using InterpolationApp.Services;

namespace InterpolationApp.ViewModels;

public sealed partial class ComplexityViewModel : ObservableObject
{
    private readonly InterpolationStateService _state;
    private readonly IMessenger _messenger;

    private CancellationTokenSource? _cts;
    private double[][]? _lastTimes;
    private string[]? _lastNames;
    private Color[]? _lastColors;

    private static readonly int[] NodeCounts =
        Enumerable.Range(1, 20).Select(i => i * 5).ToArray();

    public ComplexityViewModel(
        InterpolationStateService state,
        IMessenger messenger)
    {
        _state = state;
        _messenger = messenger;
    }

    public ComplexityDrawable Drawable { get; } = new();

    [ObservableProperty] private string _statusMessage = "Натисніть «Розпочати аналіз».";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isRunning;

    public ObservableCollection<ComplexityRow> Rows { get; } = new();

    [RelayCommand]
    private async Task StartAnalysis()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        Progress = 0;
        StatusMessage = "Аналізуємо…";
        Rows.Clear();

        _cts = new CancellationTokenSource();

        try
        {
            var interps = _state.AllInterpolators;
            var prog = new Progress<double>(p =>
            {
                Progress = p;
                StatusMessage = $"Аналізуємо… {p * 100:F0}%";
            });

            double[][] times = await ComplexityAnalyzer.RunSweepAsync(
                interps, NodeCounts, prog, _cts.Token);

            for (int ni = 0; ni < NodeCounts.Length; ni++)
            {
                Rows.Add(new ComplexityRow
                {
                    N = NodeCounts[ni],
                    LagrangeUs = times[0][ni].ToString("F3"),
                    BarycentricUs = times[1][ni].ToString("F3"),
                    AitkenUs = times[2][ni].ToString("F3"),
                });
            }

            var colors = interps.Select(i => i.ChartColor).ToArray();
            var names = interps.Select(i => i.ShortName).ToArray();
            Drawable.SetData(NodeCounts, times, names, colors);
            _lastTimes = times;
            _lastNames = names;
            _lastColors = colors;
            _messenger.Send(new InvalidateComplexityChartMessage());

            Progress = 1;
            StatusMessage = "Аналіз завершено.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Аналіз скасовано.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelAnalysis()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private async Task SaveSvg()
    {
        if (_lastTimes is null)
        {
            StatusMessage = "Немає даних. Спочатку виконайте аналіз.";
            return;
        }

        string svg = SvgExporter.ExportComplexity(NodeCounts, _lastTimes, _lastNames!, _lastColors!);
        string fileName = $"complexity_{DateTime.Now:yyyyMMdd_HHmmss}.svg";

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svg));
        var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);

        if (result.IsSuccessful)
        {
            StatusMessage = $"SVG збережено: {result.FilePath}";
        }
        else if (result.Exception is not null)
        {
            StatusMessage = $"Помилка збереження: {result.Exception.Message}";
        }
        else
        {
            StatusMessage = "Збереження скасовано.";
        }
    }
}

public sealed class ComplexityRow
{
    public int N { get; init; }
    public string LagrangeUs { get; init; } = string.Empty;
    public string BarycentricUs { get; init; } = string.Empty;
    public string AitkenUs { get; init; } = string.Empty;
}
