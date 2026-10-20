# BatchContext

Last updated: 2026-10-20

- **Assembly:** `Electron2D.Batch` development tool.
- **Namespace:** `Electron2D.Tools.Batch`
- **Source:** [BatchContext.cs](../../tools/Batch/BatchContext.cs)
- **Declaration:** `public sealed class BatchContext`

A host-created context describes one run and collects typed observations/assertions. Root and Random are borrowed. The source root becomes available after construction/loading; Configure cannot query it. Reporting calls and root access require the creating thread and reject after host release. Immutable request values remain descriptive data. Random is disposed by the host; its seed controls only that generator.

```csharp
// Inside AfterStep:
context.Observe("player-x", context.Root.GetNode<Entity>("Player").Position.X);
context.Check("player-present", context.Root.HasNode("Player"), "The player must remain in the scene.");
if (context.Step == 10) context.Complete();
```

The snippet requires a scenario with a Player node and a budget of at least ten steps.

## Properties

| Signature | Meaning |
| --- | --- |
| `public BatchMode Mode { get; }` | Authoring, Headless or Rendered. |
| `public int Step { get; }` | Current one-based attempted step; zero before simulation. |
| `public int StepBudget { get; }` | Maximum completed steps, 1..1000000. |
| `public int FPS { get; }` | Requested process/physics cadence, 1..240. |
| `public int Width { get; }` / `public int Height { get; }` | Requested native canvas size in pixels, 2..4096 per axis. |
| `public Node Root { get; }` | Borrowed source root; a native wrapper is a separate runtime Window. |
| `public RandomNumberGenerator Random { get; }` | Borrowed seeded generator for explicitly reproducible scenario choices. |

## Methods

| Signature | Result |
| --- | --- |
| `public void Observe(string name, double value)` | Records a finite number at Step. |
| `public void Observe(string name, bool value)` | Records a flag at Step. |
| `public void Observe(string name, string value)` | Records non-null text up to 4096 characters. |
| `public void Check(string name, bool condition, string message)` | Records the assertion; false terminates with ASSERTION_FAILED and exit 5. |
| `public void Complete()` | Requests success after the current completed step. |

### Observations and assertions

Names have 1..256 characters; explanations/text have at most 4096. Null values reject, numbers must be finite, and at most 4096 total observations/assertions fit a conservative four-MiB JSON budget. Invalid values, exceeded bounds, wrong-owner access and expired contexts fail rather than publishing a malformed report. These are allocating tool operations, outside engine zero-allocation measurements.

### Complete

Complete requires a started simulation step and rejects authoring. It requests completion after the current step and, for rendered mode, after actual image production. It does not directly quit an in-progress callback or skip required runtime processing. Reaching StepBudget without completion is a failure. The report separately records completed steps, so a quit before native capture cannot claim that capture completed.

The [protocol](../components/project-batch.md#request-and-result-protocol) and [executable checks](../components/project-batch.md#executable-verification-and-limits) define transport, lifecycle and verification scope.
