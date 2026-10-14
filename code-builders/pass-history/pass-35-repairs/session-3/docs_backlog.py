"""Pass 35 documents: the backlog's section 55.2, and a closing note on six older rows the pass answered."""
P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Backlog.md"
text = open(P, encoding="utf-8", newline="").read()
nl = "\r\n" if "\r\n" in text else "\n"
lines = text.split(nl)
assert "### 55.2" not in text
D = "Pass 35, 2026-10-09"

ROWS = [
    # The Rout Phase and Infantry (the design's sections 7 and 8)
    ("A concealed unit giving up its \"?\" by choice to Interdict a rout or to change its route", D + " (design 7.4; ruling R35.3)", "A10.533, p. 68", "An owner's choice in the other side's RtPh", "Concealed units neither Interdict nor bar a route; the repulse on entry is built."),
    ("The shellhole and entrenchment cases of Open Ground for a rout, and the crew of an Emplaced Gun", D + " (ruling R35.1)", "A10.531, p. 67", "Pass 100 (entrenchments); pass 135 (shellholes)", "Read as the terrain's other rules have them; neither is admitted terrain today."),
    ("A LLMC of the units with a leader lost to a mortal wound outside fire: in a rout's Interdiction, a Mopping Up casualty, a PF firer's Casualty Reduction", D + " (design 8.1; question 8.4 (4))", "A10.2, A17.11", "Pass 70 (morale)", "The leader is lost and no LLMC is made, as in the paths that already rolled the dr."),
    ("The TI rules for the SW and Deployment actions: whether a TI unit, which A.7 calls Good Order, may Deploy, Recombine, Transfer, Recover, or Drop", D + " (question 8.4 (1); ruling R35.5)", "A.7, p. 43; A1.31, A1.32, A4.43, A4.44", "The TI rules read for each action (pass 45)", "The bar on a TI unit is kept, under its own name (`FreeToActAsPlanned`)."),
    ("Play tests of the Wound Severity dr in the Mopping Up and PF paths", D + " (design 8.1)", "A17.11, p. 85", "A fixture for each path", "Both call the Rules verdicts the rout's Play test reaches; tested in Rules."),
    ("A Studio game of the Morale ceiling", D + " (design 8.2)", "A.18, p. 44", "A game with a berserk Fanatic unit under fire", "Tested in Rules only."),
    ("A25.211: before 1941 a Russian Conscript squad and HS harden to the 4-4-7 and 2-3-7 unless the OB holds a 6-2-8 or a 5-2-7", D + " (question 8.4 (3))", "A25.211, p. 93", "The scenario's year and the Russian OB as facts of the hardening; pass 80", "A 4-2-6 hardens to the 5-2-7 in every year. No card in the library is that early."),
    ("The seven differing copies pass 32 kept apart that this pass did not take: overstacking counted four ways (pass 40); Inexperience decided twice and Cowering's exemptions against the calculator's (pass 45); the CE test of a vehicle MG's repair (pass 50); \"Stunned\" grouping Recalled, Shocked, and Unconfirmed Kill three ways (pass 65); the effects of capture in three copies and who may guard prisoners in five tests (pass 75)", D + " (design 5.3; the pass 32 design, section 12, items 4, 8, 9, 10, 12, 13, 14)", "A5; A19.3, A7.9; D3.7; D5.34; A20", "Each named pass", "Each copy stays its own function, as pass 32 left it. Items 1, 2, 3, 5, 6, 7, 11, and 15 are settled by this pass."),
    # Terrain
    ("The orchard's season in the LOS", D + " (design section 9; the user's answer)", "B14.2, p. 129", "Pass 135 (task 135.2): a second LOS map for each board, by season", "Every orchard is in season; some out-of-season LOS cases are refused; fire leaves any orchard Hindrance undecided. The inventory's row moved to pass 135."),
    ("A Studio game of HE into a marsh, of a Bog Check beside a marsh, and of a vehicle in an orchard", D + " (design section 9)", "B16.31, B16.43, B14.4", "A board with a marsh and an orchard in the Studio's library", "Board 3 has neither; each is tested in Rules."),
    # Ordnance
    ("While towing: Deep Stream, wagons, the 76 to 107mm MTR exception, and Narrow Streets", D + " (design section 10)", "C10.1, p. 180", "Pass 110", "Not built; the bars on Bypass, a wall or hedge, and rubble are."),
    ("Ordnance DRM left out: 360-degree mounts and bow-mounted Guns (Case A); a vehicle as firer or target within a hex, Bypass and the CAFP (Case E); Desperation fire; Intensive Fire by an Opportunity Firer", D + " (design section 10)", "C5.1, C5.5, C13.81, A7.25", "Passes 105 and 110; pass 45 for A7.25", "Refused or not offered."),
    ("An Acquisition's other losses (a CMG's or a CC attack on another target, the crew's Inherent FP, SW, or Interdiction, limbering) and the Gyrostabilizer", D + " (design section 10)", "C6.5, C6.55, p. 174", "Pass 110", "The counter is kept through each of these."),
    ("A hit on a Location that holds both Infantry and a vehicle: the gate refuses an Infantry Target Type shot at any Location holding a vehicle, friend or foe, so the in-hex AFV Hindrance of Case E cannot be reached in play", D + " (design section 10; the user's answer)", "C3.31, C5.5; rulings R25.10, R7.2", "The mixed hit (pass 60)", "Such a shot is refused; `OwnHexHindrance` is tested in Rules for a live AFV."),
    # Vehicles
    ("Bypass and cover: a building Bypassed inside a woods hex, the LOS to the CAFP, the entrenched and Dug-In cases, and a concealed AFV's reveal", D + " (design section 11)", "D2.37, D2.38, D9.4", "Pass 100", "The hex-center LOS of ruling R11.2; the hexes-passed-through reading of R35.8."),
    ("The Stun and the crew: the crew counter's +1 after a bail-out, Stun by a Sniper, a MG TK DR, or Falling Rubble, a 1MT AFV's Recall on a Stun; whether a CE RST tank's CMG still counts as a manned MG against the CC DRM", D + " (design section 11)", "D5.34, D1.322, A11.51", "Passes 65 and 105", "Not built."),
    ("A bogged or Recalled vehicle: its other non-movement expenditures and the bow MG limits; \"as soon as possible\" for a Recalled AFV's unloading, the crew's bar on re-crewing, Armor Leader loss", D + " (design section 11)", "D8.5, D5.341", "Pass 105", "A bogged vehicle loads and unloads; a Recalled AFV carrying Passengers may Stop to unload them."),
    ("CC Reaction Fire: the Unarmed bar, Cavalry, and the CC counter on SW and Guns; Passengers CE or BU with the crew, and fire by the PRC; a vehicle in another nation's service for ESB", D + " (design section 11)", "D7.21, D5.33, D2.5", "Passes 60 and 65", "Not built."),
    ("A tank freed by its Bog Removal is treated as Non-Stopped from then on: a reading with no sentence of its own", D + " (design section 11; ruling R35.9)", "D2.13, D8.3, D8.4, C6.3", "The referee's reading", "Case L is denied against it, as against any Non-Stopped vehicle. A record made under another reading fails its check, as `p35-acquire` shows (design 14.3)."),
    # Night and weather
    ("The other Open Ground of Mud (gullies, dry streams, plowed fields, shellholes, trenches, unplowed roads in Deep Snow); a Recall exit by an unpaved road in Mud paying the Open Ground COT; Straying", D + " (design section 12)", "E3.65, E3.64, E1.53", "Passes 90, 95, and the terrain passes", "The open-ground key and an unpaved road count; the Recall exit pays the road rate; Straying is untouched."),
    ("Rules tests of the Infantry Bypass step in Mud, and Studio games of an unpaved road through other terrain and through a wall's gap in Mud", D + " (design section 12)", "E3.9, E3.64, B9.4", "A board with such a road", "Shown in the Studio for Open Ground (`p35-mud`, `p35-dry`); the two road cases rest on Rules tests."),
    # The page and the tests
    ("The sweep of Play reads phase ends only: a list that appears in the middle of a move, a rout, or a Close Combat is not swept", D + " (design 13.2)", "Rulings R23.1 to R23.6", "A sweep that takes one action of each kind in each phase", "`PlayPagePass35Tests.NoPhaseOfThePageNamesAUnitTheViewDoesNotHold` sweeps sixteen phases as opened, with every firer ticked, and every tab."),
    ("The sweep finds an identifier, not a leak in a player's words: a tag or a strength said of a unit under \"?\"", D + " (design 13.2)", "Ruling R31c.1", "A check of every name the page writes against the view's own names", "`UnitNamesTests` checks the played game's records; the page's lists are checked for identifiers alone."),
    ("The page offers a pass in a second Location holding a vehicle while another Location's Close Combat is open, and the planner refuses it", D + " (design 13.4)", "A11.12, A11.31", "The panel reading which Location is open", "\"Another Location's CC is open; one Location at a time (A11.12)\"."),
    ("A shot with no LOS draws its IFT DR, and its card reads \"Total 0 FP: the no column\"", D + " (design 13.4)", "A6.11", "The forfeit attack's record without a DR", "The record's own line is right: \"the LOS is blocked, so the attack has no effect and its firers have fired (A6.11)\"."),
    ("A page test of a draft counter in a game set up by hand; a board with a LOS obstacle other than a hill in the page tests", D + " (design 13.2, 13.4)", "None", "A minimal card that enters setup mode; a synthetic catalog whose woods and buildings have height", "The tooltip test uses a card with an order of battle; `WideBoards` has a level 2 hill."),
    ("The proof store is a copy made before pass 35: it holds none of the pass's games and none of the user's games since", D + " (design 14.2)", "None", "The gate: the store brought up to date before the baseline is built", "The replay digest over it is identical to main's; the 210 live games replay but for `p35-acquire`."),
    ("The Armor Test card played through the Play page as the table player's review of the vehicle repairs", D + " (design 5.4, item 7)", "The plan's section 22.1 (h), question 5", "A session for it; a board 4 fixture for the tests", "The vehicle repairs were each shown in a small game on board 3 (design section 11)."),
]

