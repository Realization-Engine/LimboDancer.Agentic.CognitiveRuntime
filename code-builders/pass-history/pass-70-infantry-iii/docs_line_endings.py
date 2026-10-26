"""Pass 70 documents: check_document_line_endings.py's report (CR, BOM, the final LF, em-dashes) over the documents pass 70's closing touches."""
import io

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
for name in ["Passes/ASL Unit Backlog Pass 70 Design.md", "Passes/Scenario A1 Backlog Pass 70 Review 2026-10-25.md", "ASL Unit Backlog Passes Plan.md",
             "ASL Unit Backlog.md", "ASL Card Play and Map Studio Redesign Plan.md", "ASL Rule Inventory A to E.md", "ASL Rule Coverage.md",
             "Prompts/Pass 70 Prompt I.md"]:
    raw = io.open(D + name, "rb").read()
    print(name, "CR", raw.count(b"\r"), "LF", raw.count(b"\n"), "BOM", raw.startswith(b"\xef\xbb\xbf"), "ends with LF", raw.endswith(b"\n"),
          "em-dash", raw.decode("utf-8").count("\u2014"))
