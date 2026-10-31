using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of A10.531 (p. 67; the A10.531 claim's design, section 2.4): is this Location Open Ground to this enemy unit, as
/// <see cref="ScenarioA1OpenGroundCalculator.OpenGroundTo"/> decides it. The Location, its terrain, the month, SMOKE, a wreck or AFV there, a protected
/// entry, and whether the mover entered at the road rate are on the map or in the scenario for both sides. Where the enemy unit is, and so its LOS,
/// the Hindrance and the hexside TEM on it, its Height Advantage, and whether it sees the road, are its owner's while it is hidden or concealed. The
/// verdict is given when every value of the facts the viewer lacks gives the same answer: the public facts alone say "not Open Ground" for woods,
/// brush, an orchard, SMOKE, a wreck, or a protected entry.
/// </summary>
internal static class ScenarioA1OpenGroundContract
{
    private const string Viewer = "viewerId";

    private static readonly string[] Purposes = ["rout", "dash", "interdiction", "concealment"];

    // The values an unknown fact is tried at: a month in grain's season and one outside it; no Hindrance and one.
    private static readonly int[] Months = [7, 10];

    private static readonly int[] Hindrances = [0, 1];

    private static readonly bool[] Both = [false, true];

    private static readonly bool[] OnlyTrue = [true];

    private static readonly bool[] OnlyFalse = [false];

    [RuleContract]
    private static RuleContract OpenGround() => new("a10.531-open-ground", "Open Ground to an enemy unit",
        [new("A10.531", 67), new("A4.6", 51), new("A4.132", 48), new("A12.14", 78), new("B15.6", 129)],
        [
            new("purpose", ContractFactType.Choice, false, ContractDisclosure.All, "What asks: the rout, Dash, Interdiction, or concealment (A12.14)") { Choices = Purposes },
            new("at", ContractFactType.Hex, false, ContractDisclosure.All, "The Location"),
            new("terrain", ContractFactType.Text, false, ContractDisclosure.All, "Its terrain key (a road hex is open-ground)"),
            new("month", ContractFactType.Number, true, ContractDisclosure.All, "The scenario month, 1 to 12, unknown when the game names none"),
            ScenarioA1RuleContracts.Flag("smokeInLocation", "SMOKE is in the Location"),
            ScenarioA1RuleContracts.Flag("protectedEntry", "The unit entered a shellhole, entrenchment, or pillbox at its protective cost, or holds one"),
            ScenarioA1RuleContracts.Flag("inLocationCover", "A wreck or AFV is in the Location"),
            ScenarioA1RuleContracts.Flag("roadRate", "The unit entered the Location at the road rate (A4.132)"),
            new(Viewer, ContractFactType.Unit, true, ContractDisclosure.Owner, "The enemy unit the Location is judged for") { Concerns = Viewer },
            Owners("losClear", ContractFactType.Flag, "It has a clear LOS to the Location"),
            Owners("losHindrance", ContractFactType.Number, "The Hindrance on its LOS, the map's, a vehicle's or wreck's, and SMOKE"),
            Owners("hexsideTem", ContractFactType.Flag, "Its LOS crosses a wall or hedge hexside of the Location"),
            Owners("heightAdvantage", ContractFactType.Flag, "The unit in the Location has Height Advantage over it"),
            Owners("seesAlongRoad", ContractFactType.Flag, "Its LOS reaches the road without crossing the Location's other terrain (A4.132)"),
        ],
        ["open-ground", "not-open-ground", ContractVerdict.NotEvaluated],
        [],
        new("RtPh", "6.21B"))
    {
        Decide = Verdict,
    };

    private static ContractFact Owners(string name, ContractFactType type, string description) =>
        new(name, type, true, ContractDisclosure.Owner, description)
        {
            Concerns = Viewer
        };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var purpose = Enum.Parse<OpenGroundPurpose>(facts.Text("purpose")!, ignoreCase: true);
        var terrain = facts.Text("terrain")!;
        var month = Whole(facts.Number("month"), "month", 1, 12);
        var smoke = facts.Flag("smokeInLocation")!.Value;
        var protectedEntry = facts.Flag("protectedEntry")!.Value;
        var inHexCover = facts.Flag("inLocationCover")!.Value;
        var roadRate = facts.Flag("roadRate")!.Value;
        var losClear = facts.Flag("losClear");
        var losHindrance = Whole(facts.Number("losHindrance"), "losHindrance", 0, int.MaxValue);
        var hexsideTem = facts.Flag("hexsideTem");
        var heightAdvantage = facts.Flag("heightAdvantage");
        var alongRoad = facts.Flag("seesAlongRoad");

        // Every value of the facts the viewer lacks is tried; the verdict stands only when all of them agree.
        var answers = new HashSet<bool?>();
        foreach (var someMonth in month is { } m ? [m] : Months)
        {
            foreach (var clear in Values(losClear))
            {
                foreach (var hindrance in losHindrance is { } h ? [h] : Hindrances)
                {
                    foreach (var tem in Values(hexsideTem))
                    {
                        foreach (var height in Values(heightAdvantage))
                        {
                            foreach (var road in Values(alongRoad))
                            {
                                var location = new OpenGroundLocationFacts(terrain, someMonth, smoke, protectedEntry);
                                var view = new OpenGroundViewFacts(clear, hindrance, new RoutCoverFacts(null, 0, tem, height, inHexCover), roadRate && road ? "open-ground" : null);
                                answers.Add(ScenarioA1OpenGroundCalculator.OpenGroundTo(location, view, purpose));
                            }
                        }
                    }
                }
            }
        }

        if (answers.Count != 1 || answers.Single() is not { } open)
        {
            var missing = new[] { ("month", month is null), ("losClear", losClear is null), ("losHindrance", losHindrance is null), ("hexsideTem", hexsideTem is null),
                ("heightAdvantage", heightAdvantage is null), ("seesAlongRoad", alongRoad is null) }.Where(item => item.Item2).Select(item => item.Item1);
            return new(ContractVerdict.NotEvaluated, [new($"the answer turns on facts this viewer does not know: {string.Join(", ", missing)}", "A10.531", 67)], []);
        }

        if (open)
        {
            var text = purpose == OpenGroundPurpose.Concealment
                ? "Open Ground in this unit's LOS, with nothing in the Location to keep a \"?\" (a Hindrance or TEM along the LOS does not, A12.14)"
                : "This unit could apply the -1 FFMO DRM there: Open Ground, a clear LOS, and nothing on it to cancel FFMO";
            return new("open-ground", [new(text, "A10.531", 67)], []);
        }

        return new("not-open-ground", [Why(purpose, terrain, month, smoke, protectedEntry, inHexCover, roadRate, losClear, losHindrance, hexsideTem, heightAdvantage, alongRoad)], []);
    }

    private static ContractReason Why(OpenGroundPurpose purpose, string terrain, int? month, bool smoke, bool protectedEntry, bool inHexCover, bool roadRate,
        bool? losClear, int? losHindrance, bool? hexsideTem, bool? heightAdvantage, bool? alongRoad)
    {
        var seen = roadRate && alongRoad == true ? "open-ground" : terrain;
        var terrainKnown = !roadRate || alongRoad is not null || ScenarioA1OpenGroundCalculator.OpenGroundTerrain(terrain, month) == true;
        return smoke ? new("SMOKE is in the Location", "A4.6", 51)
            : protectedEntry ? new("The unit entered or holds a shellhole, entrenchment, or pillbox at its protective cost", "A10.531", 67)
            : losClear == false ? new("The Location is out of this unit's LOS", "A4.63", 51)
            : inHexCover ? new("A wreck or AFV is in the Location", "B1.17", 113)
            : terrainKnown && ScenarioA1OpenGroundCalculator.OpenGroundTerrain(seen, month) == false
                ? new(string.Create(CultureInfo.InvariantCulture, $"Its terrain ({seen}{(seen == "grain" ? $" in month {month}" : string.Empty)}) is not Open Ground"), "B1.1", 113)
            : purpose != OpenGroundPurpose.Concealment && losHindrance > 0 ? new("A Hindrance on this unit's LOS cancels FFMO", "A4.6", 51)
            : purpose != OpenGroundPurpose.Concealment && hexsideTem == true ? new("A wall or hedge on the hexside its LOS crosses gives TEM", "B1.16", 113)
            : purpose != OpenGroundPurpose.Concealment && heightAdvantage == true ? new("The unit there has Height Advantage over it", "B10.31", 125)
            : new("Nothing lets this unit apply FFMO there", "A10.531", 67);
    }

    private static bool[] Values(bool? fact) => fact switch
    {
        true => OnlyTrue,
        false => OnlyFalse,
        null => Both,
    };

    private static int? Whole(decimal? number, string name, int least, int most) =>
        number is not { } value ? null
        : value == decimal.Truncate(value) && value >= least && value <= most ? (int)value
        : throw new ArgumentOutOfRangeException(name, value, string.Create(CultureInfo.InvariantCulture, $"'{name}' is a whole number from {least} to {most}"));
}
