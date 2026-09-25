# .NET Dice Roller Design

Status: Approved. Standalone library implemented; ASL integration in sections 3 and 4 remains planned for step 9.

Requirements: [NET Dice Roller Requirements](<NET Dice Roller Requirements.md>).

## 1. Project and API

The implementation adds `src/ASL/LimboDancer.Dice/LimboDancer.Dice.csproj` and `src/ASL/tests/LimboDancer.Dice.Tests/` to the ASL solution. Inherit `net10.0`, nullable checking, and analyzer settings from `Directory.Build.props`. Use namespace `LimboDancer.Dice`. The library references only framework assemblies. The future dependency direction is `LimboDancer.Domains.Asl.Play -> LimboDancer.Dice`.

The public surface is three types:

```csharp
public sealed record RollRequest(int Count, int Sides);

public sealed class RollResult
{
    public RollRequest Request { get; }
    public IReadOnlyList<int> Values { get; }
    // Internal construction only; owns a read-only copy of the values.
}

public sealed class DiceRoller
{
    public DiceRoller();
    public RollResult Roll(RollRequest request);
}
```

This is an API sketch, not implementation code. `Roll` is synchronous because it performs only bounded local computation. Do not add async methods, a service interface, a factory, a dependency injection package, or a persistence abstraction to the library.

Validate at the start of `Roll`: null throws `ArgumentNullException`; count outside 1..100 or sides outside 2..int.MaxValue throws `ArgumentOutOfRangeException`. Complete validation before allocation or drawing. Copy generated values into owned read-only storage; exposing an array through `IReadOnlyList<int>` alone does not prevent mutation.

Order is significant. ASL can assign roles to index 0 and index 1 without putting colors or rules into the library. Totals and modifiers are computed by the consumer.

## 2. Generation and testing seam

For each die, use `RandomNumberGenerator.GetInt32(request.Sides) + 1`. The framework call produces values from zero inclusive to sides exclusive. Adding one gives 1..sides and supports `int.MaxValue` without computing `sides + 1`. The framework handles rejection sampling; do not map random bytes with a remainder operation.

Reference: [Microsoft GetInt32 documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getint32?view=net-10.0).

Use one internal constructor accepting `Func<int, int>` with the same exclusive-upper-bound contract. Expose it to the test assembly through `InternalsVisibleTo`. The public constructor selects the framework implementation. Check injected output against its bounds and fail on an invalid value. No random source registry or public seeded mode is needed.

Each call owns its request processing and result buffer. The production roller stores no per-game state. If any draw throws, propagate the error and return no result.

## 3. ASL ownership and commit boundary

The existing [Governed Writes Design](<ASL Unit Governed Writes Design.md>) provides confirmation, attempts, revision checks, and audit. In the current code, `GameConstraintEvaluator` and `GameActionExecutor` both call `GamePlanner`. Therefore generating inside the planner would draw more than once and could draw before confirmation.

The existing `FileGameStore.Append` receives already constructed events, then checks duplicates and revision under a per-game lock. For step 9, add a narrowly scoped store operation that creates the dice events inside that same lock after these checks. Do not call the roller before an ordinary append and assume that append makes generation atomic.

The intended sequence is:

1. Plan the declaration and required roll from server state, without generating values. Confirmation authorizes the roll and its rules-defined consequences before any result is visible.
2. After gate authorization, enter the per-game commit lock. Read the canonical log. If the attempt is already committed, verify its action inputs match and return its recorded result. Otherwise check the gate-validated revision and current eligibility.
3. Generate once. Build the declaration, dice, and any immediate rules outcome events. A failed NTC is an ordinary outcome, not an append failure.
4. Validate the proposed event batch by deterministic replay, then persist it with the existing temporary-file replacement approach.
5. Read back the committed result and expose it to the caller. Link execution audit entries to the attempt and game events.

This is a proposed integration change, not an existing guarantee. Keep the operation within the current single-process game-store model. Distributed coordination is out of scope.

## 4. Event and recovery contract

Use a proposed ASL `dice-rolled` event carrying count, sides, ordered values, purpose, and source `system`. Use the existing event envelope for identity, actor, time, causes, and rule package where it supports them. Give each roll a deterministic identifier derived from the attempt and its roll ordinal. Associate any positional die roles with the ASL interpretation. Record modifiers and outcome in the ASL rules event, linked to the dice event.

An OVR election is a separate declaration event. An NTC consumes a dice event. This document does not define NTC thresholds, legal OVR elections, or the full step 9 event schema; those require the step 9 rules design.

Use the game log as the authority for committed results. The audit sink and game log are separate files today, so do not claim a transaction spans both. An audit or response failure after game commit must not cause a replacement roll.

| Failure point | Recovery |
|---|---|
| Before generation | No dice result exists. Correct or retry the action through the governed path. |
| After generation, before canonical log replacement | No accepted result exists. Do not disclose the in-memory draw. A retry may generate again after normal checks. |
| During persistence with uncertain outcome | Read the canonical log first. Reuse a matching committed attempt; if the log cannot be read reliably, stop and report failure. |
| After commit, before response or audit completion | Return the stored result on retry. Do not draw again. |
| Concurrent duplicate confirmation | The lock and committed-attempt check return one accepted result to both callers. |

The guarantee is one committed result per attempt, not preservation of every uncommitted random draw across a crash. This avoids adding a separate roll journal or random seed store. Existing filesystem durability limitations still apply.

## 5. Verification and delivery

Implement the library and its focused tests first, following DICE-01 through DICE-06. Use deterministic tests for ordering, boundaries including maximum sides, validation before draws, defensive result storage, and failure propagation. Use the existing test framework and build conventions.

Integrate with ASL only as part of step 9, with DICE-07 through DICE-12 tests. In particular, test a crash or injected failure on each side of the canonical write and prove that replay never invokes the roller. No performance benchmark suite or random-distribution test suite is needed for this wrapper.

The library and test projects are registered in the ASL solution. Their package lock files are included for locked CI restore. No ASL game runtime or step 8 documents are changed.

Usage:

```csharp
var result = new DiceRoller().Roll(new RollRequest(2, 6));
// result.Values contains two ordered values, each between 1 and 6.
```

Validation: all 24 tests passed in Release configuration with the repository analyzers and warnings-as-errors settings, using:

```text
dotnet test src/ASL/tests/LimboDancer.Dice.Tests/LimboDancer.Dice.Tests.csproj --configuration Release -p:RestoreLockedMode=true
```

This validates DICE-01 through DICE-06. Live game confirmation, persistence, idempotency, and audit behavior are not implemented or claimed by this library test run.
