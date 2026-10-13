using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a To Hit DRM is for a Gun Duel (C2.2401, p. 167; the C5 and C6 charts, p. 189).</summary>
public enum ToHitDrmClass
{
    /// <summary>The C5 chart's Firer-Based DRM: Cases A to I and its "Other" lines.</summary>
    FirerBased,

    /// <summary>Case N, the Acquisition DRM (C6.5), which the Gun Duel DRM counts beside the firer's.</summary>
    Acquisition,

    /// <summary>The C6 chart's Target-Based DRM, which a Gun Duel does not count.</summary>
    TargetBased,

    /// <summary>The doubled lower dr of a Motion firer (C5.35): a firer's, and by C2.2401 "not included in the Gun Duel DRM calculation".</summary>
    LowerDrDoubled,
}

/// <summary>A side's total Gun Duel DRM (C2.2401): the Firer-Based and Acquisition lines of its shot's To Hit DRM, and their sum.</summary>
public sealed record GunDuelDrm(int Total, IReadOnlyList<FireModifier> Lines);

/// <summary>
/// What the game's state says of a declared Defensive First Fire shot for a Gun Duel (C2.2401; pass 55, ruling R55.19): whether its target is the
/// moving vehicle of the open window; whether the firing Gun, its crew, or the firing vehicle is concealed or hidden; whether the shot is Reaction
/// Fire, made from the Location an OVR was resolved in (D7.2); whether the vehicle is conducting an OVR not yet resolved (D7.1); and whether its MA
/// has already fired at this MP expenditure, which only a Gun Duel lets it do.
/// </summary>
public sealed record GunDuelFacts(bool AtTheMovingVehicle, bool FirerConcealed, bool ReactionFire, bool Overrun, bool Dueled)
{
    /// <summary>
    /// Whether the vehicle carries an Armor Leader no record has yet revealed (D3.41, p. 200; ruling R55.29): the declaration is the DEFENDER's
    /// act, so his modifier is not counted in the vehicle's Gun Duel DRM and his line is told nowhere. An Inexperienced Crew's +1 is public.
    /// </summary>
    public bool UnrevealedArmorLeader
    {
        get; init;
    }

    /// <summary>
    /// Whether the declaring firer is a vehicle with an Armor Leader no record has yet revealed (D3.41; ruling R55.29, extended): the
    /// declaration rolls nothing and so uses nothing of his, and its texts are public, so his modifier is not counted in the DEFENDER's Gun Duel
    /// DRM either. The shot he then fires takes it, and its record reveals him.
    /// </summary>
    public bool FirerUnrevealedArmorLeader
    {
        get; init;
    }
}

/// <summary>
/// The firer of a declared shot as the first shot of a Gun Duel left it (C2.2401, p. 168): whether its crew, or the firing vehicle, is
/// eliminated, broken, Stunned, or Shocked (an Unconfirmed Kill with it, C7.42), and whether its Gun is destroyed or malfunctioned.
/// </summary>
public sealed record GunDuelReturnFirer(bool Eliminated, bool Broken, bool Stunned, bool Shocked, bool GunOut);

/// <summary>Whether a Gun Duel may be declared against a declared shot: no bar, and each side's Gun Duel DRM once both shots were read.</summary>
public sealed record GunDuelVerdict(string? Bar, GunDuelDrm? Attacker, GunDuelDrm? Defender)
{
    public bool Offered => Bar is null;
}

/// <summary>
/// Gun Duels (C2.2401, pp. 167 and 168; pass 55, rulings R55.19 and R55.20): "Vs a non-concealed, non-Aerial DEFENDER's declared Defensive First
/// Fire attack on it, a vehicle may attempt to Bounding First Fire (D3.3) its MA ... at that DEFENDER first, provided the vehicle need not change
/// CA, is not conducting an OVR (D7.1), its total Gun Duel DRM (i.e., its total Firer-Based [5.] and Acquisition [6.5] TH DRM for its potential
/// shot) is &lt; that of the DEFENDER, and the DEFENDER's attack is not Reaction Fire (D7.2)." The slice: ordnance on both sides (a Gun's or a
/// vehicle's MA shot at the moving vehicle, answered by its MA), the vehicle's DRM strictly lower, the first duel of an MP expenditure. Play reads
/// the state and plans both shots; every verdict and every text is here.
/// </summary>
public static class ScenarioA1GunDuels
{
    /// <summary>The pending choice's kind, and its two answers.</summary>
    public const string ChoiceKind = "gun-duel";
    public const string Duel = "duel";
    public const string Decline = "decline";

