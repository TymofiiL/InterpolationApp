using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterpolationApp.Interpolators;
using InterpolationApp.Models;
using InterpolationApp.Services;

namespace InterpolationApp.ViewModels;

public sealed partial class ComputeViewModel : ObservableObject
{
    private readonly InterpolationStateService _state;
    private readonly DataManager _dm;
#if MACCATALYST
    private bool _pickerWasShownBefore;
#endif

    public ComputeViewModel(InterpolationStateService state, DataManager dm)
    {
        _state = state;
        _dm = dm;
    }

    public ObservableCollection<NodeItem> Nodes => _state.Nodes;

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

    [ObservableProperty] private string _pointX = "1.5";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _polynomialExpression = string.Empty;
    [ObservableProperty] private string _polynomialExpandedExpression = string.Empty;
    [ObservableProperty] private bool _showExpression;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private ObservableCollection<ComputeResultItem> _results = new();

    [RelayCommand]
    private void AddNode()
    {
        if (_state.Nodes.Count >= DataManager.MaxNodes)
        {
            StatusMessage = $"Досягнуто максимум {DataManager.MaxNodes:N0} вузлів.";
            return;
        }
        _state.Nodes.Add(new NodeItem(_state.Nodes));
        _state.InvalidateCaches();
    }

    [RelayCommand]
    private async Task LoadFile()
    {
#if MACCATALYST
        // UIDocumentPickerViewController dismissal animation takes ~300 ms on Mac Catalyst.
        // Calling FilePicker again before it finishes returns null silently.
        if (_pickerWasShownBefore)
            await Task.Delay(500);
        _pickerWasShownBefore = true;
#endif
        var picked = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Виберіть файл з вузлами інтерполяції (TXT)",
            FileTypes = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.WinUI] = new[] { ".txt" },
                    [DevicePlatform.MacCatalyst] = new[] { "public.plain-text" },
                    [DevicePlatform.iOS] = new[] { "public.plain-text" },
                })
        });

        if (picked is null)
            return;

        IsBusy = true;
        try
        {
            // Parse and validate on a background thread — file I/O and the
            // O(n²) duplicate check must not block the UI thread.
            var (data, vr) = await Task.Run(() =>
            {
                var d = _dm.LoadFromFile(picked.FullPath);
                return (d, DataManager.ValidateData(d));
            });

            if (!vr.IsValid)
            {
                StatusMessage = $"Помилка: {vr.Message}";
                return;
            }

            _state.Nodes.Clear();
            for (int i = 0; i < data.Count; i++)
                _state.Nodes.Add(new NodeItem(_state.Nodes, data.Xs[i], data.Ys[i]));

            _state.InvalidateCaches();
            StatusMessage = $"Завантажено {data.Count} вузлів з «{picked.FileName}».";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка завантаження: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Compute()
    {
        StatusMessage = string.Empty;
        PolynomialExpression = string.Empty;
        PolynomialExpandedExpression = string.Empty;
        ShowExpression = false;
        Results = new ObservableCollection<ComputeResultItem>();

        if (!_state.TryGetData(out var data, out string dataError))
        {
            StatusMessage = $"Помилка: {dataError}";
            return;
        }

        if (!TryParsePoint(PointX, out double x, out string pointError))
        {
            StatusMessage = $"Помилка: {pointError}";
            return;
        }

        var interpolators = _state.SelectedInterpolators.ToList();
        if (interpolators.Count == 0)
        {
            StatusMessage = "Виберіть принаймні один метод.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await Task.Run(() =>
            {
                var vr = DataManager.ValidateData(data);
                if (!vr.IsValid)
                    return (Items: (List<ComputeResultItem>?)null, Message: vr.Message,
                            Expr: string.Empty, ExpandedExpr: string.Empty,
                            ShowExpr: false, HasNonFinite: false);
                var r = RunInterpolators(interpolators, data.Xs, data.Ys, x);
                return (Items: (List<ComputeResultItem>?)r.Items, Message: string.Empty,
                        r.Expr, r.ExpandedExpr, r.ShowExpr, r.HasNonFinite);
            });

            if (result.Items is null)
            {
                StatusMessage = $"Помилка: {result.Message}";
                return;
            }

            if (result.HasNonFinite)
            {
                StatusMessage = "Помилка: результат виходить за межі числа з рухомою крапкою подвійної " +
                                "точності — спробуйте менші значення вузлів або точки обчислення.";
                return;
            }

            Results = new ObservableCollection<ComputeResultItem>(result.Items);
            PolynomialExpression = result.Expr;
            PolynomialExpandedExpression = result.ExpandedExpr;
            ShowExpression = result.ShowExpr;
            StatusMessage = $"Обчислено. P({x:G5}) = {Results[0].Value}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка обчислення: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool TryParsePoint(string s, out double v, out string error)
    {
        var style = NumberStyles.Any;
        var culture = CultureInfo.InvariantCulture;
        bool parsed = double.TryParse(s, style, culture, out v) ||
                      double.TryParse(s.Replace(',', '.'), style, culture, out v);
        if (!parsed)
        {
            error = "точка обчислення x не є числом — введіть числове значення.";
            return false;
        }
        if (!double.IsFinite(v))
        {
            error = "значення точки x виходить за межі допустимого діапазону (~±1.8·10³⁰⁸). " +
                    "Введіть менше число.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private static (List<ComputeResultItem> Items, string Expr, string ExpandedExpr, bool ShowExpr, bool HasNonFinite)
        RunInterpolators(List<InterpolatorBase> interpolators, double[] xs, double[] ys, double x)
    {
        var items = new List<ComputeResultItem>();
        string expr = string.Empty;
        string expandedExpr = string.Empty;
        bool showExpr = false;
        bool hasNonFinite = false;

        foreach (var interp in interpolators)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            double val = interp.Compute(xs, ys, x);
            sw.Stop();

            if (!double.IsFinite(val)) hasNonFinite = true;

            items.Add(new ComputeResultItem
            {
                MethodName = interp.Name,
                Value = val.ToString("G10"),
                TimeUs = sw.Elapsed.TotalMicroseconds.ToString("F2") + " мкс"
            });

            if (interp is LagrangeInterpolator)
            {
                expr = LagrangeInterpolator.BuildPolynomialExpression(xs, ys);
                expandedExpr = LagrangeInterpolator.BuildExpandedPolynomialExpression(xs, ys);
                showExpr = true;
            }
        }

        return (items, expr, expandedExpr, showExpr, hasNonFinite);
    }
}
