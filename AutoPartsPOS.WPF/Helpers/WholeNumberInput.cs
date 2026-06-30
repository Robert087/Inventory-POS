using System.Globalization;

namespace AutoPartsPOS.WPF.Helpers;

public static class WholeNumberInput
{
    public static string Format(decimal value) =>
        decimal.Truncate(value).ToString("0", CultureInfo.InvariantCulture);

    public static string FormatOptional(decimal value) =>
        value == 0 ? string.Empty : Format(value);

    public static bool TryParseOptional(string? text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        if (!decimal.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        if (value != decimal.Truncate(value))
        {
            return false;
        }

        return true;
    }

    public static bool TryParseRequired(
        string? text,
        out decimal value,
        out string? errorMessage,
        string requiredMessage,
        string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            errorMessage = requiredMessage;
            return false;
        }

        if (!decimal.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value))
        {
            errorMessage = invalidMessage;
            return false;
        }

        if (value != decimal.Truncate(value))
        {
            errorMessage = invalidMessage;
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static bool TryParseRequiredPositive(
        string? text,
        out decimal value,
        out string? errorMessage,
        string requiredMessage,
        string zeroMessage,
        string invalidMessage)
    {
        if (!TryParseRequired(text, out value, out errorMessage, requiredMessage, invalidMessage))
        {
            return false;
        }

        if (value <= 0)
        {
            errorMessage = zeroMessage;
            return false;
        }

        errorMessage = null;
        return true;
    }
}
