"""First task 3 of the Rules-first four-track plan (section 11.2): the Turn Guide later Rules plan's 36 tasks merged into the pass
sections of the redesign plan, and question 9's Trip Flares moved to pass 90.

1. Each Turns task gets one home pass section, as a row of a table "Turns tasks merged here" after the pass's own notes: its Rules part
   (a claim on the board), its wiring part (track 3), and its state on 2026-10-28. Every other pass the task reaches gets a pointer.
2. The inventory: E1.95 to E1.953 move to pass 90 (question 9); the G10 rows move from claim wave 1 to pass 165, D3's pass.
3. The redesign plan: the left-out table loses its Trip Flare entry (5 rows become 1, A25.934); pass 90 counts 47 rows.
4. The later Rules plan keeps its history and acceptance records; its task register becomes a map to the pass sections.

    python merge_turns.py            dry run
    python merge_turns.py --apply    writes the three documents
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402

DOCS = REPO + "/src/ASL/docs/"
REDESIGN = DOCS + "ASL Card Play and Map Studio Redesign Plan.md"
TURNS = DOCS + "Turn.Guide/ASL Turn Guide Later Rules Implementation Plan.md"
TURNS_LINK = "<Turn.Guide/ASL Turn Guide Later Rules Implementation Plan.md>"
D42_LINK = "<Turn.Guide/ASL Turn Guide D4.2 Design.md>"
NONE = "none"

# Task: (home pass, title, Rules part, wiring part, state on 2026-10-28)
TASKS = {
    "D1.1": (110, "C9.3 Spotter timing",
             "Original designation in the owning side's PFPh or DFPh, the loss reasons, replacement at the next owner MPh opening, unknown evidence, the reduced squad's survivor (`TurnSpotterTimingRules`)",
             NONE, "Rules part done (D1.1 accepted). Two calculators decide C9.3 today (`ScenarioA1AreaCalculator`); its claim makes them one"),
    "D1.2": (110, "Spotter designation, loss, and successor",
             "The loss, successor, use, and designation verdicts (`TurnSpotterLossRules`, `TurnSpotterUseRules`, `TurnSpotterDesignationRules`)",
             "Durable designation, loss, replacement, and reduced-squad successor history; governed designation and replacement (setup predesignation, Defensive First Fire activation); Deployment to a chosen HS; concealed and HIP disclosure; several mortars on one Location and target; legacy unknowns",
             "In progress, mostly wiring"),
    "D1.3": (110, "Opportunity Fire and the remaining fire dependencies",
             "Opportunity Fire (A7.25) and its required no-shot breakdown; the artillery admissions (passes 150 and 155) and the vehicle and PRC admissions",
             "The declarations and their commands; the reviewed phase topics", "Not started"),
    "D1.4": (110, "Fire and Spotter acceptance", NONE,
             "Becomes the track 3 acceptance of the PFPh and DFPh: initial and replacement Spotter, failed eligibility, Opportunity Fire carryover, artillery and vehicle consequences, stale, disclosure, replay, the live guide",
             "Not started"),
    "D2.1": (85, "White SMOKE",
             "Creation, strength (A24.4, A24.5, A24.61; `TurnWhiteSmokeRules`), dispersal and removal, owner-relative cleanup",
             "Persisted type, size, and source provenance; legacy unknowns", "In progress (Rules part)"),
    "D2.2": (85, "Drift, Gusts, and the cave WP opening",
             "Drift (A24.61), Gusts (B25.651), the cave WP opening sequence (G11, with Chapter G's wave 2) and its morale and Flame consequences",
             "Authoritative substep evidence", "Not started"),
    "D2.3": (85, "Vehicle, FFE, and ambient dust",
             "Creation, shift, departure, wind, Motion, and cleanup of each kind (FFE with pass 155; ambient dust with Chapter F's wave 2)",
             "Source-specific histories and commands", "Not started"),
    "D2.4": (85, "Atmosphere acceptance", NONE,
             "Becomes track 3 acceptance: ordering, interrupted consequences, historical and disclosed reads, phase close without generic cleanup",
             "Not started"),
    "D3.1": (165, "G10 contracts",
             "G10 eligibility, base duration, night and minimum adjustments, interruption and restart (`TurnPackingRules`, `TurnPackingPeriod`, `TurnPackingVulnerabilityRules`, `TurnPackingCasualtyRules`)",
             NONE, "Rules part done (D3.1 accepted)"),
    "D3.2": (165, "Packing identities and periods",
             "The Horse and Mule identity (A13.32, G10.1; `TurnHorseRules`)",
             "Persisted Gun, Mule, and crew identities and midpoint loads; duration progress, reversal, and restart with the real participants",
             "In progress"),
    "D3.3": (165, "Governed packing and animal transport",
             "Prohibited unload terrain, vulnerability, cumulative Low Ammo, missing-Mule recovery",
             "Governed packing and unpacking and animal transport with assembly, containment, and movement; legacy adjudication records kept",
             "Not started"),
    "D3.4": (165, "Packing acceptance", NONE,
             "Becomes track 3 acceptance: night and minimum, midpoint and reversal, loss of eligibility, transport, Mule loss, replay, disclosure, release timing",
             "Not started"),
    "D4.1": (175, "E10 eligibility",
             "E10 eligibility, caliber and barrel compatibility, Accessible Locations, Immobility (D.7), participants and capacity (`TurnAmmoEligibilityRules`)",
             NONE, "Rules part done (D4.1 accepted)"),
    "D4.2": (175, "Ammo Dumps and shared recipients",
             "Simultaneous recipients, Ammo Dump setup, use, and replenishment, the SSR and DYO facts",
             "Transfer and offload of occupants and towed equipment",
             f"Designed, not approved: the [D4.2 design]({D42_LINK}) (Codex, 2026-10-28)"),
    "D4.3": (175, "Low Ammo and supply consequences",
             "Automatic Low Ammo and D3.71's circled B# (with pass 105); supply movement restrictions, Immobilization, capture, destruction, explosion",
             "Their mandatory consequences", "Not started"),
    "D4.4": (175, "Ammunition acceptance", NONE,
             "Becomes track 3 acceptance: bounded effects against automatic rules, shared resources, firing benefits and MP restrictions, depletion and restoration",
             "Not started"),
    "D5.1": (145, "Tunnel contracts",
             "B8.6 tunnel entry, transit, and emergence: identities, timing, eligibility, disclosure, interruption", NONE, "Not started"),
    "D5.2": (145, "Tunnel lifecycle", NONE,
             "The governed tunnel lifecycle across turns, with hidden history and phase-close evidence", "Not started"),
    "D5.3": (170, "Airborne arrival and landing",
             "Airborne arrival and landing (E8, E9), glider and paratroop contents, their required consequences",
             "The facts and commands, with the opening windows", "Not started; needs the airborne sources"),
    "D5.4": (145, "Tunnel and airborne acceptance", NONE,
             "Becomes track 3 acceptance: hidden emergence, delayed and lost arrivals, landing consequences, stale, replay, phase obligations",
             "Not started"),
    "D6.1": (73, "Close Combat and hero contracts",
             "The remaining CC declarations and hero creation and use: ownership, order, carryover, reconciled against the engine. Its rows fall in passes 73, 80, 105, 110, 115, 135, 165, and 180; D6.1 names each",
             NONE, "Not started"),
    "D6.2": (73, "Governed CC declarations and heroes", "The verdicts of the missing decisions",
             "Governed declarations, required decisions, hero creation and use with durable identity and consequences", "Not started"),
    "D6.3": (73, "CC across phases", NONE,
             "CC across phases and the turn boundary: unresolved combat and consequences kept before cleanup; the guide's routes", "Not started"),
    "D6.4": (73, "CC and hero acceptance", NONE,
             "Becomes the track 3 acceptance of the CCPh: both sides, special declarations, creation and loss, interrupted resolutions, cleanup, replay, disclosure",
             "Not started"),
    "D7.1": (90, "The profiles",
             "The water and amphibious, aerial, night and weather, and module profiles: source and applicability bound to the topic ids; a missing profile stays unknown",
             NONE, "Not started"),
    "D7.2": (125, "Water, amphibious, and aerial",
             "The missing facts of water and amphibious play and of aerial play: transport, arrival, fire consequences, profile timing (aerial with pass 160; with passes 170 and 175)",
             "The commands", "Not started; needs D5.3"),
    "D7.3": (90, "Night, weather, and modules",
             "The remaining night, weather (with pass 95), and module execution and cleanup, their exceptions and source gaps", "The same, wired",
             "Not started; needs D7.2 and D2.3"),
    "D7.4": (90, "Profile acceptance", NONE,
             "Becomes track 3 acceptance: the applicability, legacy, and hidden-world matrix, cross-profile replay, the live guide", "Not started"),
    "D8.1": (90, "Trip Flares",
             "Trip Flare activation, origin, and expiry (E1.95 to E1.953); the timing predicates are in `TurnObligationRules`",
             "The governed commands", "Not started"),
    "D8.2": (115, "Non-Flame Clearance",
             "Non-Flame Clearance eligibility, attempts, and outcomes (B24.7, B24.8; A23.72 with pass 75); the other Clearance exceptions kept as explicit scope",
             "The governed commands and the cleanup order", "Not started"),
    "D8.3": (90, "Phase-close order at night",
             "Clearance before status (with pass 115) and concealment before illumination at phase close",
             "Interrupted consequences, late and unknown origins", "Not started; needs D8.2 and D7's night facts"),
    "D8.4": (90, "Trip Flare and Clearance acceptance", NONE,
             "Becomes track 3 acceptance: activation and release windows, failed attempts, pending consequences, history, disclosure, replay, phase close",
             "Not started"),
    "D9.1": (73, "The core-topic matrix",
             "Every remaining core Rally, movement, Rout, terrain hazard, status, possession, prisoner, and turn-boundary topic with its source and its claim: the inventory's Owner column takes the place of this matrix",
             NONE, "Not started"),
    "D9.2": (73, "Core eligibility and commands", "The missing core eligibility and facts D9.1 names",
             "The governed command paths, with the mandatory hazard, prisoner, and casualty continuations; existing engine code audited first", "Not started"),
    "D9.3": (73, "Core topics in the guide", NONE,
             "The remaining core topics and cross-phase obligations in the guide's typed decisions and its shared close assessment", "Not started"),
    "D9.4": (73, "All-package acceptance", NONE,
             "Becomes the track 3 acceptance of every phase: each topic and dependency has evidence or an explicit source block", "Not started"),
}

POINTERS = {
    74: "D9 (pass 73) covers this pass's prisoner and turn-boundary topics.",
    75: "D8.2 (pass 115) takes A23.72, Clearance by DC, with this pass.",
    80: "D6 (pass 73) names the CC and hero rows of this pass.",
    95: "D2.3 (pass 85) and D7.3 (pass 90) take weather's dust and execution with this pass.",
    105: "D4.3 (pass 175) takes D3.71's circled B# and Low Ammo with this pass; D6 (pass 73) names this pass's CC rows.",
    110: "D6 (pass 73) names this pass's CC rows.",
    115: "D8.3 and D8.4 (pass 90) order Clearance at phase close; D6 (pass 73) names this pass's CC rows.",
    135: "D6 (pass 73) names this pass's CC rows.",
    150: "D1.3 (pass 110) admits the artillery dependencies of this pass.",
    155: "D1.3 (pass 110) admits the artillery dependencies; D2.3 (pass 85) takes FFE dust with this pass.",
    160: "D7.2 (pass 125) takes aerial play with this pass.",
    165: "D6 (pass 73) names this pass's CC and hero rows.",
    170: "D5.4 (pass 145) accepts airborne arrival with tunnels; D7.2 (pass 125) takes water and aerial play with this pass.",
    175: "D7.2 (pass 125) takes amphibious transport with this pass.",
    180: "D6 (pass 73) names this pass's CC rows.",
}


def block(number):
    out = []
    homed = [(task, row) for task, row in TASKS.items() if row[0] == number]
    if homed:
        out += [f"**Turns tasks merged here** (the [Rules-first plan](<Plans/ASL Rules-First Four-Track Plan.md>), section 11.2; 2026-10-28). "
                f"Each task of the [Turn Guide later Rules plan]({TURNS_LINK}) is split into its Rules part, a claim on the board, and its wiring part, "
                "track 3. A fourth task's acceptance becomes the track 3 acceptance of the phases it touches.", "",
                "| Turns task | Rules part (a claim) | Wiring part (track 3) | State, 2026-10-28 |", "|---|---|---|---|"]
        for task, (_, title, rules, wiring, state) in homed:
            out.append(f"| {task} {title} | {rules} | {wiring} | {state} |")
        out.append("")
    if number in POINTERS:
        out += [f"Turns tasks that reach this pass from another: {POINTERS[number]}", ""]
    return out


def merge_redesign(text):
    lines = text.split("\n")
    starts = [i for i, line in enumerate(lines) if re.match(r"^\*\*Pass \d+: ", line)]
    end_of_22_1 = next(i for i, line in enumerate(lines) if line.startswith("### 22.2 "))
    bounds = starts + [end_of_22_1]
    passes = {int(re.match(r"^\*\*Pass (\d+):", lines[i]).group(1)): i for i in starts}
    wanted = {row[0] for row in TASKS.values()} | set(POINTERS)
    if wanted - set(passes):
        sys.exit(f"no section for passes {sorted(wanted - set(passes))}")
    if any("**Turns tasks merged here**" in line or line.startswith("Turns tasks that reach this pass") for line in lines):
        sys.exit("the redesign plan already holds the merge")
    for start in reversed(starts):
        number = int(re.match(r"^\*\*Pass (\d+):", lines[start]).group(1))
        if number not in wanted:
            continue
        stop = bounds[bounds.index(start) + 1]
        at = stop
        while at > start and lines[at - 1].strip() == "":
            at -= 1
        lines[at:at] = [""] + block(number)[:-1]
    text = "\n".join(lines)
    # Question 9: the Trip Flare rows leave the left-out table and join pass 90.
    old_left = ("| A rule that serves the Pacific only waits with Chapter G: trip flares (E1.95 to E1.953, p. 227: a U.S. Scenario Defender in 1944-45 PTO "
                "terrain), and the Ethiopian Close Combat that A25.934 resolves by G1.64. | A25.934, E1.95, E1.951, E1.952, E1.953 | 5 |")
    new_left = ("| A rule that serves the Pacific only waits with Chapter G: the Ethiopian Close Combat that A25.934 resolves by G1.64. (The trip flares, "
                "E1.95 to E1.953, joined pass 90 on 2026-10-28, the Rules-first plan's question 9.) | A25.934 | 1 |")
    old_90 = "**Pass 90: Night** (planned at 2:45 of build; 43 rows of the inventory)."
    new_90 = "**Pass 90: Night** (planned at 2:45 of build; 47 rows of the inventory, the four Trip Flare rows included since 2026-10-28)."
    note_90 = "Moved here by pass 71 (2026-10-26), with no task renumbered: the captured MG's automatic Sniper dr at night (E1.76, with 90.6; ruling R71.2). The Sniper Check's floor at night is built (ruling R71.9)."
    add_90 = note_90 + ("\n\nMoved here by the Rules-first plan (2026-10-28, question 9), with no task renumbered: Trip Flares (E1.95 to E1.953, p. 227), "
                        "with Turns task D8.1 below. Their minigame names them by SSR on an existing board.")
    for old, new in ((old_left, new_left), (old_90, new_90), (note_90, add_90)):
        if text.count(old) != 1:
            sys.exit(f"expected once: {old[:70]}")
        text = text.replace(old, new)
    return text


def merge_inventory(lines):
    rows = inventory_table.rows(lines)
    moved = {"Pass 90": [], "Pass 165": []}
    for row in rows:
        target = None
        if row["§"] in ("E1.95", "E1.951", "E1.952", "E1.953"):
            if row["Pass or ruling"] != "Deferred with Chapter G: the rule serves the Pacific only":
                sys.exit(f"{row['§']}: unexpected pass cell {row['Pass or ruling']!r}")
            target = "Pass 90"
        elif row["chapter"] == "G" and (row["§"] == "G10" or row["§"].startswith("G10.")):
            if row["Pass or ruling"] != "Claim wave 1 (question 8)":
                sys.exit(f"{row['§']}: unexpected pass cell {row['Pass or ruling']!r}")
            target = "Pass 165"
        if target is None:
            continue
        values = inventory_table.cells(lines[row["line"]])
        names = [name for name in row if name not in ("chapter", "line")]
        values[names.index("Pass or ruling")] = target
        if target == "Pass 90":
            values[names.index("Note")] += " Joined pass 90 on 2026-10-28 (the Rules-first plan, question 9)."
        lines[row["line"]] = "| " + " | ".join(values) + " |"
        moved[target].append(row["§"])
    if len(moved["Pass 90"]) != 4 or len(moved["Pass 165"]) != 16:
        sys.exit(f"moved {moved}")
    old = ("The last three lines are the rows beyond Chapter E, by the plan's question 8: wave 1 is claimed now (G10 Animal Pack and the ASOP), wave 2 once "
           "track 1 has imported that theater's boards and counters (F, the rest of G, and W), and H1 stays with the DYO passes.")
    new = ("The last three lines are the rows beyond Chapter E, by the plan's question 8: wave 1 is claimed now (the ASOP; G10 Animal Pack is claimed now "
           "too, in pass 165 since first task 3 of 2026-10-28), wave 2 once track 1 has imported that theater's boards and counters (F, the rest of G, "
           "and W), and H1 stays with the DYO passes.")
    index = lines.index(old)
    lines[index] = new
    return moved


TURNS_TOP = f"""# Turn Guide later Rules implementation plan

