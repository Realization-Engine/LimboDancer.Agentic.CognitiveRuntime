"""Pass 45, task 45.6: the projector's A7.55 check lets a MG's later shot on its Multiple ROF stand, as the planner's check does."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs": [
        ("""    public static RecordRefusal? VerifyFireRecord(int? movementStep, bool windowOpen, int? windowStep, int firers, bool byVehicle, IEnumerable<PhaseFireFacts> fires,
        string firerLocation, string targetLocation)
    {
""",
         """    public static RecordRefusal? VerifyFireRecord(int? movementStep, bool windowOpen, int? windowStep, int firers, bool byVehicle, IEnumerable<PhaseFireFacts> fires,
        string firerLocation, string targetLocation, bool firesAloneOnRof = false)
    {
"""),
        ("""        return firers > 0 && fires.Any(item => !(byVehicle && item.Vehicle) && item.FirerLocation == firerLocation && item.TargetLocation == targetLocation && item.Step == movementStep)
""",
         """        // A7.55 (p. 57; pass 45): "they may not attack separately except with ... the subsequent shots of multiple ROF weapons (9.2)". A record in
        // which every firer fires its MG without its own FP is such a shot, as the planner's check has it; the projector had refused it at the commit.
        return firers > 0 && !firesAloneOnRof
            && fires.Any(item => !(byVehicle && item.Vehicle) && item.FirerLocation == firerLocation && item.TargetLocation == targetLocation && item.Step == movementStep)
"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [
        ("""                state.FiresThisPhase.Select(item => new Rules.PhaseFireFacts(item.FirerLocation, item.TargetLocation, item.Step, item.Vehicle)), fire.FirerLocation, fire.TargetLocation) is { } refused)
""",
         """                state.FiresThisPhase.Select(item => new Rules.PhaseFireFacts(item.FirerLocation, item.TargetLocation, item.Step, item.Vehicle)), fire.FirerLocation, fire.TargetLocation,
                fire.Facts.TryGetProperty("firers", out var firingUnits) && firingUnits.ValueKind == JsonValueKind.Array && firingUnits.GetArrayLength() > 0
                    && firingUnits.EnumerateArray().All(item => item.TryGetProperty("usesInherentFp", out var own) && own.ValueKind == JsonValueKind.False)) is { } refused)
"""),
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
