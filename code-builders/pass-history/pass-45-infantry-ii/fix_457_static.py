"""Pass 45, task 45.7: the page's dismantle read is the planner's own, since it reads the catalog's portage value."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Play/GamePlanner.SupportWeapons.cs": [
        ("""    /// <summary>Whether the game dismantles a weapon now for its possessor (A9.8; ruling R13.6): the German MMG, in its side's PFPh or DFPh.</summary>
    public static bool MayDismantle(GameState state, string weaponId)
""",
         """    /// <summary>Whether the game dismantles a weapon now for its possessor (A9.8; pass 45): a weapon the rule names, in its side's PFPh or DFPh.</summary>
    public bool MayDismantle(GameState state, string weaponId)
"""),
    ],
    "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor": [
        ("""Dismantles="@(swWeapon.Length > 0 && GamePlanner.MayDismantle(state, swWeapon))\"""",
         """Dismantles="@(swWeapon.Length > 0 && Live.Planner.MayDismantle(state, swWeapon))\""""),
    ],
}
texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:80]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
