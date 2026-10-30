namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A8.4, A8.41, A9.3, and C2.241 (pp. 61, 63, 168; the A8.41 claim's design, section 3.4): may this unit or weapon fire in
/// Final Fire at this target, and how, as <see cref="ScenarioA1FinalFireCalculator"/> decides it. Fire counters, the range, and the phase are public
/// (ruling RA8.5); the firer and what it fires are its owner's until it fires, since a concealed firer is not named to the opponent (the design's
/// review, finding 10). A refusal the public facts already make is given to every viewer; any other answer needs the owner's facts.
/// </summary>
internal static class ScenarioA1FinalFireContract
{
    private const string Firer = "firerId";

    private static readonly string[] Markers = ["none", "first-fire", "final-fire", "intensive-fire", "no-fire"];

    private static readonly string[] Weapons = ["inherent", "mg", "gun", "ordnance-sw", "ife", "other-sw"];

    private static readonly string[] MgFacts = ["loneSmc", "vehicular", "mainArmament", "asOrdnance"];

    private static readonly string[] GunFacts = ["noIntensiveFire", "crewFit"];

    private static readonly string[] IfeFacts = ["loneSmc"];

    [RuleContract]
    private static RuleContract FinalFire() => new("a8.41-final-fire", "Final Fire", [new("A8.41", 61), new("A8.4", 61), new("A9.3", 63), new("C2.241", 168)],
        [
            new(Firer, ContractFactType.Unit, true, ContractDisclosure.Owner, "The unit that fires, or that holds the weapon") { Concerns = Firer },
            new("marker", ContractFactType.Choice, false, ContractDisclosure.All, "The fire counter of what fires: the weapon's own, or the unit's for its inherent FP")
            {
                Choices = Markers
            },
            new("range", ContractFactType.Number, true, ContractDisclosure.All, "The range to the target, in hexes"),
            new("phase", ContractFactType.Choice, false, ContractDisclosure.All, "The phase") { Choices = ScenarioA1RuleContracts.Phases },
            new("weapon", ContractFactType.Choice, true, ContractDisclosure.Owner,
                "What fires: its inherent FP, a MG, a Gun as ordnance, an ordnance SW, a Gun using IFE, or another SW (a FT, a DC)")
            {
                Choices = Weapons,
                Concerns = Firer
            },
            Owners("loneSmc", "A MG or IFE fired by a lone SMC (A8.312, A9.3)"),
            Owners("vehicular", "A vehicular MG"),
            Owners("mainArmament", "That vehicular MG is the MA (A9.3, D3.5)"),
            Owners("asOrdnance", "A MG firing as ordnance"),
            Owners("noIntensiveFire", "A Gun listed \"No IF\" (C5.63)"),
            Owners("crewFit", "The Gun's crew is not pinned, shocked, or stunned (C5.6)"),
        ],
        ["may-fire", "may-fire-once-more", "refused", "not-final-fire", ContractVerdict.NotEvaluated],
        [new("fire-resolved", "the attack, and the Final Fire counter on what fired once more")],
        new("DFPh", "4.2"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(string name, string description) =>
        ScenarioA1RuleContracts.Flag(name, description, mayBeUnknown: true, disclosure: ContractDisclosure.Owner) with
        {
            Concerns = Firer
        };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        if (facts.Text("phase") != "DFPh")
        {
            return new("not-final-fire", [new("Final Fire is the DEFENDER's fire in the DFPh", "A8.4", 61)], []);
        }

        int? range = null;
        if (facts.Number("range") is { } given)
        {
            if (given < 0 || given > int.MaxValue || given != decimal.Truncate(given))
            {
                return new(ContractVerdict.NotEvaluated, [new("the range is a whole number of hexes", "A8.4", 61)], []);
            }

            range = (int)given;
        }

        var marker = facts.Text("marker") switch
        {
            "none" => FireMarker.None,
            "first-fire" => FireMarker.FirstFire,
            "final-fire" => FireMarker.FinalFire,
            "intensive-fire" => FireMarker.IntensiveFire,
            _ => FireMarker.NoFire,
        };

        // The public facts alone: with no counter every weapon fires as it would (A8.41); a Final, Intensive, or No Fire counter bars every one; a
        // First Fire counter bars a target beyond an ADJACENT hex whatever fires. The weapon does not change these answers.
        var publicOnly = new FinalFireFacts(FinalFireWeapon.Inherent, marker, range ?? 0, false, false, false, false, false, true);
        if (marker != FireMarker.FirstFire)
        {
            return Answer(publicOnly);
        }

        if (range is > 1)
        {
            return Answer(publicOnly with
            {
                Range = range
            });
        }

        var weapon = facts.Text("weapon") switch
        {
            null => (FinalFireWeapon?)null,
            "inherent" => FinalFireWeapon.Inherent,
            "mg" => FinalFireWeapon.Mg,
            "gun" => FinalFireWeapon.Gun,
            "ordnance-sw" => FinalFireWeapon.OrdnanceSw,
            "ife" => FinalFireWeapon.Ife,
            _ => FinalFireWeapon.OtherSw,
        };
        string[] needed = weapon switch
        {
            FinalFireWeapon.Mg => MgFacts,
            FinalFireWeapon.Gun => GunFacts,
            FinalFireWeapon.Ife => IfeFacts,
            _ => [],
        };
        var missing = weapon is null ? "weapon" : needed.FirstOrDefault(name => facts.Flag(name) is null);
        if (missing is not null || weapon is not { } kind)
        {
            return new(ContractVerdict.NotEvaluated, [new($"the fact '{missing}' is not known to this viewer", "A8.41", 61)], []);
        }

        var known = new FinalFireFacts(kind, marker, range, facts.Flag("loneSmc") == true, facts.Flag("vehicular") == true,
            facts.Flag("mainArmament") == true, facts.Flag("asOrdnance") == true, facts.Flag("noIntensiveFire") == true, facts.Flag("crewFit") == true);

        // A First-Fire-marked weapon with no second attack is refused at any range; one that has it needs the range to know whether the target is in reach.
        if (range is null && ScenarioA1FinalFireCalculator.Use(known) == FinalFireUse.OnceMore)
        {
            return new(ContractVerdict.NotEvaluated, [new("the range is not known: it fires again only at an ADJACENT or same-hex target", "A8.4", 61)], []);
        }

        return Answer(known);
    }

    private static ContractVerdict Answer(FinalFireFacts facts)
    {
        var verdict = ScenarioA1FinalFireCalculator.Verdict(facts);
        if (verdict.Refusal is { } refusal)
        {
            return new("refused", [new(refusal, "A8.41", 61)], []);
        }

        if (verdict.Use == FinalFireUse.Normal)
        {
            return new("may-fire", [new("It has no fire counter: it fires at any target, and a Multiple ROF weapon may keep its ROF", "A8.41", 61)], []);
        }

        var how = verdict.Sustained ? "as Sustained Fire, Area Fire, its B# two lower, and is marked Final Fire (A9.3)"
            : verdict.Intensive ? "as Intensive Fire, and is marked Intensive Fire (C2.241, C5.6)"
            : "as Area Fire, and is marked Final Fire (A8.4)";
        return new("may-fire-once-more", [new("It fires once more at this ADJACENT or same-hex target " + how, "A8.41", 61)], []);
    }
}
