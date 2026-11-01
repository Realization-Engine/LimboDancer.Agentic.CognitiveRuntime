"""Create independent Support Weapons review positions without replacing played games."""
import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "test-games"))
from game_file import GameFile  # noqa: E402

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("destination", type=Path)
parser.add_argument("--prefix", required=True)
parser.add_argument("--night", action="store_true")
parser.add_argument("--phase", choices=("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"))
args = parser.parse_args()
phases = ("rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph")
selected = (args.phase,) if args.phase else phases
targets = [args.destination / f"{args.prefix}-{phase}.game.json" for phase in selected]
if not args.destination.is_dir():
    raise SystemExit("The destination must be an existing live-game directory.")
if args.destination.name != "5a7d1f000000400080000000000057d0":
    raise SystemExit("Use the Studio tenant directory below units/live, not units/live itself.")
if any(path.exists() for path in targets):
    raise SystemExit("Refusing to overwrite an existing review position; choose a new prefix.")

for phase, target in zip(selected, targets):
    game = GameFile(f"{args.prefix}-{phase}", f"Support Weapons review: {phase}", phasing="russian" if phase == "dfph" else "german",
                    special=["night:2"] if args.night else None)
    locations = (("1", "bd03:A3:0"), ("2", "bd03:B2:0")) if phase == "rtph" else (("1", "bd03:F5:0"), ("2", "bd03:J5:0"))
    for suffix, location in locations:
        holder = f"g-squad-{suffix}"
        game.unit(holder, "squad", "attacker-squad", "german", location,
                  broken=phase in ("rph", "rtph") or (phase == "mph" and suffix == "2"), dm=phase == "rtph", melee=phase == "ccph")
        game.unit(f"g-leader-{suffix}", "leader", "attacker-leader-8-1", "german", location, melee=phase == "ccph")
        game.unit(f"g-receiver-{suffix}", "squad", "attacker-squad", "german", location, melee=phase == "ccph")
        game.weapon(f"g-heavy-{suffix}", "attacker-hmg" if phase == "ccph" else "attacker-mmg", "german", holder)
        game.weapon(f"g-lmg-{suffix}", "attacker-lmg", "german", holder)
        if phase in ("pfph", "dfph"):
            game.weapon(f"g-spare-mmg-{suffix}", "attacker-mmg", "german", holder)
            game.weapon(f"g-spare-hmg-{suffix}", "attacker-hmg", "german", holder)
        game.weapon(f"g-repair-{suffix}", "attacker-lmg", "german", f"g-receiver-{suffix}", malfunctioned=True)
        if phase == "rph":
            game.weapon(f"g-repair-mmg-{suffix}", "attacker-mmg", "german", f"g-receiver-{suffix}", malfunctioned=True)
            game.weapon(f"g-repair-hmg-{suffix}", "attacker-hmg", "german", f"g-receiver-{suffix}", malfunctioned=True)
        game.add("instance-created", {"instance": {
            "id": f"g-ground-{suffix}", "kind": "asl:mg", "definition": "attacker-lmg",
            "side": "german", "position": {"at": location}, "conditions": {"asl:malfunctioned": False}}})
        enemy_location = location if phase == "ccph" else (
            "bd03:A1:0" if phase == "rtph"
            else ("bd03:P5:0" if suffix == "1" else "bd03:R5:0"))
        game.unit(f"r-squad-{suffix}", "squad", "defender-squad", "russian", enemy_location, melee=phase == "ccph")
        game.weapon(f"r-mmg-{suffix}", "defender-mmg", "russian", f"r-squad-{suffix}")
    game.phases(*phases[:phases.index(phase) + 1])
    if phase == "rtph":
        for suffix in ("1", "2"):
            game.add("conditions-changed", {"id": f"g-squad-{suffix}", "conditions": {"asl:dm": True}})
    if phase == "ccph":
        for suffix in ("1", "2"):
            for unit in (f"g-squad-{suffix}", f"g-leader-{suffix}", f"g-receiver-{suffix}", f"r-squad-{suffix}"):
                game.add("conditions-changed", {"id": unit, "conditions": {"asl:melee": True}})
    record = dict(schemaVersion=1, tenant=game.tenant, game=game.game, label=game.label,
                  synthetic=False, events=game.events)
    with target.open("x", encoding="utf-8", newline="\n") as output:
        json.dump(record, output, indent=2)
        output.write("\n")
    print(f"Created {game.game}: {len(game.events)} events")
