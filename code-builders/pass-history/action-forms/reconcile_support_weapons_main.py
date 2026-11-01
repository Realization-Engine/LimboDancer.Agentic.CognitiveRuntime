"""Keep main's approved claim-lock wording and update the renumbered SW backlog references."""
from pathlib import Path
import re

docs = Path(__file__).resolve().parents[3] / "src/ASL/docs"
inventory = docs / "ASL Rule Inventory A to E.md"
text = inventory.read_text(encoding="utf-8")
pattern = re.compile(r"^<<<<<<< ours\n(.*?)^=======\n(.*?)^>>>>>>> theirs\n", re.M | re.S)
matches = list(pattern.finditer(text))
if len(matches) != 1 or not all(f"| {claim} |" in matches[0].group(2) for claim in ("A4.43", "A9.72", "A9.8", "A11.21")):
    raise SystemExit("Expected exactly the four SW claim-note conflicts; inspect manually.")
text = pattern.sub(lambda match: match.group(2), text)
inventory.write_text(text, encoding="utf-8", newline="\n")

for relative in ("ASL Support Weapons Phase Scope Analysis.md", "Designs/ASL Support Weapons Phase Scope Proposed Design.md",
                 "Designs/ASL Support Weapons Studio Demo.md", "ASL Rule Coverage.md", "ASL Rule Inventory A to E.md"):
    path = docs / relative
    text = path.read_text(encoding="utf-8").replace("backlog section 93", "backlog section 94").replace("backlog 93", "backlog 94")
    path.write_text(text, encoding="utf-8", newline="\n")
path = docs / "ASL Unit Backlog.md"
text = path.read_text(encoding="utf-8").replace("Separate capability gaps are recorded in section 93.", "Separate capability gaps are recorded in section 94.")
path.write_text(text, encoding="utf-8", newline="\n")
