"""Pass 35 review fix 3: no Case J against a vehicle that began its MPh bogged and has not left its Bog hex, whatever it spends MP on
(D8.4, p. 209; C.8, p. 162). Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceMapRules.cs": [(
        """    /// D8.4 (p. 209; pass 35, task 35.13 j): whether a vehicle began its MPh bogged and has not left its Bog hex: its first MP expenditure of the MPh
    /// was Bog Removal and it has entered no hex since. The steps are this MPh's, oldest first.
    /// </summary>
    public static bool? InBogHex(IReadOnlyList<(string Kind, bool BogRemoval)> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Count > 0 && steps[0] is ("start", true) && !steps.Any(step => step.Kind is "enter" or "exit") ? true : null;
    }""",
        """    /// D8.4 (p. 209; pass 35, task 35.13 j): whether a vehicle began its MPh bogged and has not left its Bog hex: it has entered no hex this MPh,
    /// and it is still bogged or has made a Bog Removal attempt. Its MP may have gone on anything else first, such as unloading (C.8, p. 162; D8.5).
    /// The steps are this MPh's, oldest first.
    /// </summary>
    public static bool? InBogHex(IReadOnlyList<(string Kind, bool BogRemoval)> steps, bool bogged = false)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Count > 0 && (bogged || steps.Any(step => step is ("start", true))) && !steps.Any(step => step.Kind is "enter" or "exit" or "overrun") ? true : null;
    }"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1ArmorCalculator.cs": [(
        """        if (shot.Movement?.InBogHex == true)
        {
        }
        else if (shot.FireKind is not null && shot.Movement?.MpInLos is { } seen)""",
        """        if (shot.Movement?.InBogHex != true && shot.FireKind is not null && shot.Movement?.MpInLos is { } seen)"""),
        ("""        else if (target.Moving == true)
        {
            drm.Add(new FireModifier("case-j", 2, "C6.1"));""",
         """        else if (shot.Movement?.InBogHex != true && target.Moving == true)
        {
            drm.Add(new FireModifier("case-j", 2, "C6.1"));"""),
    ],
    "LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs": [(
        """.Where(step => step.Vehicle == moving).Select(step => (step.Kind, step.BogRemoval))]),""",
        """.Where(step => step.Vehicle == moving).Select(step => (step.Kind, step.BogRemoval))],
                            state.Find(moving) is UnitInstance bogged && Is(bogged, Conditions.Bogged)),"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass35RulesTests.cs": [(
        """        Assert.Null(ScenarioA1OrdnanceMapRules.InBogHex([]));
""",
        """        Assert.Null(ScenarioA1OrdnanceMapRules.InBogHex([]));

        // The same when its first MP went on unloading (D8.5) and it is still bogged, or it unloaded and then made its Bog Removal attempt (C.8,
        // p. 162): neither makes it a moving target. One that bogged on entering a hex this MPh is a moving target.
        Assert.True(ScenarioA1OrdnanceMapRules.InBogHex([("unload", false)], bogged: true));
        Assert.True(ScenarioA1OrdnanceMapRules.InBogHex([("unload", false), ("start", true)]));
        Assert.Null(ScenarioA1OrdnanceMapRules.InBogHex([("unload", false)]));
        Assert.Null(ScenarioA1OrdnanceMapRules.InBogHex([("start", false), ("enter", false)], bogged: true));
        Assert.Null(ScenarioA1OrdnanceMapRules.InBogHex([], bogged: true));
"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
