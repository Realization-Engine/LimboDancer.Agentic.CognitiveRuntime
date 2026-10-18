# ASL Unit Figure Art Plan

**Started:** 2026-10-17, at the user's word: "Write it and then execute. Try to work without my intervention. Create a new branch for this work."
**Branch:** `unit-figure-art`, cut from `icon-lab-coverage` (50846cd3), whose pull request 17 had passed CI but was not yet merged; this branch carries it.
**Scope:** the infantry figures on the ASL counters only. The campaign layer (`src/Campaign`, `prototypes/europe-hex-map`) is a source of knowledge and references, and is not changed.

## 1. Why

The figures on personnel counters are rudimentary: a circle and a trapezoid, one to three of them for unit size, and the same figure for every nationality, every kind of personnel, and every state but broken (drawn prone). Only the color tells sides apart. The user's direction (2026-10-17):

- A figure for each nationality, researched, so a German squad and a Russian squad differ in outline as well as color.
- Midjourney for the body pose; the national kit drawn from known data.
- The fully realized counters seen while the art is made, in the Unit Lab, with the Icon Lab's workflow shared where practical.
- The counter states considered: single-man and multi-man counters, fanatic, berserk, prisoners, and the like.

## 2. Decisions

Made in the conversation of 2026-10-17; the ones marked *provisional* were proposed by Claude and not yet ruled on by the user. They hold until the user says otherwise.

| | Decision |
|---|---|
| F1 | **Body from Midjourney, kit from data.** The poses are generated with Midjourney, picked by the user, and traced to one filled path each. Helmets and weapons are drawn by Claude from the documented shapes and lengths of each nation's standard issue (section 6), never from Midjourney, which mixes nations' kit. |
| F2 | **The workflow lives in the Unit Lab** (user, 2026-10-17: "Should we be presenting this UI in 'Unit Lab' instead, and sharing components where practical?"). The Icon Lab's brief editor, prompt composer, and batch list are shared with it; each lab keeps its own plan file. |
| F3 | *Provisional.* **Nine poses:** standing rifleman, kneeling crew, leader, hero, wounded, broken, pinned (prone), berserk, prisoner (hands raised). |
| F4 | *Provisional.* **Markers stay markers.** Fanatic, CX, DM, disrupted, TI, the fire markers, concealment, and HIP do not change the figure, as on the physical counters; they keep their badges. |
| F5 | **One figure per side for now.** The catalog's eight sides: German, Russian, British, American, Finnish, Italian, Axis Minor, French. *Provisional:* Axis Minor is one figure; separate Hungarian, Romanian, Bulgarian, Slovak, and Croatian figures need a nationality field and go to the backlog. |
| F6 | *Provisional.* **No early-war and late-war variants** for now (M1917 against M1, SSh-36 against SSh-40); the backlog holds them, chosen later by the scenario's date. |
| F7 | **Sources are facts, not art.** The Osprey books in `E:\Archive\WWII-Docs\4-Armies-Units-and-Soldiers` and the Panzer Leader counters the campaign layer cropped are used for facts and as references in a brief; nothing is traced from them or committed (ASL-UNIT-072). |
| F8 | **Nothing changes until art exists.** A pose, helmet, or weapon without a file falls back to today's figure, so the golden SVGs change only when an accepted batch lands. |
| F10 | **The user's ruling, 2026-10-17: a squad stands in a staggered file,** each man half a pose width on and the man behind raised; every figure but the rearmost, and every helmet, is ringed in the counter's color, with the skull drawn again inside the helmet's ring so the face meets the helmet. A multi-man counter shows multiple men. |
| F11 | **The pose's weapon comes from the pick** (revising F1 after the first Midjourney run, 2026-10-17): a soldier drawn without a weapon came out as a civilian walking, so the pick carries its rifle and helmet; the tracer removes the helmet (`--bare`) and the side's helmet is drawn on the skull. The drawn weapons stay for poses traced without one. |
| F9 | **Same pose, same framing, for every nation,** so only the national kit differs; the kit is exaggerated (a larger helmet, a bolder weapon) where the far detail level needs it, since a figure there is about ten pixels tall. |

## 3. How a figure is made

A figure is composed at render time from three single-color parts:

- **A pose** (`units/figures/poses/<pose>.svg`): the traced body, bareheaded and unarmed, in a box one unit high, with two anchors: the **head** (center and radius, where the helmet sits) and the **grip** (a line from the rear hand to the front hand, along which the weapon lies). A prisoner's pose has no grip.
- **A helmet** per side (`units/figures/kit/<side>-helmet.svg`), drawn in head units: the head's radius is 1, centered at the origin.
- **A weapon** per side (`units/figures/kit/<side>-<weapon>.svg`), drawn in grip units: the rear hand at (0, 0), the front hand at (1, 0), so one drawing serves every pose that holds it.

