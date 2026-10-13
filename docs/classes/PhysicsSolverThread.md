# PhysicsSolverThread

Last updated: 2026-10-13

**Declaration:** `internal sealed class PhysicsSolverThread : IDisposable`

**Source:** [PhysicsSolverThread.cs](../../src/Servers/Physics/PhysicsSolverThread.cs) · **Component:** [Separate numerical physics thread](../components/physics-thread.md)

## Description

Retains one background numerical thread and two native wait events for one PhysicsSpace.
The space owner prepares the interval, grants that exact worker a temporary numerical
borrow, signals Solve and joins its completion before publication. The worker runs
the selected backend's complete Solve; GPU numerical state remains on device.
Exceptions return through ExceptionDispatchInfo and the selected world failure policy.

| Internal operation | Contract |
| --- | --- |
| `PhysicsSolverThread(PhysicsSpace space)` | Starts the retained worker; startup failure releases the wait events. |
| `Solve(PhysicsWorldBackend backend, double delta)` | Owner-only pipeline invocation with prepared, immutable inputs; waits for completion and ends borrowing in finally. |
| `Dispose()` | After a joined interval, signals shutdown, joins the worker and disposes both events. Repeated calls are harmless. |

World release owns this lifetime. No user callbacks, scene publication, query dispatch
or device retirement run on this worker. A blocked provider blocks its synchronous
caller just as an owner-thread solve would; there is no implicit abort or replay.
Warmed dispatch reuses all state and events. PhysicsSeparateThreadTests and the
public-only consumer verify real CPU/provider work, owner callbacks, failures,
retirement and zero owner/all-thread managed bytes. GPU/native/foreign execution
and scheduling performance remain separate gates under ADRs 0054/0103.
