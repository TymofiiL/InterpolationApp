using System.Globalization;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InterpolationApp.Controls;
using InterpolationApp.Messages;
using InterpolationApp.Models;
using InterpolationApp.Services;

namespace InterpolationApp.ViewModels;

public sealed partial class GraphViewModel : ObservableObject
{
    private readonly InterpolationStateService _state;
    private readonly IMessenger _messenger;

    public GraphViewModel(
        InterpolationStateService state,
        IMessenger messenger)
    {
        _state = state;
        _messenger = messenger;
    }

    public ChartDrawable Drawable { get; } = new();

    [ObservableProperty] private string _rangeA = "-1";
    [ObservableProperty] private string _rangeB = "5";
    [ObservableProperty] private string _numPts = "500";
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool UseLagrange
    {
        get => _state.UseLagrange;
        set
        {
            if (_state.UseLagrange == value)
            {
                return;
            }
            _state.UseLagrange = value;
            OnPropertyChanged();
        }
    }

    public bool UseBarycentric
    {
        get => _state.UseBarycentric;
        set
        {
            if (_state.UseBarycentric == value)
            {
                return;
            }
            _state.UseBarycentric = value;
            OnPropertyChanged();
        }
    }

    public bool UseAitken
    {
        get => _state.UseAitken;
        set
        {
            if (_state.UseAitken == value)
            {
                return;
            }
            _state.UseAitken = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task Plot()
    {
        StatusMessage = string.Empty;

        if (!_state.TryGetData(out var data, out string dataError))
        {
            StatusMessage = $"Помилка: {dataError}";
            return;
        }

        if (!ParseDouble(RangeA, out double a, out string errA))
        {
            StatusMessage = $"Помилка: ліва межа a — {errA}";
            return;
        }
        if (!ParseDouble(RangeB, out double b, out string errB))
        {
            StatusMessage = $"Помилка: права межа b — {errB}";
            return;
        }
        if (a >= b)
        {
            StatusMessage = "Помилка: ліва межа a має бути строго менше правої межі b.";
            return;
        }

        if (!int.TryParse(NumPts, out int pts) || pts < 2 || pts > 5000)
        {
            StatusMessage = "Кількість точок має бути від 2 до 5000.";
            return;
        }

        var interpolators = _state.SelectedInterpolators.ToList();
        if (interpolators.Count == 0)
        {
            StatusMessage = "Виберіть принаймні один метод.";
            return;
        }

        string? validationError = null;
        var series = new List<PlotSeries>();
        await Task.Run(() =>
        {
            var vr = DataManager.ValidateData(data);
            if (!vr.IsValid)
            {
                validationError = vr.Message;
                return;
            }

            double step = (b - a) / (pts - 1);
            double[] xs = Enumerable.Range(0, pts).Select(i => a + i * step).ToArray();

            foreach (var interp in interpolators)
            {
                double[] ys = xs.Select(x => interp.Compute(data.Xs, data.Ys, x)).ToArray();
                series.Add(new PlotSeries
                {
                    Name = interp.Name,
                    Color = interp.ChartColor,
                    Xs = xs,
                    Ys = ys,
                });
            }
        });

        if (validationError is not null)
        {
            StatusMessage = $"Помилка: {validationError}";
            return;
        }

        if (series.Any(s => s.Ys.Any(y => !double.IsFinite(y))))
        {
            StatusMessage = "Помилка: обчислені значення виходять за межі числа з рухомою крапкою " +
                            "подвійної точності — спробуйте менші значення вузлів або звузіть діапазон.";
            return;
        }

        Drawable.Series = series;
        Drawable.NodePoints = data.Xs.Zip(data.Ys).Select(p => (p.First, p.Second)).ToList();
        Drawable.ResetView();

        _messenger.Send(new InvalidateChartMessage());
        StatusMessage = $"Графік побудовано: {series.Count} метод(и), {pts} точок.";
    }

    [RelayCommand]
    private void ZoomIn()
    {
        Drawable.Zoom(1.4);
        _messenger.Send(new InvalidateChartMessage());
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Drawable.Zoom(1.0 / 1.4);
        _messenger.Send(new InvalidateChartMessage());
    }

    [RelayCommand]
    private void ResetZoom()
    {
        Drawable.ResetView();
        _messenger.Send(new InvalidateChartMessage());
    }

    [RelayCommand]
    private async Task SaveSvg()
    {
        if (Drawable.Series.Count == 0 && Drawable.NodePoints.Count == 0)
        {
            StatusMessage = "Немає даних для збереження. Спочатку побудуйте графік.";
            return;
        }

        string svg = SvgExporter.Export(Drawable.Series, Drawable.NodePoints);
        string fileName = $"interpolation_{DateTime.Now:yyyyMMdd_HHmmss}.svg";

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

    private static bool ParseDouble(string s, out double v, out string error)
    {
        var style = NumberStyles.Any;
        var culture = CultureInfo.InvariantCulture;
        bool parsed = double.TryParse(s, style, culture, out v) ||
                      double.TryParse(s.Replace(',', '.'), style, culture, out v);
        if (!parsed)
        {
            error = "не є числом — введіть числове значення.";
            return false;
        }
        if (!double.IsFinite(v))
        {
            error = "виходить за межі допустимого діапазону (~±1.8·10³⁰⁸). Введіть менше число.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}
