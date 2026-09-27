using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A Heat of Battle DR and what it did (A15.1 to A15.5, pp. 83 and 84; unit steps 28 and 30): the Final DR with its DRM, the
/// hero it created or the heroic leader it made, the unit's Battle Hardened definition, Fanaticism, Berserk, and Surrender
/// with the units it may surrender to.
/// </summary>
public sealed record HeatOfBattleOutcome(IReadOnlyList<int> Dice, int OriginalDr, IReadOnlyList<FireModifier> Drm, int FinalDr, string Result)
{
    /// <summary>The hero an MMC created (A15.21); null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeroDefinitionId { get; init; }

    /// <summary>Whether a leader became heroic, rallying if broken (A15.21).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic { get; init; }

    /// <summary>The unit of next higher quality the unit is exchanged for (A15.3); null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HardenedDefinitionId { get; init; }

    /// <summary>Whether an already elite MMC or best possible leader became Fanatic (A15.3, A10.8).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>
    /// Whether the result Battle Hardens the unit (A15.3): it is exchanged for an unbroken, unpinned unit, even when it has no
    /// better class and only becomes Fanatic, or already is (ruling R28.7).
    /// </summary>
    [JsonIgnore]
    public bool Hardening => Result is BattleHardening or HeroAndBattleHardening;

    /// <summary>Whether a Berserk result became Battle Hardening because no Known enemy unit was in the unit's LOS (A15.44).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NoKnownEnemyInLos { get; init; }

    /// <summary>
    /// For a Surrender (A15.5): the ADJACENT Known Good Order armed enemy Infantry units it surrenders to, the captor's choice
    /// among them; empty when there are none, and the unit is only broken and Disrupted.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors { get; init; }

    public const string HeroCreation = "hero-creation";
    public const string HeroAndBattleHardening = "hero-creation-and-battle-hardening";
    public const string BattleHardening = "battle-hardening";
    public const string Berserk = "berserk";
    public const string Surrender = "surrender";
}

/// <summary>
/// Heat of Battle for the Fire and Rally packages (A15.1 to A15.3, p. 83; A10.8, p. 69): who is subject to it, its DRM,
/// and its results. A pure function of the unit, its status, and one recorded DR.
/// </summary>
public static class ScenarioA1HeatOfBattle
{
    // A15.1: the nationality DRM of the Heat of Battle table.
    private static readonly Dictionary<string, int> NationalityDrm = new(StringComparer.Ordinal)
    {
        ["american"] = 0,
        ["british"] = -1,
        ["finnish"] = -1,
        ["french"] = 1,
        ["german"] = 0,
        ["italian"] = 3,
        ["japanese"] = 4,
        ["russian"] = 2,
    };

    /// <summary>
    /// Whether a unit is subject to Heat of Battle (A15.1): armed Personnel other than heroes, heroic leaders, and units
    /// already berserk. Crews, Cavalry, and the other exempt units are not in the catalog.
    /// </summary>
    public static bool Subject(FireDefinition definition, bool heroic, bool berserk = false) =>
        (definition.IsMmc || definition.IsLeader) && !heroic && !berserk;

    /// <summary>
    /// Whether the unit's class needs the Inexperienced fact for the +1 DRM (A15.1, A19.2): a Green MMC is Inexperienced
    /// unless stacked with an unbroken leader, which only the caller knows; a Conscript always is (A19.3).
    /// </summary>
    public static bool NeedsInexperience(FireDefinition definition) => definition.Class == "green";

