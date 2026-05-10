namespace InterpolationApp.Messages;

/// <summary>Sent when the polynomial chart data has been updated and needs a redraw.</summary>
public sealed record InvalidateChartMessage;

/// <summary>Sent when the complexity chart data has been updated and needs a redraw.</summary>
public sealed record InvalidateComplexityChartMessage;
