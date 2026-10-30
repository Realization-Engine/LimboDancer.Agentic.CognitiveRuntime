# A24.4 SMOKE height wiring prerequisite

Implementation in progress under the user's approved Wiring 002 design, section 5, and instruction to finish crossed-LOS SMOKE. Claim A24.4, Codex, reviewer Claude, branch `codex/rules-smoke-los-adapters`. Caller work stays on `wire-fire-3`. Neither claim is marked wired.

## Rule and source map

A24.4, physical PDF p. 92, gives conventional SMOKE two levels and WP four, with an exclusive upper boundary. B25.2's calculator continues to own Blaze height and wind. A6.4 to A6.43, p. 53, supplies the blind-hex calculation for intervening SMOKE. A24.2 and A24.8, pp. 91 and 93, supply the common Location cap and outgoing modifier through the existing by-value members.

The [combined Q&A](https://www.klasm.com/ASL/ErrataQA/QA/ASL_QA_Total.pdf), PDF p. 73, extends A24.4's own-hex downward example to a firer above the top, regardless of range. This is in the Perry Sez compilation, not new wording in the rulebook. PDF p. 205 attributes the cellar clarification to Journal 1 and MMP's website: ground-level SMOKE does not occupy the cellar, but affects its LOS to ground level or higher without the outgoing modifier. The original Journal page was not independently inspected. The rulebook and these attributed clarifications are recorded separately.

## Implementation

`ScenarioA1SmokeHeightCalculator` receives physical source classification or a `SmokeLosFacts` value. It decides height, endpoint occupancy, and intersection. The source's ground elevation is separate from its placement elevation. An elevated source never fills levels beneath its base merely because its hex was crossed.

Maps exposes the LOS's already resolved endpoint elevation, rooftop, cellar, slope, hillock and embankment facts. Each crossed hex carries independent distances from both ends; target distance is not inferred by subtracting ranges. Existing LOS equality remains unchanged. Play gathers those facts and asks Rules. It performs no blind-hex or smoke-height arithmetic. It groups source values by full BoardLocation and applies the existing common cap once. A source irrelevant to the planar footprint causes no height query or unknown-fact refusal.

The Workbench contract `a24.4-smoke-height` exposes only public physical source and chemical classifications. The existing `a24-smoke` bundle includes two-level, four-level, unknown-chemical and separate-Blaze checks. It grants no placement permission. The LOS tests cover the rulebook boundaries and blind-hex distances directly.

Existing white-counter duration remains `TurnWhiteSmokeRules`'s recorded reading. This prerequisite adds no duration or placement control. Earlier recorded map facts retain their recorded answers; new caller proposals use the new height read.

## Explicit remaining limits

Two independent read-only reviews did not establish a general underside rule for SMOKE placed upstairs, or the embankment composition. Those geometries remain undecided, with a caller refusal, rather than an invented answer. The known cellar clarification is not generalized to all upstairs sources. These limits are also in backlog section 85 and remain open before full acceptance.

The calculator allocates only its small verdict and does no I/O or caching. The caller's source index belongs to one immutable state and contains no viewer identity. Geometry facts add bounded data to the existing LOS result, not a second LOS walk. Legacy/null records keep their existing reading. Focused tests, Workbench verification and caller replay gates are required; no gate success is claimed here.