    /// <summary>Why no Gun Duel is offered, as a verdict names it.</summary>
    public const string NotDefensiveFirstFire = "not-defensive-first-fire";
    public const string NotOrdnance = "not-ordnance";
    public const string Concealed = "firer-concealed";
    public const string Reaction = "reaction-fire";
    public const string Overrunning = "overrun";
    public const string AlreadyFired = "fired-at-this-expenditure";
    public const string NoShot = "no-shot";
    public const string CaChange = "ca-change";
    public const string SpecialAmmunition = "special-ammunition";
    public const string DrmUnread = "drm-unread";
    public const string NotLower = "drm-not-lower";

    /// <summary>The causes a return shot is void for, as its record names them.</summary>
    public const string Eliminated = "eliminated";
    public const string Broken = "broken";
    public const string Stunned = "stunned";
    public const string Shocked = "shocked";
    public const string GunOut = "gun";
    public const string Other = "other";

    /// <summary>
    /// The class of a To Hit DRM by the name the Ordnance calculators give it (the C5 and C6 charts, p. 189; C5.9's closing note, p. 173: "CX
    /// (A4.51), Overstack (A5.12), Leadership (A7.531; D3.44), Encircled (A7.7), Spotted Fire (9.3) ... and Stun (D5.34)" are firer-based too).
    /// Null for a name not classified here: a shot that carries one is offered no duel, and the Rules test that lists every name fails.
    /// </summary>
    public static ToHitDrmClass? Classify(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name == "case-c4:lower-dr")
        {
            return ToHitDrmClass.LowerDrDoubled;
        }

