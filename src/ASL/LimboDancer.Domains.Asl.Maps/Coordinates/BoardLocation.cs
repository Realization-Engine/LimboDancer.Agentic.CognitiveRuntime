using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Coordinates;

/// <summary>
/// A location on a board: <c>&lt;boardRef&gt;:&lt;hex&gt;:&lt;level&gt;</c>, optionally with a hexside
/// suffix <c>/&lt;side&gt;</c>. Level 0 is ground level, positive levels are upper building levels,
/// and -1 is a cellar where the hex has one. <c>bd01:E4:0</c> is the Scenario A1 form.
/// </summary>
public sealed record BoardLocation(BoardRef Board, HexName Hex, int Level, HexsideDirection? Side = null)
{
    public static BoardLocation Parse(string text) =>
        TryParse(text, out var location) ? location : throw new FormatException($"'{text}' is not a board location.");

    /// <summary>Parses the exact canonical text form. Published short forms go through <see cref="LocationParser"/>.</summary>
    public static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out BoardLocation? location)
    {
        location = null;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var parts = text.Split(':');
        if (parts.Length != 3 || !BoardRef.TryParse(parts[0], out var board) || !HexName.TryParse(parts[1], out var hex))
        {
            return false;
        }

        var levelText = parts[2];
        HexsideDirection? side = null;
        var slash = levelText.IndexOf('/', StringComparison.Ordinal);
        if (slash >= 0)
        {
            var sideText = levelText[(slash + 1)..];
            if (sideText.Length != 1 || sideText[0] is < '0' or > '5')
            {
                return false;
            }

            side = (HexsideDirection)(sideText[0] - '0');
            levelText = levelText[..slash];
        }

        if (!TryParseLevel(levelText, out var level))
        {
            return false;
        }

        location = new BoardLocation(board, hex, level, side);
        return true;
    }

    /// <summary>The same location with its hexside expressed canonically on the given board geometry.</summary>
    public BoardLocation Canonicalize(BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (Side is not { } side)
        {
            return this;
        }

        var canonical = geometry.Canonicalize(new HexsideRef(geometry.IndexOf(Hex), side));
        return this with
        {
            Hex = geometry.NameOf(canonical.Hex),
            Side = canonical.Side
        };
    }

    public BoardLocation AtGroundLevel() => this with { Level = 0, Side = null };

    public override string ToString()
    {
        var text = string.Concat(Board.Value, ":", Hex.ToString(), ":", Level.ToString(CultureInfo.InvariantCulture));
        return Side is { } side ? text + "/" + ((int)side).ToString(CultureInfo.InvariantCulture) : text;
    }

    // Canonical level text is a plain integer: no sign on non-negative values, no leading zeros.
    private static bool TryParseLevel(string text, out int level)
    {
        level = 0;
        var negative = text.StartsWith('-');
        var digits = negative ? text[1..] : text;
        if (digits.Length is 0 or > 2
            || !digits.All(char.IsAsciiDigit)
            || (digits.Length > 1 && digits[0] == '0')
            || (negative && digits == "0"))
        {
            return false;
        }

        level = int.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        return true;
    }
}
