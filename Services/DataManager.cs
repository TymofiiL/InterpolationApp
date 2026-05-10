using System.Globalization;
using InterpolationApp.Models;

namespace InterpolationApp.Services;

public sealed class DataManager
{
    private static readonly char[] Separators = { ' ', '\t', ';', ',' };

    public InterpolationData LoadFromFile(string filePath)
    {
        var xs = new List<double>();
        var ys = new List<double>();

        var style = NumberStyles.Any;
        var culture = CultureInfo.InvariantCulture;

        foreach (string rawLine in File.ReadAllLines(filePath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
            {
                continue;
            }

            string[] parts = line.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            string xStr = parts[0].Replace(',', '.');
            string yStr = parts[1].Replace(',', '.');

            if (double.TryParse(xStr, style, culture, out double xi) &&
                double.TryParse(yStr, style, culture, out double yi))
            {
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