        var colon = name.IndexOf(':', StringComparison.Ordinal);
        return (colon < 0 ? name : name[..colon]) switch
        {
            // C5: Case A (a CA change, and the one a turret keeps, D3.51), B (AFPh), C, C1, C2 (Bounding Fire), C3 (a LATW), D (pinned), E (within
            // the hex), F (Intensive Fire), H (non-qualified), I (BU); CX, Stun, Spotted Fire, the firer's overstacking, Leadership, and an Armor
            // Leader's or an Inexperienced Crew's line (D3.44, D3.45).
            "case-a" or "case-b" or "case-c" or "case-c1" or "case-c2" or "case-c3" or "case-d" or "case-e" or "case-f" or "case-h" or "case-i"
                or "cx" or "stun" or "spotted" or "overstack-firer" or "leadership"
                or ScenarioA1ArmorLeaders.LeaderModifier or ScenarioA1ArmorLeaders.InexperiencedModifier => ToHitDrmClass.FirerBased,
            "case-n" => ToHitDrmClass.Acquisition,

            // C6: Case J and its subcases, K, L, M, O, P, Q, R, and the target's overstacking.
            "case-j" or "case-j1" or "case-j2" or "case-j3" or "case-j4" or "case-k" or "case-l" or "case-m" or "case-o" or "case-p" or "case-q" or "case-r"
                or "overstack-target" => ToHitDrmClass.TargetBased,
            _ => null,
        };
    }

    /// <summary>
    /// A shot's total Gun Duel DRM (C2.2401): "its total Firer-Based [5.] and Acquisition [6.5] TH DRM for its potential shot", read from the DRM
    /// list the shot itself would resolve with, before any roll; "neither the +1 DRM for a Gyrostabilizer nor the doubling of the lower dr for
    /// other ordnance in TH Case C4 (5.35) is included". Null when the shot has no To Hit DR to read, or carries a DRM not classified.
    /// </summary>
    public static GunDuelDrm? Drm(OrdnanceShot shot, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(reference);

        // Dice that decide nothing but the DRM list: an Original 12.
        var toHit = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = new OrdnanceRolls([6, 6], null, null, null, null)
        }, reference).ToHit;
        if (toHit is null)
        {
            return null;
        }

        var lines = new List<FireModifier>();
        foreach (var item in toHit.Drm)
        {
            switch (Classify(item.Name))
            {
                case null:
                    return null;
                case ToHitDrmClass.FirerBased or ToHitDrmClass.Acquisition:
                    lines.Add(item);
                    break;
            }
        }

        return new GunDuelDrm((int)lines.Sum(item => item.Value), lines);
    }

    /// <summary>
    /// Whether the moving vehicle may declare a Gun Duel against a declared shot (C2.2401; rulings R55.19, R55.20). <paramref name="declared"/> is
    /// the DEFENDER's shot, accepted and not yet rolled; <paramref name="answer"/> reads the vehicle's potential shot at the firer, its MA's
    /// Bounding First Fire with the default ammunition, or null when its MA cannot fire now: it is asked only once the declared shot can be dueled.
    /// </summary>
    public static GunDuelVerdict Verdict(GunDuelFacts facts, OrdnanceShot declared, Func<OrdnanceShot?> answer, ScenarioA1OrdnanceReference reference)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(declared);
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(reference);
        static GunDuelVerdict No(string bar, GunDuelDrm? attacker = null, GunDuelDrm? defender = null) => new(bar, attacker, defender);

        // "DEFENDER's declared Defensive First Fire attack on it": a shot at the moving vehicle, named on the Vehicle Target Type.
        if (declared.FireKind != "first-fire" || declared.FiringSide != "non-phasing" || declared.VehicleTarget is null || !facts.AtTheMovingVehicle)
        {
            return No(NotDefensiveFirstFire);
        }

        // Ruling R55.20: a Gun's or a vehicle's MA shot. A LATW is left out with the fire that is not ordnance: a PF's shot waits on its PF Check
        // (C13.31), so its declaration is not yet an attack with a To Hit DRM to compare.
        if (!reference.Guns.TryGetValue(declared.Gun?.DefinitionId ?? string.Empty, out var gun) || gun.GunType is "latw" or "mortar"
            || declared.TargetType is not null || ScenarioA1OrdnanceCalculator.IsCanister(declared))
        {
            return No(NotOrdnance);
        }

        if (facts.FirerConcealed || declared.Crew?.Concealed == true)
        {
            return No(Concealed);
        }

        if (facts.ReactionFire)
        {
            return No(Reaction);
        }

        if (facts.Overrun)
        {
            return No(Overrunning);
        }

        // C5.3 (p. 172): "may not fire again until it has expended another MP". Ruling R55.20: the second duel of an expenditure is not built.
        if (facts.Dueled)
        {
            return No(AlreadyFired);
        }

        if (answer() is not { Vehicle: not null, FireKind: ScenarioA1FireCalculator.BoundingFirstFire } potential)
        {
            return No(NoShot);
        }

        // "provided the vehicle need not change CA".
        if (potential.HexspinesToTurn != 0)
        {
            return No(CaChange);
        }

        // Ruling R55.20: the duel's shot is AP at a vehicle and HE otherwise (C2.21); Special Ammunition and Canister are not offered in it.
        if (potential.Ammunition != (potential.VehicleTarget is null ? null : "ap"))
        {
            return No(SpecialAmmunition);
        }

        // D3.41 (ruling R55.29): an Armor Leader stays on his owner's side record "until used"; the DEFENDER's declaration uses nothing of his,
        // so the vehicle's total is summed as if he were absent. This can deny the ATTACKER a duel his modifier would earn: a known omission.
        // The shot the vehicle then fires takes his modifier as any MA shot does.
        if (facts.UnrevealedArmorLeader && potential.Vehicle is { ArmorLeadership: < 0 } led)
        {
            potential = potential with
            {
                Vehicle = led with
                {
                    ArmorLeadership = null
                }
            };
        }

        // The same for the declaring vehicle's leader: both totals are summed without any Armor Leader not yet revealed.
        var compared = facts.FirerUnrevealedArmorLeader && declared.Vehicle is { ArmorLeadership: < 0 } leading
            ? declared with
            {
                Vehicle = leading with
                {
                    ArmorLeadership = null
                }
            }
            : declared;
        var attacker = Drm(potential, reference);
        var defender = Drm(compared, reference);
        if (attacker is null || defender is null)
        {
            return No(DrmUnread, attacker, defender);
        }

        // Ruling R55.20: with equal totals C2.2401 lets the vehicle duel, and "the lower Final TH ... DR fires first". That case is not built
        // (pass 110), so no duel is offered there, and the ATTACKER is denied a duel the rule allows: a known omission.
        return attacker.Total < defender.Total ? new GunDuelVerdict(null, attacker, defender) : No(NotLower, attacker, defender);
    }

    /// <summary>One line of a Gun Duel DRM in words: "Case A +6", "Armor Leader -1".</summary>
    private static string LineText(FireModifier line)
    {
        var colon = line.Name.IndexOf(':', StringComparison.Ordinal);
        var head = colon < 0 ? line.Name : line.Name[..colon];
        var words = head switch
        {
            ScenarioA1ArmorLeaders.LeaderModifier => "Armor Leader",
            ScenarioA1ArmorLeaders.InexperiencedModifier => "Inexperienced Crew",
            "cx" => "CX",
            "stun" => "Stun",
            "spotted" => "Spotted Fire",
            "overstack-firer" => "overstacking",
            "leadership" => "Leadership",
            _ when head.StartsWith("case-", StringComparison.Ordinal) => "Case " + head["case-".Length..].ToUpperInvariant(),
            _ => head,
        };
        return string.Create(CultureInfo.InvariantCulture, $"{words} {line.Value:+0;-0}");
    }

    /// <summary>A Gun Duel DRM in words: "+6 (Case A +6)", or "+0" with no line.</summary>
    public static string DrmText(GunDuelDrm drm)
    {
        ArgumentNullException.ThrowIfNull(drm);
        var total = drm.Total.ToString("+0;-0", CultureInfo.InvariantCulture);
        return drm.Lines.Count == 0 ? total : $"{total} ({string.Join(", ", drm.Lines.Select(LineText))})";
    }

    /// <summary>The declared shot's proposal, before its commit: what declaring it lets the moving vehicle do.</summary>
    public static string ProposalText(string vehicle, string firer, GunDuelDrm attacker, GunDuelDrm defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        return $"play.gun-duel: declaring this shot lets {vehicle} answer with a Gun Duel (C2.2401): its Gun Duel DRM {DrmText(attacker)} is lower than {firer}'s {DrmText(defender)}; "
            + "no dice are rolled until it answers";
    }

    /// <summary>What the pending choice asks, for the Play page: the declared shot, both totals, and the vehicle's option.</summary>
    public static string ChoiceText(string firer, string firerAt, string vehicle, GunDuelDrm attacker, GunDuelDrm defender)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(defender);
        // One sentence, with its rule once (the table player's review): the page ends it with the ruling every pending choice is under.
        return $"{firer} in {firerAt} has declared a shot at {vehicle}, which may fire its MA first in a Gun Duel (C2.2401); "
            + $"Gun Duel DRM: {vehicle} {DrmText(attacker)}; {firer} {DrmText(defender)}";
    }

    /// <summary>The declaration in words, for the record; <paramref name="where"/> is " in" and the firer's Location, or empty.</summary>
    public static string DeclarationText(string firer, string where, string vehicle) =>
        $"{firer}{where} declares a shot at {vehicle}, which may fire its MA first in a Gun Duel (C2.2401)";

    /// <summary>The answer in words, for the plan's reasons and the record.</summary>
    public static string AnswerText(string option, string vehicle, string firer) => option == Duel
        ? $"{vehicle} declares a Gun Duel and fires its MA first at {firer}, whose declared shot follows unless the first eliminates, breaks, stuns, or shocks it (C2.2401)"
        : $"{vehicle} declines the Gun Duel, and the declared shot of {firer} resolves (C2.2401)";

    /// <summary>The refusal of a duel the vehicle's MA can no longer make: the choice is answered by declining.</summary>
    public static string NoLongerText(string vehicle, string reason) =>
        $"play.gun-duel: {vehicle} cannot fire its MA first now ({reason}); decline the Gun Duel, and the declared shot resolves (C2.2401)";

    /// <summary>
    /// Whether a declared shot is void after the vehicle's first shot (C2.2401, p. 168), and why: the first shot "voids the opponent's return
    /// shot by eliminating, breaking, stunning or shocking it"; a Gun the first shot destroyed or malfunctioned (C11.4, C11.6) fires no more
    /// either. Null when the firer still fires its declared shot. The planner asks before it writes the shot or the void's record, and the
    /// projector asks again with the same facts from its own state.
    /// </summary>
    public static string? ReturnShotVoid(GunDuelReturnFirer firer)
    {
        ArgumentNullException.ThrowIfNull(firer);
        return firer.Eliminated ? Eliminated : firer.Broken ? Broken : firer.Stunned ? Stunned : firer.Shocked ? Shocked : firer.GunOut ? GunOut : null;
    }

    /// <summary>
    /// A return shot that is not made, in words, for the record: void for its cause; or, for <see cref="Other"/>, not fired for the reason the
    /// checks of every shot gave when they refused it, which is none of the rule's causes.
    /// </summary>
    public static string VoidText(string firer, string cause, string? reason = null) => cause switch
    {
        Eliminated => $"the return shot of {firer} is void: the first shot eliminated it",
        Broken => $"the return shot of {firer} is void: its crew is broken",
        Stunned => $"the return shot of {firer} is void: it is Stunned",
        Shocked => $"the return shot of {firer} is void: it is Shocked",
        GunOut => $"the return shot of {firer} is void: its Gun is destroyed or malfunctioned",
        _ when reason is { Length: > 0 } => $"the return shot of {firer} is not fired: {reason}",
        _ => $"the return shot of {firer} is not fired: it can no longer fire",
    } + " (C2.2401)";

    /// <summary>
    /// Why an ordnance record may not say it is a Gun Duel's first shot (C2.2401, D3.3), or null: a vehicle's MA fires in its own MPh while the
    /// DEFENDER's window is open only as the answer <c>duel</c> to the pending choice, once, at the MP expenditure it was declared at.
    /// </summary>
    public static RecordRefusal? ShotRecordRefusal(bool recordedAsDuel, bool owed) => recordedAsDuel && !owed
        ? new RecordRefusal("UNIT-STATE-049", "An ordnance record names itself a Gun Duel's first shot, and no Gun Duel was declared for its vehicle at this MP expenditure (C2.2401).")
        : null;

    /// <summary>
    /// Why a void return shot's record is refused (C2.2401), or null: it follows the first shot of a Gun Duel against the Gun it names, and its
    /// cause is the one the firer's state gives: a firer the first shot left able to fire has no void shot, but for the cause <see cref="Other"/>,
    /// a shot the checks of every shot refused, which replay cannot make again.
    /// </summary>
    public static RecordRefusal? VoidRecordRefusal(bool firstShotMade, bool sameFirer, string cause, GunDuelReturnFirer firer) =>
        !firstShotMade || !sameFirer ? new RecordRefusal("UNIT-STATE-049", "A void return shot follows the first shot of a Gun Duel against the firer it names (C2.2401).")
        : cause != (ReturnShotVoid(firer) ?? Other)
            ? new RecordRefusal("UNIT-STATE-049", "A void return shot names the cause the first shot gave its firer: eliminated, broken, Stunned, Shocked, or its Gun out of action (C2.2401).")
        : null;
}
