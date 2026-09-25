# ASL Unit Display Design

**Status:** Accepted design, with the open questions decided (section 15). Phase 1 (Personnel and SW) is built; section 16 records what was built and where it departs from this design.

**Date:** 2026-09-24

**Baseline:** `main@24fa67b`

**Scope:** How units are described and drawn on digital maps: a unit vocabulary, unit documents, unit style sheets, a layout and SVG renderer, and a Unit Lab in Map Studio. The first phase covers Personnel and support weapons (SW). Guns, vehicles, and entities that are not units follow on the same design (section 13).

**Parent requirements:** ASL-UNIT-070 to 072 in the [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>). Section 14 proposes the requirement changes this design needs. It supersedes the placeholder counter style of the [ASL Unit Counter Map Rendering Design](<ASL Unit Counter Map Rendering Design.md>); that document's fixture binding, isolation, and interaction rules still apply.

**Source:** Rule and page references are to the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`), by physical page.

## 1. Outcome

A designer can describe any unit, from the ASL rulebook or of their own invention, as a structured document, and see it drawn on any board in Map Studio. What the unit is and what it says are kept apart from how it looks, the way HTML is kept apart from CSS:

- the **vocabulary** declares what kinds of unit exist and what can be said about them, like a schema of HTML elements and attributes;
- a **unit document** says what one unit is and shows, like an HTML page;
- a **style sheet** says how units look, like CSS, with selectors that match kinds, attributes, traits, and states;
- the **renderer** lays out a document under a style sheet and writes deterministic SVG.

The ASL rulebook becomes one vocabulary pack (`asl`) with two style sheets: one that reproduces the information of cardboard counters with digital clarity, and one that follows the printed conventions closely. New unit concepts are new packs and new style rules, not new code.

## 2. Principles

1. **Structure is not style.** A document never says "underline the FP". It says the unit has Assault Fire; a style sheet decides how that shows.
2. **Information parity.** Under the ASL style sheets, every fact a printed ASL counter conveys can be read from the digital unit (the parity table in section 4 is the test). The look is free.
3. **Open vocabulary.** Kinds, attributes, traits, and states are declared data. A new pack can extend ASL kinds or define unrelated ones. No enumeration in code closes the set.
4. **Rules stay out of drawing.** The renderer draws any document the vocabulary accepts, including combinations no ASL rule allows. A separate plausibility check (section 11) warns; it never blocks drawing.
5. **Deterministic output.** The same vocabulary, document, style sheet, detail level, and perspective produce the same SVG bytes, written through `SvgWriter` with fixed-point numbers.
6. **Perspective first.** A document handed to the renderer is already what its viewer may know (ASL-UNIT-031). A concealed unit arrives as a concealed placeholder, not as a unit with a hide flag.

## 3. The three layers

### 3.1 Vocabulary

A vocabulary pack is a versioned JSON file declaring four things.

**Kinds** are the element types. Each kind has a name, a label, an optional parent it extends, the faces it has, and the attributes and traits it accepts. Kinds form a tree, so a selector for a parent matches every kind below it:

```text
asl:unit
  asl:personnel
    asl:mmc           squad, half-squad, crew
      asl:squad
      asl:half-squad
      asl:crew
    asl:smc
      asl:leader
      asl:hero
asl:equipment
  asl:sw
    asl:mg            LMG, MMG, HMG as a size attribute, not three kinds
    asl:ft
    asl:dc
    asl:latw          ATR, PSK, BAZ, PIAT, PF (ordnance values arrive with Guns)
    asl:light-mortar
    asl:radio         radio and field phone
