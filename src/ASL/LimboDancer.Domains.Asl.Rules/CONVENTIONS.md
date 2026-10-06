# Conventions of the Rules project

**Status:** Standing, from pass 32 (the Rules migration; the plan's section 19, decision 7, and section 22.2). The pass 32 design, `docs/Passes/ASL Unit Backlog Pass 32 Design.md`, gives the reasons.

1. **Every ASL rule is a calculator here.** It takes facts and returns a verdict. Play reads the state and the map, hands the facts over, and writes the events; the Units projector asks the same calculators and applies the answer. Neither decides a rule of its own.
2. **This project references Abstractions only.** Never Units, Maps, or Play. Units and Play reference this project.
3. **Facts are plain.** Ints, bools, strings, and this project's own records. A three-valued fact is a `bool?`. A Location, a unit, a hexside, a facing is the string or the int the caller gives; a refusal text uses it as it stands. No type of Units or Maps crosses into this project.
4. **A fact reader is for a search only.** A calculator whose decision is a search or a scan may take a small interface declared here, implemented in Play, that returns facts: neighbors, a step's facts, who sees a Location. No reader hands over the state or the map as a whole.
5. **The recorded fact records keep their shape.** `FireAttack`, `RallyAttempt`, `CloseCombatFacts`, `VehicleCloseCombatFacts`, `AmbushFacts`, and `OrdnanceShot` are compared as serialized text by the record verifiers. No field is added, removed, renamed, or reordered.
6. **The order of checks is behavior.** A calculator returns the first refusal its checks give, in the order the member had them before it moved. Every refusal text is kept to the character.
7. **Two copies of a rule that differ stay two functions,** each named for where it is used, until a rule pass decides between them. The pass 32 design's section 12 lists them.
8. **A move changes no result.** The proofs: the recorded and live games replay to the same states and diagnostics, the planner answers the same requests the same way, and every refusal text and roll key is unchanged. A public name that tests or the Studio call stays where it was as a one-line forward.
9. **File names** take the `ScenarioA1` prefix. Definitions and tables are in `ScenarioA1Definitions.cs`, hex geometry in `ScenarioA1Geometry.cs`, result tables in `ScenarioA1ResultTables.cs`; a subsystem's calculator, models, and costs are files of their own beside the existing calculators, which do not grow.
