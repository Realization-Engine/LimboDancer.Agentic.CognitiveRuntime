# ASL Unit Backlog Pass 22d Design

**Status:** Built. Pass 22d (the workspaces) of the [ASL Card Play and Map Studio Redesign Plan](<../ASL Card Play and Map Studio Redesign Plan.md>).

**Date:** 2026-09-30

**Related documents:** the [Scenario A1 Backlog Pass 22d Review](<Scenario A1 Backlog Pass 22d Review 2026-09-30.md>), the plan's sections 12.3, 12.4, 14, 15.3, 15.5, 16.6, 16.7, and 16.14, and the [ASL Unit Backlog](<../ASL Unit Backlog.md>), section 36.

The pass adds no rule, ruling, counter, or package. The pages compose the same services; the viewport script changed only to remove its host listener on dispose.

## 1. Outcome

- **The board viewport (22d.1).** B06 `BoardViewport` owns the host element, the script module, the viewport it makes, the .NET reference the script calls back, and their disposal. It tells the page once when the viewport is ready; the page loads the board when both the viewport and the board exist, in either order. Script callbacks go to plain delegates, so a hover redraws the page only when the page decides to. Commands (load, layers, units, LOS, highlight, tool, patch, comparison, focus, copy, fit) do nothing before the viewport exists or once disposal starts. A component disposed while the module or viewport is still being made disposes what it made. The script's dispose now removes its keyboard listener from the host, so a host never answers a key twice.
- **The board viewer (22d.2).** B01 toolbar (title, status, links, the view, Fit), B02 view picker, B03 comparison controls, B04 Layers panel (with the derivation trace in Hex facts), and B05 game and replay strip: units on or off, the source, S09 perspective, S10 revision, the counter style, and the perspective and revision in words (the adjudicator sees every unit; a side what it may see). The inspector has B15 `InspectorTabs`: Selection (B07 the unit, with B08 its facts in the game and U05 its details; B09 the units in the hex; the hex's facts), LOS (B10, with B11 inline: blocked, clear, not answered, not read, and "not definitive" on a board that is not verified), and Evidence (the pixel clicked, E06 the hex's grid samples, B12 the board's provenance). A unit clicked while the LOS tab is open keeps it. A route to another board clears the old LOS line and selection.
- **The board editor (22d.3).** B13 header: name, reference, status, version, saved or unsaved changes (an edit, or a name different from the saved one), Undo, Redo, Save or Save draft, and the last command's result. B14 tool options with E08 `TerrainPicker` (also in a feature's properties). B15 tabs for Properties, Layers, Validation (with its count; a board click keeps the Validation list open), and Hex. Leaving with unsaved changes asks first (`NavigationLock`), with the navigation's cancellation and no timeout of its own.
- **The Unit Lab (22d.4).** U01 template picker with the catalog source, U02 attached equipment keyed by its draft, U03 tier grid, U04 face gallery, U05 details, U06 findings through S06 (with each diagnostic's path), U07 document output with S14 `JsonDisclosure`, U08 style sheet in an Advanced section that opens when the sheet has errors (the face gallery then says why it is blank), and U09 placement with U10 inline. The unit editor groups Identity, then each face (E03, with E04 attributes and E01 inputs), the whole unit, and the states (a state that turns a face says which). The preview column stays beside the fields; the style sheet and placement follow below.

## 2. Tests

`WorkspaceComponentTests` (MapStudio): the viewport mounts once, forwards its callbacks, sends its commands to the viewport it made, and disposes it (later commands do nothing); the tabs' keyboard (Right, Left, End) and tab order; an LOS Location that is not read; the editor header's saved and unsaved states. The Unit Lab, Game states, and viewer tests pass unchanged through the components.

## 3. Left out

Section 36 of the backlog: the states grouped by kind, the viewer's unit sources and layer names in words, Home and End scrolling the inspector, and the editor at narrow widths (28c.2).
