"""Pass 65 documents: CR, BOM, the final newline, and em-dashes of every document the documents step touched (check_document_line_endings.py's form)."""
import io
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402

D = REPO + "/src/ASL/docs/"
for name in ["Passes/ASL Unit Backlog Pass 65 Design.md", "Passes/Scenario A1 Backlog Pass 65 Review 2026-10-24.md", "ASL Unit Backlog Passes Plan.md",
             "ASL Unit Backlog.md", "ASL Rule Inventory A to E.md", "ASL Rule Coverage.md", "ASL Card Play and Map Studio Redesign Plan.md",
             "Prompts/Pass 65 Prompt I.md"]:
    raw = io.open(D + name, "rb").read()
    print(name, "CR", raw.count(b"\r"), "LF", raw.count(b"\n"), "BOM", raw.startswith(b"\xef\xbb\xbf"), "ends with LF", raw.endswith(b"\n"),
          "em-dash", raw.decode("utf-8").count("—"))