Date: 2026-10-27. **Merged into the pass sections on 2026-10-28** (first task 3 of the [Rules-first four-track plan](<../Plans/ASL Rules-First Four-Track Plan.md>), section 11.2, at the user's word).

## Where the work is now

The 36 tasks of this plan are merged into the pass sections of the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, each split into its Rules part (a claim on the board of the Rules-first plan) and its wiring part (track 3). A package's fourth task becomes the track 3 acceptance of the phases it touches. The task register below maps each task to its pass; what remains is written there, not here. The catalogs, walkthroughs, and source register in this folder stay, and the pass sections cite them.

This file keeps its history: the ledger and the acceptance and progress records that follow, as they were. On 2026-10-28 the ledger read 3 of 36 complete (D1.1, D3.1, D4.1), with D1.2, D2.1, and D3.2 in progress, and the [D4.2 design](<ASL Turn Guide D4.2 Design.md>) prepared for review and not approved (Codex's review note of that day: implementation awaits design approval; commits may follow a successful Studio check and only the unit tests of the changed behavior; full suites and ASL.Turns coverage belong at the gate and CI).

| Package | Its tasks' pass section |
|---|---|
"""


def merge_turns(text):
    if "**Merged into the pass sections" in text:
        sys.exit("the later Rules plan is already merged")
    contract = text[text.index("## Common implementation and acceptance contract"):text.index("## Finite task register")]
    contract = contract.replace("## Common implementation and acceptance contract",
                                "## Common implementation and acceptance contract\n\nKept as written: track 3 accepts each wiring part against it.", 1)
    keep = text.index("### D1.1 acceptance")
    rows = []
    for package in range(1, 10):
        tasks = ", ".join(f"{task} (pass {row[0]})" for task, row in TASKS.items() if task.startswith(f"D{package}."))
        rows.append(f"| D{package} | {tasks} |")
    return TURNS_TOP + "\n".join(rows) + "\n\n" + contract + "## History: acceptance and progress records\n\n" + text[keep:]


def main():
    apply = "--apply" in sys.argv
    if len(TASKS) != 36:
        sys.exit(f"{len(TASKS)} tasks")
    redesign = merge_redesign(open(REDESIGN, encoding="utf-8", newline="").read())
    lines, nl = inventory_table.read_lines()
    moved = merge_inventory(lines)
    turns = merge_turns(open(TURNS, encoding="utf-8", newline="").read())
    homes = {}
    for task, row in TASKS.items():
        homes.setdefault(row[0], []).append(task)
    for number in sorted(homes):
        print(f"pass {number}: {', '.join(homes[number])}")
    print("pointers:", sorted(POINTERS))
    print("inventory moved:", {k: len(v) for k, v in moved.items()})
    if apply:
        open(REDESIGN, "w", encoding="utf-8", newline="").write(redesign)
        open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
        open(TURNS, "w", encoding="utf-8", newline="").write(turns)
        print("written")
    else:
        print("dry run")


if __name__ == "__main__":
    main()
