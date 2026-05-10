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
        _state.Nodes.Add(new NodeItem(_state.Nodes));
        _state.InvalidateCaches();
    }

    [RelayCommand]
    private async Task LoadFile()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
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

            if (result is null)
            {
                return;
            }

            var data = _dm.LoadFromFile(result.FullPath);
            var vr = DataManager.ValidateData(data);
            if (!vr.IsValid)
            {
                StatusMessage = $"Помилка: {vr.Message}";
                return;
            }

            _state.Nodes.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                _state.Nodes.Add(new NodeItem(_state.Nodes, data.Xs[i], data.Ys[i]));
            }
            _state.InvalidateCaches();
            StatusMessage = $"Завантажено {data.Count} вузлів з «{result.FileName}».";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Помилка завантаження: {ex.Message}";
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

        if (!_state.TryGetData(out var data))
        {
            StatusMessage = "Помилка: деякі поля містять некоректні числа.";
            return;
        }

        var vr = DataManager.ValidateData(data);
        if (!vr.IsValid)
        {
            StatusMessage = $"Помилка: {vr.Message}";
            return;
        }

        if (!double.TryParse(PointX,
                NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
            !double.TryParse(PointX.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out x))
        {
            StatusMessage = "Помилка: некоректне значення точки x.";
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
            var newItems = new List<ComputeResultItem>();
            string newExpr = string.Empty;
            string newExpandedExpr = string.Empty;
            bool showExpr = false;

            await Task.Run(() =>
            {
                foreach (var interp in interpolators)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    double val = interp.Compute(data.Xs, data.Ys, x);
                    sw.Stop();

                    newItems.Add(new ComputeResultItem
                    {
                        MethodName = interp.Name,
                        Value = val.ToString("G10"),
                        TimeUs = sw.Elapsed.TotalMicroseconds.ToString("F2") + " мкс"
                    });

                    if (interp is LagrangeInterpolator)
                    {
                        newExpr = LagrangeInterpolator.BuildPolynomialExpression(data.Xs, data.Ys);
                        newExpandedExpr = LagrangeInterpolator.BuildExpandedPolynomialExpression(data.Xs, data.Ys);
                        showExpr = true;
                    }
                }
            });

            Results = new ObservableCollection<ComputeResultItem>(newItems);

            PolynomialExpression = newExpr;
            PolynomialExpandedExpression = newExpandedExpr;
            ShowExpression = showExpr;

            StatusMessage = Results.Count > 0
                ? $"Обчислено. P({x:G5}) = {Results[0].Value}"
                : "Немає вибраних методів.";
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
}
