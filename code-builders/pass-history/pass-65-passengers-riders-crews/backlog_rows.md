# Backlog rows of pass 65, gathered for the documents step

Each row is filed in `src/ASL/docs/ASL Unit Backlog.md`, a new section "Added by pass 65", at the documents step. Gathered from the increments' reports (notes.md) and the fix rounds.

## From the fix rounds

- Pass 105: D5.311, an OT AFV whose crew's CE DRM is reduced below normal by Elevation Effects or Air Bursts is treated as unarmored; the attack on it (not on its PRC) is resolved per A7.308 or on the TK Table with its Unarmored Vehicle TK#, with no Air Burst TEM (R1 finding 7, Round A).
- D5.311: a crew already broken inside its AFV attacked again by an Unprotected attack (`UnprotectedCrewOutcome` does not know it is broken) (Round A).
- D5.311: the failure to rout of a broken crew inside its AFV does not Abandon the AFV (Round A).
- `hazardous-movement` still reaches an AFV crew's CE line and an unarmored vehicle's Vehicle line (A4.62 is for the units moving) (Round A).
- D3.43: a re-crewing unit that leaves its vehicle by a failed TC, a Recall's Abandonment, or Survival (`instance-moved`) does not carry the vehicle's Armor Leader; the record needs an `armorLeader` fact (R2 finding 3, Round B).
- D6.82 (p. 206): a Carrier's inherent crew is a HS unless the Carrier is "specified as towing ordnance"; `InherentHalfSquad` reads whether it tows a Gun now. Read the scenario card's specification once cards name it (R2 finding 15, Round B).
- Victory's `HasInherentCrew` still scans the state's units once per armed vehicle for its crew counter by id; an index of crew counters by vehicle in `ObjectIndex` would remove it (R5, Round C).
- To check: a truck's Vehicle line came to the Original DR + 1 in a Round A test (DR 6 immobilized at KN 7); which +1 it takes (Round A).
