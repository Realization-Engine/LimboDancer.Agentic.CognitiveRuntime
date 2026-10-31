namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A disclosed entry in its recorded order. Detailed layouts keep their own supplied arithmetic.</summary>
public sealed record ActionOutcomeEntry(string EventId, string Text, FireView? Fire, CloseCombatTable? Combat)
{
    public string Kind
    {
        get; init;
    } = "action";

    public bool ShowPrimaryDice
    {
        get; init;
    } = true;

    public IReadOnlyList<RollRow> Rolls
    {
        get; init;
    } = [];
}

public sealed record ActionOutcomeView(string Key, string EventId, string Context, string Summary,
    IReadOnlyList<ActionOutcomeEntry> Entries, IReadOnlyList<RollRow> Rolls);
