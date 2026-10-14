"""Pass 45: the reviews' fixes in the Studio. Every anchor is checked before anything is written."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
S = "LimboDancer.Domains.Asl.MapStudio/"
PAGE = S + "Components/Pages/Play.razor"
EDITS = {
    PAGE: [
        # UI 1 (BLOCKER): a possessed MG's Bore Sighted Location reaches the setup.
        ("""            placement["holding"] = new { holder, role = "possessed" };
            placement["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false };
            return placement;
""",
         """            placement["holding"] = new { holder, role = "possessed" };
            placement["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false };

            // C6.41, C6.42 (pass 45): a MMG's or HMG's Bore Sighted Location goes with it, as a Gun's does.
            if (item.BoreSighted is { } mgSighted)
            {
                placement["boreSighted"] = mgSighted;
            }

            return placement;
"""),
        # UI 10: the Location in the house style.
        ("""            + (placement.BoreSighted is { } sighted ? $", Bore Sighted on {sighted}" : string.Empty))
""",
         """            + (placement.BoreSighted is { } sighted ? $", Bore Sighted on {PlaceOf(sighted)}" : string.Empty))
"""),
        # UI 4, 5: the A7.72 read once per revision and view, and not while setup is open.
        ("""            .Concat(state.Encircled(unit) ? ["Encircled (A7.7)"] : Live.Planner.UpperLevelEncircled(state, unit) ? ["Encircled: no path to ground level (A7.72)"] : [])
""",
         """            .Concat(state.Encircled(unit) ? ["Encircled (A7.7)"] : CutOffUpstairs.Contains(unit.Id) ? ["Encircled: no path to ground level (A7.72)"] : [])
"""),
        ("""    private Task ProposeSimple(ActionDescriptor action, string kind) => Propose(action,
""",
         """    private (long Revision, string View, HashSet<string> Ids)? cutOffCache;

    /// <summary>
    /// The visible units Encircled by A7.72, read once for a revision and a view: the search walks the map for each unit upstairs. Not read while
    /// setup is open, when a view does not yet hold every unconcealed enemy unit the rule counts.
    /// </summary>
    private HashSet<string> CutOffUpstairs
    {
        get
        {
            if (state is null || history is null || !state.SetupClosed)
            {
                return [];
            }

            if (cutOffCache is not { } kept || kept.Revision != history.Events.Count || kept.View != viewAs)
            {
                cutOffCache = (history.Events.Count, viewAs, [.. VisibleUnits.Where(unit => state.Location(unit.Id)?.Location is { Level: > 0 }
                    && Live.Planner.UpperLevelEncircled(state, unit)).Select(unit => unit.Id)]);
            }

            return cutOffCache.Value.Ids;
        }
    }

    private Task ProposeSimple(ActionDescriptor action, string kind) => Propose(action,
"""),
        # UI 7: an AAMG at another level of its hex is not fired; the row says so.
        ("""rows.Add(new RangeRow(unit.Id, $"the AAMG of {Words(unit.Id)}", at.ToString(), mg.NormalRange, mg.Limit, FireRange.Words(mg), mg.Band,""",
         """rows.Add(new RangeRow(unit.Id, $"the AAMG of {Words(unit.Id)}", at.ToString(), mg.NormalRange, mg.Limit,
                    mg.Band == FireRangeBand.SameHexOtherLevel ? "no attack at another level of its hex" : FireRange.Words(mg), mg.Band,"""),
        # UI 8: two Locations of one hex differ by their levels, with or without the map's layers.
        ("""    private int LevelAbove(BoardLocation from, BoardLocation to) => MapLayers?.Board is { } board && GameMaps.FactsAt(board, from) is { } one && GameMaps.FactsAt(board, to) is { } two
        ? two.BaseLevel + to.Level - (one.BaseLevel + from.Level) : 0;
""",
         """    private int LevelAbove(BoardLocation from, BoardLocation to) => from.Board == to.Board && from.Hex == to.Hex ? to.Level - from.Level
        : MapLayers?.Board is { } board && GameMaps.FactsAt(board, from) is { } one && GameMaps.FactsAt(board, to) is { } two
            ? two.BaseLevel + to.Level - (one.BaseLevel + from.Level) : 0;
"""),
        # UI 9, table player 5: one weapon or several, and the offer announced.
        ("""                            <span class="toolbar-note">@string.Join(", ", forfeit.Weapons.Select(id => Words(id))) kept its Multiple ROF, so its FP left no Residual FP. Give the ROF up now, before anything else, and @forfeit.Fp Residual FP stands in @PlaceOf(forfeit.At); or go on, and it may fire again (A8.23).</span>
                            <button id="propose-forfeit-rof" @onclick='@(() => ProposeSimple(GameActions.ForfeitRof, "forfeit-rof"))'>Propose: give up the ROF for @forfeit.Fp Residual FP</button>
""",
         """                            <span class="toolbar-note" id="forfeit-rof-note" role="status">@string.Join(", ", forfeit.Weapons.Select(id => Words(id))) @(forfeit.Weapons.Count == 1 ? "kept its Multiple ROF, so its FP" : "each kept their Multiple ROF, so their FP") left no Residual FP. Give the ROF up now, before anything else, and @forfeit.Fp Residual FP stands in @PlaceOf(forfeit.At); or go on, and @(forfeit.Weapons.Count == 1 ? "it" : "they") may fire again (A8.23).</span>
                            <button id="propose-forfeit-rof" aria-describedby="forfeit-rof-note" @onclick='@(() => ProposeSimple(GameActions.ForfeitRof, "forfeit-rof"))'>Propose: give up the ROF for @forfeit.Fp Residual FP</button>
"""),
    ],
    S + "Components/Play/SmallArmsFirePanel.razor": [
        ("""        <label title="A7.34, A9.5: MGs, and squads whose Range is underscored (to three hexes), attack a second Location that shares a hexside with the target, or the next level of its building hex, as Area Fire on the same DR. Every member of the group must be capable of it."></label> <LocationField Id="fire-spray" Label="Spray also" Name="the Location sprayed" Value="@Spray" OnChange="@OnSpray" />
""",
         """        <LocationField Id="fire-spray" Label="Spray also" Name="the Location sprayed" Value="@Spray" OnChange="@OnSpray"
            Title="A7.34, A9.5: MGs, and squads whose Range is underscored (to three hexes), attack a second Location that shares a hexside with the target, or the next level of its building hex, as Area Fire on the same DR. Every member of the group must be capable of it." />
"""),
    ],
    S + "Services/FireText.cs": [
        ("""        return weapon.Malfunctioned ? $"{weapon.EquipmentId} malfunctioned (A9.7)"
""",
         """        // A.11, A9.7 (pass 45): a weapon whose lowered B# became an X# is removed, not malfunctioned.
        return weapon.Removed == true ? $"{weapon.EquipmentId} is removed: the DR reached its X# (A.11, A9.7)"
            : weapon.Malfunctioned ? $"{weapon.EquipmentId} malfunctioned (A9.7)"
"""),
    ],
    S + "Services/DisplayText.cs": [
        ("""        ["ffnam"] = "FFNAM",
        ["ffmo"] = "FFMO",
""",
         """        ["ffnam"] = "FFNAM",
        ["ffmo"] = "FFMO",
        ["bore-sighting"] = "Bore Sighting",
        ["mandatory-fire-direction"] = "a MG's target at 17 hexes or more is treated as concealed",
"""),
        ("""        ["fire-ift"] = "IFT DR",
""",
         """        ["fire-ift"] = "IFT DR",
        ["fire-cower-selection"] = "Random Selection dr for Cowering",
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:90]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
