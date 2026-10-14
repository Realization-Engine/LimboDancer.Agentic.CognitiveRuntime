"""The memory index after the remedies of 2026-10-12: two lines replaced, one added."""
PATH = "C:/Users/dland/.claude/projects/E--Archive-GitHub-dlandi-LimboDancer-MCP/memory/MEMORY.md"
DASH = " \u2014 "
lines = open(PATH, encoding="utf-8", newline="").read().split("\n")
NEW = {
    "handover-prompt-is-mine.md": "- [Handover prompt is mine](handover-prompt-is-mine.md)" + DASH + "I write the next session's prompt: state, the user's word, what is open; never a procedure, command, or skip list",
    "pass-standing-rules-and-harness-lessons.md": "- [Pass standing rules and harness lessons](pass-standing-rules-and-harness-lessons.md)" + DASH + "kept in src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md; section 0 is the checklist (846eb5a3 on docs/standing-rules-checklist, not pushed 2026-10-12: ask)",
}
for name, line in NEW.items():
    found = [index for index, text in enumerate(lines) if "(" + name + ")" in text]
    assert len(found) == 1, (name, found)
    lines[found[0]] = line
assert not any("gate-script-and-bash-hook.md" in text for text in lines)
at = [index for index, text in enumerate(lines) if "(pass-standing-rules-and-harness-lessons.md)" in text][0]
lines.insert(at + 1, "- [Gate script and Bash hook](gate-script-and-bash-hook.md)" + DASH + "one gate.sh for every pass with an enforced trial; a hook refuses heredocs, sed -i, multi-line python -c, cd into the repository; add checks, not only lessons")
open(PATH, "w", encoding="utf-8", newline="").write("\n".join(lines))
print("ok", len(lines))
