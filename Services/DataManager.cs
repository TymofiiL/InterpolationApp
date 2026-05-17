using System.Globalization;
using InterpolationApp.Models;

namespace InterpolationApp.Services;

public sealed class DataManager
{
    public const int MaxNodes = 10_000;

    // Comma is intentionally absent here: "0,5 0,25" must not split on the
    // comma so that the decimal-replacement step can turn it into "0.5".
    private static readonly char[] FieldSeparators = { ' ', '\t', ';' };
    // Fallback used only when no non-comma separator is found (e.g. "0.5,0.25").
    private static readonly char[] AllSeparators   = { ' ', '\t', ';', ',' };

    public InterpolationData LoadFromFile(string filePath)
    {
        var xs = new List<double>();
        var ys = new List<double>();

        var style = NumberStyles.Any;
        var culture = CultureInfo.InvariantCulture;

        // File.ReadLines is lazy — stops reading as soon as the cap is hit,
        // so a multi-gigabyte file never fully enters memory.
        foreach (string rawLine in File.ReadLines(filePath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Try without comma first so "0,5 0,25" stays as two tokens ["0,5","0,25"]
            // rather than splitting into four. Fall back to comma as separator only when
            // no other delimiter is present (e.g. "0.5,0.25").
            string[] parts = line.Split(FieldSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                parts = line.Split(AllSeparators, StringSplitOptions.RemoveEmptyEntries);
            }
            if (parts.Length < 2)
            {
                continue;
            }

            string xStr = parts[0].Replace(',', '.');
            string yStr = parts[1].Replace(',', '.');

            if (double.TryParse(xStr, style, culture, out double xi) &&
                double.TryParse(yStr, style, culture, out double yi))
            {
                if (xs.Count >= MaxNodes)
                {
                    throw new InvalidDataException(
                        $"Файл містить більше {MaxNodes:N0} вузлів. " +
                        $"Поліноміальна інтерполяція з такою кількістю точок є чисельно " +
                        $"нестабільною і надмірно повільною. " +
                        $"Скоротіть файл до {MaxNodes:N0} вузлів або менше.");
                }

                xs.Add(xi);
                ys.Add(yi);
            }
        }

        if (xs.Count == 0)
        {
            throw new InvalidDataException(
                "Файл не містить жодного коректного рядка даних.\n" +
                "Формат: «x y» (по одному вузлу на рядок).");
        }

        return new InterpolationData { Xs = xs.ToArray(), Ys = ys.ToArray() };
    }

    public static ValidationResult ValidateData(InterpolationData data)
    {
        if (data.Xs.Length < 2)
        {
            return ValidationResult.Fail("Потрібно не менше двох вузлів інтерполяції.");
        }

        if (data.Xs.Length != data.Ys.Length)
        {
            return ValidationResult.Fail("Масиви X та Y повинні мати однакову довжину.");
        }

        for (int i = 0; i < data.Xs.Length; i++)
        {
            if (!double.IsFinite(data.Xs[i]) || !double.IsFinite(data.Ys[i]))
            {
                return ValidationResult.Fail(
                    $"Вузол [{i}]: x={data.Xs[i]}, y={data.Ys[i]} — значення має бути скінченним числом.");
            }
        }

        for (int i = 0; i < data.Xs.Length - 1; i++)
        {
            for (int j = i + 1; j < data.Xs.Length; j++)
            {
                if (Math.Abs(data.Xs[i] - data.Xs[j]) < 1e-14)
                {
                    return ValidationResult.Fail(
                        $"Значення x[{i}] = {data.Xs[i]} та x[{j}] = {data.Xs[j]} " +
                        "збігаються. Вузли x_i мають бути попарно різними.");
                }
            }
        }

        return ValidationResult.Ok();
    }

    public InterpolationData ParseFromText(string text)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            File.WriteAllText(tempFile, text);
            return LoadFromFile(tempFile);
        }
        catch
        {
            return new InterpolationData();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