```

**Attributes** are typed values: `integer`, `text`, `enumeration` (with its members), `rating` (an integer with an optional sign, such as a leadership DRM of -2), and `list`. Each has a name, a label, an optional unit, and whether it belongs to a face or to the whole unit.

**Traits** are named capabilities that are either present or absent, like HTML classes: `asl:assault-fire`, `asl:spraying-fire`, `asl:elr-5`, `asl:self-rally`.

**States** are conditions a unit can be in, like CSS pseudo-classes: `asl:pinned`, `asl:cx`. A state may switch the displayed face (broken shows the broken face) or belong to an exclusive group (a unit cannot be both berserk and broken).

Names are namespaced by pack (`asl:`, or for example `sla:` for a Squad Leader Apocalypse pack). A pack may extend another pack's kinds and add attributes to them. It may not change another pack's declarations. The vocabulary has a version identity like a board package; documents and style sheets record the vocabulary versions they were written against.

### 3.2 Unit documents

A unit document describes one displayed unit. It is a projection, not game state: it carries what the viewer may know, in the vocabulary's terms.

```json
{
  "vocabulary": ["asl@1.0.0"],
  "id": "ger-467-a",
  "kind": "asl:squad",
  "side": "german",
  "location": "bd01:E4:0",
  "faces": {
    "front": {
      "firepower": 4, "range": 6, "morale": 7,
      "identity": "A", "class": "1st-line",
      "traits": ["asl:assault-fire"]
    },
    "broken": { "broken-morale": 7, "bpv": 10, "traits": ["asl:self-rally"] }
  },
  "unit": { "smoke-exponent": 1 },
  "states": ["asl:pinned"],
  "attached": [
    {
      "id": "ger-lmg-1",
      "kind": "asl:mg",
      "faces": {
        "front": { "size": "light", "firepower": 3, "range": 6, "breakdown": 12, "rate-of-fire": 1, "portage": 1 },
        "malfunctioned": { "repair": 6 }
      }
    }
  ],
  "stackOrder": 0
}
```

- `faces` holds each face the kind declares; which one shows follows from the states. A broken squad shows its `broken` face; a malfunctioned MG its `malfunctioned` face.
- `unit` holds attributes that belong to the unit rather than one face.
- `attached` holds possessed equipment (A4.43): SW rendered with its owner, not as a separate counter underneath.
- `location` uses the map model's locations, so the level within a hex is known (A2.8).

The values in this example are illustrative, not a catalog entry (ASL-UNIT-012).

### 3.3 Style sheets

A style sheet is a text file in a small CSS-like language: rules of selectors and declarations, applied by a cascade.

```css
/* The ASL digital theme, Personnel excerpt */
@tokens {
  side-german: #7b8c97;
  side-russian: #9b7b52;
  ink: #17212b;
}

asl|personnel {
  face-template: "class  .     size"
                 "fp     range morale"
                 "ident  ident ident";
  fill: side(fill);
  stroke: token(ink);
}

asl|personnel::slot(firepower) { content: attr(firepower); font-weight: bold; }
asl|personnel::slot(range)     { content: attr(range); }
asl|personnel::slot(morale)    { content: attr(morale); }

asl|personnel.asl\:assault-fire::slot(firepower) { mark: underline; }
asl|personnel.asl\:spraying-fire::slot(range)    { mark: underline; }
asl|personnel.asl\:elr-5::slot(morale)           { mark: underline; }
asl|personnel[smoke-exponent]::slot(firepower)   { superscript: attr(smoke-exponent); }

asl|leader {
  face-template: "drm" "morale";
}

asl|unit:asl\:pinned { badge: "PIN" top-right; }
asl|unit:asl\:broken { face: broken; fill: side(fill-muted); }

