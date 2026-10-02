namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Leader Creation Table (A18.2, p. 85), which the Rally package (A18.11) and the Close Combat package (A18.12) share: a
/// dr with cumulative drm for nationality, the base unit's Morale Level, and the cause's own drm; the leader is the unit's
/// nationality's counter of that grade.
/// </summary>
public static class ScenarioA1FieldPromotion
{
    /// <summary>Whether the table applies to a nationality: never to Finns or Japanese (the table's note).</summary>
    public static bool Applicable(string nationality) => nationality is not ("finnish" or "japanese");

    /// <summary>
    /// The dr's outcome, or the reason it is undecided. <paramref name="extra"/> are the cause's drm after the nationality and
    /// Morale Level ones: +1 broken for a Self-Rally (A18.11), -1 per odds column below 1-1 in CC (A18.2).
    /// </summary>
    public static (LeaderCreationOutcome? Outcome, string? Undecided) Create(FireDefinition unit, int morale, int dr, IReadOnlyList<FireModifier> extra,
        IReadOnlyDictionary<string, FireDefinition> definitions, string reasonPrefix, bool fanatic = false)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(extra);
        ArgumentNullException.ThrowIfNull(definitions);
        if (ScenarioA1FireReference.IsNkvd(unit.Id))
        {
            return Commissar(dr, extra, fanatic, definitions, reasonPrefix);
        }

        if (!Applicable(unit.Nationality))
        {
            return (null, $"{reasonPrefix}.leader-creation-not-applicable:{unit.Nationality}");
        }

        // A18.2 (ruling R27.1): the table's nationality drm; a nationality it does not list (French, Axis Minor) has none, and a nationality the
        // game has not reviewed is refused rather than given none.
        int? nationality = unit.Nationality switch
        {
            "american" or "british" or "german" => -1,
            "russian" or "italian" => 1,
            "french" or "axis-minor" => 0,
            _ => null,
        };
        if (nationality is null)
        {
            return (null, $"{reasonPrefix}.leader-creation-nationality-unreviewed:{unit.Nationality}");
        }

        var drm = new List<FireModifier>();
        if (nationality != 0)
        {
            drm.Add(new FireModifier("nationality:" + unit.Nationality, nationality.Value, "A18.2"));
        }

        if (morale >= 8)
        {
            drm.Add(new FireModifier("morale-8-or-more", -1m, "A18.2"));
        }
        else if (morale <= 6)
        {
            drm.Add(new FireModifier("morale-6-or-less", 1m, "A18.2"));
        }

        drm.AddRange(extra);
        var final = dr + (int)drm.Sum(item => item.Value);
        (int Morale, int Leadership)? grade = final switch
        {
            >= 7 => null,
            6 => (6, 1),
            4 or 5 => (7, 0),
            2 or 3 => (8, 0),
            _ => (8, -1),
        };
        if (grade is not { } wanted)
        {
            return (new LeaderCreationOutcome(dr, drm, final, null), null);
        }

        var leader = definitions.Values.Where(item => item.IsLeader && item.Nationality == unit.Nationality && !ScenarioA1FireReference.IsCommissar(item.Id)
            && item.Morale == wanted.Morale && item.Leadership == wanted.Leadership).Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();
        return leader is null
            ? (null, $"{reasonPrefix}.leader-counter-missing:{unit.Nationality}:{wanted.Morale}{(wanted.Leadership > 0 ? "+" : "-")}{Math.Abs(wanted.Leadership)}")
            : (new LeaderCreationOutcome(dr, drm, final, leader), null);
    }

    /// <summary>
    /// The NKVD Field Promotion Commissar Creation table (A25.25, p. 96; backlog pass 15, ruling R15.7), which replaces the Leader Creation Table for
    /// an NKVD MMC: dr 1 a 10-0, 2 or 3 a 9-0, 4 or 5 an 8+1 Commissar, 6 or more none; drm -1 per odds column below 1-1 (the cause's own), -1 when
    /// the base unit is Fanatic, and +1 when it is broken (the cause's own in a Self-Rally).
    /// </summary>
    private static (LeaderCreationOutcome? Outcome, string? Undecided) Commissar(int dr, IReadOnlyList<FireModifier> extra, bool fanatic,
        IReadOnlyDictionary<string, FireDefinition> definitions, string reasonPrefix)
    {
        var drm = extra.Select(item => item with { Rule = "A25.25" }).ToList();
        if (fanatic)
        {
            drm.Add(new FireModifier("fanatic", -1m, "A25.25"));
        }

        var final = dr + (int)drm.Sum(item => item.Value);
        var commissar = final switch
        {
            <= 1 => "defender-commissar-10-0",
            2 or 3 => "defender-commissar-9-0",
            4 or 5 => "defender-commissar-8-plus-1",
            _ => null,
        };
        return commissar is not null && !definitions.ContainsKey(commissar)
            ? (null, $"{reasonPrefix}.leader-counter-missing:{commissar}")
            : (new LeaderCreationOutcome(dr, drm, final, commissar), null);
    }
}
