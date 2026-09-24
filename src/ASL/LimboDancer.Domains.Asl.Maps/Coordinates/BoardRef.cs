using System.Diagnostics.CodeAnalysis;

namespace LimboDancer.Domains.Asl.Maps.Coordinates;

public enum BoardRefKind
{
    /// <summary>A VASL board: <c>bd</c> followed by the VASL board name, such as <c>bd01</c> or <c>bdRB</c>.</summary>
    Vasl,

    /// <summary>An authored board: <c>ab-</c> followed by a lowercase slug.</summary>
    Authored,

    /// <summary>A composed map: <c>map-</c> followed by a lowercase slug.</summary>
    ComposedMap,
}

/// <summary>Names a board lineage. A board version is identified separately by its content hash.</summary>
public sealed record BoardRef
{
    private BoardRef(BoardRefKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    public BoardRefKind Kind
    {
        get;
    }

    public string Value
    {
        get;
    }

    public static BoardRef Parse(string text) =>
        TryParse(text, out var boardRef) ? boardRef : throw new FormatException($"'{text}' is not a board reference.");

    public static bool TryParse([NotNullWhen(true)] string? text, [NotNullWhen(true)] out BoardRef? boardRef)
    {
        boardRef = null;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.StartsWith("ab-", StringComparison.Ordinal) && IsSlug(text.AsSpan(3)))
        {
            boardRef = new BoardRef(BoardRefKind.Authored, text);
        }
        else if (text.StartsWith("map-", StringComparison.Ordinal) && IsSlug(text.AsSpan(4)))
        {
            boardRef = new BoardRef(BoardRefKind.ComposedMap, text);
        }
        else if (text.StartsWith("bd", StringComparison.Ordinal) && IsVaslName(text.AsSpan(2)))
        {
            boardRef = new BoardRef(BoardRefKind.Vasl, text);
        }

        return boardRef is not null;
    }

    /// <summary>The VASL board name, such as <c>01</c> for <c>bd01</c>; only for VASL boards.</summary>
    public string VaslBoardName => Kind == BoardRefKind.Vasl
        ? Value[2..]
        : throw new InvalidOperationException($"'{Value}' is not a VASL board.");

    public static BoardRef ForVaslBoard(string vaslBoardName) => Parse("bd" + vaslBoardName);

    public override string ToString() => Value;

    private static bool IsVaslName(ReadOnlySpan<char> name)
    {
        if (name.IsEmpty)
        {
            return false;
        }

        foreach (var character in name)
        {
            if (!char.IsAsciiLetterOrDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSlug(ReadOnlySpan<char> slug)
    {
        if (slug.IsEmpty || slug[0] == '-' || slug[^1] == '-')
        {
            return false;
        }

        foreach (var character in slug)
        {
            if (!(char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-'))
            {
                return false;
            }
        }

        return true;
    }
}
