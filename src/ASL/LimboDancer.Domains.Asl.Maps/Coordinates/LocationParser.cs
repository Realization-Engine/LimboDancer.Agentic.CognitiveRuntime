using System.Globalization;

namespace LimboDancer.Domains.Asl.Maps.Coordinates;

/// <summary>
/// The boards a text refers to: a scenario's board list or a composed map. It maps published board numbers
/// (the <c>1</c> in <c>1E4</c>) to board references, and may name a single current board for bare hex names.
/// </summary>
public sealed class BoardSet
{
    private readonly Dictionary<int, BoardRef> boardsByNumber;

    public BoardSet(IReadOnlyDictionary<int, BoardRef> boardsByNumber, BoardRef? currentBoard = null)
    {
        ArgumentNullException.ThrowIfNull(boardsByNumber);
        this.boardsByNumber = new Dictionary<int, BoardRef>(boardsByNumber);
        CurrentBoard = currentBoard;
    }

    public static BoardSet Empty { get; } = new(new Dictionary<int, BoardRef>());

    public BoardRef? CurrentBoard
    {
        get;
    }

    public static BoardSet SingleBoard(BoardRef board) => new(new Dictionary<int, BoardRef>(), board);

    public bool TryGetBoard(int boardNumber, out BoardRef? board) => boardsByNumber.TryGetValue(boardNumber, out board);
}

public enum LocationParseStatus
{
    Parsed,
    Malformed,
    UnmappedBoardNumber,
    NoCurrentBoard,
}

public sealed record LocationParseResult(LocationParseStatus Status, BoardLocation? Location, string Message)
{
    public bool Succeeded => Status == LocationParseStatus.Parsed;
}

/// <summary>
/// Resolves published location forms to canonical <see cref="BoardLocation"/> values without guessing:
/// board numbers resolve only through the supplied <see cref="BoardSet"/>, and bare hex names only against
/// its current board (ASL-RD-001, ASL-RD-011).
/// </summary>
public static class LocationParser
{
    public static LocationParseResult Parse(string text, BoardSet boards)
    {
        ArgumentNullException.ThrowIfNull(boards);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Fail(LocationParseStatus.Malformed, "Location text is empty.");
        }

        if (text.Contains(':', StringComparison.Ordinal))
        {
            return BoardLocation.TryParse(text, out var exact)
                ? new LocationParseResult(LocationParseStatus.Parsed, exact, "Canonical location.")
                : Fail(LocationParseStatus.Malformed, $"'{text}' is not a canonical board location.");
        }

        var digitCount = 0;
        while (digitCount < text.Length && char.IsAsciiDigit(text[digitCount]))
        {
            digitCount++;
        }

        if (!HexName.TryParse(text[digitCount..], out var hex))
        {
            return Fail(LocationParseStatus.Malformed, $"'{text}' does not contain a hex name.");
        }

        if (digitCount == 0)
        {
            return boards.CurrentBoard is { } current
                ? new LocationParseResult(LocationParseStatus.Parsed, new BoardLocation(current, hex, 0), "Hex on the current board.")
                : Fail(LocationParseStatus.NoCurrentBoard, $"'{text}' names no board and no current board is set.");
        }

        if (text[0] == '0' || !int.TryParse(text.AsSpan(0, digitCount), NumberStyles.None, CultureInfo.InvariantCulture, out var boardNumber))
        {
            return Fail(LocationParseStatus.Malformed, $"'{text}' has an invalid board number.");
        }

        return boards.TryGetBoard(boardNumber, out var board) && board is not null
            ? new LocationParseResult(LocationParseStatus.Parsed, new BoardLocation(board, hex, 0), $"Board {boardNumber} from the board set.")
            : Fail(LocationParseStatus.UnmappedBoardNumber, $"Board {boardNumber} in '{text}' is not in the board set.");
    }

    private static LocationParseResult Fail(LocationParseStatus status, string message) => new(status, null, message);
}