The figure is the pose's path, the helmet moved onto the head anchor, and the weapon moved onto the grip, all filled with the slot's color, the same as today's figures. Squads, half-squads, and crews repeat it two or three times; the renderer spaces the copies by the pose's own width.

A build step (`code-builders/unit-figures/build_figures.py`) reads the SVG sources and writes one manifest, `units/figures/figures.json`, with each part's path data and anchors; the rendering project embeds it, as it embeds the style sheets.

The style sheets choose the pose: `glyph(size-figures, <pose>)`, by kind, face, and state. Today's `glyph(size-figures)` and `glyph(size-figures, prone)` keep working and mean the standing and the pinned poses.

## 4. Stages

Each stage lists what it changes, what proves it, and what the user decides at its end. Tests run on the blast radius: the projects a stage touches, filtered where possible; the whole projects at the end, and the strict build of the solution with warnings as errors.

### Stage 1. Shared art components

- Move the workflow parts of the Icon Lab (`IconBriefEditor`, `IconPromptBench`, `IconBatchList`, and the `IconPlan` store) so a second lab can use them with its own plan file, its own style block, its own vocabulary of keys, and its own way of previewing a pick.
- The Icon Lab works as before.
- **Proof:** the Icon Lab's tests, unchanged in substance; the Icon Lab checked in the Studio.

### Stage 2. The renderer reads figure files

- `units/figures/figures.json` embedded in `LimboDancer.Domains.Asl.Units.Rendering`; a `FigureArt` reader; `Glyphs.Draw` composes a figure from a pose, the unit's side's helmet, and its weapon when the files exist, and falls back to today's figure part by part.
- The glyph's argument names the pose; the side comes from the unit.
- `build_figures.py` and its test.
- **Proof:** the golden SVGs unchanged with an empty manifest; a new test that composes a figure from a small fixture manifest and pins its output; the rendering tests.

### Stage 3. The Unit Lab's Art tab

- A fourth group in the Unit Lab, **Art**, beside Fields, Preview, and More:
  - **Coverage:** a grid of side against pose, each cell the full counter rendered by the style sheet chosen, at the detail level chosen, with what each cell lacks (pose, helmet, weapon).
  - **Poses** and **Kit:** the parts drawn, with their briefs.
  - **Prompts** and **Batches:** the shared components on `units/figures/figure-plan.json`.
- **Proof:** bUnit tests of the tab; the tab checked in the Studio at 1440px and 1024px.

### Stage 4. The pilot: German and Russian

- The figure style block and the rifleman pose's brief and prompt, ready to send to Midjourney.
- The German (M35 helmet, Kar98k) and Russian (SSh-40 helmet, Mosin-Nagant 91/30) kit drawn from section 6.
- Until the user's Midjourney pick arrives, the pilot runs on a stand-in pose: today's figure redrawn with the head and grip anchors, so the kit can be judged on a counter.
- **Go or no-go (the user):** can a German and a Russian squad be told apart at the far detail level? If not, the kit is redrawn bolder or the approach is rethought before more work.

### Stage 5. The other eight poses

- Briefs and prompts for kneeling crew, leader, hero, wounded, broken, pinned, berserk, and prisoner; traced as the picks arrive.
- The style sheets choose them by kind, face, and state.

### Stage 6. The other six sides' kit

- British, American, Finnish, Italian, Axis Minor, French: helmets and weapons from section 6.
- Backlog entries for F5 and F6.

### Stage 7. Prisoners

- A captured unit (A20.5; `instance-captured`) is drawn in the prisoner pose. The renderer learns the relation from the game's projection, the same way it learns a state.

### Stage 8. Golden SVGs and the play check

- Each accepted batch regenerates the golden SVGs (`ASL_UNITS_UPDATE_GOLDEN=1`); the diff is read, not only accepted.
- Play and Replay checked in the Studio with the new figures.
- Strict solution build, the affected test projects whole, the Node viewport test, then the pull request's CI.

## 5. What needs the user

- **The Midjourney runs.** Claude writes each prompt in the Unit Lab's Prompts tab; the user runs it and saves the four results; Claude traces the user's pick. This is the only step Claude cannot take, so the stages above are ordered to leave as little as possible waiting on it: everything but the traced poses is built with stand-ins.
- The go or no-go of stage 4, and the provisional decisions of section 2.
- The merge into main, after a green CI run on the pull request.

