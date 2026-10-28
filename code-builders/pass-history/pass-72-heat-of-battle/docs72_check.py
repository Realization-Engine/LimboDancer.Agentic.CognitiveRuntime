"""Pass 72 documents: CR, BOM, the final newline, and em-dashes of every document the pass's closing touched (check_document_line_endings.py's list is pass 55's)."""
import io

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
for name in ["Passes/ASL Unit Backlog Pass 72 Design.md", "Passes/Scenario A1 Backlog Pass 72 Review 2026-10-27.md", "ASL Unit Backlog Passes Plan.md",
             "ASL Unit Backlog.md", "ASL Rule Inventory A to E.md", "ASL Rule Coverage.md", "ASL Card Play and Map Studio Redesign Plan.md"]:
    raw = io.open(D + name, "rb").read()
    print(name, "CR", raw.count(b"\r"), "LF", raw.count(b"\n"), "BOM", raw.startswith(b"\xef\xbb\xbf"), "ends with LF", raw.endswith(b"\n"),
          "em-dash", raw.decode("utf-8").count("\u2014"))
