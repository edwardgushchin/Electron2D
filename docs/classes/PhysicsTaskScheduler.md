# PhysicsTaskScheduler

Last updated: 2026-10-27

**Declaration:** `internal sealed class PhysicsTaskScheduler : IDisposable`

**Source:** [PhysicsTaskScheduler.cs](../../src/Servers/Physics/PhysicsTaskScheduler.cs) · **Component:** [Scene physics bodies](../components/physics-bodies.md)

World-owned adapter for backend enqueue/finish callbacks. The CPU backend creates it with up to four available processors and binds its numerical world. The GPU backend owns an unbound scheduler for decoding completed host contact reports; constructing or using it creates no CPU numerical world. A single-worker interval executes immediately and returns no task handle. The first parallel interval creates the retained worker threads, queues, wake events and 64 job handles. Each range runs once with a unique physical worker index; constraint tasks retain their own backend worker identity. Queue ranges respect the requested minimum size; uneven tails are distributed without gaps.

Each queue reserves one record per retained job, its maximum possible pending occupancy. Short enqueue/dequeue sections use retained SpinLock gates without owner tracking; callbacks, wake signaling and completion waits remain outside those gates. This prevents a delayed first contention from creating a monitor and its wait event inside an otherwise warmed step. A queue never grows while its gate is held. [Late queue contention](../components/physics-allocation-measurement.md#late-worker-queue-contention-2026-10-27) records the causal control and verification limits.

Finish waits for every range and returns the job storage before reporting a captured worker failure. Constraint failures publish cancellation to the step context so other constraint workers can exit. This reports fatal backend faults; it does not promise that a partly solved world can resume. Dispose drains queued work, joins threads and releases wait primitives before the native world is destroyed. Game integration and events execute outside these workers.

PhysicsParallelTests forces delayed contention on a fresh queue gate and requires zero owner/all-thread bytes while the actual enqueue/finish callback completes. It also fills all 64 task slots at zero allocation, verifies exhaustion/join/reuse, and checks uneven ranges, repeated handle reuse, failure/cancellation, large scene contacts, one-way state, owner callbacks and serial/parallel population transitions. The [performance report](../components/box2d-performance.md) measures all-thread allocation in a warmed backend process. Foreign native/browser execution and hardware acceptance are separate.

Solver work stealing and cancellation now use a 64-bit publication token with a
48-bit phase ordinal and a 16-bit stage index. Block claims also use 64-bit atomics.
PhysicsParallelTests retains injected-worker failure/cancellation checks;
PhysicsSolverIterationTests crosses 65,535 graph phases with multiple contact blocks.
Stage descriptions are immutable and reused rather than allocated per sweep.

RunRanges joins retained background ranges and an owner range before returning.
Counts below 256 or a single selected lane execute on the owner. A failed owner
range still waits for all workers; simultaneous failures aggregate both errors.
Successful warmed dispatch allocates no managed memory. GPU worker capture ends
before extension projection and user callbacks; disposal joins the threads before
releasing the resident store. See [contact publication verification](../components/physics-scene-performance.md#contact-publication-workers-2026-10-20).
