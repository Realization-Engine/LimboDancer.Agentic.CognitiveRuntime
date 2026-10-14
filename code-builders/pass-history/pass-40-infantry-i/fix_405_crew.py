"""Pass 40 task 40.5: a crew adds its inherent FP to any FPF attack, whatever its own counter (A8.311); and the crew game staged for it."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
TOOLS = "E:/Archive/GitHub/dlandi/pass32-tools/pass40/"


def apply(path, edits):
    text = open(path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(path, "w", encoding="utf-8", newline="").write(text)


apply(ROOT + "LimboDancer.Domains.Asl.Rules/ScenarioA1MandatoryFire.cs", [
    ("    public static bool CrewAddsInherentFp(bool gunFired, bool finalProtectiveFire, bool finalFireMarked) => !gunFired || (finalProtectiveFire && finalFireMarked);",
     "    public static bool CrewAddsInherentFp(bool gunFired, bool finalProtectiveFire) => !gunFired || finalProtectiveFire;"),
])
apply(ROOT + "LimboDancer.Domains.Asl.Rules/ScenarioA1FireCalculator.cs", [
    ("                && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true, attack.FireKind == FinalProtectiveFire, item.FinalFireMarked == true)))",
     "                && !ScenarioA1MandatoryFire.CrewAddsInherentFp(item.GunFired == true, attack.FireKind == FinalProtectiveFire)))"),
])
apply(TOOLS + "make_tpbf.py", [
    ("  crew       a Russian crew mans an AT Gun in [D10]; German squads in [F10] and [F9]. The German MPh (a crew's FP in FPF)",
     "  crew       a Russian crew mans an AT Gun in [D10] beside a squad marked Final Fire; a German squad in [F10] and a berserk one in [F9].\n"
     "             The German MPh: the Gun fires at the first squad, and the berserk one then charges into [D10] (a crew's FP in FPF)"),
    ("    unit(\"g-one\", \"asl:squad\", \"attacker-squad\", \"german\", \"bd03:F10:0\")\n    unit(\"g-two\", \"asl:squad\", \"attacker-squad\", \"german\", \"bd03:F9:0\")",
     "    unit(\"r-one\", \"asl:squad\", \"defender-squad\", \"russian\", \"bd03:D10:0\", **{\"asl:final-fire\": True})\n"
     "    unit(\"g-one\", \"asl:squad\", \"attacker-squad\", \"german\", \"bd03:F10:0\")\n"
     "    unit(\"g-two\", \"asl:squad\", \"attacker-squad\", \"german\", \"bd03:F9:0\", **{\"asl:berserk\": True})"),
    ("\"crew\": \"Pass 40 task 40.5: a Russian crew at its AT Gun in [D10] and two German squads; a crew's FP in FPF (A8.311; board 3)\"",
     "\"crew\": \"Pass 40 task 40.5: a Russian crew at its AT Gun and a squad marked Final Fire in [D10], a berserk squad's charge; a crew's FP in FPF (A8.311; board 3)\""),
])
print("ok")