end = len(lines)
while end > 0 and lines[end - 1].strip() == "":
    end -= 1
block = ["", "### 55.2 What the pass left out, and what it found", "",
         "Written with the pass's documents on 2026-10-09. Each row is something a section of the [pass 35 design](<Passes/ASL Unit Backlog Pass 35 Design.md>) lists as left out, or that a test game or a test found beside its task.",
         "", "| Item | Deferred by | Rules | Depends on | What happens now |", "|---|---|---|---|---|"]
block += ["| " + " | ".join(row) + " |" for row in ROWS]
lines[end:end] = block

CLOSE = [
    ("| A voluntary rout by a broken unit that need not rout and is not under DM |", " **Closed by ruling R35.4 (pass 35): the page says \"may rout if currently under DM\", so the refusal stands.**"),
    ("| Fire from a marsh hex, and HE halved into marsh |", " **Pass 35: HE is halved into a marsh (ruling R35.6); fire from a marsh hex stays refused.**"),
    ("| Movement's read of who sees a concealed mover or a Dummy stack:", " **Built in pass 35 (ruling R35.5): the viewer is Good Order as A.7 has it, not hidden, and not a Passenger.**"),
    ("| A concealed unit revealing itself to Interdict or to repulse a routing unit |", " **Pass 35: the repulse on entry is built (ruling R35.3); the reveal by choice is section 55.2's row.**"),
    ("| Tests of pass 31c not written:", " **Written in pass 35 (task 35.16), `PlayPagePass35Tests`.**"),
    ("| Page tests not written in pass 31d:", " **Written in pass 35 (task 35.16), `PlayPagePass35Tests` and `SetupPlansPageTests`.**"),
]
for prefix, note in CLOSE:
    hits = [i for i, line in enumerate(lines) if line.startswith(prefix)]
    assert len(hits) == 1, (prefix, len(hits))
    line = lines[hits[0]].rstrip()
    assert line.endswith("|")
    lines[hits[0]] = line[:-1].rstrip() + note + " |"

intro = [i for i, line in enumerate(lines) if line.startswith("Pass 35 writes its other rows here with its documents")]
assert len(intro) == 1
lines[intro[0]] = "Pass 35 (2026-10-09) repaired wrong results in rules already built. Section 55.1 was added at the user's word after task 35.15; section 55.2 holds the pass's other rows."
open(P, "w", encoding="utf-8", newline="").write(nl.join(lines) + nl)
print("rows:", len(ROWS), "closed:", len(CLOSE))
