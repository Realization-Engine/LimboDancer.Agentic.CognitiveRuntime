"""Step 0 of the Rules-first four-track plan: add the "Rules calculator" column to the rule inventory.

Reads the inventory's rows, the read classification of the rows whose Where cell names no Rules code
(classification-batch1..4.txt, one line a row: rule | class | Rules members | residual | Where now | comment),
and the residuals found in passing (PASSING below). Writes the column after Where, and for the classified rows
replaces the Where cell with the corrected one.

    python write_rules_calculator.py            dry run: counts and samples, nothing written
    python write_rules_calculator.py --apply    rewrites the inventory

The inventory must not yet have the column; the script refuses a second run.
"""
import collections
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", "..", ".."))
ASL = os.path.join(REPO, "src", "ASL")
INVENTORY = os.path.join(ASL, "docs", "ASL Rule Inventory A to E.md")

HEADER = "| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |"
NEW_HEADER = "| § | Title | Page | Status | Where | Rules calculator | Test | Pass or ruling | Note | By |"
RULER = "|---|---|---:|---|---|---|---|---|---|"
NEW_RULER = "|---|---|---:|---|---|---|---|---|---|---|"
IMPLEMENTED = {"built", "built with a deviation", "partly", "refused"}
RULES = "LimboDancer.Domains.Asl.Rules"
MAPS = "LimboDancer.Domains.Asl.Maps"
LOS_RULING = "Maps (LOS and range stay in Maps, ruling 2026-10-05)"

# Rule decisions outside Rules found in passing by the classifying agents, each read on its lines on 2026-10-28.
PASSING = {
    "A1.32": "GamePlanner.SupportWeapons.cs:380-394 PlanRecombine: RPh, two Good Order HS of one definition in one Location, the RPh action",
    "A2.6": "GamePlanner.Vehicles.cs:581-584 PlanMoveVehicle: no exit in Reverse or Bypass; GameProjector.cs:2671 StepVehicle",
    "A2.52": "GamePlanner.Passengers.cs:201-209 PlanVehicleEntry: an off-board vehicle enters in its side's MPh, its first expenditure an entry",
    "A3.1": "GamePlanner.SupportWeapons.cs:104-108 RallyPhaseActionBar: one RPh action a unit",
    "A4.2": "GamePlanner.Passengers.cs:211-214 PlanVehicleEntry: no entry while another stack moves",
    "A8.222": "GamePlanner.Vehicles.cs:229-231: an AFV with no Vulnerable crew and no Infantry takes no Residual FP attack",
    "A10.4": "GamePlanner.SupportWeapons.cs:542-545 PlanDrop: a broken unit may not leave a SW its best load keeps",
    "A11.6": "GamePlanner.Overrun.cs:239-243 PaatcFacts: which leader's leadership counts",
    "A15.43": "GamePlanner.Passengers.cs:216-219 PlanVehicleEntry: a berserk unit charges before any other moves",
    "A20.5": "GamePlanner.SupportWeapons.cs:479 PlanTransfer and 640 PlanRecover: an Unarmed unit takes or Recovers no SW",
    "A22.3": "GamePlanner.DemolitionCharges.cs:237-239: one FT or DC a Player Turn",
    "A23.3": "GamePlanner.DemolitionCharges.cs:52-54 PlanPlaceDc: an ADJACENT Location, not its own",
    "A23.6": "GamePlanner.DemolitionCharges.cs:242-244 PlanThrowDc: an ADJACENT Location, not its own",
    "B9.4": "GamePlanner.Vehicles.cs:680-701 PlanMoveVehicle: a halftrack failing its hedge Bog DR bogs in the hex it left",
    "B23.4": "GamePlanner.VehicleTerrain.cs:74 VehicleOutright: vehicles stay at ground level",
    "C6.5": "LiveOrdnance.cs:273-276 FromState: where the Acquisition applies; Canister claims none",
    "C6.51": "LiveOrdnance.cs:273-276 FromState: the Acquisition while its units are apart",
    "C10.3": "GamePlanner.Movement.cs:516: the Manhandling DRM (MF spent, -2 by road)",
    "D2.5": "GamePlanner.Vehicles.cs:854 PlanEsb: the ESB maximum, a quarter of the MP",
    "D2.11": "GamePlanner.VehicleTerrain.cs:280-283 VehicleTurnCost: one MP for a VCA change in a road hex",
    "D6.4": "GameProjector.cs:2657-2659 StepVehicle: who may board",
    "D6.5": "GameProjector.cs:2661-2663 StepVehicle: who may disembark",
    "D7.15": "GamePlanner.Overrun.cs:151-154: a wall or hedge TEM only across the hexside entered",
}


