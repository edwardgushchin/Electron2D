# WorkerPoolRuntime

Last updated: 2026-10-25

Internal implementation of [WorkerThreadPool](WorkerThreadPool.md), in [WorkerPoolRuntime.cs](../../src/Core/OS/WorkerPoolRuntime.cs), compiled into Electron2D.dll. It has no public consumer or backend dependency surface. [Worker execution](../components/worker-pool.md) records the exercised contract and limits.

Private Job records retain callback delegates, debugger descriptions, index/progress counters, runner/waiter ownership and captured failure. Worker objects retain a background Thread, current Job, cooperative wait floor and ordinary slot nesting depth. Thread-static current-worker identity belongs to that exact pool. Prepared dictionaries/stacks/queues recycle records without reusing IDs; queue scans preserve remaining FIFO order when older jobs cannot be helped.

Completion becomes visible under the pool gate after callback dispatch restores the prior context and all group runners exit. Final registered wait retirement clears references and returns the record. Waiting outside workers blocks; worker waiting helps newer queued jobs outside the gate. Captured failures are rethrown only after retirement. Draining disposal rejects new jobs, wakes workers, joins them and releases pending storage; a callback cannot dispose its own pool. Partial startup failure joins the workers already started.

Internal per-worker allocation counters sample successive outer completion boundaries, including queue/monitor work, callback dispatch, nested work and game callback allocations. The zero-allocation test uses prepared allocation-free callbacks; it does not claim native/OS allocation absence or hard real-time scheduling. Internal isolated pools and waiter counters provide deterministic executable tests without adding public configuration/factory aliases.

ResourceLoader uses targeted group waits to isolate loader scopes, and targeted older-task waits after load-cycle and prepared active-stack checks. Preparation waiters do not retire the owner ticket; publication requests retirement and the final registered waiter recycles storage. Public wait behavior is unchanged. Active job stacks are allocated at pool startup and reused.

Engine thread preparation is captured once per pool. Workers report initializer success/failure through the retained startup barrier before job dispatch; construction waits for all workers and joins them on failure. Scene registers its context preparation at assembly initialization without making Core refer to Scene types. The constructor retains the shared gate until every successful worker publishes its post-signal entering flag and blocks on the gate; this prepares runtime contention state before dispatch. Only startup observes worker WaitSleepJoin under retained gate ownership. The worker allocation baseline is established after context preparation/signalling; frame samples begin after workload warmup. The barrier is disposed after joined shutdown or failed construction. See [ready workers](../components/worker-pool.md#ready-workers-and-scene-context).

The job retains only the internal [factory construction scope](../classes/ElectronObject.md#factory-construction-provenance), installs/restores it through dispatch and nested helping, and clears it on retirement. User ExecutionContext and SynchronizationContext remain uncaptured. A closed scope releases all object references and ceases to block scene activation.

Startup also prepares the construction-context read used by dispatch on every worker before its readiness signal. This avoids delayed CoreCLR thread-static storage when a previously unused worker first receives a job. [Physical group motion](../components/physics-group-motion.md) retains the attributed allocation and exercised profiles.
