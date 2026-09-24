# Scenario A1 concealment after defender resolution

The existing published occupied-case package keeps `A1-concealed-occupancy-attempt` indeterminate before the defender reveals a unit. This separate [post-reveal package](<./SourceRegistry/asl-scenario-a1.post-reveal-package.json>) admits two exact, test-supplied outcomes under A12.15. It pins the earlier package digest and the existing verified A12.15 and A4.14 fragments to the edition 3.01 PDF (physical pages 78 and 49, printed pages A36 and A7). The xUnit source review checks those fragment IDs and digests against the registered inventory and verified TIR records.

| Supplied defender resolution | Bounded read-only conclusion |
| --- | --- |
| A non-Dummy unit is revealed; no A4.14 exception or Infantry overrun election applies | The ordinary mover is forced back to its previous Location, spends the attempted MF there and ends its MPh. This conclusion does not resolve subsequent fire, residual FP, FFE or minefield effects. |
| All defenders in the Location are Dummies | Remove the Dummies; the mover may continue from the attempted Location without being forced back. This is a qualified continuation, not an executed move. |

Both cases require an unconcealed, non-Dummy ordinary Infantry attacker making a non-bypass obstacle-entry attempt in the MPh, an explicitly identified previous Location, a versioned board 01 ground-level building override, and no special modifier. The defender resolution is a **supplied state variable**. Neither the board image nor the metadata reveals concealed counters. Unknown reveal, concealed or Dummy attacker, bypass, exception, overrun election, variant terrain, changed version, and extra or missing evidence refuse a conclusion. A12.151 bypass, concealed-attacker verification, and follow-on attacks remain separate branches. Optional A4.15 overrun after an A12.15 SMC reveal now has its [own exact package review](<./Scenario A1 Concealed SMC Infantry OVR Review.md>). A declined election reaches this earlier package only under its own exact observation contract.

The published seven-case package and its admission digest are unchanged. A caller must request this new exact package version to use post-reveal conclusions. An observation and conclusion never mutate a board snapshot or execute movement.
