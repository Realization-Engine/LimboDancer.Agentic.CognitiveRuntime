using System.Globalization;
using LimboDancer.Domains.Asl.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Components.Cards;

/// <summary>
/// A card as the editor's forms hold it (pass 28, ruling R28.1): every field of the card format, numbers kept as typed so a field that is not yet a
/// number is reported rather than lost. <see cref="Build"/> turns it into a card, or says in plain words why it cannot.
/// </summary>
public sealed class CardDraft
{
    public string Id { get; set; } = "my-game";

    public string Title { get; set; } = "My game";

    public string Place { get; set; } = string.Empty;

    public string Day { get; set; } = "0";

    public string Month { get; set; } = "0";

    public string Year { get; set; } = "0";

    public string Introduction { get; set; } = string.Empty;

    public List<BoardDraft> Boards { get; set; } = [new() { Board = "bd01" }];

    public string North { get; set; } = "top";

    public string PlayableText { get; set; } = string.Empty;

    public string PlayableFrom { get; set; } = string.Empty;

    public string PlayableTo { get; set; } = string.Empty;

    public string PlayableBoard { get; set; } = string.Empty;

    public string Turns { get; set; } = "10";

    public bool HalfTurn { get; set; }

    public string SetsUpFirst { get; set; } = "german";

    /// <summary>The side that moves first, empty when a die roll decides it (ruling R17.12).</summary>
    public string MovesFirst { get; set; } = "german";

    public string MovesFirstNote { get; set; } = "A die roll before play decides which side moves first.";

    public string Defender { get; set; } = string.Empty;

    public SideDraft[] Sides { get; set; } = [new(), new() { Side = "russian" }];

    public List<RuleDraft> Rules { get; set; } = [];

    public VictoryDraft Victory { get; set; } = new();

    public string SourceBasis { get; set; } = "A user card (ruling R22.2).";

    public string SourceLegacy { get; set; } = "none";

    /// <summary>The adaptation notes, one per line.</summary>
    public string Adaptation { get; set; } = string.Empty;

    public string Aftermath { get; set; } = string.Empty;

    /// <summary>A new minimal card (ruling R22.3).</summary>
    public static CardDraft Minimal() => new();

