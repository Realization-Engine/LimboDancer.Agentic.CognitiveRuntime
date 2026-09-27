namespace LimboDancer.Domains.Asl.ScenarioA1;

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
        IReadOnlyDictionary<string, FireDefinition> definitions, string reasonPrefix)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(extra);
        ArgumentNullException.ThrowIfNull(definitions);
        if (!Applicable(unit.Nationality))
        {
            return (null, $"{reasonPrefix}.leader-creation-not-applicable:{unit.Nationality}");
        }

        var drm = new List<FireModifier>();
        var nationality = unit.Nationality switch
        {
            "american" or "british" or "german" => -1,
            "russian" or "italian" => 1,
            _ => 0,
        };
        if (nationality != 0)
        {
            drm.Add(new FireModifier("nationality:" + unit.Nationality, nationality, "A18.2"));
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

        var leader = definitions.Values.Where(item => item.IsLeader && item.Nationality == unit.Nationality
            && item.Morale == wanted.Morale && item.Leadership == wanted.Leadership).Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();
        return leader is null
            ? (null, $"{reasonPrefix}.leader-counter-missing:{unit.Nationality}:{wanted.Morale}{(wanted.Leadership > 0 ? "+" : "-")}{Math.Abs(wanted.Leadership)}")
            : (new LeaderCreationOutcome(dr, drm, final, leader), null);
    }
}
