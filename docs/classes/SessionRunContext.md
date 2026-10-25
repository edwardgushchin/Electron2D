# SessionRunContext

Last updated: 2026-10-26

- **Assembly/namespace:** `Electron2D.Editor.Session` / `Electron2D.Editor`.
- **Source:** [SessionRunContext.cs](../../editor/Session/Jobs/SessionRunContext.cs).
- **Declaration:** `public sealed class SessionRunContext`

Borrowed run configuration, root, seeded generator and bounded observations/completion, created by SessionRunHost. Construction is internal. Mutating/reporting/root access requires the game owner thread and a live context; the host releases it after scenario cleanup. Cold report allocation is outside engine frame-allocation claims.

## Public members

| Signature | Contract |
| --- | --- |
| `public SessionRunMode Mode { get; }` | Headless or real native rendering. |
| `public int Step { get; }` | Current one-based step, zero before simulation. |
| `public int StepBudget { get; }` | Maximum completed steps. |
| `public int FPS { get; }` | Fixed process/physics cadence per second. |
| `public int Width { get; }` | Requested native canvas width in pixels. |
| `public int Height { get; }` | Requested native canvas height in pixels. |
| `public Node Root { get; }` | Borrowed loaded scene. |
| `public RandomNumberGenerator Random { get; }` | Borrowed run-local seeded generator. |
| `public void Observe(string name, double value)` | Finite numeric observation. |
| `public void Observe(string name, bool value)` | Boolean observation. |
| `public void Observe(string name, string value)` | Bounded text observation. |
| `public void Check(string name, bool condition, string message)` | Record assertion; failure remains terminal. |
| `public void Complete()` | Request completion after the current step. |

## Example

See [SessionScenario's executable extension pattern](SessionScenario.md#example) and the [compiled consumer](../../tests/ProjectJobs/Program.cs). Context is supplied by callbacks; retain no ownership of it or Root/Random.

## Property descriptions

### Mode

The explicitly requested profile. Headless has no native renderer/capture. Rendered uses a real Window and explicit backend.

### Step

Zero during Configure/SceneLoaded, otherwise the one-based step being prepared/observed. It does not prove that BeforeStep has completed a frame; AfterStep supplies that boundary.

### StepBudget

Validated positive maximum. Exhaustion without Complete fails, rather than inferring successful gameplay.

### FPS

Validated 1–240 cadence. Headless advances Engine.AdvanceFrame by 1/FPS; rendered runs the ordinary MovieWriter clock. This does not promise deterministic external IO, solver or wall time.

### Width

Requested root canvas width, 2–4096 pixels. Native capture validates the actual dimensions.

### Height

Requested root canvas height, 2–4096 pixels. Headless retains configuration without allocating a native canvas.

### Root

Borrowed Node after scene load. Access before publication, from another thread or after release rejects. The host owns activation, teardown and disposal; authoring uses a different graph.

### Random

Borrowed seeded RandomNumberGenerator owned by the context. Only this generator is seeded; global/project/native randomness is not implied. The host disposes it after scenario cleanup.

## Method descriptions

### Observe

All three overloads record name/current Step and one typed payload. Names require 1–256 characters. Numeric values must be finite; text is nonnull and at most 4096 characters. The combined observations/assertions admit at most 4096 entries and a conservative four-MiB JSON budget. Null/invalid values reject; budget/owner/lifetime errors throw. Entries belong to this result, not the authoring model.

### Check

Records the named condition and nonnull message up to 4096 characters within the same report bounds. A false condition fixes nonzero assertion outcome and throws; catching that exception in game code cannot restore success. Null/bounds/thread/lifetime errors reject. See SessionAssertion in the structured result.

### Complete

Requires Step>0 and a live owner-thread context. Sets completion after this step; subsequent frames stop normally. The host verifies completed-step accounting and the job still requires every requested artifact. Step=0 throws InvalidOperationException. Reaching a budget without calling it is failure.

## Verification and limits

[Job contracts](../../tests/ProjectJobs/JobContracts.cs) exercise observations, caught assertion failure, missing input actions, callback failure and completion exhaustion. Actual images/provenance execute on both native backends. Physical GPU/native heap, foreign hosts and actual external agent acceptance remain open.
