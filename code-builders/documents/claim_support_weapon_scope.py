"""Record the approved combined Support Weapons implementation's Rules holds."""
from pathlib import Path
import inventory_table

path = Path(inventory_table.PATH)
lines, _ = inventory_table.read_lines()
groups = {
    "A4.43": ("A4.43", "A4.431", "A4.44"),
    "A9.72": ("A9.72",),
    "A9.8": ("A9.8",),
    "A11.21": ("A11.21",),
}
names = {
    "A4.43": "SupportWeaponPossession",
    "A9.72": "SupportWeaponRepair",
    "A9.8": "SupportWeaponConfiguration",
    "A11.21": "WithdrawalEquipment",
}
for row in inventory_table.rows(lines):
    claim = next((key for key, rules in groups.items() if row["§"] in rules), None)
    if claim is None:
        continue
    if row.get("Owner"):
        raise SystemExit(f"Already held: {row['§']}: {row['Owner']}")
    cells = inventory_table.cells(lines[row["line"]])
    header = next(inventory_table.cells(line) for line in reversed(lines[:row["line"]]) if line.startswith(inventory_table.HEADER_START))
    cells[header.index("Owner")] = f"{claim} (Codex)"
    cells[header.index("Claim state")] = "claimed"
    lines[row["line"]] = "| " + " | ".join(cells) + " |"
at = lines.index("## 7. Wiring")
while not lines[at - 1].startswith("|"):
    at -= 1
for claim, name in names.items():
    lines.insert(at, f"| {claim} | Codex | sub-agent | codex/support-weapons-implementation | ScenarioA1{name}Rules.cs, ScenarioA1{name}Contract.cs | claimed | 2026-10-31 | Approved Support Weapons phase-scope design. User explicitly permits Rules and dependent UI together on this isolated branch before merge; full Studio demo required and merge remains unapproved. Existing legacy answers preserved. |")
    at += 1
path.write_text("\n".join(lines), encoding="utf-8", newline="\n")
