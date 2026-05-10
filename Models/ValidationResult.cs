namespace InterpolationApp.Models;

/// <summary>
/// Encapsulates the outcome of input-data validation.
/// Use the static factory methods <see cref="Ok"/> and <see cref="Fail"/>.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>True when the data passed all checks.</summary>
    public bool IsValid { get; private init; }

    /// <summary>Human-readable error message (empty when IsValid is true).</summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>Creates a successful validation result.</summary>
    public static ValidationResult Ok() => new() { IsValid = true };

    /// <summary>Creates a failed validation result with an error message.</summary>
    public static ValidationResult Fail(string message) =>
        new() { IsValid = false, Message = message };
}
