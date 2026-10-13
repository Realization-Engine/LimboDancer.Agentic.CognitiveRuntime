# Pass 50, second session: the gate

Repository `E:\Archive\GitHub\dlandi\LimboDancer.MCP`. Pass 50 (Armored combat I) is built to the merge gate on branch `feature/asl-backlog-pass-50`, head 702024df, cut from main at b4ce31ef. Nothing is pushed or merged. The branch `main` is checked out in a Codex worktree (`C:/Users/dland/.codex/worktrees/formation-design-review`), and a second Codex worktree holds `asl-narrative-extensions`; leave both alone. This prompt is untracked until the session's first commit.

Read `src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md` first and follow it; its last nine lessons are pass 50's. Then read the [pass 50 design](<../Passes/ASL Unit Backlog Pass 50 Design.md>), sections 10 and 11, and the [pass 50 review](<../Passes/Scenario A1 Backlog Pass 50 Review 2026-10-12.md>).

## The user's word for this session

On 2026-10-12, after the first session's report, the user said: "Proceed with the gate". That is the word for the gate and for nothing after it.

- **Run the gate, then stop before the merge.** The merge, the push, and any closing commit on main are each the user's to start, by name.
- **Work alone through the gate.** A failure is diagnosed and repaired on the branch, committed, and the affected steps run again. Do not stop for a style error, a failing test, or a differing proof line; stop only for what the first session's prompt lists (anything on `main`, on the Codex worktrees, or on the Studio at 5178; a force push; a catalog version; a tool call refused with "no verdict").
- **The user has not answered the first session's report.** Everything in it is still open to a veto: build on it, and if the user's first message vetoes something, the design's section and the Passes Plan's row name it.

## What the first session left open to a veto

- **Not built, against the plan's entry:** task 50.4, Gun Duels (to pass 55), and task 50.5, MG fire against armor (to pass 60).
- **Not built, against the frame:** a BMG's hull pivot for a shot; a turret turned without fire at the end of a fire phase; "one hex for all weapons" (D3.51); the loss of the MA's Acquisition by a CMG; D2.18. The design's section 10.1 has the full list, and the backlog's section 58 the rows.
- **Rulings R50.1 to R50.14,** all made without the user, in the Passes Plan's section 5. R50.13 (no Acquisition DRM in Bounding First Fire, C6.55) came from the reviews' one BLOCKER.

## Start

1. `git fetch origin`, then `git status`, `git log -3 --oneline origin/main`, `git worktree list`. Confirm the branch is `feature/asl-backlog-pass-50` at 702024df and the tree is clean but for `src/ASL/boards`, `.claude`, and this prompt.
2. **Check whether main has moved from b4ce31ef.** If it has, say so first: the baseline is built from main as it stands, and a file changed on both sides is the user's to hear of before the merge.
3. Check that no Studio of the last session runs on 6671 (`preview_list`, and the `dotnet` processes by command line).

## The gate

The steps are section 1.6 of the standing rules. In `E:\Archive\GitHub\dlandi\pass32-tools`, pass 45's scripts are the form: `gate45-baseline.sh`, `gate45-final.sh`, `gate45-gate.sh`. Copy each to a `gate50-` name and change the baseline commit, the two checks, the output folders, the skip lists, and the swept games.

