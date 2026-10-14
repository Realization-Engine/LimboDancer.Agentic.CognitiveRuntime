"""Pass 32.f commit 5, fix: the Self-Rally refusal texts stay inline in Play (the text list's cut of the first-rallier hole); Rules decides which bar applies."""
import pathlib

ROOT = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
RALLY = ROOT / "LimboDancer.Domains.Asl.Play/GamePlanner.Rally.cs"
RULES = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RallyRules.cs"
MODELS = ROOT / "LimboDancer.Domains.Asl.Rules/ScenarioA1RallyRuleModels.cs"


def apply(p, edits):
    t = p.read_bytes().decode("utf-8")
    assert "\r\n" not in t, p
    for old, new in edits:
        assert t.count(old) == 1, (p.name, old[:70])
        t = t.replace(old, new)
    p.write_bytes(t.encode("utf-8"))
    print("applied", p.name, len(edits))


apply(MODELS, [(
    "/// <summary>\n/// What the Rally package asks for next (ruling R5.8)",
    "/// <summary>Which Self-Rally rule bars a unit without the capability (A10.63, A18.11, A10.71): none, not its own side's RPh, the side's one MMC Self-Rally used, or a broken leader in its Location.</summary>\n"
    "public enum SelfRallyBar\n{\n    None,\n    NotOwnPhase,\n    Used,\n    BrokenLeader,\n}\n\n"
    "/// <summary>\n/// What the Rally package asks for next (ruling R5.8)",
)])

apply(RULES, [(
    "    /// <summary>\n"
    "    /// Pass 31 (play test R-04; A10.63, A18.11, A10.71): which of the Self-Rally rules bars this unit is said, not only that one does; the unit that used\n"
    "    /// the side's one MMC Self-Rally is named (pass 31c, backlog section 48) when it is recorded.\n"
    "    /// </summary>\n"
    "    public static string[] SelfRallyRefusal(IReadOnlyList<string> precheck, string unitId, bool phasing, bool firstMmcRallyTaken, Func<string?> firstMmcRallier)\n"
    "    {\n"
    "        ArgumentNullException.ThrowIfNull(precheck);\n"
    "        ArgumentNullException.ThrowIfNull(firstMmcRallier);\n"
    "        return precheck.Contains(\"asl.a1.rally.self-rally-not-capable\")\n"
    "            ? [!phasing\n"
    "                ? $\"play.rally-self: {unitId} has no Self-Rally capability, and only its own side's RPh gives one MMC a Self-Rally without it (A10.63, A18.11); it needs an unbroken leader in its Location\"\n"
    "                : firstMmcRallyTaken\n"
    "                    ? $\"play.rally-self: {unitId} cannot Self-Rally: its side's one MMC Self-Rally of this RPh was used{(firstMmcRallier() is { } first ? $\" by {first}\" : string.Empty)} (A18.11). It needs an unbroken leader in its Location\"\n"
    "                    : $\"play.rally-self: {unitId} has no Self-Rally capability, and a broken leader is in its Location, so its side's first MMC Rally attempt is not open to it (A10.71, A18.11); rally the leader first\"]\n"
    "            : [];\n"
    "    }\n",
    "    /// <summary>\n"
    "    /// Pass 31 (play test R-04; A10.63, A18.11, A10.71): which of the Self-Rally rules bars this unit is said, not only that one does. The texts stay\n"
    "    /// in Play: the one that names the unit that used the attempt (pass 31c, backlog section 48) holds a hole the text list cuts at a nested quote.\n"
    "    /// </summary>\n"
    "    public static SelfRallyBar SelfRallyRefusal(IReadOnlyList<string> precheck, bool phasing, bool firstMmcRallyTaken)\n"
    "    {\n"
    "        ArgumentNullException.ThrowIfNull(precheck);\n"
    "        return !precheck.Contains(\"asl.a1.rally.self-rally-not-capable\") ? SelfRallyBar.None\n"
    "            : !phasing ? SelfRallyBar.NotOwnPhase\n"
    "            : firstMmcRallyTaken ? SelfRallyBar.Used\n"
    "            : SelfRallyBar.BrokenLeader;\n"
    "    }\n",
)])

apply(RALLY, [(
    "            var why = ScenarioA1RallyRules.SelfRallyRefusal(precheck, unit.Id, unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side),\n"
    "                () => FirstMmcRallier(existing, state, unit.Side));\n",
    "            string[] why = ScenarioA1RallyRules.SelfRallyRefusal(precheck, unit.Side == state.PhasingSide, state.FirstMmcRallyTaken.Contains(unit.Side)) switch\n"
    "            {\n"
    "                SelfRallyBar.NotOwnPhase =>\n"
    "                    [$\"play.rally-self: {unit.Id} has no Self-Rally capability, and only its own side's RPh gives one MMC a Self-Rally without it (A10.63, A18.11); it needs an unbroken leader in its Location\"],\n"
    "                // Pass 31c (backlog section 48): the refusal names the unit that used the attempt.\n"
    "                SelfRallyBar.Used =>\n"
    "                    [$\"play.rally-self: {unit.Id} cannot Self-Rally: its side's one MMC Self-Rally of this RPh was used{(FirstMmcRallier(existing, state, unit.Side) is { } first ? $\" by {first}\" : string.Empty)} (A18.11). It needs an unbroken leader in its Location\"],\n"
    "                SelfRallyBar.BrokenLeader =>\n"
    "                    [$\"play.rally-self: {unit.Id} has no Self-Rally capability, and a broken leader is in its Location, so its side's first MMC Rally attempt is not open to it (A10.71, A18.11); rally the leader first\"],\n"
    "                _ => [],\n"
    "            };\n",
)])
