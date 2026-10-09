# PhysicsTaskScheduler

Last updated: 2026-10-06

**Declaration:** `internal sealed class PhysicsTaskScheduler : IDisposable`

**Source:** [PhysicsTaskScheduler.cs](../../src/Servers/Physics/PhysicsTaskScheduler.cs) · **Component:** [Scene physics bodies](../components/physics-bodies.md)

World-owned adapter for backend enqueue/finish callbacks. PhysicsSpace creates it with up to four available processors and binds the backend world. A single-worker interval executes immediately and returns no task handle. The first parallel interval creates the retained worker threads, queues, wake events and 64 job handles. Each range runs once with a unique physical worker index; constraint tasks retain their own backend worker identity. Queue ranges respect the requested minimum size; uneven tails are distributed without gaps.

Finish waits for every range and returns the job storage before reporting a captured worker failure. Constraint failures publish cancellation to the step context so other constraint workers can exit. This reports fatal backend faults; it does not promise that a partly solved world can resume. Dispose drains queued work, joins threads and releases wait primitives before the native world is destroyed. Game integration and events execute outside these workers.

PhysicsParallelTests checks uneven ranges, repeated handle reuse, failure/cancellation, large scene contacts, one-way state, owner callbacks and serial/parallel population transitions. The [performance report](../components/box2d-performance.md) measures all-thread allocation in a warmed backend process. Foreign native/browser execution and hardware acceptance are separate.

Solver work stealing and cancellation now use a 64-bit publication token with a
48-bit phase ordinal and a 16-bit stage index. Block claims also use 64-bit atomics.
PhysicsParallelTests retains injected-worker failure/cancellation checks;
PhysicsSolverIterationTests crosses 65,535 graph phases with multiple contact blocks.
Stage descriptions are immutable and reused rather than allocated per sweep.