## 6. The kit, from documented issue

Shapes in profile, as a soldier is seen on the counter, facing right. Lengths as a share of a soldier about 1.75 m tall, before the exaggeration of F9.

| Side | Helmet in profile | Rifle (squad) | Leader's weapon |
|---|---|---|---|
| German | M35 Stahlhelm: rounded crown, short front visor, flared skirt sweeping down over the ears and the back of the neck | Kar98k, 1.11 m (0.63) | MP40, 0.83 m stock extended (0.47) |
| Russian | SSh-40: rounded dome with a slight outward flare at the rim, no visor | Mosin-Nagant 91/30, 1.23 m (0.70); bayonet fixed, 1.66 m (0.95) | PPSh-41 with its drum, 0.84 m (0.48) |
| British | Mk II "Brodie": shallow dome, wide flat brim all round | Lee-Enfield No. 4, 1.13 m (0.65) | Sten, 0.76 m (0.43) |
| American | M1: smooth dome, short brim all round, the rim level | M1 Garand, 1.10 m (0.63) | Thompson M1A1, 0.81 m (0.46) |
| Finnish | M35-pattern steel helmet (German-supplied) or the M36 peaked field cap | Mosin-Nagant M/39, 1.18 m (0.67) | Suomi KP/-31 with its drum, 0.87 m (0.50) |
| Italian | M33: shallow rounded bowl with a small even brim and a crest-less top | Carcano M91, 1.29 m (0.74) | Beretta MAB 38, 0.95 m (0.54) |
| Axis Minor | One shared figure (F5): a German-pattern steel helmet with a rounder skirt, the Hungarian M38 and Romanian M39 being close to it | Mannlicher-pattern rifle, about 1.10 m (0.63) | A carbine as the leader's weapon |
| French | Adrian M26: a crest along the crown, peaks front and back | MAS-36, 1.02 m (0.58) | MAS-38, 0.73 m (0.42) |

Sources: the standard issue of each army, as documented in the general literature; the archive's Osprey titles confirm the German, British, American, and Italian kit. Where the archive has nothing (Russian, Finnish, French), the shapes above are well established; the user judges the drawings.

## 7. Record

Durations go in `src/ASL/docs/ASL Unit Time Log.md`. Out-of-scope items go in `src/ASL/docs/ASL Unit Backlog.md`. Progress is reported by stage number.

## 8. Status, 2026-10-17

| Stage | State | Commit |
|---|---|---|
| 1. Shared art components | Built | 0450b5bd |
| 2. The renderer reads figure files | Built | b0153b17 |
| 3. The Unit Lab's Art tab | Built | bc34d57c |
| 4. The pilot | Built up to the Midjourney run: the pose tracer, the German and Russian kit, the prompts | cabe9c26 |
| 5. The other eight poses | Briefs and prompts written, style rules in place; waiting on the Midjourney runs | bc34d57c |
| 6. The other six sides' kit | Built | cabe9c26 |
| 7. Prisoners | Built: `asl:captured` is a vocabulary state (asl@1.17.0) | 08e56fd8 |
| 8. Golden SVGs and the play check | The suite run on the branch; golden SVGs unchanged but for the new state | |

**Where the build left the plan.**

- Stage 1 did not move the Icon Lab's components into a shared folder. They stay in `Components/Icons` and take an `IArtShelf` (what a lab has drawn) in place of the icon library, which made them shared with less churn.
- Poses gained a height (stage 3): a lying pose is cropped to the body, or the pinned figure draws as a hairline.
- The pose style asks Midjourney for stocky figures: beside the old drawn figures, the thin geometric stand-ins read weakly at the far level.
- The pinned pose falls back to the standing figure (as pinned units were drawn before) and the broken pose to the prone one, so no counter changes until art is accepted.

**What waits on the user.**

1. The Midjourney runs: the Unit Lab, Art, Prompts tab, one pose at a time (or a set of four). Save the four results; name the pick in Batches.
2. Claude traces the pick: `python code-builders/unit-figures/trace_pose.py <image> <pose> --cell <n>`, sets the grip by eye, rebuilds `figures.json`, and shows it in the Art tab.
3. The go or no-go of stage 4 on the German and Russian squads at the far level.
4. Accepting a pose or a kit part (`data-accepted="true"`), which changes the counters in play and regenerates the golden SVGs (stage 8).
5. The provisional decisions F3 to F6.
