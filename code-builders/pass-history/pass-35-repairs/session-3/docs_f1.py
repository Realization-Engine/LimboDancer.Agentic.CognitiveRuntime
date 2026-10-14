"""Finding 1 of the pass 35 reviews: the documents that said a repulsed unit surrenders. Exact blocks, each asserted once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
NOTE = ("A repulsed unit left ADJACENT to a Known enemy unit is eliminated for Failure to Rout as the RtPh ends and is never taken prisoner: "
        "A10.533's example (p. 68) says its enemy would have had to give up its \"?\" by choice before the rout entered to take it prisoner, and "
        "that keeping the \"?\" brings no No Quarter penalty (corrected 2026-10-09 after the referee's review; as first built it surrendered when a "
        "captor could take it).")
EDITS = {
    "ASL Unit Backlog Passes Plan.md": [(
        "A10.533 says a repulsed unit left ADJACENT to a Known enemy unit is eliminated; as built, the phase's end treats it as any broken unit that "
        "ends ADJACENT to its enemy, so it surrenders when a captor can take it (A20.21) and is eliminated otherwise. A20.21 covers any broken unit "
        "ADJACENT to Good Order armed enemy Infantry that cannot rout away, and the repulsed unit is one.",
        NOTE)],
    "ASL Rule Inventory A to E.md": [
        ("A repulsed unit that ends ADJACENT to its enemy surrenders when a captor can take it and is eliminated otherwise, where the rule's words give elimination:",
         "A repulsed unit that ends ADJACENT to its enemy is eliminated, as the rule's words and its example give (corrected 2026-10-09; it surrendered as first built):"),
        ("A repulsed unit that ends ADJACENT to its enemy surrenders under this rule (ruling R35.3).",
         "A repulsed unit that ends ADJACENT to its enemy does not surrender under this rule: it is eliminated (A10.533; ruling R35.3 as corrected 2026-10-09)."),
    ],
    "Passes/ASL Unit Backlog Pass 35 Design.md": [
        ("it surrenders if a captor can take it (A20.21), and is eliminated otherwise.",
         "it surrenders if a captor can take it (A20.21), and is eliminated otherwise. (Corrected in the fourth session after the referee's review: it is "
         "eliminated and never surrenders, by A10.533's example; section 19.)"),
        ("R2 on the hill in [E5] now must rout from the squad revealed in [F5] and surrenders to it as the phase ends.",
         "R2 on the hill in [E5] now must rout from the squad revealed in [F5] and surrenders to it as the phase ends. (As then built; since the "
         "fourth session's correction a repulsed unit is eliminated.)"),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:60])
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
