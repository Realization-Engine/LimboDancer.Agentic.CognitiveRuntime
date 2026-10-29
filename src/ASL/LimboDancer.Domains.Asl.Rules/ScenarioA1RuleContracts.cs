using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Marks a static method with no parameters that returns a <see cref="RuleContract"/>, so the registry finds it (the C9.3 claim, ruling RC93.2): each
/// claim writes its contract in a file of its own and changes no shared file to register it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RuleContractAttribute : Attribute;

/// <summary>
/// The registry of wiring contracts (the Rules-first plan, sections 5 and 8; the proving slice, first task 4, 2026-10-28) and their JSON entry point.
/// Each contract only reads its calculator: no calculator changes for its contract, and the order of its checks and its texts stay its own.
/// </summary>
public static class ScenarioA1RuleContracts
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Lazy<ScenarioA1RallyReference> RallyReference = new(() => new ScenarioA1RallyPackage().Reference);

    /// <summary>The phases, as a contract's phase fact names them.</summary>
    internal static readonly string[] Phases = ["RPh", "PFPh", "MPh", "DFPh", "AFPh", "RtPh", "APh", "CCPh"];
    private static readonly string[] RallyingSides = ["phasing", "non-phasing"];
    private static readonly string[] Barrels = [.. Enum.GetNames<TurnAmmoBarrel>()];
    private static readonly string[] Uses = [.. Enum.GetNames<TurnAmmoUse>()];

    /// <summary>Every contract, each a method marked <see cref="RuleContractAttribute"/> in this assembly, by id, which is the order of their rules.</summary>
    public static IReadOnlyList<RuleContract> All
    {
        get;
    } = [.. typeof(ScenarioA1RuleContracts).Assembly.GetTypes()
        .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        .Where(method => method.IsDefined(typeof(RuleContractAttribute), false) && method.GetParameters().Length == 0 && method.ReturnType == typeof(RuleContract))
        .Select(method => (RuleContract)method.Invoke(null, null)!)
        .OrderBy(contract => contract.Id, StringComparer.Ordinal)];

    /// <summary>The contract with an id, or null.</summary>
    public static RuleContract? Find(string id) => All.FirstOrDefault(contract => string.Equals(contract.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// The JSON entry point (the plan's section 5): the facts as one JSON object, name to value, null for a fact unknown; the verdict as JSON. A fact
    /// that is missing, unknown where it may not be, or not of its type gives the outcome "not-evaluated", as does a calculator that refuses its
    /// input; nothing throws for the facts.
    /// </summary>
    public static string Evaluate(string contractId, string factsJson)
    {
        ArgumentNullException.ThrowIfNull(contractId);
        ArgumentNullException.ThrowIfNull(factsJson);
        var contract = Find(contractId) ?? throw new ArgumentException($"No contract '{contractId}'.", nameof(contractId));
        JsonObject? facts;
        try
        {
            facts = JsonNode.Parse(factsJson) as JsonObject;
        }
        catch (JsonException)
        {
            facts = null;
        }

        var verdict = facts is null ? NotEvaluated(contract, "the facts are not a JSON object") : Evaluate(contract, facts);
        return JsonSerializer.Serialize(verdict, Json);
    }

    /// <summary>The verdict of a contract on facts given as a JSON object.</summary>
    public static ContractVerdict Evaluate(RuleContract contract, JsonObject facts)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(facts);
        foreach (var name in facts.Select(pair => pair.Key))
        {
            if (!contract.Facts.Any(fact => fact.Name == name))
            {
                return NotEvaluated(contract, $"'{name}' is not a fact of this contract");
            }
        }

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var fact in contract.Facts)
        {
            var node = facts.TryGetPropertyValue(fact.Name, out var given) ? given : null;
            if (node is null)
            {
                if (!fact.MayBeUnknown)
                {
                    return NotEvaluated(contract, $"the fact '{fact.Name}' is missing or not known to this viewer");
                }

                values[fact.Name] = null;
                continue;
            }

            if (Read(fact, node) is not { } value)
            {
                return NotEvaluated(contract, $"the fact '{fact.Name}' is not a {fact.Type.ToString().ToLowerInvariant()}"
                    + (fact.Choices is { } choices ? $" of {string.Join(", ", choices)}" : string.Empty));
            }

            values[fact.Name] = value;
        }

        try
        {
            return contract.Decide(new ContractFacts(values));
        }
        catch (Exception error) when (error is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return NotEvaluated(contract, $"the calculator refuses these facts: {error.Message}");
        }
    }

    private static object? Read(ContractFact fact, JsonNode node)
    {
        if (node is not JsonValue value)
        {
            return null;
        }

        return fact.Type switch
        {
            ContractFactType.Flag => value.TryGetValue<bool>(out var flag) ? flag : null,
            ContractFactType.Number => value.TryGetValue<decimal>(out var number) ? number : null,
            ContractFactType.Choice => value.TryGetValue<string>(out var choice) && fact.Choices is { } choices && choices.Contains(choice) ? choice : null,
            _ => value.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) ? text : null,
        };
    }

    private static ContractVerdict NotEvaluated(RuleContract contract, string why) =>
        new(ContractVerdict.NotEvaluated, [new ContractReason(why, contract.Rules[0].Rule, contract.Rules[0].Page)], []);

    /// <summary>A flag fact, as the contracts declare most of theirs.</summary>
    internal static ContractFact Flag(string name, string description, bool mayBeUnknown = false, ContractDisclosure disclosure = ContractDisclosure.All) =>
        new(name, ContractFactType.Flag, mayBeUnknown, disclosure, description);

    /// <summary>
    /// A10.6 Rally (p. 68), as <see cref="ScenarioA1RallyCalculator.Precheck"/> decides whether an attempt may be made, and its arithmetic the Rally DR's
    /// target (the proving slice: a rule built and played, every fact gathered by Play). The verdict reads a probe Rally DR of 6 that is never drawn.
    /// </summary>
    [RuleContract]
    private static RuleContract Rally() => new("a10.6-rally", "Rally", [new("A10.6", 68), new("A10.61", 68), new("A10.62", 68)],
        [
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = Phases },
            new("rallyingSide", ContractFactType.Choice, false, ContractDisclosure.All, "Whether the rallying side is the phasing side") { Choices = RallyingSides },
            new("locationId", ContractFactType.Hex, false, ContractDisclosure.All, "The broken unit's Location"),
            new("terrain", ContractFactType.Choice, false, ContractDisclosure.All, "The Location's terrain, for the A10.61 terrain DRM") { Choices = [.. ScenarioA1RallyCalculator.TerrainDrm.Keys] },
            new("unitId", ContractFactType.Unit, false, ContractDisclosure.All, "The broken unit"),
            new("unitDefinition", ContractFactType.Text, false, ContractDisclosure.Owner, "Its definition in the catalog") { Concerns = "unitId" },
            Flag("unitBroken", "It is broken"),
            Flag("unitDisrupted", "It is Disrupted"),
            Flag("unitWounded", "It is wounded"),
            Flag("unitDesperationMorale", "It is under DM (A10.62)"),
            Flag("unitConcealed", "It is concealed"),
            Flag("unitAttemptedThisPlayerTurn", "It has attempted to rally this Player Turn"),
            Flag("unitOtherActionThisPhase", "It has taken another RPh action this phase"),
            Flag("unitFanatic", "It is Fanatic", mayBeUnknown: true),
            new("leaderId", ContractFactType.Unit, true, ContractDisclosure.All, "The leader who directs the attempt, if any"),
            new("leaderDefinition", ContractFactType.Text, true, ContractDisclosure.Owner, "The leader's definition in the catalog") { Concerns = "leaderId" },
            Flag("leaderBroken", "The leader is broken", mayBeUnknown: true),
            Flag("leaderWounded", "The leader is wounded", mayBeUnknown: true),
            Flag("leaderConcealed", "The leader is concealed", mayBeUnknown: true),
            Flag("goodOrderLeaderInLocation", "A Good Order leader of its side is in the Location"),
            Flag("brokenLeaderInLocation", "A broken leader of its side is in the Location"),
            Flag("firstMmcRallyOfOwnPlayerTurn", "No MMC of its side has rallied this Player Turn (A10.63 Self-Rally)"),
            Flag("enemyGoodOrderInLosWithin16", "A Good Order enemy unit within 16 hexes has a LOS to it (A12.141)", mayBeUnknown: true),
            Flag("knownEnemyInLos", "A Known enemy unit has a LOS to it (A15)", mayBeUnknown: true),
            new("captors", ContractFactType.Text, true, ContractDisclosure.All, "The prisoners it guards (A20.4), comma-separated, or \"none\""),
        ],
        ["may-attempt", "refused", ContractVerdict.NotEvaluated],
        [
            new("dice-rolled", "the Rally DR, kind \"rally\""),
            new("rally-attempted", "the unit, the leader, the dice, the DRM, the Final DR, the Morale Level, and whether it rallied"),
            new("conditions-changed", "the unit's broken and DM conditions; a \"?\" lost in view (A12.141)"),
        ],
        new("RPh", "1.23B"))
    {
        Decide = RallyVerdict,
    };

    private static ContractVerdict RallyVerdict(ContractFacts facts)
    {
        var reference = RallyReference.Value;
        var leader = facts.Text("leaderId") is { } leaderId
            ? new RallyLeader(leaderId, facts.Text("leaderDefinition"), facts.Text("locationId"), facts.Flag("leaderBroken"), facts.Flag("leaderWounded"), facts.Flag("leaderConcealed"))
            : null;
        var attempt = new RallyAttempt(facts.Text("phase"), facts.Text("rallyingSide"),
            new RallyUnit(facts.Text("unitId"), facts.Text("unitDefinition"), facts.Text("locationId"), facts.Flag("unitBroken"), facts.Flag("unitDisrupted"),
                facts.Flag("unitWounded"), facts.Flag("unitDesperationMorale"), facts.Flag("unitConcealed"), facts.Flag("unitAttemptedThisPlayerTurn"),
                facts.Flag("unitOtherActionThisPhase"))
            {
                Fanatic = facts.Flag("unitFanatic"),
            },
            leader, facts.Text("locationId"), facts.Text("terrain"), facts.Flag("goodOrderLeaderInLocation"), facts.Flag("brokenLeaderInLocation"),
            facts.Flag("firstMmcRallyOfOwnPlayerTurn"), facts.Flag("enemyGoodOrderInLosWithin16"), null)
        {
            KnownEnemyInLos = facts.Flag("knownEnemyInLos"),
            Captors = facts.Text("captors") switch
            {
                null => null,
                "none" => [],
                var list => [.. list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)],
            },
        };

        var refusals = ScenarioA1RallyCalculator.Precheck(attempt, reference);
        if (refusals.Count != 0)
        {
            var outcome = refusals.Any(reason => reason.Contains("fact-missing", StringComparison.Ordinal)) ? ContractVerdict.NotEvaluated : "refused";
            return new ContractVerdict(outcome, [.. refusals.Select(reason => new ContractReason(reason, "A10.6", 68))], []);
        }

        var probe = ScenarioA1RallyCalculator.Resolve(attempt with
        {
            Rolls = new RallyRolls([3, 3], null),
        }, reference);
        ContractRoll roll;
        if (probe.Arithmetic is { } arithmetic)
        {
            var drm = arithmetic.FinalDr - arithmetic.OriginalDr;
            var named = arithmetic.Drm.Count == 0 ? "none" : string.Join(", ", arithmetic.Drm.Select(item =>
                $"{item.Name} {item.Value.ToString("+0;-0;0", CultureInfo.InvariantCulture)} ({item.Rule})"));
            roll = new ContractRoll("rally", arithmetic.MoraleLevel - drm,
                $"Rally DR: it rallies on a Final DR of {arithmetic.MoraleLevel} or less, its Morale Level; DRM {drm.ToString("+0;-0;0", CultureInfo.InvariantCulture)}: {named}");
        }
        else
        {
            roll = new ContractRoll("rally", null, "Rally DR: its target depends on dice the attempt itself rolls");
        }

        return new ContractVerdict("may-attempt", [new ContractReason("The attempt may be made; nothing bars it before the Rally DR", "A10.6", 68)], [roll]);
    }

    /// <summary>
    /// E10.1 to E10.2 eligibility (p. 244), as <see cref="TurnAmmoEligibilityRules.EvaluateRecipients"/> decides it for one recipient (the proving slice: a
    /// calculator with no wiring, its facts entered by hand). The supply's facts are its owner's.
    /// </summary>
    [RuleContract]
    private static RuleContract AmmoEligibility() => new("e10.1-eligibility", "Ammo Vehicle eligibility", [new("E10.1", 244), new("E10.11", 244), new("E10.2", 244)],
        [
            new("supplyId", ContractFactType.Unit, false, ContractDisclosure.All, "The Ammo Vehicle"),
            new("supplyOwner", ContractFactType.Text, false, ContractDisclosure.All, "Its side"),
            Flag("supplyActive", "It is active on the map"),
            Flag("supplyAuthorized", "Its ammunition is authorized by SSR or DYO (E10.1)", mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with { Concerns = "supplyId" },
            Flag("supplyAvailable", "It still carries ammunition", mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with { Concerns = "supplyId" },
            new("supplyCaliberMm", ContractFactType.Number, true, ContractDisclosure.Owner, "The caliber of its ammunition, in mm") { Concerns = "supplyId" },
            new("supplyBarrel", ContractFactType.Choice, false, ContractDisclosure.Owner, "The barrel its ammunition fits") { Choices = Barrels, Concerns = "supplyId" },
            new("recipientId", ContractFactType.Unit, false, ContractDisclosure.All, "The recipient"),
            new("recipientOwner", ContractFactType.Text, false, ContractDisclosure.All, "Its side"),
            Flag("recipientActive", "It is active on the map"),
            Flag("recipientIsVehicle", "It is a vehicle"),
            Flag("recipientHasLowAmmoBNumber", "Its MA has a B# that Low Ammo lowers"),
            Flag("recipientLowAmmo", "It is under Low Ammo", mayBeUnknown: true),
            Flag("recipientCrewExposed", "Its crew is exposed (CE)", mayBeUnknown: true),
            Flag("recipientSameOrAccessibleLocation", "It is in the same or an Accessible Location (E10.2)", mayBeUnknown: true),
            new("recipientCaliberMm", ContractFactType.Number, true, ContractDisclosure.All, "The caliber of its MA, in mm"),
            new("recipientBarrel", ContractFactType.Choice, false, ContractDisclosure.All, "Its MA's barrel") { Choices = Barrels },
            Flag("completeRecipientInventory", "Every recipient of this use is named", mayBeUnknown: true),
            new("use", ContractFactType.Choice, false, ContractDisclosure.All, "The use: a fire benefit or a replenishment") { Choices = Uses },
            Flag("notBoundingFire", "The recipient does not use Bounding Fire this phase", mayBeUnknown: true),
        ],
        ["eligible", "ineligible", ContractVerdict.NotEvaluated],
        [new("none yet", "E10 is not wired: track 3 adds the assignment and its events")],
        new("RPh", "8.43B"))
    {
        Decide = AmmoVerdict,
    };

    private static ContractVerdict AmmoVerdict(ContractFacts facts)
    {
        var supply = new TurnAmmoSupplyFacts(facts.Text("supplyId"), facts.Text("supplyOwner"), facts.Flag("supplyActive"), facts.Flag("supplyAuthorized"),
            facts.Flag("supplyAvailable"), new TurnAmmoArmament(facts.Number("supplyCaliberMm"), Enum.Parse<TurnAmmoBarrel>(facts.Text("supplyBarrel")!)));
        var recipient = new TurnAmmoRecipientFacts(facts.Text("recipientId"), facts.Text("recipientOwner"), facts.Flag("recipientActive"),
            facts.Flag("recipientIsVehicle"), facts.Flag("recipientHasLowAmmoBNumber"), facts.Flag("recipientLowAmmo"), facts.Flag("recipientCrewExposed"),
            facts.Flag("recipientSameOrAccessibleLocation"), new TurnAmmoArmament(facts.Number("recipientCaliberMm"), Enum.Parse<TurnAmmoBarrel>(facts.Text("recipientBarrel")!)));
        var verdict = TurnAmmoEligibilityRules.EvaluateRecipients(supply, [recipient], facts.Flag("completeRecipientInventory"),
            Enum.Parse<TurnAmmoUse>(facts.Text("use")!), facts.Flag("notBoundingFire"));
        var outcome = verdict.State switch
        {
            TurnAmmoEligibility.Eligible => "eligible",
            TurnAmmoEligibility.Ineligible => "ineligible",
            _ => ContractVerdict.NotEvaluated,
        };
        return new ContractVerdict(outcome, [new ContractReason(verdict.Reason, "E10.1", 244)], []);
    }

    /// <summary>
    /// A12.14 (p. 77) and E1.31 (p. 223), as <see cref="ScenarioA1AdvanceCalculator.LosesConcealment"/> decides whether a concealed unit advancing into a
    /// Location loses its "?" (the proving slice: what a viewer may know). Whether a Good Order enemy within 16 hexes sees it is the opponent's fact:
    /// that side knows what its own units see, and the owner does not, since an enemy may be hidden. What is under the "?" is the owner's, and the
    /// calculator does not need it. The LOS is read only when night and terrain leave the question open.
    /// </summary>
    [RuleContract]
    private static RuleContract AdvanceConcealment() => new("a12.14-advance-concealment", "Concealment lost on advancing",
        [new("A12.14", 77), new("E1.31", 223)],
        [
            new("advancerId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The concealed unit that advances") { Concerns = "advancerId" },
            new("to", ContractFactType.Hex, false, ContractDisclosure.All, "The Location it advances into"),
            Flag("night", "The scenario is at night"),
            new("terrain", ContractFactType.Text, false, ContractDisclosure.All, "The terrain of the Location it advances into"),
            Flag("enemyGoodOrderInLosWithin16", "A Good Order enemy unit within 16 hexes has a LOS to that Location", mayBeUnknown: true,
                disclosure: ContractDisclosure.Opponent) with { Concerns = "advancerId" },
        ],
        ["loses-concealment", "keeps-concealment", ContractVerdict.NotEvaluated],
        [new("conditions-changed", "the advancer's \"?\" removed (Concealed false), one event for each concealed advancer")],
        new("APh", "7.21A"))
    {
        Decide = facts =>
        {
            // The LOS is read only when night and terrain leave the question open; an unknown LOS then leaves the verdict unknown.
            var unknown = false;
            var loses = ScenarioA1AdvanceCalculator.LosesConcealment(facts.Flag("night")!.Value, facts.Text("terrain")!, () =>
            {
                if (facts.Flag("enemyGoodOrderInLosWithin16") is { } seen)
                {
                    return seen;
                }

                unknown = true;
                return false;
            });
            if (unknown)
            {
                return new ContractVerdict(ContractVerdict.NotEvaluated,
                    [new ContractReason("the fact 'enemyGoodOrderInLosWithin16' is needed here and is not known to this viewer", "A12.14", 77)], []);
            }

            return loses
                ? new ContractVerdict("loses-concealment", [new ContractReason("It enters Open Ground by day in the LOS of a Good Order enemy within 16 hexes", "A12.14", 77)], [])
                : new ContractVerdict("keeps-concealment", [new ContractReason("Night, terrain other than Open Ground, or no Good Order enemy within 16 hexes sees it", "A12.14", 77)], []);
        },
    };
}
