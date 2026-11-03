using System.Globalization;

namespace InvoiceInsight.Helpers;

public static class TextFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    public const string CurrencyPrefix = "SYR";

    public static string Money(decimal value, string format = "N2") =>
        $"{CurrencyPrefix} {value.ToString(format, Culture)}";

    // The charts work with double values.
    public static string Money(double value, string format = "N2") =>
        $"{CurrencyPrefix} {value.ToString(format, Culture)}";

    public static string Number(int value) =>
        value.ToString("N0", Culture);

    // Trimmed, upper-case form for comparing names and invoice numbers,
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();
}
