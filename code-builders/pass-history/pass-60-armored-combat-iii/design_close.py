"""The design's status line and outline at the end of the build, and its sections 14 and 15 (a fragment file): design_close.py <fragment>."""
import sys
from pathlib import Path

DESIGN = Path(__file__).resolve().parents[3] / "src" / "ASL" / "docs" / "Passes" / "ASL Unit Backlog Pass 60 Design.md"
text = DESIGN.read_text(encoding="utf-8")
assert "\r\n" not in text and "## 14. As built" not in text
old_status = ("**Status:** The frame, written on 2026-10-15 under the user's word of that day (\"this is my word to start pass 60, run without my "
              "intervention up to the gate\"), given on 2026-10-13 as a mode and on 2026-10-15 as the start.")
new_status = ("**Status:** Built to the merge gate on 2026-10-15 under the user's word of that day (\"this is my word to start pass 60, run without "
              "my intervention up to the gate\"), given on 2026-10-13 as a mode and on 2026-10-15 as the start: the frame, six tasks in six commits "
              "(the plan's 60.1 to 60.6, with 50.5 carried in from pass 50 and built with 60.6), the five reviews and their fixes, a narrower Studio "
              "check at the user's word, and the documents. Sections 13 to 15 say how it ended; section 13.1 lists what departs from the plan's entry "
              "and the frame. **The gate is not run:** it is the user's to start (section 15).")
old_outline = "Then, as each is built: 7 to 12, the increments 2a to 2f; 13, the reviews and the Studio check; 14, as built."
new_outline = ("Then: 7, HE Equivalency (60.4); 8, the Specific Collateral Attack and mixed Locations (60.1); 9, Indirect Fire (60.2); 10, FT, MOL, "
               "and DC at an AFV (60.3); 11, Guns as targets (60.5); 12, a MG's To Kill attack (60.6, with 50.5); 13, the reviews, their fixes, and "
               "the Studio check; 14, as built; 15, the gate.")
for old in (old_status, old_outline):
    assert text.count(old) == 1, old[:60]
text = text.replace(old_status, new_status).replace(old_outline, new_outline)
text = text.rstrip("\n") + "\n" + Path(sys.argv[1]).read_text(encoding="utf-8")
DESIGN.write_text(text, encoding="utf-8", newline="")
print("design closed")
