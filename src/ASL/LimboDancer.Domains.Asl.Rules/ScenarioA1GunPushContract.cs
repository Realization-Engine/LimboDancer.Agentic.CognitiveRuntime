using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contracts of C10.3 and C10.31 (p. 181; the C10.3 claim's design, section 5.4): may this crew push this Gun into this hex, with what
/// Manhandling DRM and result, and may the Gun be hooked up after its push, as <see cref="ScenarioA1GunPushCalculator"/> decides them. The weather, the
/// terrain, the hexside, the road, the MF expended and left (A8.1), the Labor and Low Ammo counters, the DR, and its result are public; the Gun's M#,
/// its mode, NM, RFNM, and the extra pushers are its owner's while it is hidden or concealed, so the opponent's view of such a Gun leaves them unknown and
/// the verdict not evaluated, unless a public fact alone already refuses the push. They are always the owner's here, since a fact's disclosure is
/// static; an unconcealed Gun's printed values are public by the rule, so the opponent's view of it is not evaluated too, which never leaks. Who may
/// push is not a fact here: C10.3 asks a Good Order crew (C10.111 p. 180: also unpinned), which ruling RC10.6 leaves to
/// <see cref="ScenarioA1MovementCalculator.MoveBars"/> until a C10.111 claim, so the wiring asks that first.
/// </summary>
internal static class ScenarioA1GunPushContract
{
    private const string Gun = "gunId";
    private const string Refused = "refused";
    private const string Pushable = "pushable";
    private const string EnterOutcome = "enter";
    private const string EnterStopOutcome = "enter-stop";
    private const string StayOutcome = "stay";
    private const string Allowed = "allowed";
    private const string None = "none";

    private static readonly string[] Results = [None, EnterOutcome, EnterStopOutcome, StayOutcome];

    private static readonly string[] GunFacts = ["qsu", "limbered", "nm", "rfnm", "unhookedThisPlayerTurn"];

    [RuleContract]
    private static RuleContract Manhandling() => new("c10.3-manhandling", "Pushing a Gun", [new("C10.3", 181), new("E3.61", 230), new("E3.7332", 231),
        new("B9.3", 119), new("B24.8", 143)],
        [
            new(Gun, ContractFactType.Unit, true, ContractDisclosure.Owner, "The Gun pushed") { Concerns = Gun },
            new("at", ContractFactType.Hex, false, ContractDisclosure.All, "The Gun's Location"),
            new("to", ContractFactType.Hex, false, ContractDisclosure.All, "The hex it is pushed into"),
            ScenarioA1RuleContracts.Flag("mud", "It is Mud (E3.6)"),
            ScenarioA1RuleContracts.Flag("deepSnow", "It is Deep Snow (E3.73)"),
            ScenarioA1RuleContracts.Flag("roadHexsideCrossed", "The push crosses a road hexside"),
            ScenarioA1RuleContracts.Flag("pavedRoad", "That road is paved"),
            ScenarioA1RuleContracts.Flag("plowedRoad", "That road is plowed (by SSR)"),
            ScenarioA1RuleContracts.Flag("lowAmmoPlaced", "A Low Ammo counter was placed on the Gun before the DR (D3.71)"),
            ScenarioA1RuleContracts.Flag("bypass", "The push uses Bypass"),
            ScenarioA1RuleContracts.Flag("assault", "The pushers use Assault Movement"),
            ScenarioA1RuleContracts.Flag("minimumMove", "The push is a Minimum Move"),
            ScenarioA1RuleContracts.Flag("carryingPp", "A pusher carries PP"),
            ScenarioA1RuleContracts.Flag("wire", "The pushers move beneath a Wire or Panji counter"),
            ScenarioA1RuleContracts.Flag("intoDeepStream", "The hex entered is a deep stream"),
            ScenarioA1RuleContracts.Flag("intoBamboo", "The hex entered is Bamboo"),
            ScenarioA1RuleContracts.Flag("viaTb", "The push enters by a TB"),
            new("hexTem", ContractFactType.Number, false, ContractDisclosure.All, "The TEM of the hex entered, without SMOKE, Hindrance, or FFMO"),
            new("hexsideTem", ContractFactType.Number, false, ContractDisclosure.All, "The TEM of the hexside crossed: a wall +2, a hedge +1"),
            new("mfSpent", ContractFactType.Number, false, ContractDisclosure.All, "The MF the pusher expends for the hex, doubled for the push"),
            new("labor", ContractFactType.Number, false, ContractDisclosure.All, "The pushers' Labor status: 0, 1, or 2 (B24.8)"),
            new("leastHalfMfLeft", ContractFactType.Number, true, ContractDisclosure.All,
                "The least MF left, in half MF, of the Infantry that has pushed this Player Turn, this push's pushers included"),
            new("lastResult", ContractFactType.Choice, false, ContractDisclosure.All, "The Gun's last Manhandling result this MPh") { Choices = Results },
            new("originalDr", ContractFactType.Number, true, ContractDisclosure.All, "The Original Manhandling DR, once rolled"),
            Owners("mNumber", ContractFactType.Number, "The Gun's M#"),
            Owners("qsu", ContractFactType.Flag, "The Gun is QSU"),
            Owners("limbered", ContractFactType.Flag, "The Gun is limbered"),
            Owners("nm", ContractFactType.Flag, "The Gun is NM"),
            Owners("rfnm", ContractFactType.Flag, "The Gun is RFNM"),
            Owners("unhookedThisPlayerTurn", ContractFactType.Flag, "The Gun was unhooked this Player Turn"),
            Owners("extraCrewsOrHalfSquads", ContractFactType.Number, "The pushing crews and HS beyond the first pusher"),
            Owners("extraSquads", ContractFactType.Number, "The pushing squads beyond the first pusher"),
            Owners("onlyPusherIsSquad", ContractFactType.Flag, "The only pushing MMC is a squad"),
        ],
        [Refused, Pushable, EnterOutcome, EnterStopOutcome, StayOutcome, ContractVerdict.NotEvaluated],
        [new("manhandling-rolled", "the Gun, the roll, the DRM, the M#, and the result")],
        new("MPh", "3.32A"))
    {
        Decide = ManhandlingVerdict,
    };

    [RuleContract]
    private static RuleContract HookAfterPush() => new("c10.31-hook-after-push", "Hooking up a pushed Gun", [new("C10.31", 181), new("C10.11", 180),
        new("E3.64", 230), new("E3.733", 231)],
        [
            new(Gun, ContractFactType.Unit, true, ContractDisclosure.Owner, "The Gun to hook up") { Concerns = Gun },
            ScenarioA1RuleContracts.Flag("pushedThisMph", "The Gun was pushed this MPh"),
            new("lastResult", ContractFactType.Choice, false, ContractDisclosure.All, "Its last Manhandling result this MPh") { Choices = Results },
            new("leastHalfMfLeft", ContractFactType.Number, true, ContractDisclosure.All,
                "The least MF left, in half MF, of the Infantry that has pushed this Player Turn"),
            ScenarioA1RuleContracts.Flag("mud", "It is Mud (E3.6)"),
            ScenarioA1RuleContracts.Flag("deepSnow", "It is Deep Snow (E3.73)"),
            ScenarioA1RuleContracts.Flag("roadExit", "A paved (Mud) or plowed (Deep Snow) road hexside leaves the Gun's hex"),
            ScenarioA1RuleContracts.Flag("vehicleHasHookUpMp", "The vehicle has not exceeded the MP it may expend before hooking up (C10.11)"),
        ],
        [Allowed, Refused, ContractVerdict.NotEvaluated],
        [new("gun-hooked", "the Gun hooked up to the vehicle")],
        new("MPh", "3.32A"))
    {
        Decide = HookAfterPushVerdict,
    };

    private static ContractFact Owners(string name, ContractFactType type, string description) =>
        new(name, type, true, ContractDisclosure.Owner, description)
        {
            Concerns = Gun
        };

    /// <summary>A whole number within the bounds, or null.</summary>
    private static int? Whole(ContractFacts facts, string name, int least, int most) =>
        facts.Number(name) is { } given && given == decimal.Truncate(given) && given >= least && given <= most ? (int)given : null;

    private static ManhandlingResult? Last(ContractFacts facts) => ScenarioA1GunPushCalculator.ResultOf(facts.Text("lastResult"));

    private static ContractVerdict ManhandlingVerdict(ContractFacts facts)
    {
        if (Whole(facts, "hexTem", -9, 9) is not { } hexTem || Whole(facts, "hexsideTem", -9, 9) is not { } hexsideTem
            || Whole(facts, "mfSpent", 0, 99) is not { } mfSpent || Whole(facts, "labor", 0, 9) is not { } labor)
        {
            return new(ContractVerdict.NotEvaluated, [new("the TEM are whole numbers from -9 to 9, the MF spent from 0 to 99, and the Labor from 0 to 9", "C10.3", 181)], []);
        }

        var given = facts.Number("leastHalfMfLeft") is null ? (int?)null : Whole(facts, "leastHalfMfLeft", 0, 99);
        if (facts.Number("leastHalfMfLeft") is not null && given is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("the MF left is a whole number of half MF from 0 to 99", "C10.3", 181)], []);
        }

        // A refusal the known facts already make is certain; one only an unknown fact of the Gun could make leaves the question open.
        var push = new GunPushFacts(facts.Flag("qsu") ?? true, facts.Flag("limbered") ?? true, facts.Flag("nm") ?? false, facts.Flag("rfnm") ?? false,
            facts.Flag("unhookedThisPlayerTurn") ?? false, facts.Flag("bypass")!.Value, facts.Flag("assault")!.Value, facts.Flag("minimumMove")!.Value,
            facts.Flag("carryingPp")!.Value, facts.Flag("wire")!.Value, facts.Flag("intoDeepStream")!.Value, facts.Flag("intoBamboo")!.Value,
            facts.Flag("viaTb")!.Value, Last(facts), 2 * mfSpent, [given ?? int.MaxValue]);
        if (ScenarioA1GunPushCalculator.PushRefusal(push) is { } certain)
        {
            return new(Refused, [new(certain, "C10.3", 181)], []);
        }

        if (GunFacts.FirstOrDefault(name => facts.Flag(name) is null) is { } unknown)
        {
            return new(ContractVerdict.NotEvaluated, [new($"the fact '{unknown}' of the Gun is not known to this viewer", "C10.3", 181)], []);
        }

        if (given is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("the MF left of the Infantry that has pushed this Player Turn is not known", "C10.3", 181)], []);
        }

        if (Whole(facts, "mNumber", 2, 12) is not { } mNumber || Whole(facts, "extraCrewsOrHalfSquads", 0, 9) is not { } crews
            || Whole(facts, "extraSquads", 0, 9) is not { } squads || facts.Flag("onlyPusherIsSquad") is not { } onlySquad)
        {
            return new(ContractVerdict.NotEvaluated, [new("the M# (2 to 12) and the extra pushers (0 to 9) must be known to this viewer", "C10.3", 181)], []);
        }

        var mud = facts.Flag("mud")!.Value;
        var deepSnow = facts.Flag("deepSnow")!.Value;
        var drmFacts = new ManhandlingDrmFacts(mfSpent, hexTem, hexsideTem, mud, deepSnow, facts.Flag("roadHexsideCrossed")!.Value, facts.Flag("pavedRoad")!.Value,
            facts.Flag("plowedRoad")!.Value, crews, squads, onlySquad, facts.Flag("lowAmmoPlaced")!.Value, labor);
        var drm = ScenarioA1GunPushCalculator.ManhandlingDrm(drmFacts);
        var terms = new ContractReason(Terms(drmFacts, drm), "C10.3", 181);
        if (facts.Number("originalDr") is null)
        {
            return new(Pushable, [new($"It may be pushed with a Manhandling DRM of {Signed(drm)} against its M# {mNumber}", "C10.3", 181), terms],
                [new("manhandling", mNumber - drm, $"Manhandling DR: an Original DR of at most {mNumber - drm} enters; below it the Gun may be pushed on")]);
        }

        if (Whole(facts, "originalDr", 2, 12) is not { } dr)
        {
            return new(ContractVerdict.NotEvaluated, [new("the Original DR is a whole number from 2 to 12", "C10.3", 181)], []);
        }

        var final = dr + drm;
        return ScenarioA1GunPushCalculator.Result(final, mNumber) switch
        {
            ManhandlingResult.Enter => new(EnterOutcome,
                [new($"Final DR {final} is below the M# {mNumber}: the Gun enters and may be pushed on or hooked up", "C10.3", 181), terms], []),
            ManhandlingResult.EnterStop => new(EnterStopOutcome,
                [new($"Final DR {final} equals the M# {mNumber}: the Gun and its pushers enter and move no farther this MPh", "C10.3", 181), terms], []),
            _ => new(StayOutcome,
                [new($"Final DR {final} is above the M# {mNumber}: neither moves farther this MPh, and the pushers gain a Labor counter", "C10.3", 181), terms], []),
        };
    }

    private static string Signed(int value) => value.ToString("+0;-0;0", CultureInfo.InvariantCulture);

    private static string Terms(ManhandlingDrmFacts facts, int drm)
    {
        var road = ScenarioA1GunPushCalculator.RoadDrmApplies(facts.RoadHexsideCrossed, facts.Mud, facts.DeepSnow, facts.PavedRoad, facts.PlowedRoad);
        return $"DRM {Signed(drm)}: TEM {Signed(Math.Max(0, facts.HexTem) + Math.Max(0, facts.HexsideTem))}, MF {Signed(facts.MfSpent)}, "
            + $"weather {Signed(ScenarioA1GunPushCalculator.WeatherDrm(facts.Mud, facts.DeepSnow))}, road {Signed(road ? -2 : 0)}, "
            + $"extra pushers {Signed(ScenarioA1GunPushCalculator.ExtraPushersDrm(facts.ExtraCrewsOrHalfSquads, facts.ExtraSquads, facts.OnlyPusherIsSquad))}, "
            + $"Low Ammo {Signed(facts.LowAmmoPlaced ? -2 : 0)}, Labor {Signed(ScenarioA1GunPushCalculator.LaborDrm(facts.Labor))}";
    }

    private static ContractVerdict HookAfterPushVerdict(ContractFacts facts)
    {
        var known = facts.Number("leastHalfMfLeft");
        var least = known is null ? null : Whole(facts, "leastHalfMfLeft", 0, 99);
        if (known is not null && least is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("the MF left is a whole number of half MF from 0 to 99", "C10.31", 181)], []);
        }

        var pushed = facts.Flag("pushedThisMph")!.Value;
        var hook = new HookAfterPushFacts(pushed, Last(facts), [least ?? int.MaxValue], facts.Flag("mud")!.Value, facts.Flag("deepSnow")!.Value,
            facts.Flag("roadExit")!.Value, facts.Flag("vehicleHasHookUpMp")!.Value);
        if (ScenarioA1GunPushCalculator.HookAfterPushRefusal(hook) is { } certain)
        {
            return new(Refused, [new(certain, "C10.31", 181)], []);
        }

        if (pushed && least is null)
        {
            return new(ContractVerdict.NotEvaluated, [new("the MF left of the Infantry that has pushed this Player Turn is not known", "C10.31", 181)], []);
        }

        return pushed
            ? new(Allowed, [new("Its pushers could still push it on, and the vehicle has the MP to hook it up", "C10.31", 181)], [])
            : new(Allowed, [new("It was not pushed this MPh, so C10.31 puts no condition on the hook-up", "C10.31", 181)], []);
    }
}
