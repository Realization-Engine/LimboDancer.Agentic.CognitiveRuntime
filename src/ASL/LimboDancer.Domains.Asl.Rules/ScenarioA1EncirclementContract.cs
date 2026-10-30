namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of A7.7, A7.71, and A7.72 (pp. 57 and 58; the D5.34 claim's design, section 1.4): whether an attack, with the phase's earlier
/// attacks on the Location, seals its Encirclement, and whether a unit is Encircled and what that does, as <see cref="ScenarioA1EncirclementCalculator"/>
/// decides them. The attacks, their LOS entries, the phase, the Encirclement marker, and the Melee, berserk, and heroic counters are public; a hidden
/// pillbox and a concealed unit's kind are their owner's, so the opponent's view leaves them unknown and the verdict not evaluated. A7.72 counts only
/// unconcealed enemy units, so its cut-off path is public too.
/// </summary>
internal static class ScenarioA1EncirclementContract
{
    private const string Target = "targetId";
    private const string Unit = "unitId";
    private const string Seals = "seals";
    private const string NoSeal = "no-seal";
    private const string Encircled = "encircled";
    private const string NotEncircled = "not-encircled";

    private static readonly string[] Patterns = ["opposite-hexspines", "three-vertices", "three-hexsides", "above-and-below", "none"];

    private static readonly string[] Kinds = ["infantry", "hero", "vehicle", "vulnerable-prc", "prc"];

    // One set of LOS entries per pattern, as ScenarioA1FireMapRules.PerimeterPosition numbers them (hexside i is 2i + 1, the hexspine after it 2i + 2).
    private static readonly int[] OppositeHexspines = [2, 8];
    private static readonly int[] ThreeVertices = [1, 7];
    private static readonly int[] ThreeHexsides = [1, 5, 9];
    private static readonly int[] AboveAndBelow = [ScenarioA1EncirclementCalculator.FromAbove, ScenarioA1EncirclementCalculator.FromBelow];
    private static readonly int[] NoEntries = [];

    [RuleContract]
    private static RuleContract Seal() => new("a7.7-encirclement-seal", "Encirclement seal", [new("A7.7", 57), new("A7.71", 58), new("A10.532", 67),
        new("B30.32", 150)],
        [
            new(Target, ContractFactType.Unit, false, ContractDisclosure.All, "A unit in the target Location") { Concerns = Target },
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            ScenarioA1RuleContracts.Flag("consecutive", "The attacks were made consecutively, with no fire at another target between them"),
            new("countingUnits", ContractFactType.Number, false, ContractDisclosure.All,
                "The units of valid fire in the sequence, this attack's included: inherent FP, a SW, an ordnance hit, or vehicular armament at Normal Range (at most 16 hexes)"),
            new("pattern", ContractFactType.Choice, false, ContractDisclosure.All,
                "Where their LOS enter the Location: opposite hexspines, three vertices between them both ways, three non-contiguous hexsides, from above and below, or none of these")
            {
                Choices = Patterns
            },
            ScenarioA1RuleContracts.Flag("inPillbox", "The target is in a pillbox (B30.32)", disclosure: ContractDisclosure.Owner) with
            {
                Concerns = Target
            },
        ],
        [Seals, NoSeal, ContractVerdict.NotEvaluated],
        [new("encirclement-placed", "the Encirclement counter on the target Location, for the target's side")],
        new("PFPh", "2.22A"))
    {
        Decide = SealVerdict,
    };

    [RuleContract]
    private static RuleContract EncircledUnit() => new("a7.7-encircled", "Encircled", [new("A7.7", 57), new("A7.72", 58)],
        [
            new(Unit, ContractFactType.Unit, false, ContractDisclosure.All, "The unit") { Concerns = Unit },
            ScenarioA1RuleContracts.Flag("marker", "An Encirclement counter in its Location Encircles its side"),
            ScenarioA1RuleContracts.Flag("melee", "It is in Melee in a Location under an Encirclement counter of either side"),
            ScenarioA1RuleContracts.Flag("berserk", "It is berserk"),
            ScenarioA1RuleContracts.Flag("heroic", "It is heroic"),
            ScenarioA1RuleContracts.Flag("upperLevelCutOff",
                "It is in an upper level building Location and cannot trace a path to ground level free of an unbroken, armed, unconcealed enemy unit (A7.72)"),
            new("kind", ContractFactType.Choice, false, ContractDisclosure.Owner,
                "What it is: Infantry, a hero, a vehicle, the Vulnerable PRC of an Immobile vehicle, or another PRC aboard a vehicle")
            {
                Choices = Kinds,
                Concerns = Unit
            },
        ],
        [Encircled, NotEncircled, ContractVerdict.NotEvaluated],
        [],
        new("PFPh", "2.22A"))
    {
        Decide = EncircledVerdict,
    };

    private static ContractVerdict SealVerdict(ContractFacts facts)
    {
        if (ScenarioA1EncirclementCalculator.SealRefusal(facts.Text("phase"), facts.Flag("consecutive"), facts.Flag("inPillbox")) is { } refusal)
        {
            return new(NoSeal, [new(refusal, refusal.EndsWith("(B30.32)", StringComparison.Ordinal) ? "B30.32" : "A7.7",
                refusal.EndsWith("(B30.32)", StringComparison.Ordinal) ? 150 : 57)], []);
        }

        if (facts.Number("countingUnits") is not { } given || given != decimal.Truncate(given) || given < 0 || given > int.MaxValue)
        {
            return new(ContractVerdict.NotEvaluated, [new("the counting units are a whole number, zero or more", "A7.7", 57)], []);
        }

        var units = (int)given;
        int[] entries = facts.Text("pattern") switch
        {
            "opposite-hexspines" => OppositeHexspines,
            "three-vertices" => ThreeVertices,
            "three-hexsides" => ThreeHexsides,
            "above-and-below" => AboveAndBelow,
            _ => NoEntries,
        };
        if (ScenarioA1EncirclementCalculator.Seals(units, entries))
        {
            return new(Seals, [new("The attacks seal the Encirclement: the Location is marked, and the units of the target's side there are Encircled", "A7.7", 57)], []);
        }

        return units < 2
            ? new(NoSeal, [new("Encirclement needs fire by two or more units, a FG's units each counted", "A7.71", 58)], [])
            : new(NoSeal, [new("Their LOS enter the Location neither through opposite hexspines, with three vertices between them both ways, through three "
                + "non-contiguous hexsides, nor from above and below", "A7.7", 57)], []);
    }

    private static ContractVerdict EncircledVerdict(ContractFacts facts)
    {
        var (kind, aboard, vulnerable) = facts.Text("kind") switch
        {
            "hero" => ("asl:hero", false, false),
            "vehicle" => ("asl:vehicle", false, false),
            "vulnerable-prc" => ("asl:squad", true, true),
            "prc" => ("asl:squad", true, false),
            _ => ("asl:squad", false, false),
        };
        var berserk = facts.Flag("berserk")!.Value;
        var heroic = facts.Flag("heroic")!.Value;
        var melee = facts.Flag("melee")!.Value;
        var subject = ScenarioA1EncirclementCalculator.Subject(kind, berserk, heroic, null, aboard, vulnerable);
        if (!subject)
        {
            var why = berserk || heroic ? "A berserk or heroic unit is never Encircled"
                : kind == "asl:hero" ? "A hero is never Encircled"
                : kind == "asl:vehicle" ? "A vehicle is never Encircled; only the Vulnerable PRC of an Immobile vehicle"
                : "A PRC is Encircled only as the Vulnerable PRC of an Immobile vehicle";
            return new(NotEncircled, [new(why, "A7.7", 57)], []);
        }

        const string Effects = "Encircled: its Morale Level is one lower against attacks on its Location, all its fire takes +1 on the IFT (or its To Hit DR "
            + "if ordnance), and the first Location it enters costs double MF";
        if (ScenarioA1EncirclementCalculator.Encircled(true, facts.Flag("marker")!.Value, melee, melee, subject))
        {
            return new(Encircled, [new(Effects, "A7.7", 57)], []);
        }

        return facts.Flag("upperLevelCutOff")!.Value
            ? new(Encircled, [new(Effects + ", for it cannot trace a path to ground level", "A7.72", 58)], [])
            : new(NotEncircled, [new("No Encirclement counter Encircles it, and it is not cut off in an upper level", "A7.7", 57)], []);
    }
}
