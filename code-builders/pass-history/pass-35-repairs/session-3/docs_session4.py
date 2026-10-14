"""Pass 35, fourth session: the documents that waited on the reviews. The design's section 19 and status, the inventory's notes for the
rules the fixes touched, the backlog's section 55.2 rows, and the lessons. Run once; each anchor is asserted."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"


def read(name):
    return open(ROOT + name, encoding="utf-8", newline="").read()


def write(name, text):
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)


# ---- The design: the status line's end, and section 19.
DESIGN = "Passes/ASL Unit Backlog Pass 35 Design.md"
text = read(DESIGN)
assert "## 19. As built" not in text
lines = text.split("\n")
status = next(i for i, line in enumerate(lines) if line.startswith("**Status:**"))
lines[status] = lines[status].rstrip() + (" **Fourth session (2026-10-09 and 10):** the five reviews, their fixes in seven commits (d27713d2 to 39d5045c), "
                                          "the Studio check of the whole pass, and the documents; section 19 is the pass as built. The gate is the user's to start.")
text = "\n".join(lines).rstrip("\n") + """

## 19. As built

Written on 2026-10-10, after the reviews, their fixes, and the Studio check. The [review](<Scenario A1 Backlog Pass 35 Review 2026-10-09.md>) has each finding and what was done with it; this section says where the pass as built differs from sections 7 to 13 as designed.

### 19.1 What the reviews changed in the design