    /// <summary>The fields of a card: a user card as it is, a built-in card as a copy under a new id (ruling R22.2).</summary>
    public static CardDraft From(ScenarioCard card, bool user)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new CardDraft
        {
            Id = user ? card.Id : card.Id + "-copy",
            Title = user ? card.Title : card.Title + " (copy)",
            Place = card.Place,
            Day = Text(card.Date.Day),
            Month = Text(card.Date.Month),
            Year = Text(card.Date.Year),
            Introduction = card.Introduction,
            Boards = [.. card.Boards.Select(board => new BoardDraft { Board = board.Board, Column = Text(board.Column), Row = Text(board.Row), Reversed = board.Reversed })],
            North = card.North,
            PlayableText = card.PlayableArea?.Text ?? string.Empty,
            PlayableFrom = card.PlayableArea?.Hexrows?.From ?? string.Empty,
            PlayableTo = card.PlayableArea?.Hexrows?.To ?? string.Empty,
            PlayableBoard = card.PlayableArea?.Hexrows?.Board ?? string.Empty,
            Turns = Text(card.Turns.Count),
            HalfTurn = card.Turns.HalfTurn,
            SetsUpFirst = card.Turns.SetsUpFirst,
            MovesFirst = card.Turns.MovesFirst ?? string.Empty,
            MovesFirstNote = card.Turns.MovesFirstNote ?? "A die roll before play decides which side moves first.",
            Defender = card.ScenarioDefender ?? string.Empty,
            Sides = [.. card.Sides.Take(2).Select(SideDraft.From)],
            Rules = [.. card.SpecialRules.Select(RuleDraft.From)],
            Victory = VictoryDraft.From(card.VictoryConditions),
            SourceBasis = card.Source.Basis,
            SourceLegacy = card.Source.Legacy,
            Adaptation = string.Join('\n', card.Source.Adaptation),
            Aftermath = card.Aftermath ?? string.Empty,
        };
    }

    /// <summary>The card the fields describe, under the catalog named, or the problems that stop it, each a code and a plain message.</summary>
    public (ScenarioCard? Card, List<string> Problems) Build(string catalog)
    {
        var problems = new List<string>();
        var read = new Numbers(problems);
        var boards = Boards.Select((board, index) => new ScenarioCardBoard(board.Board.Trim(), read.Int(board.Column, $"card.boards: board {index + 1}'s column"),
            read.Int(board.Row, $"card.boards: board {index + 1}'s row"), board.Reversed)).ToList();
        var sides = Sides.Select(side => side.Build(read)).ToList();
        var rules = Rules.Select((rule, index) => rule.Build(index + 1)).ToList();
        var victory = Victory.Build(read);
        var playable = PlayableText.Trim().Length == 0 && PlayableFrom.Trim().Length == 0 ? null
            : new ScenarioCardArea(PlayableText.Trim().Length > 0 ? PlayableText.Trim() : $"Only hexrows {PlayableFrom.Trim()} to {PlayableTo.Trim()} are playable.",
                PlayableFrom.Trim().Length > 0,
                PlayableFrom.Trim().Length > 0 ? new ScenarioCardHexrows(PlayableFrom.Trim(), PlayableTo.Trim(), PlayableBoard.Trim().Length > 0 ? PlayableBoard.Trim() : null) : null);
        var card = new ScenarioCard(ScenarioCards.Format, Id.Trim(), Title, catalog,
            new ScenarioCardSource(SourceBasis, SourceLegacy, Lines(Adaptation)), Place,
            new ScenarioCardDate(read.Int(Day, "card.date: the day"), read.Int(Month, "card.date: the month"), read.Int(Year, "card.date: the year")), Introduction,
            boards, North, playable,
            new ScenarioCardTurns(read.Int(Turns, "card.turns: the Game Turns"), HalfTurn, SetsUpFirst, MovesFirst.Length > 0 ? MovesFirst : null,
                MovesFirst.Length > 0 ? null : MovesFirstNote),
            Defender.Length > 0 ? Defender : null, sides, rules, victory, Aftermath.Trim().Length > 0 ? Aftermath.Trim() : null);
        foreach (var side in Sides.Where(side => side.Groups.Count > 0 && side.Elr.Trim().Length > 0))
        {
            problems.Add($"card.elr: {side.Side} has OB groups, which give their own ELR; leave the side's ELR empty (ruling R22.3)");
        }

        return (problems.Count == 0 ? card : null, problems);
    }

    /// <summary>A side's new nationality, which the sides naming it on the Turn Record Chart and as Scenario Defender follow (table player, pass 22).</summary>
    public void ChangeSide(int at, string side)
    {
        var old = Sides[at].Side;
        Sides[at].Side = side;
        if (side != "axis-minor")
        {
            Sides[at].Nation = string.Empty;
        }

        string Follow(string value) => value == old ? side : value;
        (SetsUpFirst, MovesFirst, Defender) = (Follow(SetsUpFirst), Follow(MovesFirst), Follow(Defender));
        foreach (var outcome in Victory.Outcomes)
        {
            outcome.Winner = Follow(outcome.Winner);
            foreach (var condition in outcome.Conditions)
            {
                condition.Side = Follow(condition.Side);
            }
        }

        Victory.Otherwise = Follow(Victory.Otherwise);
    }

    internal static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    internal static string Text(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Space- or comma-separated words, such as hexes or tokens.</summary>
    internal static List<string> Words(string text) => [.. text.Split([' ', ',', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    internal static List<string> Lines(string text) => [.. text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    /// <summary>Reads the typed numbers, noting each that is not one.</summary>
    internal sealed class Numbers(List<string> problems)
    {
        public int Int(string text, string what)
        {
            if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            problems.Add($"{what} is '{text}', which is not a whole number");
            return 0;
        }

        public int? OptionalInt(string text, string what) => text.Trim().Length == 0 ? null : Int(text, what);

        public double? OptionalDouble(string text, string what)
        {
            if (text.Trim().Length == 0)
            {
                return null;
            }

            if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            problems.Add($"{what} is '{text}', which is not a number");
            return null;
        }
    }
}

/// <summary>A board of the card in its slot (A2.1; ruling R17.3).</summary>
public sealed class BoardDraft
{
    public string Board { get; set; } = string.Empty;

    public string Column { get; set; } = "0";

    public string Row { get; set; } = "0";

    public bool Reversed { get; set; }
}

/// <summary>A side's fields (rulings R17.5 to R17.8, R20.4, R22.3, R27.1).</summary>
public sealed class SideDraft
{
    public string Side { get; set; } = "german";

    /// <summary>An Axis Minor side's nation (A25.8; ruling R27.1); empty for any other side.</summary>
    public string Nation { get; set; } = string.Empty;

    public string San { get; set; } = "0";

    /// <summary>A minimal card's side ELR (ruling R22.3); empty on a card with an OB.</summary>
    public string Elr { get; set; } = "3";

    public string Edge { get; set; } = string.Empty;

    public string Basis { get; set; } = "none";

    public string EdgeNote { get; set; } = string.Empty;

    public string Integrity { get; set; } = string.Empty;

    public string Balance { get; set; } = string.Empty;

    /// <summary>The counters the Balance adds (ruling R20.4); null when the card lists none, kept apart from an empty list.</summary>
    public List<UnitDraft>? BalanceUnits { get; set; }

    public List<GroupDraft> Groups { get; set; } = [];

    public static SideDraft From(ScenarioCardSide side) => new()
    {
        Side = side.Side,
        Nation = side.Nation ?? string.Empty,
        San = CardDraft.Text(side.San),
        Elr = CardDraft.Text(side.Elr),
        Edge = side.FriendlyEdge.Edge,
        Basis = side.FriendlyEdge.Basis,
        EdgeNote = side.FriendlyEdge.Note ?? string.Empty,
        Integrity = CardDraft.Text(side.IntegrityBpv),
        Balance = side.Balance,
        BalanceUnits = side.BalanceUnits is { } units ? [.. units.Select(UnitDraft.From)] : null,
        Groups = [.. side.Groups.Select(GroupDraft.From)],
    };

    internal ScenarioCardSide Build(CardDraft.Numbers read)
    {
        var groups = Groups.Select(group => group.Build(read, Side)).ToList();
        return new ScenarioCardSide(Side, read.Int(San, $"card.san: {Side}'s SAN"), null,
            new ScenarioCardEdge(Edge, Edge.Length == 0 ? "none" : Basis == "none" ? "manufactured" : Basis, EdgeNote.Trim().Length > 0 ? EdgeNote.Trim() : null),
            Balance, groups, BalanceUnits?.Select(unit => unit.Build(read, $"{Side}'s Balance")).ToList(),
            groups.Count == 0 && Elr.Trim().Length > 0 ? read.Int(Elr, $"card.elr: {Side}'s ELR") : null,
            Side == "axis-minor" && Nation.Length > 0 ? Nation : null)
        {
            IntegrityBpv = read.OptionalInt(Integrity, $"card.integrity: {Side}'s Battlefield Integrity total"),
        };
    }
}

/// <summary>An OB group (A19.1; rulings R17.5, R17.8, R17.12).</summary>
public sealed class GroupDraft
{
    public string Name { get; set; } = "New group";

    public string Elr { get; set; } = "3";

    public string SetupOrder { get; set; } = string.Empty;

    public string Dummies { get; set; } = string.Empty;

    public List<AreaDraft> Areas { get; set; } = [];

    public List<UnitDraft> Units { get; set; } = [];

    public static GroupDraft From(ScenarioCardGroup group) => new()
    {
        Name = group.Name,
        Elr = CardDraft.Text(group.Elr),
        SetupOrder = CardDraft.Text(group.SetupOrder),
        Dummies = CardDraft.Text(group.Dummies),
        Areas = [.. group.Areas.Select(AreaDraft.From)],
        Units = [.. group.Units.Select(UnitDraft.From)],
    };

    internal ScenarioCardGroup Build(CardDraft.Numbers read, string side) =>
        new(Name, read.Int(Elr, $"card.elr: {side}'s group '{Name}' ELR"), [.. Areas.Select(area => area.Build(read, Name))],
            [.. Units.Select(unit => unit.Build(read, $"{side}'s group '{Name}'"))],
            read.OptionalInt(SetupOrder, $"card.setup: '{Name}' place in the setup order"), read.OptionalInt(Dummies, $"card.ob: '{Name}' \"?\" count"));
}

/// <summary>A setup or entry area (rulings R17.8, R19.4, R25.2).</summary>
public sealed class AreaDraft
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = "building";

    /// <summary>A building's hexes, its anchor first, or an entry's named hexes; separated by spaces.</summary>
    public string Hexes { get; set; } = string.Empty;

    public string Board { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;

    public string Turn { get; set; } = string.Empty;

    public string Edge { get; set; } = string.Empty;

    public string Limit { get; set; } = string.Empty;

    public string Counters { get; set; } = string.Empty;

    public string MinMmc { get; set; } = string.Empty;

    /// <summary>Whether its counters may set up under "?", or no word on it (ruling R19.4).</summary>
    public bool? Concealed { get; set; }

    public static AreaDraft From(ScenarioCardSetup area) => new()
    {
        Id = area.Id,
        Kind = area.Kind,
        Hexes = area.Hexes is { } hexes ? string.Join(' ', hexes) : string.Empty,
        Board = area.Board ?? string.Empty,
        From = CardDraft.Text(area.From),
        To = CardDraft.Text(area.To),
        Turn = CardDraft.Text(area.Turn),
        Edge = area.Edge ?? string.Empty,
        Limit = area.Limit ?? string.Empty,
        Counters = CardDraft.Text(area.Counters),
        MinMmc = CardDraft.Text(area.MinMmc),
        Concealed = area.Concealed,
    };

    internal ScenarioCardSetup Build(CardDraft.Numbers read, string group)
    {
        var what = $"card.setup: '{group}' area '{Id}'";
        var hexes = CardDraft.Words(Hexes);
        return Kind switch
        {
            "building" => new ScenarioCardSetup(Id.Trim(), Kind, hexes, Opt(Board), null, null, null, null, Opt(Limit),
                read.OptionalInt(Counters, $"{what} counters"), read.OptionalInt(MinMmc, $"{what} MMC at least"), Concealed),
            "hex-numbers" => new ScenarioCardSetup(Id.Trim(), Kind, null, Opt(Board), read.OptionalInt(From, $"{what} from"), read.OptionalInt(To, $"{what} to"), null, null, Opt(Limit),
                read.OptionalInt(Counters, $"{what} counters"), read.OptionalInt(MinMmc, $"{what} MMC at least"), Concealed),
            _ => new ScenarioCardSetup(Id.Trim(), Kind, hexes.Count > 0 ? hexes : null, Opt(Board), null, null, read.OptionalInt(Turn, $"{what} Game Turn"), Opt(Edge), Opt(Limit)),
        };
    }

    private static string? Opt(string text) => text.Trim().Length > 0 ? text.Trim() : null;
}

/// <summary>A counter line: a catalog definition, a count, and its area (ruling R17.8).</summary>
public sealed class UnitDraft
{
    public string Definition { get; set; } = string.Empty;

    public string Count { get; set; } = "1";

    public string Area { get; set; } = string.Empty;

    public static UnitDraft From(ScenarioCardUnit unit) => new() { Definition = unit.Definition, Count = CardDraft.Text(unit.Count), Area = unit.Area ?? string.Empty };

    internal ScenarioCardUnit Build(CardDraft.Numbers read, string where) =>
        new(Definition, read.Int(Count, $"card.ob: {where} count of '{Definition}'"), Area.Length > 0 ? Area : null);
}

/// <summary>An SSR (ruling R17.10), numbered by its place.</summary>
public sealed class RuleDraft
{
    public string Text { get; set; } = string.Empty;

    public string Status { get; set; } = "not-enforced";

    public string Tokens { get; set; } = string.Empty;

    public string Rules { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public static RuleDraft From(ScenarioCardRule rule) => new()
    {
        Text = rule.Text,
        Status = rule.Status,
        Tokens = string.Join(' ', rule.Tokens),
        Rules = string.Join(' ', rule.Rules),
        Note = rule.Note ?? string.Empty,
    };

    internal ScenarioCardRule Build(int number) =>
        new(number, Text, Status, Status == "token" ? CardDraft.Words(Tokens) : [], CardDraft.Words(Rules), Note.Trim().Length > 0 ? Note.Trim() : null);
}

/// <summary>The Victory Conditions (A26; rulings R17.11, R21.3).</summary>
public sealed class VictoryDraft
{
    public string Kind { get; set; } = "other";

    public string Text { get; set; } = "The players judge the result.";

    public string Rules { get; set; } = string.Empty;

    /// <summary>Whether the game evaluates them: the outcomes and the result when none holds are then part of the card.</summary>
    public bool Evaluated { get; set; }

    public List<OutcomeDraft> Outcomes { get; set; } = [];

    public string Otherwise { get; set; } = string.Empty;

    public static VictoryDraft From(ScenarioCardVictory victory) => new()
    {
        Kind = victory.Kind,
        Text = victory.Text,
        Rules = string.Join(' ', victory.Rules),
        Evaluated = victory.Outcomes is not null,
        Outcomes = [.. (victory.Outcomes ?? []).Select(OutcomeDraft.From)],
        Otherwise = victory.Otherwise ?? string.Empty,
    };

    internal ScenarioCardVictory Build(CardDraft.Numbers read) =>
        new(Kind, Text, CardDraft.Words(Rules), Evaluated ? [.. Outcomes.Select(outcome => outcome.Build(read))] : null,
            Evaluated && Otherwise.Length > 0 ? Otherwise : null);
}

/// <summary>An outcome: its winner when any of its conditions holds (ruling R21.3).</summary>
public sealed class OutcomeDraft
{
    public string Winner { get; set; } = string.Empty;

    public bool Immediate { get; set; }

    public List<ConditionDraft> Conditions { get; set; } = [];

    public static OutcomeDraft From(ScenarioCardOutcome outcome) => new()
    {
        Winner = outcome.Winner,
        Immediate = outcome.Immediate,
        Conditions = [.. outcome.Any.Select(ConditionDraft.From)],
    };

    internal ScenarioCardOutcome Build(CardDraft.Numbers read) => new(Winner, Immediate, [.. Conditions.Select(condition => condition.Build(read))]);
}

/// <summary>A condition the game evaluates (ruling R21.3); only the fields of its type are written.</summary>
public sealed class ConditionDraft
{
    public string Type { get; set; } = "control-margin";

    public string Side { get; set; } = string.Empty;

    public string Buildings { get; set; } = string.Empty;

    public string Versus { get; set; } = string.Empty;

    public string Margin { get; set; } = string.Empty;

    public string Building { get; set; } = string.Empty;

    public string AtLeast { get; set; } = string.Empty;

    public bool MeleeUncontrolled { get; set; }

    public string Ratio { get; set; } = string.Empty;

    public string Edge { get; set; } = string.Empty;

    /// <summary>The hexes an exit is made on or adjacent to, each with its board (<c>bd02:I1</c>).</summary>
    public string Near { get; set; } = string.Empty;

    public static ConditionDraft From(ScenarioCardCondition condition) => new()
    {
        Type = condition.Type,
        Side = condition.Side,
        Buildings = string.Join(' ', condition.Buildings ?? []),
        Versus = string.Join(' ', condition.Versus ?? []),
        Margin = CardDraft.Text(condition.Margin),
        Building = condition.Building ?? string.Empty,
        AtLeast = CardDraft.Text(condition.AtLeast),
        MeleeUncontrolled = condition.MeleeUncontrolled is true,
        Ratio = condition.Ratio?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        Edge = condition.Edge ?? string.Empty,
        Near = string.Join(' ', condition.Near ?? []),
    };

    internal ScenarioCardCondition Build(CardDraft.Numbers read)
    {
        var what = $"card.victory: the '{Type}' condition of {Side}";
        return Type switch
        {
            "control-margin" => new(Type, Side, Buildings: CardDraft.Words(Buildings), Versus: CardDraft.Words(Versus), Margin: read.OptionalInt(Margin, $"{what}, its margin")),
            "control-count" => new(Type, Side, Building: Opt(Building), AtLeast: read.OptionalInt(AtLeast, $"{what}, its hexes"), MeleeUncontrolled: MeleeUncontrolled ? true : null),
            "squad-ratio" => new(Type, Side, Ratio: read.OptionalDouble(Ratio, $"{what}, its ratio")),
            "sole-unbroken" => new(Type, Side, Building: Opt(Building)),
            "exit-vp" => new(Type, Side, AtLeast: read.OptionalInt(AtLeast, $"{what}, its Exit VP"), Edge: Opt(Edge), Near: CardDraft.Words(Near)),
            "cvp" => new(Type, Side, AtLeast: read.OptionalInt(AtLeast, $"{what}, its CVP")),
            _ => new(Type, Side),
        };
    }

    private static string? Opt(string text) => text.Trim().Length > 0 ? text.Trim() : null;
}
