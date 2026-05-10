namespace InterpolationApp.Models;

public sealed class ComputeResultItem
{
    public string MethodName { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string TimeUs { get; init; } = string.Empty;
}
