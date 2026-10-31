using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of a morale check made outside an attack (pass 70, ruling R70.3): the check its <see cref="Subjects"/> take (<see cref="Nmc"/>, <c>1MC</c> to
/// <c>4MC</c>, <see cref="Ptc"/>, or <see cref="None"/> for a leader's loss alone), each as a Fire target with its optional facts, the side's ELR, and the
/// rolls. Every fact is supplied by the caller; the member never reads a game. The record keeps them with no rolls, as a fire record keeps its attack.
/// </summary>
public sealed record MoraleCheckFacts(string? Check, IReadOnlyList<FireTarget>? Subjects, int? TargetSideElr, FireRolls? Rolls)
{
    public const string Nmc = "NMC";
    public const string Ptc = "PTC";
    public const string None = "none";

    /// <summary>The other friendly units of the Location: they take no check, but may lead one or take a LLMC (A10.2, A10.7). Null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? Others
    {
        get; init;
    }

    /// <summary>The leaders lost outside the check, as they were just before (A10.2: "eliminated ... by ... any other cause"): each causes a LLMC. Null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? LostLeaders
    {
        get; init;
    }

    /// <summary>The unbroken leaders broken outside the check, as they were just before (A10.2: "Should an unbroken leader break"): each causes a LLTC. Null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? BrokenLeaders
    {
        get; init;
    }

    /// <summary>Whether the units' side is faced with No Quarter (A20.3): a Heat of Battle Surrender is Berserk. Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NoQuarter
    {
        get; init;
    }

    /// <summary>Whether the Location is in Melee (A11.141: "Units in CC never take LLMC/LLTC"). Null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? InMelee
    {
        get; init;
    }

    /// <summary>The owners' answers to the check's options (ruling R5.8), as an attack's; null takes every option, a Battle Hardening included.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Choices
    {
        get; init;
    }

    /// <summary>
    /// Pass 72 (A19.132; ruling R72.19): the stamp of a check recorded from the pass on, as an attack's <see cref="FireAttack.UnitSubstitution"/>: the verifier
    /// checks each unit's <see cref="FireTarget.UnderscoredElr"/> against the state. A check recorded before the pass has none, and a unit's SSR ELR it carries
    /// is checked only where it is present. The member does not read it. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UnitSubstitution
    {
        get; init;
    }
}

/// <summary>
/// A unit of a lost leader's Location as the LLMC reads it (A10.2; pass 70, the Rules boundary review): active, of the leader's side, one of the leaders lost,
/// a vehicle, a Dummy, a prisoner, and whether the catalog knows its definition.
/// </summary>
public sealed record LeaderLossUnitFacts(string Id, bool Active, bool OfSide, bool Lost, bool Vehicle, bool Dummy, bool Captured, bool InCatalog);

/// <summary>
/// One Morale Check member for every MC, NMC, PTC, LLMC, and LLTC made outside the Fire and Rally packages (pass 70, task 70.1; rulings R70.3 to R70.10):
/// the rout Interdiction NMC's Heat of Battle, the LLMC after a leader lost outside fire, and the Bail Out NMC. It is the Fire calculator's own resolution of a
/// check, with no IFT roll, so Heat of Battle (A15.1), ELR (A19.11 to A19.13), wounds (A17.11), and the Leader Loss cascade (A10.2) are decided once.
/// </summary>
public static class ScenarioA1MoraleCheck
{
    /// <summary>The causes a <c>morale-check-resolved</c> record names (pass 70, ruling R70.3).</summary>
    public const string Interdiction = "interdiction";
    public const string MoppingUp = "mopping-up";
    public const string Sniper = "sniper";
    public const string Rally = "rally";
    public const string Panzerfaust = "panzerfaust";
    public const string FailureToRout = "failure-to-rout";
    public const string VoluntaryBreak = "voluntary-break";
    public const string Searching = "searching";

    /// <summary>A Voluntary Rout leader eliminated because the unit he routed with failed its Interdiction NMC (A10.711; the table player's finding at the reviews of pass 70).</summary>
    public const string VoluntaryRout = "voluntary-rout";

    /// <summary>The words of a check's cause, for its record line, such as "Interdiction (A10.53)".</summary>
    public static string CauseText(string cause) => cause switch
    {
        VoluntaryBreak => "a Voluntary Break (A10.41)",
        VoluntaryRout => "the failed Interdiction NMC of the unit he routed with (A10.711)",
        Interdiction => "Interdiction (A10.53)",
        MoppingUp => "a Mopping Up's Casualty dr (A12.153)",
        Searching => "a Search's Casualty dr (A12.154)",
        Sniper => "a Sniper attack (A14.3)",
        Rally => "a Rally attempt's Fate (A10.64)",
        Panzerfaust => "a PF or ATR Check (C13.31)",
        FailureToRout => "Failure to Rout (A10.5)",
        _ => cause,
    };

    /// <summary>
    /// Resolves a morale check: the effects on its subjects and the Location's other units, in that order, as a fire attack's target effects; or Indeterminate
    /// with the first missing roll or fact, asked for one at a time as an attack's are.
    /// </summary>
    public static FireResolution Resolve(MoraleCheckFacts facts, ScenarioA1FireReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        var missing = new List<string>();
        if (facts.Check is not (MoraleCheckFacts.Nmc or MoraleCheckFacts.Ptc or MoraleCheckFacts.None or "1MC" or "2MC" or "3MC" or "4MC"))
        {
            missing.Add("asl.a1.morale.fact-missing:check");
        }

        if (facts.Subjects is null)
        {
            missing.Add("asl.a1.morale.fact-missing:subjects");
        }

        if (facts.Rolls is null)
        {
            missing.Add("asl.a1.morale.fact-missing:rolls");
        }

        var units = (facts.Subjects ?? []).Concat(facts.Others ?? []).Concat(facts.LostLeaders ?? []).Concat(facts.BrokenLeaders ?? []).ToArray();
        foreach (var unit in units.Where(unit => unit.UnitId is null || unit.Dummy == true || unit.DefinitionId is not { } id || !reference.Definitions.ContainsKey(id)))
        {
            missing.Add("asl.a1.morale.unit-outside:" + (unit.UnitId ?? "?"));
        }

        if (units.Select(unit => unit.UnitId).Distinct(StringComparer.Ordinal).Count() != units.Length)
        {
            missing.Add("asl.a1.morale.unit-named-twice");
        }

        foreach (var leader in (facts.LostLeaders ?? []).Concat(facts.BrokenLeaders ?? []).Where(unit => unit.DefinitionId is { } id
            && reference.Definitions.TryGetValue(id, out var definition) && !definition.IsLeader))
        {
            missing.Add("asl.a1.morale.not-a-leader:" + leader.UnitId);
        }

        return missing.Count != 0 ? new FireResolution(FireResolution.Indeterminate, missing, null, [], [], null, [])
            : ScenarioA1FireCalculator.ResolveCheck(facts, reference);
    }

    /// <summary>
    /// A10.53, A15.1 (pp. 67, 83; pass 70, ruling R70.4): the Interdiction NMC is "subject to ... everything that normally entails", so its Original DR of 2
    /// calls for a Heat of Battle DR, made by the member after the Interdiction's record.
    /// </summary>
    public static bool InterdictionHeatOfBattleDue(int originalDr) => originalDr == 2;

    /// <summary>
    /// Ruling R70.5: a Heat of Battle that rallies a routing unit (Battle Hardening, a heroic leader) or makes it berserk ends its rout in the Location where it
    /// was interdicted; so does a surrender to a captor, which waits for the captor's choice. A unit still broken routs on.
    /// </summary>
    public static bool EndsRout(FireUnitEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        return !effect.Eliminated && (!effect.Broken || effect.Berserk == true || effect.Events.Contains("surrendered", StringComparer.Ordinal));
    }

    /// <summary>
    /// Ruling R70.5, after a LLMC the Interdiction's loss of a leader called for: the routing unit, as the check left it, routs no further when it is no longer
    /// active, no longer broken, berserk, or waiting on a captor's choice of its surrender (A15.5).
    /// </summary>
    public static bool EndsRoutAfterLeaderLoss(bool active, bool broken, bool berserk, bool surrenderPending) => !active || !broken || berserk || surrenderPending;

    /// <summary>
    /// The units of a lost leader's Location that take the LLMC or LLTC (A10.2, p. 65; pass 70, rulings R70.3, R70.44): the other active units of his side
    /// there, Passengers and Riders included, but a vehicle, a Dummy, a prisoner, and a unit outside the catalog; none when the side has no ELR declared, as
    /// Fire refuses such a game (A19.13; a backlog row). A rout is not Defensive First Fire, so A10.2's exception for a leader lost in DFF does not reach a
    /// bystander (ruling R70.44). In the order given.
    /// </summary>
    public static IReadOnlyList<string> LeaderLossSubjects(IReadOnlyList<LeaderLossUnitFacts> units, int? sideElr)
    {
        ArgumentNullException.ThrowIfNull(units);
        return sideElr is null ? [] : [.. units.Where(unit => unit.Active && unit.OfSide && !unit.Lost && !unit.Vehicle && !unit.Dummy && !unit.Captured && unit.InCatalog)
            .Select(unit => unit.Id)];
    }

    /// <summary>
    /// The proposal's line for a leader's loss before the phase ends (A10.2; pass 70, ruling R70.3): it may call for a LLMC, decided and rolled at the commit,
    /// so the line tells nothing of the units under "?" there (A12.14; the referee's finding at the reviews).
    /// </summary>
    public static string LeaderLossDueText(IReadOnlyList<string> leaders)
    {
        ArgumentNullException.ThrowIfNull(leaders);
        return $"play.leader-loss: the loss of {string.Join(", ", leaders)} may call for a LLMC of the units in {(leaders.Count == 1 ? "his Location" : "their Locations")}, "
            + "rolled at the commit if any is due (A10.2)";
    }

    /// <summary>Whether a resolved check holds any check at all: one with none (no LLMC owed, a Heat of Battle not reached) leaves nothing to record.</summary>
    public static bool AnyCheck(FireResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return resolution.Effects.Any(effect => effect.Checks.Count > 0 || effect.HeatOfBattle is not null) || resolution.CompanionEffects is { Count: > 0 };
    }

    /// <summary>
    /// Whether the check's record is the units' side's own business (A12.14; pass 70): a unit that took a check keeps its "?", so the other side may not
    /// learn of it; the record and its effects are withheld from it.
    /// </summary>
    public static bool Withheld(FireResolution resolution, IReadOnlyList<FireTarget> units)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(units);
        return resolution.Effects.Any(effect => effect.Checks.Count > 0 && !effect.ConcealmentLost && !effect.Eliminated
            && units.FirstOrDefault(unit => unit.UnitId == effect.UnitId) is { } unit && (unit.Concealed == true || unit.Hidden == true));
    }

    /// <summary>
    /// The other units of a check that its record leaves unnamed (A12.14, A12.3; the referee's finding at the reviews of pass 70): a concealed or hidden
    /// unit that took no check, no Heat of Battle, and kept its "?", so a record both sides read says nothing of it. The caller names them only when the
    /// check resolves the same without them (a leader among them may have lent his leadership); otherwise it withholds the record.
    /// </summary>
    public static IReadOnlySet<string> Unnamed(FireResolution resolution, IReadOnlyList<FireTarget> others)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        ArgumentNullException.ThrowIfNull(others);
        bool Untouched(FireUnitEffect? effect) => effect is null || (effect.Checks.Count == 0 && effect.HeatOfBattle is null && !effect.ConcealmentLost && !effect.Eliminated);
        return others.Where(unit => unit.UnitId is not null && (unit.Concealed == true || unit.Hidden == true)
                && Untouched(resolution.Effects.FirstOrDefault(effect => effect.UnitId == unit.UnitId)))
            .Select(unit => unit.UnitId!).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The result of a Bail Out NMC in the words of <c>rider-bailed-out</c> (D6.24; pass 70, ruling R70.9), from the member's check of the unit: an Original 12
    /// on a broken unit eliminates it, a failure Casualty Reduces it; on an unbroken unit an Original 12 Reduces it, a failure by more than its ELR Replaces it
    /// (<paramref name="replaced"/>: the member Replaced it), another failure breaks it, the highest passing DR pins it. An Unarmed unit that fails is Casualty
    /// Reduced, and one that passes is left as it was, as the member resolves it (ruling R70.47; A20.54, p. 88).
    /// </summary>
    public static string BailOutResult(FireCheck check, bool broken, bool replaced, bool unarmed = false)
    {
        ArgumentNullException.ThrowIfNull(check);
        return unarmed ? check.Passed ? ScenarioA1RiderRules.Passed : ScenarioA1RiderRules.Reduced
            : broken ? check.OriginalDr == 12 ? ScenarioA1RiderRules.Eliminated : check.Passed ? ScenarioA1RiderRules.Passed : ScenarioA1RiderRules.Reduced
            : check.OriginalDr == 12 ? ScenarioA1RiderRules.Reduced
            : !check.Passed ? replaced ? ScenarioA1RiderRules.Replaced : ScenarioA1RiderRules.Broken
            : check.FinalDr == check.MoraleLevel ? ScenarioA1RiderRules.Pinned
            : ScenarioA1RiderRules.Passed;
    }

    /// <summary>A15.2/.21 (PDF p. 83): preserve the shared MC's Hero wound or elimination, and never turn his passing MC into a Pin.</summary>
    public static string BailOutResult(FireCheck check, bool broken, bool replaced, bool unarmed, bool heroic) =>
        heroic ? check.Consequence : BailOutResult(check, broken, replaced, unarmed);
}