def project_files():
    """File name -> set of projects, over the code of src/ASL."""
    files = collections.defaultdict(set)
    for folder, dirs, names in os.walk(ASL):
        dirs[:] = [d for d in dirs if d not in ("bin", "obj", "node_modules", "boards", ".git")]
        for name in names:
            if name.endswith((".cs", ".razor")):
                files[name].add(os.path.relpath(folder, ASL).replace("\\", "/").split("/")[0])
    return files


def segments(where):
    """The Where cell's segments, each with the file it names (or None)."""
    out = []
    for part in [p.strip() for p in where.split(";") if p.strip()]:
        match = re.match(r"([A-Za-z0-9_.]+?\.(?:cs|razor))\b", part)
        out.append((match.group(1) if match else None, part))
    return out


def read_classification():
    rows = {}
    for i in range(1, 5):
        with open(os.path.join(HERE, f"classification-batch{i}.txt"), encoding="utf-8") as fh:
            for line in fh:
                if " | " not in line:
                    continue
                parts = [p.strip() for p in line.rstrip().removesuffix("|").split(" | ")]
                rule, cls, members, residual, where_now = parts[:5]
                if rule in rows:
                    sys.exit(f"{rule} is classified twice")
                rows[rule] = (cls, members, residual, where_now)
    return rows


def classified_cell(cls, members, residual):
    has = members and members != "none"
    if cls == "A":
        return members
    if cls == "B":
        return f"{members}; residual outside Rules: {residual}"
    if cls == "C":
        return f"none; decided outside Rules: {residual}"
    if cls == "D":
        return LOS_RULING + (f"; {members}" if has else "")
    if cls == "E":
        return "none needed: a fact, not a decision"
    sys.exit(f"unknown class {cls}")


def main():
    apply = "--apply" in sys.argv
    files = project_files()
    classified = read_classification()
    with open(INVENTORY, encoding="utf-8", newline="") as fh:
        text = fh.read()
    if "\r\n" in text or text.startswith("﻿"):
        sys.exit("the inventory is not LF without a BOM")
    if NEW_HEADER in text:
        sys.exit("the inventory already has the Rules calculator column")

    lines = text.split("\n")
    out = []
    in_table = False
    seen = set()
    kinds = collections.Counter()
    samples = collections.defaultdict(list)
    passing_used = set()
    for line in lines:
        if line == HEADER:
            in_table = True
            out.append(NEW_HEADER)
            continue
        if in_table and line == RULER:
            out.append(NEW_RULER)
            continue
        if in_table and line.startswith("| "):
            cells = [c.strip() for c in line.strip()[1:-1].split("|")]
            if len(cells) != 9 or "| " + " | ".join(cells) + " |" != line:
                sys.exit(f"a row does not read back as it stands: {line[:80]}")
            rule, title, page, status, where, test, pas, note, by = cells
            if rule in seen:
                sys.exit(f"{rule} appears twice")
            seen.add(rule)
            if status not in IMPLEMENTED:
                cell = "none" if status == "not built" else ""
                kind = status
            elif rule in classified:
                cls, members, residual, where_now = classified[rule]
                cell = classified_cell(cls, members, residual)
                where = where_now
                kind = "read " + cls
            else:
                rules_parts = [part for name, part in segments(where) if name and RULES in files.get(name, ())]
                maps_parts = [part for name, part in segments(where) if name and MAPS in files.get(name, ())]
                if rules_parts:
                    cell = "; ".join(rules_parts) + " (from Where, not read)"
                    kind = "from Where"
                elif maps_parts:
                    cell = LOS_RULING
                    kind = "Maps only"
                else:
                    sys.exit(f"{rule} names no Rules or Maps code and was not classified")
            if rule in PASSING:
                if status not in IMPLEMENTED:
                    sys.exit(f"{rule} has a residual found in passing but its status is {status}")
                cell = f"{cell}; residual outside Rules (found in passing): {PASSING[rule]}"
                passing_used.add(rule)
            if "|" in cell or "|" in where:
                sys.exit(f"{rule}: a cell holds a pipe")
            kinds[kind] += 1
            if len(samples[kind]) < 2:
                samples[kind].append(f"{rule}: {cell[:160]}")
            out.append("| " + " | ".join([rule, title, page, status, where, cell, test, pas, note, by]) + " |")
            continue
        if in_table and not line.startswith("|"):
            in_table = False
        out.append(line)

    missing = sorted(set(classified) - seen) + sorted(set(PASSING) - passing_used)
    if missing:
        sys.exit(f"not found in the inventory: {missing}")
    print("rows:", len(seen))
    for kind, n in sorted(kinds.items()):
        print(f"  {kind}: {n}")
        for sample in samples[kind]:
            print("      " + sample)
    print("residuals found in passing:", len(passing_used))
    if apply:
        with open(INVENTORY, "w", encoding="utf-8", newline="\n") as fh:
            fh.write("\n".join(out))
        print("written")
    else:
        print("dry run: nothing written")


if __name__ == "__main__":
    main()
