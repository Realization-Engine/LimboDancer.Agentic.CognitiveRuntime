import pathlib, sys

root = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL")
P = "Rules.ScenarioA1VehicleProjection"
edits = {
    root / "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [(
        f'            if (Active(state, step.Vehicle) is not UnitInstance vehicle || !{P}.StepAllowed(state.Phase, vocabulary.IsA(vehicle.Kind, "asl:vehicle"),\n',
        f'            if (!{P}.StepPhase(state.Phase) || Active(state, step.Vehicle) is not UnitInstance vehicle || !{P}.StepAllowed(vocabulary.IsA(vehicle.Kind, "asl:vehicle"),\n',
    )],
    root / "LimboDancer.Domains.Asl.Rules/ScenarioA1VehicleProjection.cs": [(
        '    /// <summary>\n'
        '    /// D2.1 (UNIT-STATE-034): a vehicle step moves a vehicle of the phasing side that has not ended its move, in the MPh; only a Load spends no MP.\n'
        '    /// </summary>\n'
        '    public static bool StepAllowed(string? phase, bool vehicle, bool phasing, bool movementEnded, int halfMp, bool load) =>\n'
        '        phase == "mph" && vehicle && phasing && !movementEnded && halfMp >= 0 && (halfMp != 0 || load);\n',
        '    /// <summary>D2.1 (UNIT-STATE-034): a vehicle step is made in the MPh; the projector asks before it reads the vehicle.</summary>\n'
        '    public static bool StepPhase(string? phase) => phase == "mph";\n'
        '\n'
        '    /// <summary>\n'
        '    /// D2.1 (UNIT-STATE-034): a vehicle step moves a vehicle of the phasing side that has not ended its move; only a Load spends no MP.\n'
        '    /// </summary>\n'
        '    public static bool StepAllowed(bool vehicle, bool phasing, bool movementEnded, int halfMp, bool load) =>\n'
        '        vehicle && phasing && !movementEnded && halfMp >= 0 && (halfMp != 0 || load);\n',
    )],
}
for path, pairs in edits.items():
    text = path.read_bytes().decode("utf-8")
    for old, new in pairs:
        n = text.count(old)
        if n != 1:
            sys.exit(f"{path.name}: expected 1 match, found {n}")
        text = text.replace(old, new)
    path.write_bytes(text.encode("utf-8"))
print("applied")