    /// <summary>
    /// The DR's result, or the reason it is undecided. <paramref name="broken"/> is the unit's status when the Original 2
    /// was rolled (A15.1: the +1 applies even if that 2 rallied it). <paramref name="knownEnemyInLos"/> and
    /// <paramref name="captors"/> are the caller's reads of the map, needed only for a Berserk (A15.44) or Surrender (A15.5)
    /// result: whether a Known enemy unit is in the unit's LOS, and the ADJACENT Known Good Order armed enemy Infantry.
    /// </summary>
    public static (HeatOfBattleOutcome? Outcome, string? Undecided) Resolve(FireDefinition unit, bool broken, bool? inexperienced, bool fanatic,
        IReadOnlyList<int> dice, IReadOnlyDictionary<string, FireDefinition> definitions, bool? knownEnemyInLos = null, IReadOnlyList<string>? captors = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(definitions);
        if (!NationalityDrm.TryGetValue(unit.Nationality, out var nationality))
        {
            return (null, "asl.a1.hob.nationality-unreviewed:" + unit.Id);
        }

        if (NeedsInexperience(unit) && inexperienced is null)
        {
            return (null, "asl.a1.hob.inexperience-undecided:" + unit.Id);
        }

        inexperienced = unit.Class == "conscript" || (NeedsInexperience(unit) && inexperienced == true);

        var drm = new List<FireModifier>();
        if (unit.Class == "elite")
        {
            drm.Add(new FireModifier("elite", -1m, "A15.1"));
        }

        if (ScenarioA1FireReference.IsNkvd(unit.Id))
        {
            drm.Add(new FireModifier("nkvd", -1m, "A25.25"));
        }

        if (nationality != 0)
        {
            drm.Add(new FireModifier("nationality:" + unit.Nationality, nationality, "A15.1"));
        }

        if (broken)
        {
            drm.Add(new FireModifier("broken", 1m, "A15.1"));
        }

        if (inexperienced == true)
        {
            drm.Add(new FireModifier("inexperienced", 1m, "A15.1"));
        }

        var original = dice[0] + dice[1];
        var final = original + (int)drm.Sum(item => item.Value);

        // A15.1: 6 or less creates a hero, 5 to 8 Battle Hardens, so 5 and 6 do both; 9 to 11 is Berserk, 12 or more Surrender,
        // which a Fanatic unit treats as Berserk (the table's note, A15.5). A Berserk result with no Known enemy unit in the
        // unit's LOS is Battle Hardening instead (A15.44).
        var hero = final <= 6;
        var hardening = final is >= 5 and <= 8;
        var berserk = !hero && !hardening && (final <= 11 || fanatic);
        if (berserk)
        {
            if (knownEnemyInLos is null)
            {
                return (null, "asl.a1.hob.known-enemy-in-los-undecided:" + unit.Id);
            }

            hardening = knownEnemyInLos == false;
        }

        var result = hero && hardening ? HeatOfBattleOutcome.HeroAndBattleHardening
            : hero ? HeatOfBattleOutcome.HeroCreation
            : hardening ? HeatOfBattleOutcome.BattleHardening
            : berserk ? HeatOfBattleOutcome.Berserk
            : HeatOfBattleOutcome.Surrender;
        var outcome = new HeatOfBattleOutcome(dice.ToArray(), original, drm, final, result);
        if (berserk && hardening)
        {
            outcome = outcome with { NoKnownEnemyInLos = true };
        }

        if (result == HeatOfBattleOutcome.Surrender)
        {
            if (captors is null)
            {
                return (null, "asl.a1.hob.captors-undecided:" + unit.Id);
            }

            outcome = outcome with { Captors = [.. captors] };
        }
        if (hero)
        {
            if (unit.IsLeader)
            {
                // A15.21: a leader who becomes heroic keeps his definition and leadership and rallies.
                outcome = outcome with { Heroic = true };
            }
            else if (HeroOf(unit, definitions) is { } heroId)
            {
                outcome = outcome with { HeroDefinitionId = heroId };
            }
            else
            {
                return (null, "asl.a1.hob.hero-counter-missing:" + unit.Nationality);
            }
        }

        if (hardening)
        {
            // A15.3: the next higher quality; an elite MMC or the best possible leader becomes Fanatic, once.
            if (ScenarioA1FireReference.HardenedOf(unit.Id) is { } next)
            {
                if (!definitions.ContainsKey(next))
                {
                    return (null, "asl.a1.hob.hardening-counter-missing:" + next);
                }

                outcome = outcome with { HardenedDefinitionId = next };
            }
            else if (ScenarioA1FireReference.IsHighestQuality(unit.Id))
            {
                if (!fanatic)
                {
                    outcome = outcome with { Fanatic = true };
                }
            }
            else
            {
                return (null, "asl.a1.hob.hardening-unreviewed:" + unit.Id);
            }
        }

        return (outcome, null);
    }

    /// <summary>The hero counter of the unit's nationality (A15.2).</summary>
    public static string? HeroOf(FireDefinition unit, IReadOnlyDictionary<string, FireDefinition> definitions) =>
        definitions.Values.Where(item => item.Kind == "asl:hero" && item.Nationality == unit.Nationality)
            .Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();
}