| Section | As designed | As built |
|---|---|---|
| 7.2, 7.5 (question 1) | The surrender is taken as the RtPh ends, "the result is the same either way" | The event is still recorded as the phase ends. But a unit that has surrendered by the page before the other side routs (a Disrupted unit, A19.12; an ATTACKER's unit, the example on p. 69) is no longer a Known enemy unit to the other side's rout routes. The result was not the same either way for those routes. Ruling R35.2 |
| 7.4 | A repulsed unit left ADJACENT to its enemy surrenders when a captor can take it | It is eliminated and never surrenders (A10.533's example). Ruling R35.3 |
| 7.4 | Height Advantage keeps a unit on a hill from Interdiction | Not when the unit enters the hill hex across the Crest Line hexside the enemy's LOS crosses (B1.14, B10.31). The cover read takes the Location the step comes from; a unit whose only way out is such a climb is trapped and surrenders |
| 7.4 | A lone SMC has his own range alone to Interdict at | A lone leader does; a lone hero has his weapons' range (A15.23) |
| 7.4 (ADJACENT) | "No step the game admits is one-way" | A marsh hex is never entered in the APh (B16.4), so the step read for ADJACENT is an advance: two marsh hexes are not ADJACENT. A marsh hex and dry ground are, by the step out; that is still the "either direction" reading |
| 9 (35.5) | SMOKE doubled with the COT one level up, in an entry | In a Bypass too (B.2) |
| 10 (35.11) | An Acquisition is lost when its firer leaves its Location or its Gun turns in place without firing | Also when a vehicle changes its VCA with its TCA not kept apart from it (C6.5). A TCA kept apart is not yet read |
| 11 (35.13 a) | The exit pays the road rate "as an entry does" | It now does in Mud as well: an unpaved road in Mud pays the Open Ground cost and the Mud MP (E3.6). The road rate and the Mud test are one Rules member each |
| 11 (35.13 j) | No Case J against a vehicle whose first MP of the MPh was its Bog Removal | No Case J against one that began its MPh bogged and has entered no hex since, whatever its MP went on, such as the unloading 35.13 f allows (D8.4, C.8) |
| 11 (Non-Stopped) | A reading with no sentence of its own | Ruling R35.9 cites C.8 and records it as a reading |
| 5.3, item 7 | The rout's MF allotment merged | It was not merged in the build; it is now one Rules member, `RoutHalfMf` |
| 5.3, item 15 | The road rate of an exit merged with an entry's | It was copied, not merged; it is now one member, `RoadHalfMp`, with `MudTurnsRoadOff` |
| 13.1 (35.15) | The refusal names the kinds | By the vocabulary's labels ("Wire", "Fortified Building Location"), and it no longer cites pass numbers |

Found in the Studio during the fixes and repaired: a Recalled AFV was offered an exit by an edge that is not its Friendly Board Edge, and the gate refused it; it is now offered only that edge (D5.341).

### 19.2 What the reviews changed in the Studio

`Play.razor`: the thrown DC's draft is cleared with the fire drafts and by its own commit; a committed shot drops a target vehicle that is gone, a Spotter or leader that broke, and Intensive Fire; CC Reaction Fire names only units of the hex the vehicle is in; the exit edge and the berserk "keep" text are cleared with the move; another game clears every draft; a change of view clears the editor's "broken" and "off board" boxes; whether a bogged vehicle may unload is Rules' answer; where a rout may end is Rules' sentence. `PlayRecords.cs`: the Wound Severity record reads the man and the outcome from what the same action did next. `ReplaySteps.cs`: a rout's title prefers its repulsed step. `DisplayText.cs`: "Stun +1" for the vehicle Close Combat's DRM.

### 19.3 The Studio games, as their files stand

The table player found that sections 7 to 12 cite a Studio game for repairs whose file holds only its setup. That is so, and for two reasons the sections did not give: a game was often proposed and read without being confirmed, and a played game was sometimes reset to its setup so that it could be shown again (a played copy of `p35-repulse` was kept in the tools folder). A proposal's review shows the rule at work, but it leaves nothing in the file. Of the 69 `p35-*` games in the live folder on 2026-10-10, 41 hold played events and 28 hold their setup alone.

**Hold played events:** p35-acquire, p35-bff-bog, p35-bog-mired, p35-bog-unload, p35-bog2, p35-bypass, p35-cc-counter, p35-cc-panel, p35-cc-panel2, p35-check-narrow, p35-check-wide, p35-crest, p35-dry-exit, p35-grain-jul, p35-grain-nov, p35-harden, p35-hidden-viewer, p35-interdict-fanatic, p35-interdict-hero, p35-interdict-leader2, p35-mud-exit, p35-night, p35-ord-draft, p35-ordnance, p35-rally-decline, p35-rally-fanatic, p35-rally-plain, p35-rally-take, p35-recall-stop, p35-recombine, p35-repulse, p35-rout, p35-rst-cc, p35-rst-ovr, p35-seen-viewer, p35-smoke-bypass-up, p35-surrender-first, p35-vca-acquire, p35-vca-keep, p35-wound-again, p35-wound.

**Hold their setup alone** (read through proposals, or controls that were proposed and cancelled): p35-berserk-deploy, p35-bog-recall, p35-bog, p35-bu-road, p35-crest2, p35-day, p35-dry, p35-esb-german, p35-esb-russian, p35-interdict-between, p35-interdict-commissar, p35-interdict-leader, p35-interdict-smoke, p35-interdict-squad, p35-interdict-trap, p35-interdict-wreck, p35-levels, p35-mud, p35-night0, p35-own-hex2, p35-own-hex3, p35-rally-enemy-turn, p35-recall-edge, p35-smoke-bypass, p35-surrender-later, p35-smoke-hill, p35-tow2, p35-wire.

So these repairs have no recorded event in any stored game, beyond those the sections already name as having no game: 35.17 in the p35 games, ADJACENT, the Commissar, the lone leader and the wreck and SMOKE variants of Interdiction, the uphill SMOKE of an entry, the towing bars, the ATR's Case L, the own-hex Hindrance, the road rate in snow, the Non-Stopped fact under a shot, the CE and Bounding First Fire bars, the ESB table, the Recall's ESB bar, and the night and weather MP of 35.14. Each was seen in a proposal in the session that built it. Six labels promise a shot or a step their events do not hold (p35-ordnance, p35-own-hex2, p35-night, p35-bypass, p35-bff-bog, p35-bog-mired); they are left as written, and this section is the correction. No game was played again for this.

The games of the fourth session, each written by a script kept in `pass32-tools/pass35/session3/`: p35-crest and p35-crest2 (fix 2), p35-bog-unload (fix 3), p35-dry-exit and p35-mud-exit (fix 4), p35-vca-acquire and p35-vca-keep (fix 5), p35-interdict-hero and p35-interdict-leader2 (fix 6), p35-surrender-first and p35-surrender-later (fix 7), p35-smoke-bypass and p35-smoke-bypass-up (fix 10), p35-recall-edge, p35-ord-draft, and p35-check-wide and p35-check-narrow (the Studio check). Fix 1 was played in p35-repulse.

### 19.4 The estimate

Section 17 gave 6:53 by the clock through section 13. The fourth session's rows in the time log come to 1:34 by the clock: 0:13 for the five reviews and their report, 0:47 for fixes 1 to 6 with a stop before each commit, 0:23 for fixes 7 to 26 in one run, and 0:11 for the Studio check. The clock is a floor throughout.
"""
write(DESIGN, text)
print("design ok")

# ---- The inventory: a note on each rule a fix touched, appended to the row's last cell.
INVENTORY = "ASL Rule Inventory A to E.md"
NOTES = {
    "A15.23": "Pass 35 review (2026-10-09): a hero alone with a MG Interdicts at the MG's range, since his MG fires at full FP (ScenarioA1RoutCalculator.InterdictionRange).",
    "A19.12": "Pass 35 review (2026-10-10): for the other side's rout routes a Disrupted unit bound to surrender is a prisoner from the start of the RtPh (ScenarioA1RoutCalculator.SurrenderedBeforeTheOtherSideRouts; ruling R35.2); its surrender's sentence cites this rule.",
    "A20.21": "Pass 35 review (2026-10-10): an ATTACKER's unit bound to surrender is no enemy to the DEFENDER's rout routes (ruling R35.2), and a unit whose only way out is a climb across the Crest Line its enemy's LOS crosses is trapped.",
    "B.2": "Pass 35 review (2026-10-10): the doubling holds in a Bypass one level up as well (ScenarioA1TerrainCosts.BypassStep).",
    "B1.14": "Pass 35 review (2026-10-09): the exception is built for a rout: a unit entering the hill hex across the Crest Line hexside the enemy's LOS crosses has no Height Advantage and is Interdicted (the rout's cover read takes the Location the step comes from; BacklogPass35Tests.ARoutUpAHillIsInterdictedOnlyAcrossTheCrestLineTheLosCrosses).",
    "B16.4": "Pass 35 review (2026-10-10): the APh bar is read for ADJACENT, so two marsh hexes are not ADJACENT (GamePlanner.Map.cs: AdvanceStep).",
    "C6.5": "Pass 35 review (2026-10-09): lost also when a vehicle changes its VCA, its MA's CA with it, without having fired on its target in the phase (ScenarioA1FireFollowUps.AcquiringCaChanged); a TCA kept apart from the VCA is not yet read.",
    "D5.341": "Pass 35 review (2026-10-10): the page offers a Recalled AFV an exit only by its Friendly Board Edge (GamePlanner.Recall.cs: VehicleExits).",
    "D8.4": "Pass 35 review (2026-10-09): no Case J while the vehicle has entered no hex this MPh and is still bogged or has made a Bog Removal attempt, whatever else it spent MP on (ScenarioA1OrdnanceMapRules.InBogHex takes the bogged state).",
    "E3.6": "Pass 35 review (2026-10-09): an exit off the map by an unpaved road in Mud pays the Open Ground cost and the Mud MP, as an entry does (ScenarioA1VehicleTerrainCosts.MudTurnsRoadOff, RoadHalfMp; ScenarioA1RecallCalculator.ExitTerrain).",
}
lines = read(INVENTORY).split("\n")
for rule, note in NOTES.items():
    hits = [i for i, line in enumerate(lines) if line.startswith(f"| {rule} |")]
    assert len(hits) == 1, (rule, len(hits))
    row = lines[hits[0]].rstrip()
    assert row.endswith("|"), rule
    body = row[:-1].rstrip()
    lines[hits[0]] = body + " " + note + " |"
write(INVENTORY, "\n".join(lines))
print("inventory ok", len(NOTES))

# ---- The backlog: rows appended to section 55.2's table.
BACKLOG = "ASL Unit Backlog.md"
BY = "Pass 35 reviews, 2026-10-09"
ROWS = [
    ("Case J against a vehicle that has paid only its Start MP, or only unloaded, in a hex", "C.8, p. 162; D2.51, p. 198; C6.1", "A moving-target fact (entered a hex or used VBM this Player Turn, or began in Motion) in place of the MP seen", "Case J, J1, or J2 is charged against any vehicle fired on in the MPh, but one still in its Bog hex (fix 3). In the fix 5 control a tank that only Started and Stopped fired with Case B in the AFPh, the same question for the firer."),
    ("A vehicle whose TCA is kept apart from its VCA, when the VCA changes", "C6.5, p. 174; D3.12, p. 199", "Whether a recorded TCA turns with the hull", "It keeps its Acquisition through a hull turn, which errs toward keeping."),
    ("An Acquisition counter shown on the map or in a list", "C6.5", "A map layer or a line in the unit list", "The page shows none; the loss or the gain is seen only in a shot's DRM and record."),
    ("A lone leader's Support Weapons other than a MG, for Interdiction", "A10.532, p. 68 (EXC: mortars); A9.12", "The SW's kind as a fact", "A lone leader has no range to Interdict at, whatever he holds."),
    ("A Disrupted unit's Low Crawl under No Quarter", "A20.3, p. 87; A19.12", "A No Quarter fact to the Low Crawl bar", "Refused Low Crawl always, though A20.3 lets it Low Crawl to avoid surrender."),
    ("A unit pinned by Interdiction while still ADJACENT", "A10.53, p. 67; A20.21 EXC", "A pinned fact to the surrender verdict", "It surrenders where the rule eliminates it. Older than the pass."),
    ("The repulse's Random Selection leaves Dummies out, so its dice count tells how many real units are there", "A.9, p. 43; A10.533", "Ruling R10.11 read again", "As an ordinary entry draws; kept."),
    ("ADJACENT between a marsh hex and dry ground", "A.8; B16.4", "A ruling on \"either direction\"", "ADJACENT, by the step out of the marsh."),
    ("The marsh repairs in Ground or Deep Snow, the level clause of the Bog Check beside a marsh, and a road hexside in Mud", "B16.8, B16.43, p. 130", "Marsh in Snow; the level and the road as facts to EntryCost", "The Bog Check and the halving apply in Snow; level -2 and lower counts; an unpaved road in Mud is not read as a road hexside. Board 3 has no marsh."),
    ("The step bar of a TI vehicle in two places", "C10.11, D8.5", "One Rules member read by MoveBar and the projector (pass 50)", "The projector keeps its own list with the same exemption."),
    ("The projector's first-expenditure fact to BoggedMaySpend", "D8.3, D8.5", "The projector's own step count", "A literal; latent, since a failed Bog Removal ends the move."),
    ("The \"touch\" of a Bypassed hexside, and which counters hinder within a Gun's own hex, decided in Play", "D9.4, p. 210; C5.5, p. 172", "The facts handed to Rules; the two hexes consecutive on the LOS", "A hex approximation in Play's lambda, one case too many and one too few; Rules receives a bool."),
    ("The rout cover reader's side, and a wall whose TEM fire cannot decide", "A10.531; B9.3", "The routing side as an argument; a null TEM that Rules resolves", "The side is the one last asked for its enemies; an undecided wall gives none. No wrong result is known."),
    ("The Wound Severity dr of a PF's firer decided by exclusion in Play; two small compositions in Play (the Recall Abandonment's word, the pushed Gun)", "A17.11; D5.341; C6.6", "The plain facts handed to Rules", "Right today; a new firer effect would roll a dr wrongly."),
    ("EnemyUnitFacts.Broken, a fact no Rules member reads", "(none)", "Its removal with its gathering and one test helper", "Gathered and unread since the scans moved to Good Order."),
    ("The refusal \"No cc armament\" of a CE tank with a Restricted Slow Traverse turret", "D1.321, p. 194; A11.62", "Words for the verifier's reason codes", "The code is shown with its hyphens as spaces."),
    ("Why a broken Morale Level is what it is, in an NMC record", "A10.8; A17.3", "The unit's Fanatic and wounded state read by the record", "\"against broken morale 8\" with no reason."),
    ("The unit and the way forward in the CC counter refusal of Reaction Fire", "D7.21, p. 207", "The unit's id as a parameter", "The refusal names neither."),
    ("A record sentence for a resolved vehicle Close Combat", "A11.5 to A11.62", "A reader of the event in PlayRecords and ReplaySteps", "Its FP, odds, Kill Number, and DRM reach a player only in the proposal's review. Older than the pass."),
    ("The Gun's target list offers a Location the Gun has no LOS to; the refusal reads \"Vehicle fire outside\"", "A6; C3", "The list read through the LOS", "Seen in the Studio check: [H10] offered to the tank in [E10], and refused by the gate. Older than the pass."),
    ("The phasing side's view in the DFPh, the \"Latest\" line through phase changes, and the hand-over prompt at 320 pixels", "(page)", "A sentence beside the disabled button; the line's source; the prompt's place", "Seen in the Studio check: the button to end the DFPh is only disabled; \"Latest\" did not follow five phase changes; the prompt lies over the foot of the Activity list."),
    ("The view sweep test's gaps, and a possible flake in the map layer reads", "(tests)", "A positive control, a proposal, records, a hand-over; a wait on the viewport's own push", "NoPhaseOfThePageNamesAUnitTheViewDoesNotHold makes no proposal and reads no record; MapLayer returns the last push of any view."),
    ("The \"With SMC\" list of the Close Combat panel offers leaders that are broken, captured, or have attacked", "A11", "The attacker list's filter", "Offered; the gate refuses. Not in the pass's diff."),
    ("Play tests for review fixes 3 to 6, and Studio games for the thrown DC's draft, the CC Reaction Fire binding, the \"Stun +1\" words, and a repulse's Replay title", "(tests)", "Test helpers for a bogged vehicle with a Passenger under a Gun, an exit by road, a tank's Acquisition; a game that holds a DC", "The Studio games' records stand for fixes 3 to 6; the four page changes rest on the build and on reading."),
]
lines = read(BACKLOG).split("\n")
anchor = [i for i, line in enumerate(lines) if line.startswith("| The Armor Test card played through the Play page")]
assert len(anchor) == 1, len(anchor)
assert not any("Case J against a vehicle that has paid only its Start MP" in line for line in lines)
new = [f"| {item} | {BY} | {rules} | {needs} | {now} |" for item, rules, needs, now in ROWS]
lines[anchor[0] + 1:anchor[0] + 1] = new
write(BACKLOG, "\n".join(lines))
print("backlog ok", len(new))

# ---- The standing rules: the fourth session's lessons.
RULES_DOC = "Prompts/Pass Standing Rules and Harness Lessons.md"
text = read(RULES_DOC)
assert "pass 35, fourth session" not in text
LESSONS = """- **The reviews of a rule pass (pass 35, fourth session):** five read-only agents in one message, the referee split in two by chapter, each pointed at its own section of the session's prompt for its pages and members, came back in five to ten minutes. Read the lines and the page behind each finding before reporting it: the one graded a blocker was right, by an example on the page that the ruling had not weighed. A rule's example is part of the rule; read it when a ruling is written.
- **A new test that fails may be showing the rule.** The first Play test of the Crest Line fix failed because the fix also made the unit trapped, which the page's example gives; it became a test of its own. Read a failure against the page before changing the test.
- **A proposal read and cancelled leaves nothing in the game's file.** Confirm what a game is meant to show, and keep a control beside it under its own id, or the design cites a game that holds only its setup (41 of 69 `p35-*` games hold played events). Say in the design which games are recorded.
- **Board 3, more (pass 35, fourth session):** its only road is Dirt Road, which crosses the board's edge at [Q1], [Q10], [Y1], and [Y10]; [I10] is a wooden building on a level 1 hill beside the woods of [H10], which sees [D10], [C10], [B10], and [A10] clear at ranges 4 to 7; [C9] is woods beside [D9] and [C10]. The board viewer's terrain layer answers "which edge hexes hold this terrain" by `isPointInFill` on the layer's paths at each label's point. A Recalled AFV leaves only by its Friendly Board Edge, the left for the German side.
- **Driving the Play page, more:** keep the helpers as one string in `sessionStorage` and `eval` it at the top of each script. A script of more than four actions with a change of view between them passes the 45 second limit; it still runs to its end, so read the page before sending the next. The scripted dice queue carries over from one proposal to the next: a shot takes the dice an earlier script left. The phasing side's view cannot end the DFPh. The Gun's target list may offer a Location with no LOS. No live game holds a DC. A kind's label is `vocabulary.TryGetKind(kind, out var known) ? known.Label : kind`.
- **When the user says to build all the fixes and ask before the commit,** fixes that share files go in one commit with each listed in its message; one commit a fix holds only while each stops for the user's word.
"""
write(RULES_DOC, text.rstrip("\n") + "\n" + LESSONS)
print("lessons ok")