@detail far {
  asl|personnel { face-template: "glyph"; }
  asl|personnel::slot(glyph) { content: glyph(size-figures); }
}
```

**Selectors** match:

| Selector | Matches | Like CSS |
|---|---|---|
| `asl\|squad` | a kind and every kind that extends it | element type |
| `.asl\:assault-fire` | a trait on the shown face | class |
| `[class=elite]`, `[smoke-exponent]` | an attribute's value or presence | attribute selector |
| `:asl\:pinned` | a state | pseudo-class |
| `[side=german]` | the side | attribute selector |
| `::slot(firepower)` | one region of the face | pseudo-element |
| `::attached(asl\|mg)` | possessed equipment of a kind | child combinator |
| `:concealed` | a concealed placeholder (section 7.3) | pseudo-class |

**Declarations** are a fixed set of properties:

- layout: `face-template` (named regions, like `grid-template-areas`), `face-size`, `shape`, `corner-radius`;
- paint: `fill`, `stroke`, `stroke-width`, `opacity`, `pattern`;
- text: `content`, `font-size`, `font-weight`, `color`;
- marks: `mark` (`underline`, `overline`, `box`, `circle`) and `superscript`, which carry the rulebook's typographic codes when a sheet wants them;
- decoration: `badge`, `glyph`, and `face`, which picks the face to show.

Values are literals, `attr(name)`, `token(name)`, `side(role)`, and `glyph(name, argument)`.

**Cascade:** as in CSS, a declaration from a more specific selector wins, and between equal specificity the later rule wins. Specificity counts states and traits above attributes above kinds. A kind selector's specificity grows with the kind's depth, so `asl|squad` beats `asl|personnel`. There is no inheritance of property values between units, and no `!important`.

**`@detail near | mid | far`** blocks apply at the named zoom tier (section 7.1). **`@tokens`** defines named values; **`side()`** reads the side's palette (section 7.4).

The language is deliberately small, and is parsed by our own parser. It is not browser CSS, and nothing reaches the browser except the SVG it produces.

## 4. The ASL pack: Personnel and SW

Each row is one fact a printed counter conveys: where the rulebook defines it, the vocabulary term that carries it, and how the two ASL style sheets show it. The `asl-classic` sheet keeps the printed convention; the `asl-digital` sheet chooses a clearer digital form. This table is the information-parity test (principle 2).

### 4.1 Personnel

| Printed fact | Rule, page | Vocabulary | `asl-classic` | `asl-digital` |
|---|---|---|---|---|
| Personnel kind: SMC or MMC; squad, HS, or crew | A1.1 to A1.123, p. 44 | kind | silhouette of 1, 2, or 3 figures | size glyph (1, 2, 3 figures) and kind label on hover |
| Firepower | A1.21, p. 44 | `firepower` | left number | left number |
| Smoke Placement Exponent | A1.21, p. 44 | `smoke-exponent` (unit) | superscript after FP | small smoke badge with the exponent |
| May use Assault Fire | A1.21, p. 44 | trait `asl:assault-fire` | FP underlined | FP underlined and "AF" in the detail panel |
| Normal Range | A1.22, p. 44 | `range` | middle number | middle number |
| May use Spraying Fire | A1.22, p. 44 | trait `asl:spraying-fire` | range underlined | range underlined |
| Morale | A1.23, p. 44 | `morale` | right number | right number |
| ELR of 5 | A1.23, p. 44 | trait `asl:elr-5` | morale underlined | morale underlined |
| Identity letter, or crew numeral | A1.24, p. 44 | `identity` | letter after the Strength Factor; numeral above the silhouette | small identity tag |
| Class (E, 1, 2, G, C) | A1.25, p. 44 | `class` | letter or number upper right | class tag upper right |
| Class variant (circle or square) | A1.25, p. 44 | `class-variant` | circle or square round the class | tag shape |
| Broken side | A1.4, p. 45 | face `broken`, state `asl:broken` | reverse side, prone figures | broken face with muted fill |
| Broken Morale Level | A1.4, p. 45 | `broken-morale` (broken face) | large number lower right | large number |
| Can Self-Rally | A1.4, p. 45 | trait `asl:self-rally` (broken face) | broken morale in a square | broken morale in a square |
| Basic Point Value | A1.4, p. 45 | `bpv` (broken face) | small number upper left | detail panel only |
| No broken side (heroes, Japanese squads and leaders) | A1.4, p. 45 | the kind or document declares no `broken` face | not applicable | not applicable |
| Special status by SSR or DYO | A1.5, p. 45 | trait list (for example `asl:sapper`, `asl:assault-engineer`) | not printed | badge |
| Unit Size Number | A1.6, p. 45 | `unit-size` (unit; from the kind, not printed) | not printed | detail panel |
| Leader Leadership DRM (top) and Morale (bottom) | A10.7, p. 68 | `leadership`, `morale` | two numbers, DRM on top | DRM badge and morale |
| Hero Strength Factor 1-4-9 and wounded side 1-3-8 | A15.2, p. 83 | kind `asl:hero`, faces `front` and `wounded` | wounded reverse side | wounded face |

### 4.2 Support weapons

| Printed fact | Rule, page | Vocabulary | `asl-classic` | `asl-digital` |
|---|---|---|---|---|
| MG Strength Factor: FP and Normal Range | A9.1, p. 62 | `firepower`, `range` | FP-range | FP-range |
| MG size | A9.1, p. 62 | `size` (light, medium, heavy) | LMG, MMG, HMG label | label |
| Multiple ROF | A9.2, p. 62 | `rate-of-fire` | number in a square | ROF badge |
| Breakdown Number, B# or X# | A9.7, p. 65 | `breakdown`, trait `asl:breakdown-removes` for X# | "B11" or "X11" | "B11" or "X11" |
| Portage cost, #PP | A4.4, p. 50 | `portage` | "2PP" | "2PP" |
| Malfunctioned side with its Repair Number, "R#" (a repair dr of 6 eliminates the SW by rule; that is not printed) | A9.7 to A9.72, p. 65 | face `malfunctioned`, `repair`; state `asl:malfunctioned` | reverse side, "R#" | malfunctioned face with a warning stripe and "R#" |
| Flamethrower: FP 24, Normal Range one hex | A22.1, p. 89 | kind `asl:ft`, `firepower`, `range` | FT counter | FT glyph with values |
| Demolition Charge: 30 FP | A23.1, p. 90 | kind `asl:dc`, `firepower` | DC counter | DC glyph with value |
| Radio or Field Phone: Radio Contact value, possibly a dated series such as 6/7/8 with the dates on the reverse | C1.2 to C1.23, p. 163 | kind `asl:radio`, `contact` (a list), `contact-dates` (reverse face), trait `asl:field-phone` | "6/7/8"; dates on the reverse | radio glyph with the value in force at the scenario date, series in the detail panel |
| LATW and light mortars | C13.1, p. 183 | kinds `asl:latw`, `asl:light-mortar`; ordnance attributes deferred | counter | glyph and label; ordnance values with Guns (section 13) |

### 4.3 States

On cardboard these are separate marker counters stacked on the unit. Here they are states of the unit, shown by style rules.

| State | Rule, page | Vocabulary | `asl-digital` |
|---|---|---|---|
| Broken | A10, pp. 65 to 71 | `asl:broken` (switches to the broken face) | broken face, muted fill |
| Pinned | A7.8, p. 58 | `asl:pinned` | "PIN" badge |
| Counter Exhaustion | A4.51, p. 51 | `asl:cx` | "CX" badge |
| Desperation Morale | A10.62, p. 68 | `asl:dm` | "DM" badge |
| Temporarily Immobilized | A4.8, p. 52 | `asl:ti` | "TI" badge |
| Berserk | A15.4, p. 83 | `asl:berserk` | red frame and badge |
| Fanatic | A10.8, p. 69 | `asl:fanatic` | frame and badge |
| Wounded (SMC) | A15.2, p. 83; A17, p. 85 | `asl:wounded` (switches a hero to its wounded face) | wounded face or badge |
| Disrupted | A19.12, p. 86 | `asl:disrupted` | badge |
| Concealed | A12, pp. 76 to 80 | a concealed placeholder document (section 7.3) | "?" placeholder |
| Malfunctioned (SW) | A9.7, p. 65 | `asl:malfunctioned` | malfunctioned face |

States are open: a pack adds its own, and a style sheet decides how they show. Badges beyond the face size collapse into a count with the full list in the inspector.

## 5. Creating new units

A new unit concept is a new pack, some documents, and some style rules; no code changes. For example, a Squad Leader Apocalypse pack might say:

```json
{
  "pack": "sla", "version": "0.1.0", "extends": ["asl@1.0.0"],
  "kinds": [
    { "name": "sla:scavenger-band", "extends": "asl:mmc", "label": "Scavenger band", "faces": ["front", "broken"] },
    { "name": "sla:drone", "extends": "asl:equipment", "label": "Recon drone", "faces": ["front", "downed"] }
  ],
  "attributes": [
    { "name": "sla:supply", "type": "integer", "label": "Supply", "scope": "unit" },
    { "name": "sla:endurance", "type": "integer", "label": "Endurance", "scope": "face" }
  ],
  "traits": [ { "name": "sla:scrounger", "label": "Scrounger" } ],
  "states": [ { "name": "sla:irradiated", "label": "Irradiated" } ]
}
```

A scavenger band extends `asl:mmc`, so every ASL Personnel style rule already draws it; its own rules add what is new:

```css
sla|scavenger-band { face-template: "class . supply" "fp range morale" "ident ident ident"; }
sla|scavenger-band::slot(supply) { content: attr(sla\:supply); badge-shape: circle; }
sla|drone { shape: circle; face-template: "glyph" "endurance"; }
asl|unit:sla\:irradiated { badge: "RAD" top-left; stroke: #6a8f1f; }
```

A kind need not extend an ASL kind at all; it then starts from the base `unit` style of whichever sheet is in use.

## 6. Components and code

| Component | Project | Responsibility |
|---|---|---|
| Vocabulary, vocabulary loader and validator | `LimboDancer.Domains.Asl.Units` | Packs, kinds, attributes, traits, states; version identity; validates documents against the vocabulary. This is the taxonomy the domain model will reuse (ASL-UNIT-010). |
| Unit document model and JSON reader | `LimboDancer.Domains.Asl.Units` | Documents, faces, attached equipment, placeholders. |
| Style sheet parser and cascade | `LimboDancer.Domains.Asl.Units.Rendering` | The style language of section 3.3; computes the style of each face region at a detail level. |
| Layout and SVG writer | `LimboDancer.Domains.Asl.Units.Rendering` | Face templates, marks, badges, glyphs, attachments, stacks; writes SVG with `SvgWriter`. |
| Unit overlay | `LimboDancer.Domains.Asl.Units.Rendering` | Anchors units on a board or composed map (`BoardGeometry`, `VaslMap.Locate`) and produces `layer-units`. Replaces `DemoUnitOverlay` in Map Studio. |
| Plausibility check | `LimboDancer.Domains.Asl.Units` | Optional rule-based warnings (section 11). |
| Unit Lab and overlay integration | `LimboDancer.Domains.Asl.MapStudio` | Section 10. |

`Units.Rendering` references `Units` and `Maps.Rendering`; neither references Blazor (as for the map projects, ASL-MAP-003).

## 7. Layout and rendering

### 7.1 Size and detail levels

A face is square, sized in board pixels relative to the hex: by default 0.55 of the hex height (about 35 pixels on a standard board, close to a half-inch counter on a 3/4-inch hex). This is a starting value to try in the Unit Lab, not a fixed rule: `face-size` is an ordinary style property, so a sheet can change it. The detail level follows the viewport's zoom, measured as screen pixels per board pixel:

| Tier | When | Default content |
|---|---|---|
| `far` | the face would be under about 16 screen pixels | side-colored shape and size glyph |
| `mid` | about 16 to 40 screen pixels | Strength Factor or leader values, class, state badges |
| `near` | over about 40 screen pixels | every slot, marks, identity, attachment values |

The overlay renders all three tiers once and the viewport switches between them by zoom, so zooming does not ask the server again.

### 7.2 Faces, attachments, and stacks

- A face is laid out on a grid from `face-template`; each named region is a slot filled by its `content`.
- Attached equipment is drawn as a smaller face tucked under the owner's lower edge, at 0.6 of the owner's size, one per item, with its own style rules.
- A stack is a cluster: up to three faces side by side, then a second row, with a count badge beyond six. Order follows `stackOrder`, then ID. The inspector lists every member.
- The level within a hex (A2.8) shows as a level tab on the stack when it is not ground level.

### 7.3 Perspective and concealment

The renderer never decides what a viewer may see. A source that hides a unit sends a concealed placeholder: `{ "kind": "unit", "concealed": true, "side": "german", "location": "..." }`, possibly with a size class if the viewer is entitled to it. The `:concealed` selector styles it; the default is a side-colored "?" face (A12). The same unit sent to its owner is a normal document with the `asl:concealed` state, drawn with a concealment frame.

### 7.4 Side palette

A side is a named palette with `fill`, `fill-muted`, `ink`, and `accent` roles. Palettes live in palette files, separate from style sheets, and a style sheet or the viewer selects one set. Two sets ship:

- **`asl-customary`**: ASL's customary nationality colors (for example German field grey, Russian brown, American olive). These are a convention, not artwork.
- **`limbodancer`**: palettes of our own, designed for contrast on the digital map and for color-blind readers, with the same roles.

Both ASL style sheets work with either set; `asl-classic` defaults to `asl-customary` and `asl-digital` to `limbodancer`. A pack can add sets or palettes for its own sides.

### 7.5 SVG output

Each unit is a `<g>` with `data-unit-id`, `role="button"`, `tabindex="0"`, and an accessible name built from the vocabulary labels, such as "German 1st Line squad A, 4-6-7, pinned, with light MG 3-6". Text, marks, and badges are SVG elements with fixed-point coordinates. No external images, fonts, or scripts. The same inputs produce byte-identical output (principle 5).

## 8. File formats

| File | Format | Location |
|---|---|---|
| Vocabulary pack | JSON, canonical form | `src/ASL/units/vocabulary/{pack}.vocab.json` |
| Style sheet | text, the language of section 3.3 | `src/ASL/units/styles/{name}.uss` |
| Unit documents | JSON; one document or a list | fixtures in `src/ASL/units/examples/`; Lab saves under the boards folder |

The `asl` pack and the two ASL style sheets are original LimboDancer content: vocabulary and styles, no rulebook text or counter artwork, so they can be committed (ASL-UNIT-072). Example documents are labelled synthetic.

## 9. Errors

- A document using an undeclared kind, attribute, trait, or state is refused with a diagnostic naming it. An attribute of the wrong type is refused.
- A style sheet with a syntax error is refused with a line and column. An unknown property is a warning and is ignored, so newer sheets degrade on older renderers.
- A slot whose `content` names a missing attribute renders empty, and the Lab shows a warning. It is not an error: a leader has no range.

## 10. Unit Lab

A Map Studio page, `/units/lab`, where a designer:

- picks a pack and a kind, fills in faces and attributes from forms the vocabulary generates, toggles traits and states, and attaches equipment;
- previews the unit in each detail tier, each face, both perspectives, and under each style sheet;
- edits a style sheet with live preview and sees parser diagnostics;
- places units on any board or composed map and sees them in the viewer;
- saves documents and sheets.

The board viewer's "Demo units" toggle becomes a "Units" layer that shows any saved placement set, on any board or map (ASL-UNIT-071).

## 11. Plausibility check

A separate, optional check reports combinations the ASL rules forbid, with the rule: a leader with a Normal Range (A1.22), Firepower on a broken face (A1.4), a hero with a broken face (A1.4), an MG without a breakdown number where the inherent B12 was not meant (A9.7). It runs in the Lab and never stops a unit from being drawn. Packs may add their own checks later.

## 12. Tests

- **Parity:** for every row of section 4, a document with and without the fact renders differently under both ASL sheets, and the difference is in the expected slot.
- **Cascade:** specificity, source order, kind inheritance, and `@detail` selection on small synthetic sheets.
- **Extension:** a test pack that extends `asl:mmc` inherits ASL styling and adds its own slot; a pack kind that extends nothing uses the base style.
- **Determinism and goldens:** one golden SVG per kind and state under each ASL sheet, and a pairwise set of attribute combinations.
- **Errors:** unknown kind, attribute, trait, state; wrong types; parser diagnostics with positions.
- **Accessibility:** accessible names list kind, side, values, states, and attachments.
- **Studio:** bUnit tests for the Lab forms; overlay placement on a standard board, a b board, and a composed map.

## 13. Phases

1. **Personnel and SW (this document):** the vocabulary model, documents, style language, cascade, layout, SVG, the `asl` Personnel and SW pack, both ASL sheets, the Lab, and the Units layer in the viewer.
2. **Guns:** ordnance attributes (caliber and AP/HE limits, type, ROF, range limit, special ammunition, M#, IFE, target size), facing and covered arc drawn as a direction, limbered and malfunctioned faces (C2.2 to C2.29, pp. 167 to 168). LATW and light mortar values arrive here.
3. **Vehicles:** movement type, armor status, main and secondary armament, turret types, ground pressure, armor factors with turret variants, target size, vehicular MGs, wreck face, hull and turret facing, motion, BU and CE (D1.1 to D1.9, pp. 193 to 195).
4. **Entities that are not units:** snipers, fortifications, informational markers (ASL-UNIT-026).

The style language and layout are designed for all four phases now; only the vocabulary grows.

## 14. Requirement changes

Made in the Unit Requirements on 2026-09-24, when this design was accepted:

- **ASL-UNIT-012** splits: the face content the display must carry is taken from the rulebook's counter anatomy (this document, section 4), now; per-unit printed values for real scenarios keep the counter data source decision (D1), later.
- **D2** is deferred: display input is documents from the Lab or fixtures, labelled synthetic (already allowed by ASL-UNIT-050).
- New requirements: an open unit vocabulary with packs (ASL-UNIT-073); separation of documents and style sheets (ASL-UNIT-074); information parity with printed counters under the ASL sheets (ASL-UNIT-075); detail tiers (ASL-UNIT-076); states drawn on the unit rather than as stacked markers (ASL-UNIT-077); deterministic, accessible SVG (ASL-UNIT-078).
- The sequence in section 13 of the Unit Requirements moves the display ahead of the catalog and state model.

## 15. Decisions

Decided on 2026-09-24:

1. **Style language:** the CSS-like text syntax of section 3.3, with our own parser.
2. **Side palettes:** both. ASL's customary nationality colors and palettes of our own ship as two palette sets (section 7.4).
3. **Figures:** unit size is shown by 1 to 3 figure glyphs in both ASL sheets.
4. **Face size:** 0.55 of the hex height, to be tried in the Unit Lab and adjusted through `face-size` if it does not read well (section 7.1).

## 16. As built: phase 1

Built on branch `feature/asl-units-01`, from `main@cdc738b`.

### 16.1 Projects and files

| Project or folder | Content |
|---|---|
| `LimboDancer.Domains.Asl.Units` | `Vocabulary/`: pack model, `VocabularyPackReader`, `UnitVocabulary`. `Documents/`: `UnitDocument`, `UnitDocumentReader`, `UnitPlacementSet`, `UnitDocumentJson` (canonical writer), `UnitLabels` (accessible names and detail rows). `Plausibility/UnitPlausibility`. References `Maps` for locations. |
| `LimboDancer.Domains.Asl.Units.Rendering` | `Styles/`: style model, `StyleSheetParser`, `StyleCascade`. `Palettes/PaletteSet`. `UnitRenderer` (layout and SVG), `Glyphs`, `UnitOverlay` (targets, overlay builder, preview), `UnitStyles`. References `Units` and `Maps.Rendering`. |
| `src/ASL/units/` | `vocabulary/asl.vocab.json`, `styles/asl-classic.uss` and `asl-digital.uss`, `palettes/asl-customary.palette.json` and `limbodancer.palette.json`, and synthetic `examples/` (`catalog.units.json` for the Lab, `bd01-demo.units.json` as the built-in placement set). They are embedded in the projects that read them. |
| Map Studio | `Services/UnitLibrary`, `Services/UnitDraft`, `Components/Units/UnitEditor.razor`, `Components/Pages/UnitLab.razor`, the Units layer in `BoardViewer.razor`, and tier switching in `boardViewport.js`. `DemoUnitOverlay` and its `bd01.json` fixture are removed. |

Neither unit project references Blazor. Both have test projects, with central package versions and lock files.

### 16.2 Additions to the design

- **Vocabulary.** A pack also declares its `sides` and `faces` with labels. A kind may set `sizeClass` (1 to 3 figures), an `accessibleName` template, and an `attachedName` template, each by face with a `default`. An attribute or trait may name the faces it belongs on. The core kind `unit` is the root every kind extends. An `augments` list adds attributes and traits to a kind of a pack the pack extends. An attribute may be written by its local name (`firepower`) when exactly one attribute the kind accepts has that name.
- **Documents.** A document may carry `sizeClass` and a free `note`. A placement set is `{ schemaVersion, setId, label, synthetic, vocabulary, units }`; each unit is a document with a location.
- **Style language.**
  - `:face(name)` matches the shown face, so a sheet styles a face whether a state selected it or the Lab forced it.
  - `@palette name;` names the sheet's default palette set.
  - `attr(name, first)` takes a list's first item; `attr(name, front)` reads another face when the shown face lacks the value, which lets a malfunctioned face keep its MG size label. `attr()` also reads `id`, `side`, and `size-class`.
  - Further properties: `attachment-scale`, `display`, `stroke-style`, `align`, `badges`, `badge-fill`, `badge-color`, `badge-shape`.
  - Lengths are fractions: `face-size` of the hex height, everything else of the face size.
- **Layout.** Attached equipment is drawn first, so the owner covers its upper part. Stacks overlap at 0.62 of the face size. Units at different levels of one hex form separate stacks, each non-ground stack with a level tab (`L1`, `L2`, or `C` for a cellar). The overlay's accessible names add ", level N".
- **Tiers.** The viewport picks far below 16 screen pixels per face, mid to 40, and near above, from the layer's `data-face-size` and the zoom, and sets `data-active-tier`; CSS shows one tier.
- **Studio.** The viewer's Units layer lists the placement sets that have a unit on the displayed board or map, a sheet selector, and `?units=` to open a set. The inspector shows the unit's accessible name, detail rows, location, and every member of the selected hex's stacks. The Lab edits a unit through forms generated from the vocabulary, previews every sheet at every tier, each face, and the owner's and opponent's views, edits a sheet live with diagnostics, runs the plausibility check, and places units on any board or composed map. It saves under `{boards folder}/units/`: `documents/`, `styles/`, and `placements/`. Built-in sheet names and set ids are read-only.

### 16.3 Departures

- **`@detail` precedence.** Rules inside a matching `@detail` block outrank every rule outside one; specificity and order decide within each group. Section 3.3 left detail rules in the ordinary cascade, where a far-tier rule for `asl|personnel` would lose to the base rule for `asl|leader`.
- **Badges accumulate.** Every matching `badge` declaration adds a badge, in cascade order, instead of the last one winning; `badges: none` hides them. Otherwise two states on one unit could not both show.
- **Radio value in force.** There is no scenario date yet, so `asl-digital` shows the first value of a contact series; the full series and its dates are in the detail panel and on the reverse face.
- **Kind label on hover.** The unit's `<title>` is its accessible name, which includes the kind label.
- **Text width.** Marks and badges size text by an estimate of 0.6 em per character; there are no font metrics.
- **Plausibility** is shown in the Lab only, not in the viewer's inspector.
- **No pinned board version.** A placement set names hexes by board and is anchored through the displayed board's geometry when the viewer loads it. The Counter Rendering Design's optional `expectedBoardVersion` is not carried over; a unit whose hex is not on the displayed board or map is left out with a diagnostic, never moved.

### 16.4 Tests

`Units.Tests` (101): the pack and every section 4 term, extension packs and their refusals, document errors with paths, names, details, canonical writing, placement sets, and the plausibility rules. `Units.Rendering.Tests` (146): parser diagnostics with line and column, the cascade, extension styling, one parity case per row of section 4 under both sheets, one golden per kind and state under each sheet (far, mid, and near side by side), a pairwise set, determinism, accessibility, and overlay placement on a standard board, a b board, and a composed map. Map Studio tests cover the unit library, Lab forms with bUnit, the viewer's Units layer, and, in `boardViewport.pointer.test.mjs`, unit selection under pointer capture, keyboard selection, and tier switching.

### 16.5 Not yet done

- The `limbodancer` palettes have not been checked with a color-vision simulator.
- The vocabulary file's canonical form is a convention; its identity hash normalizes line endings only.
- Guns, vehicles, and entities that are not units (section 13, phases 2 to 4).
