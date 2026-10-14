# The memory index after pass 55: three lines brought up to date. Each anchor is checked before the file is written.
PATH = "C:/Users/dland/.claude/projects/E--Archive-GitHub-dlandi-LimboDancer-MCP/memory/MEMORY.md"
with open(PATH, encoding="utf-8", newline="") as f:
    lines = f.read().split("\n")
NEW = {
    "- [Pass 55 status](pass-55-autonomy.md)": "- [Pass 55 status](pass-55-autonomy.md) — COMPLETE: merged 50987ff3, CI green at 53a22fcf, documents closed 3557744f (2026-10-13); all open rulings approved; ask before pass 60",
    "- [Gate tool repairs after pass 55](gate-tool-repairs-after-pass-55.md)": "- [Gate repairs of 2026-10-13](gate-tool-repairs-after-pass-55.md) — MADE: base when the branch is cut, strict before the documents, baseline/build/trial/proofs+docker/suite; the new order is untimed on a whole gate",
    "- [Pass 50 Armored combat I status](pass-50-armored-combat-i-status.md)": "- [Pass 50 Armored combat I status](pass-50-armored-combat-i-status.md) — MERGED 85d628bd and pushed 2026-10-12 (documents closed e65194c2); 50.4 built in pass 55 as a slice, 50.5 not built; R50.1 to R50.14 approved by the user 2026-10-13",
}
for prefix, new in NEW.items():
    hits = [i for i, line in enumerate(lines) if line.startswith(prefix)]
    assert len(hits) == 1, (prefix, len(hits))
    lines[hits[0]] = new
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(lines))
print("ok")
