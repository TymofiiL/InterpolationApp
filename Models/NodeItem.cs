using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InterpolationApp.Models;

/// <summary>
/// Represents a single editable row (x_i, y_i) in the node table.
/// Carries its own <see cref="DeleteCommand"/> so the row can remove
/// itself from the parent collection without coupling to a parent ViewModel.
/// </summary>
public sealed partial class NodeItem : ObservableObject
{
    // ── Fields kept as backing store for source-generated properties ──
    [ObservableProperty] private string _xText;
    [ObservableProperty] private string _yText;

    private readonly ObservableCollection<NodeItem> _owner;

    public NodeItem(ObservableCollection<NodeItem> owner,
                    double x = 0, double y = 0)
    {
        _owner = owner;
        _xText = x.ToString(CultureInfo.InvariantCulture);
        _yText = y.ToString(CultureInfo.InvariantCulture);
    }

    // ── Commands ──────────────────────────────────────────────────────
    /// <summary>Removes this node from the parent collection.</summary>
    [RelayCommand]
    private void Delete() => _owner.Remove(this);

    // ── Helpers ───────────────────────────────────────────────────────
    /// <summary>
    /// Attempts to parse both text fields as doubles.
    /// Returns false and sets x/y to 0 when parsing fails.
    /// </summary>
    public bool TryGetValues(out double x, out double y)
    {
        x = 0; y = 0;
        var style = NumberStyles.Any;
        var culture = CultureInfo.InvariantCulture;

        bool ok = double.TryParse(XText, style, culture, out x) &&
                  double.TryParse(YText, style, culture, out y);

        // Also accept comma as decimal separator
        if (!ok)
        {
            ok = double.TryParse(XText.Replace(',', '.'), style, culture, out x) &&
                 double.TryParse(YText.Replace(',', '.'), style, culture, out y);
        }
        return ok;
    }
}
