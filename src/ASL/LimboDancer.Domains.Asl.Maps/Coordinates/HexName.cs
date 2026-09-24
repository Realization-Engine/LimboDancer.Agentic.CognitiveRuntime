using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace LimboDancer.Domains.Asl.Maps.Coordinates;

/// <summary>
/// A published hex name such as <c>E4</c>, <c>AA8</c>, or <c>B0</c>: a column letter repeated
/// (A to Z, then AA to ZZ, and so on) followed by the printed row number.
/// </summary>
public readonly record struct HexName(int Column, int RowNumber)
{
    public static HexName Parse(string text)
    {
        if (!TryParse(text, out var name))
        {
            throw new FormatException($"'{text}' is not a hex name.");
        }

        return name;
    }

    public static bool TryParse([NotNullWhen(true)] string? text, out HexName name)
    {
        name = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var letterCount = 0;
        while (letterCount < text.Length && text[letterCount] is >= 'A' and <= 'Z')
        {
            letterCount++;
        }

        if (letterCount == 0 || letterCount == text.Length || !IsRowNumber(text.AsSpan(letterCount)))
        {
            return false;
        }

        var letter = text[0];
        for (var index = 1; index < letterCount; index++)
        {
            if (text[index] != letter)
            {
                return false;
            }
        }

        if (!int.TryParse(text.AsSpan(letterCount), NumberStyles.None, CultureInfo.InvariantCulture, out var rowNumber))
        {
            return false;
        }

        name = new HexName(((letterCount - 1) * 26) + (letter - 'A'), rowNumber);
        return true;
    }

    public static string ColumnLetters(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return new string((char)('A' + (column % 26)), (column / 26) + 1);
    }

    public override string ToString() => ColumnLetters(Column) + RowNumber.ToString(CultureInfo.InvariantCulture);

    // A row number is digits without a redundant leading zero ("0" itself is allowed).
    private static bool IsRowNumber(ReadOnlySpan<char> digits)
    {
        if (digits.Length == 0 || digits.Length > 4 || (digits.Length > 1 && digits[0] == '0'))
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (digit is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }
}
