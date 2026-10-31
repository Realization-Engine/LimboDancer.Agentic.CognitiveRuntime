"""Move existing page assertions to the approved outcome-card UI, preserving their behavior checks."""
from pathlib import Path
import os
import re

root = Path(__file__).resolve().parents[3]
tests = root / "src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests"
selected = set()
for path in tests.glob("PlayPage*Tests.cs"):
    original = path.read_text(encoding="utf-8-sig")
    for match in re.finditer(r'#play-(?:latest|fires|rallies|ordnance|close-combats|night|snipers)', original):
        methods = list(re.finditer(r'public (?:async )?(?:void|Task) (\w+)\(', original[:match.start()]))
        if methods:
            selected.add(path.stem + "." + methods[-1].group(1))

    def record(match):
        many, category, tail = match.groups()
        if tail == " .record-when":
            return 'page.Find("#play-latest-panel .action-outcome-context")'
        defaults = {"fires": ".fire-record", "ordnance": ".ordnance-record", "close-combats": ".cc-record", "night": ".night-record", "snipers": ".sniper-record"}
        selector = tail.strip() or defaults.get(category, "li[data-event]")
        if category == "fires" and selector != ".fire-record":
            selector = ".fire-record " + selector
        if category == "rallies" and selector == "li":
            selector = ".dm-record"
        return f'page.OutcomeRecord{"s" if many else ""}("{selector}")'

    changed = re.sub(r'page\.Find(All)?\("#play-(rallies|ordnance|close-combats|fires|night|snipers)([^"\n]*)"\)', record, original)
    changed = changed.replace('#play-latest-tables .fire-latest-card', '#play-latest-panel')
    changed = changed.replace('#play-latest-tables', '#play-latest-panel')
    changed = changed.replace('"#play-latest"', '"#play-latest-panel"')
    if changed != original:
        path.write_text(changed, encoding="utf-8", newline="\n")

destination = Path(os.environ["ASL_GATE_WORK"]) / "action-outcome/affected-checks.txt"
destination.parent.mkdir(parents=True, exist_ok=True)
destination.write_text("\n".join(sorted(selected)) + "\n", encoding="utf-8")
print(f"Updated existing assertions; selected {len(selected)} affected methods. Filter list: {destination}")