1. **The store.** It holds 461 games. Bring it up to date with the live folder before the baseline: the pass's own games are `p50-*` in `src/ASL/boards/units/live/5a7d1f000000400080000000000057d0/`. Take the recorded ones (`p50-mg-both2`, `p50-mg-turn2`, `p50-mg-first2`, `p50-repair2`, `p50-repair-ctl2`, `p50-bff-outset2`, `p50-bff-move2`, `p50-afph2`, `p50-afph-ctl2`, `p50-same-rear`, `p50-same-front`, `p50-walk320`); those labelled SUPERSEDED were recorded by earlier builds of the pass and may not replay, which is no fault.
2. **The baseline, from main's own sources** (`git archive` of b4ce31ef, or of main if it moved, into a folder of its own; check the archive holds `ScenarioA1UpperLevelEncirclement` and no `VehicleWeaponShots`): the build, the text list, the replay digest, the planner sweep. Start it first and alone; it runs about 40 minutes. Main cannot read a game that holds a `delay` step or a tank's named weapons as the branch does: expect differing or refused lines for the `p50-*` games on the baseline's side, and name them.
3. **The gate's side:** the solution build with warnings as errors into `bin/gate`; the proof tool rebuilt against `bin/gate` and its copy compared with `cmp`; the whole local suite one project at a time with `--no-build`; CI's Node viewport test by hand; the chart supplement regenerated; the text list; the replay digest; the sweep with its comparison; the Docker Linux check on the committed branch.
4. **`PROOF_SKIP` for the gate's side:** pass 45's list, and pass 50's from `pass50/blast_review.sh` (`GameState.VehicleWeaponShots`, `FireResolved.TurretFacing`, `RepairAttempted.Weapon`, `FireVehicleFire.Weapons`, `FireVehicleFire.OtherPhase`, `FireVehicleFire.InWoodsOrBuilding`, `FireWeaponEffect.VehicleWeapon`, `OrdnanceVehicleFirer.Weapons`, `FireSpentFacts.VehicleMgUnfired`, `VehicleStepped.Turret`, `OrdnanceVehicleFirer.MpInLos`, `OrdnanceVehicleFirer.BoundingFirer`). The first session wrote that list by hand; check it against `pass45/new_properties45.py` run over `b4ce31ef..HEAD`, which will also find `VehicleWeaponShot.Step`, `OrdnanceVehicleWeapons.BoundingFireSpent`, `VehicleWeaponsFacts.BoundingFireMarked`, and `FireVehicleWeapon`'s members. `PROOF_SKIP_JSON` needs the keys the pass added to a record's JSON: `weapons` on a vehicle firer of an ordnance shot at the least; read a differing fire plan before guessing the rest.
5. **The sweep's games:** pass 45's eleven, and games that hold what this pass changed: a tank that fires its MA (`p30-armor`, `p35-tow2`), an OVR, a vehicle repair, and two of the pass's own (`p50-bff-move2`, `p50-mg-turn2`).

**How a difference is read:** as in pass 45's design, section 14. Every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault. What to expect:

- **The replay digest:** no line of a game made before the pass may differ. The first session ran it four times against pass 45's last digest with that result, but against the copies of `bin/p31b`, not a baseline built from main.
- **The sweep:** differences on purpose. A tank is now offered as a firer on the IFT; its MA is no longer refused in its own MPh, nor in the AFPh after it moved or from Motion; a tank's MA that kept its ROF in the PFPh is now refused in the AFPh (D3.5); the repair of a tank's weapon is offered by weapon, and a tank's MA is no longer offered as an AAMG; a vehicle's `delay` step exists; ordnance at a vehicle in the firer's own Location is no longer refused; the refusals' words changed. Every ordnance plan by a tank now carries `vehicle.weapons`.
- **The text list:** the new and reworded sentences of the pass.
- **A digest and a sweep that differ in nothing are a void run.**

**Known before the gate starts:**

- The Rules and Play test projects have not been run whole on the branch; only the classes the pass touches (Rules 308, Play 174). Units (409) and MapStudio (390) ran whole. A test elsewhere that asserts an old refusal ("vehicle-fire-outside" for a moved or Motion firer, "gun-already-fired", a repair's words, a proposal's "DRM case-" words) may fail, as `ScenarioA1Pass7Tests.AMovingFirerIsOutside` did; read a failure against the rule before changing either side.
- The two test projects of the pass build with warnings as errors; the solution as a whole has not been built so.
- `AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked` is a known race between test classes in Docker (backlog 57.2): a red run for that test alone is run again.
- The first session's replay tool is `proof-p50`, bound to `bin/p31b`. The gate's is bound to `bin/gate`.

## After the gate

Write the gate's section in the design (section 12, as pass 45's section 14: a table of the steps and results, what the gate found and each fix, what went wrong in the gate itself), a line in the review's Status, the time log's row with times read from the clock, any lesson in the standing rules, and the memory. Commit them. Then stop and report: what the gate found first, then each step's result, then what is still open to a veto. Do not merge.

If the user then says to merge: in a temporary worktree as the standing rules say (`git worktree add -b merge-50 E:/Archive/GitHub/dlandi/m50 origin/main`), `--no-ff`, push with no force, read the CI run, and ask for the closing commit on main by name.
