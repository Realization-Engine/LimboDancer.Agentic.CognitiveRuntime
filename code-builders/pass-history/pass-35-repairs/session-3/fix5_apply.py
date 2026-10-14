"""Pass 35 review fix 5: a vehicle that changes its VCA, its MA's CA with it, without having fired on its acquired target in the phase loses its
Acquisition (C6.5, p. 174). Exact blocks, each asserted once; run once."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/ScenarioA1FireFollowUps.cs": [(
        """    public static bool AcquisitionLostByMoveOrTurn(string? firerWas, string? firerNow, bool turnedWithoutFiring, int shotsThisPhase) =>
        (firerWas is not null && firerNow != firerWas) || (turnedWithoutFiring && shotsThisPhase == 0);
""",
        """    public static bool AcquisitionLostByMoveOrTurn(string? firerWas, string? firerNow, bool turnedWithoutFiring, int shotsThisPhase) =>
        (firerWas is not null && firerNow != firerWas) || (turnedWithoutFiring && shotsThisPhase == 0);

    /// <summary>
    /// C6.5 (p. 174), D3.12: whether the acquiring weapon's CA changed: a Gun turned in place (C3.22), or a vehicle changed its VCA while its
    /// TCA is not kept apart from it, so that its MA turned with the hull. A vehicle whose TCA is recorded apart from its VCA keeps its MA's CA
    /// through a VCA change.
    /// </summary>
    public static bool AcquiringCaChanged(bool gunTurned, bool vcaChanged, bool tcaApart) => gunTurned || (vcaChanged && !tcaApart);
"""),
    ],
    "LimboDancer.Domains.Asl.Units/State/GameProjector.cs": [(
        """                // C6.5 (pass 35, task 35.11): lost when its firer leaves its Location, or turns without having fired on its target this phase.
                if (Rules.ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn(FirerAt(previous, acquisition.Gun), FirerAt(next, acquisition.Gun),
                    payload is GunTurned turned && turned.Gun == acquisition.Gun, previous?.OrdnanceShots.FirstOrDefault(item => item.Gun == acquisition.Gun)?.Shots ?? 0))""",
        """                // C6.5 (pass 35, task 35.11): lost when its firer leaves its Location, or turns without having fired on its target this phase. A
                // vehicle's VCA change turns its MA with it unless its TCA is kept apart (the referee's review).
                if (Rules.ScenarioA1FireFollowUps.AcquisitionLostByMoveOrTurn(FirerAt(previous, acquisition.Gun), FirerAt(next, acquisition.Gun),
                    Rules.ScenarioA1FireFollowUps.AcquiringCaChanged(payload is GunTurned turned && turned.Gun == acquisition.Gun,
                        payload is VehicleStepped { Kind: VehicleStepped.Turn } hull && hull.Vehicle == acquisition.Gun,
                        previous?.TurretFacings.Any(item => item.Vehicle == acquisition.Gun) == true),
                    previous?.OrdnanceShots.FirstOrDefault(item => item.Gun == acquisition.Gun)?.Shots ?? 0))"""),
    ],
}
for name, pairs in EDITS.items():
    text = open(ROOT + name, encoding="utf-8", newline="").read()
    for old, new in pairs:
        assert text.count(old) == 1, (name, text.count(old), old[:70])
    for old, new in pairs:
        text = text.replace(old, new)
    with open(ROOT + name, "w", encoding="utf-8", newline="") as out:
        out.write(text)
    print(name, len(pairs))
