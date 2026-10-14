"""Pass 40 task 40.5: the Play test of a crew's FP in FPF, and a constant array in the changed Close Combat test."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("CloseCombatStepsTests.cs", [
    ("            firers = new[] { \"r4\" },\n            target = \"bd01:A1:0\"", "            firers = R4,\n            target = \"bd01:A1:0\""),
])
apply("BacklogPass8Tests.cs", [
    ("    private static readonly string[] CrewIds = [\"de-crew\"];\n",
     "    private static readonly string[] FpfGroup = [\"g1\", \"de-crew\"];\n\n"
     "    [Fact]\n"
     "    public async Task ACrewThatFiredItsGunAddsItsOwnFpToFpf()\n"
     "    {\n"
     "        // Pass 40, task 40.5 (A8.311, p. 61; ruling R40.18): \"Infantry manning ordnance cannot fire it during FPF ... but must add their inherent\n"
     "        // FP to the FPF attack\". The squad beside the crew is marked Final Fire; the Gun fires at the Russian squad as it comes ADJACENT, and the\n"
     "        // crew then joins the squad's FPF at it. Outside FPF the crew has no FP left (ruling R8.4).\n"
     "        await Setup(\"russian\", 7, 1942, Crew(\"de-crew\", \"bd01:B10:0\", \"german\"), Gun(\"de-gun\", \"attacker-inf-gun\", \"bd01:B10:0\", \"north\", \"de-crew\", \"german\"),\n"
     "            Unit(\"g1\", \"asl:squad\", \"attacker-squad\", \"bd01:B10:0\", \"german\", \"asl:final-fire\"), Squad(\"r1\", \"bd01:B8:0\", \"russian\"));\n"
     "        await Advance(2);\n"
     "        Assert.Equal((\"mph\", \"russian\"), (Current.Phase, Current.PhasingSide));\n"
     "        Committed(await Move(R1, \"bd01:B9:0\"));\n"
     "        Committed(await FireAt(\"de-gun\", \"bd01:B9:0\", null, null, 5, 6));\n"
     "        Refused(await Fire(CrewIds, \"bd01:B9:0\", 6, 5), \"fire\");\n"
     "        var before = Revision;\n"
     "        Committed(await Fire(FpfGroup, \"bd01:B9:0\", 6, 5, 3, 3, 3, 3));\n"
     "        var record = Since(before).Select(item => item.Payload).OfType<FireResolved>().Single();\n"
     "        Assert.Equal(FpfGroup, record.Firers);\n"
     "        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);\n"
     "    }\n\n"
     "    private static readonly string[] CrewIds = [\"de-crew\"];\n"),
])
print("ok")
