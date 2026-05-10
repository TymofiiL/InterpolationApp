using System.Globalization;

namespace InterpolationApp.Converters;

/// <summary>Inverts a bool: true to false, false to true.</summary>
public sealed class InvertBoolConverter : IValueConverter
{
    private readonly Func<object?, object> _impl = static v => v is bool b && !b;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _impl(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _impl(value);
}

/// <summary>Returns true when the string is non-null and non-empty.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    private readonly Func<object?, object> _convert = static v => v is string s && !string.IsNullOrEmpty(s);
    private readonly Func<object?, object> _convertBack = static _ => throw new NotSupportedException();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _convert(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _convertBack(value);
}

/// <summary>Returns true when the integer is not zero (used for Results.Count).</summary>
public sealed class IntNotZeroConverter : IValueConverter
{
    private readonly Func<object?, object> _convert = static v => v is int i && i != 0;
    private readonly Func<object?, object> _convertBack = static _ => throw new NotSupportedException();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _convert(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        _convertBack(value);
}
