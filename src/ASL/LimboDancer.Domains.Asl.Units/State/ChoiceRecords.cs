using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// <c>choice-pending</c>: a resolution stopped before an optional step that a side chooses (ruling R5.8): the Leader Creation dr after a
/// Self-Rally Original 2 (A18.11), Battle Hardening (A15.3), the Unlikely Kill dr (A7.309), or which Location keeps a Gun's Acquisition
/// (C6.51). <paramref name="Key"/> names the step within its resolution, <paramref name="Side"/> the side that chooses, and
/// <paramref name="Options"/> the answers it may give. <paramref name="Resume"/> is what the resolution needs to continue: its facts and
/// the rolls it already drew; the Units project does not read it. Until the side chooses, nothing else happens in the game.
/// </summary>
public sealed record ChoicePending(string Key, string Kind, string Side, IReadOnlyList<string> Options, JsonElement Resume) : EventPayload
{
    public const string LeaderCreation = "leader-creation";
    public const string BattleHardening = "battle-hardening";
    public const string UnlikelyKill = "unlikely-kill";
    public const string Acquisition = "acquisition";

    /// <summary>
    /// A12.41 (backlog pass 11, ruling R11.12): concealed Personnel entered by an enemy vehicle are revealed or take one combined PAATC, their owner's
    /// choice; the options are <see cref="Reveal"/> and <see cref="Check"/>.
    /// </summary>
    public const string Paatc = "paatc";
    public const string Reveal = "reveal";
    public const string Check = "paatc";

    /// <summary>The answers to a yes-or-no choice: take the option, or decline it.</summary>
    public const string Take = "take";
    public const string Decline = "decline";
}

/// <summary><c>choice-made</c>: the choosing side's answer to the pending choice (ruling R5.8).</summary>
public sealed record ChoiceMade(string Key, string Option) : EventPayload;

/// <summary>The choice the game waits for, from its <c>choice-pending</c> event.</summary>
public sealed record PendingChoice(string Key, string Kind, string Side, IReadOnlyList<string> Options, string Event, JsonElement Resume);

/// <summary>
/// <c>surrender-rejected</c>: the captor's side rejects a pending surrender (A20.3, ruling R5.6): the unit is eliminated, and its side is
/// faced with No Quarter from then on.
/// </summary>
public sealed record SurrenderRejected(string Unit) : EventPayload;

/// <summary>
/// <c>prisoner-freed</c>: a prisoner is no longer guarded (A20.5, A20.55; backlog pass 14, rulings R14.5 to R14.7): it escaped by attacking in CC, or its Guard
/// abandoned it; it stays an Unarmed unit of its own side.
/// </summary>
public sealed record PrisonerFreed(string Unit) : EventPayload;

/// <summary>
/// <c>sniper-attacked</c>: a side's Sniper attack (A14.1 to A14.3; backlog pass 15, ruling R15.5), after the roll <paramref name="Trigger"/> equal to
/// its SAN: its dr's roll and value, the hex its Random Location DR found and the Location attacked, the unit attacked, and the result
/// (<c>none</c>, <c>eliminated</c>, <c>broken</c>, <c>casualty-reduced</c>, <c>wounded</c>, <c>pinned</c>, or <c>no-target</c>). The events that follow
/// apply it.
/// </summary>
public sealed record SniperAttacked(string Sniper, string Trigger, string Roll, int Dr, BoardLocation? Target, string? Unit, string Result) : EventPayload;

/// <summary>
/// <c>wind-changed</c>: the Wind Change DR <paramref name="Roll"/> made at the start of a RPh (B25.65; backlog pass 16, ruling R16.10), and what it
/// changed: the Base NVR of a night game (E1.12, with the further dr <paramref name="NvrRoll"/> a Scattered sky with a moon calls for), the
/// precipitation (E3.51, E3.71: <c>rain</c>, <c>heavy-rain</c>, <c>snow</c>, <c>heavy-snow</c>, or none), and whether a Gust blows (E3.4).
/// </summary>
public sealed record WindChanged(string Roll, int? Nvr, string? Precipitation, bool Gust, string? NvrRoll) : EventPayload;

/// <summary>
/// <c>starshell-fired</c>: a Starshell attempt (E1.92 to E1.923; backlog pass 16, ruling R16.8) by <paramref name="Unit"/> from <paramref name="From"/>,
/// its Usage dr <paramref name="UsageRoll"/>, and, when that passed, the placement method, roll, and the Starshell <paramref name="Starshell"/> in its
/// final Location <paramref name="At"/> (none when it fell off the map).
/// </summary>
public sealed record StarshellFired(string Unit, BoardLocation From, string Method, string UsageRoll, bool Passed, string? PlacementRoll, BoardLocation? At,
    string? Starshell) : EventPayload;

/// <summary>
/// <c>prisoners-massacred</c>: prisoners eliminated in their Location (A20.4, ruling R5.7), by units of the Guard's side that may massacre
/// (Russian or berserk Infantry, not in Melee) in a fire phase of their own side. The victims' side has its ELR raised by one, once, to at
/// most 6, and is faced with No Quarter. <paramref name="Berserk"/> marks the massacre a berserk unit makes at the start of its fire phase,
/// which returns it to normal.
/// </summary>
public sealed record PrisonersMassacred(IReadOnlyList<string> Units, IReadOnlyList<string> Prisoners, bool Berserk) : EventPayload;

/// <summary>
/// <c>acquisition-changed</c>: where a Gun's Acquisition is now (C6.5, C6.51, ruling R5.13): the Known units it is on, which the planner
/// reads after a shot, or none when its target left the Gun's LOS and the counter stays in the last Location in LOS.
/// </summary>
public sealed record AcquisitionChanged(string Gun, BoardLocation Location, IReadOnlyList<string> Units) : EventPayload;
