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

    /// <summary>
    /// A4.151 (pass 40): a SMC moves away from an Infantry OVR into an adjacent Location "of the ATTACKER's choice"; the options are the Locations.
    /// </summary>
    public const string OverrunEvade = "overrun-evade";

    /// <summary>
    /// C2.2401 (pass 55, ruling R55.19): the DEFENDER has declared a Defensive First Fire ordnance shot at a moving vehicle, which may fire its MA
    /// at that firer first; the ATTACKER's options are <see cref="Duel"/> and <see cref="Decline"/>. Its <see cref="Resume"/> holds the declared
    /// shot's arguments and no roll.
    /// </summary>
    public const string GunDuel = Rules.ScenarioA1GunDuels.ChoiceKind;
    public const string Duel = Rules.ScenarioA1GunDuels.Duel;
    public const string Reveal = "reveal";
    public const string Check = "paatc";

    /// <summary>The answers to a yes-or-no choice: take the option, or decline it.</summary>
    public const string Take = "take";
    public const string Decline = "decline";

    /// <summary>
    /// A12.14 (pass 70, ruling R70.21): a concealed stack has done something that loses its "?" in the LOS of the viewing side's concealed units only; that
    /// side forces the loss (<see cref="Take"/>, one of them forfeiting its own "?" for a moment) or declines it (<see cref="Decline"/>). The choice and its
    /// answer are recorded for the viewing side only.
    /// </summary>
    public const string ConcealmentLoss = Rules.ScenarioA1ConcealmentChoices.ConcealmentLossChoice;

    /// <summary>
    /// A14.4 (pass 71, ruling R71.10): after an effective Sniper attack, the attacked side's units in the target Location that may take a Sniper Check
    /// take it together (<see cref="Take"/>) or not (<see cref="Decline"/>). Its <see cref="Resume"/> names the Sniper, the Location, and the Sniper
    /// attacks still to resolve, never a unit.
    /// </summary>
    public const string SniperCheck = "sniper-check";
}

/// <summary>
/// <c>concealment-verified</c>: the viewing side's concealed units that forced a "?" loss forfeit their own "?" for a moment, shown to the moving side
/// only, and are concealed again at once (A12.14: "to prove that it is not a Dummy"; pass 70, ruling R70.21). It changes nothing in the state.
/// </summary>
public sealed record ConcealmentVerified(IReadOnlyList<string> Units) : EventPayload;

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
/// <c>san-reduced</c>: a side's Sniper was eliminated, by a Sniper Check (<c>sniper-check</c>) or by the enemy Sniper's attack (<c>sniper-attack</c>), and
/// its SAN is one lower for the rest of the game (A14.4; pass 71, ruling R71.9). The side's printed SAN stays as recorded; the reductions are counted.
/// </summary>
public sealed record SanReduced(string Side, string Cause) : EventPayload;

/// <summary>
/// <c>sniper-checked</c>: the Sniper Check of the units of the attacked side in the target Location (A14.4; pass 71, ruling R71.10): the Sniper, the
/// Location, the units that took it, the roll of its one combined DR, its DRM, and the result (<c>sniper-eliminated</c>, <c>sniper-pinned</c>, or
/// <c>none</c>). The events after it apply it.
/// </summary>
public sealed record SniperChecked(string Sniper, BoardLocation Location, IReadOnlyList<string> Units, string Roll, int Drm, string Result) : EventPayload;

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
