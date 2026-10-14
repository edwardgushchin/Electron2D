# Worker execution decisions

Last updated: 2026-10-13

<a id="adr-0104"></a>
## ADR 0104: Retained typed worker pool with explicit completion ownership

### Status

Accepted.

### Decision

WorkerThreadPool is a sealed ElectronObject service with static public operations and a retained, lazily started implementation under ADR 0095. Action and Action<int> replace Callable; signed 64-bit task/group IDs share a monotonically increasing namespace and are never reused. Every submitted task or group must be waited, even after a completion query. The final waiter retires its ID and releases its callback, description and failure references. Concurrent waiters that registered before retirement finish safely; later queries or waits reject the ID.

Regular callbacks execute once. Groups distribute indices [0, elements) exactly once, report finished attempts after each callback returns or throws, and finish only after all runners exit. Empty groups are immediately complete. Any negative tasksNeeded uses the whole pool; positive values limit group runners, capped by elements and workers. Zero is rejected for a nonempty group instead of creating an unfinishable job. Group callbacks expose only their group ID; regular callbacks expose only their task ID. Other threads see -1 for both.

High-priority queues are serviced before ordinary queues, FIFO among eligible jobs, without interrupting running callbacks. Startup settings threading/worker_pool/max_threads (-1: max(1, processor count - 1)), low_priority_thread_ratio (.5), and max_pending_tasks (1024) are sampled with feature overrides on the first validated submission. A low-priority runner owns one of clamp(floor(workers * ratio), 1, max(1, workers - 1)) ordinary worker slots. A worker already holding such a slot may reuse it while helping nested work. All records, maps and queue capacity are prepared at startup; capacity exhaustion rejects submission atomically until completed IDs are retired. max_pending_tasks is an Electron2D preparation extension enforcing ADR 0014, not a reference compatibility switch.

Waiting outside the pool blocks without executing callbacks on the waiting thread. A worker waiting for newer work helps eligible queued jobs, preserving caller context across nested dispatch. Waiting for an unfinished self/older task or group is rejected with InvalidOperationException to prevent stack/dependency cycles; completed older jobs remain waitable. Helping does not execute jobs at or below the current wait floor. Invalid IDs/kinds use ArgumentException; submission validation uses ordinary typed exceptions. A failed regular task rethrows its captured exception when waited. A failed group continues every index and rethrows the lowest-index failure after full completion. No cancellation or forced thread termination is exposed: callers implement cooperative cancellation in their own callback state and still wait for cleanup.

Callbacks run on dedicated background .NET threads with no captured caller ExecutionContext or SynchronizationContext. Callback state, external resources and thread-affine objects remain caller-owned: submit immutable data or synchronized state, retain it until completion, and publish scene changes with SceneTree.Defer or the typed Node thread-safe/group-message family under ADR 0006. The latter targets the current main processing owner; explicit worker processing groups remain an open scene slice. Worker dispatch does not grant permission for off-owner scene/render operations. Threads live for the permanent service lifetime; the borrowed named service rejects caller disposal before changing state and cannot be removed from the built-in registry. Internal isolated pools drain accepted work and join workers on disposal; workers cannot dispose their own pool.

Browser profiles explicitly reject submissions until a threaded browser runtime with shared-memory/bootstrap guarantees is implemented. This does not exclude Web as a target under ADR 0021. Desktop/mobile .NET threading is the implementation foundation; only exercised profiles count as verified. The job pool supplies the scheduling prerequisite for threaded ResourceLoader, baking and other asynchronous consumers; their ownership, progress, cancellation and dependency-loading contracts remain separate implementation work under ADR 0013.

### Verification boundary

Check callback identity, priority, parallelism, group progress, zero/negative/oversized runner counts, capacity recovery, concurrent waits, nested waits, failure draining, ambient context isolation, disposal, and warmed submit/dispatch/query/wait allocation counters on sending and worker threads. Exercise a public scene consumer that computes independent routes and publishes results through SceneTree.Defer, then check real rendered output on stated backends. Native/OS allocations, other platforms, browser bootstrap and human acceptance require separate evidence.

### Related decisions

- [0001/0004/0021/0045: Typed API, product, targets and acronym spelling](product.md)
- [0006: Scene scheduling](scene.md#adr-0006)
- [0013/0014: Resource ownership and allocation budget](resources.md)
- [0095: Retained service objects](singleton-services.md#adr-0095)

Invalid submission arguments are checked before lazy startup. Completion/progress/wait calls with no pending runtime reject their ID without starting workers or freezing startup settings. The cold public API test checks both boundaries before a later valid configured submission.

ResourceLoader may use internal waits restricted to one dependency group, and may help an older root task after its own load-cycle checks and an active worker-stack check. These do not change the public WorkerThreadPool older/self guard. Dedicated target waits do not execute unrelated callbacks under resource staging scopes.

Navigation iteration teardown and deterministic host synchronization may use a target-only older-job wait for pure captured geometry jobs. Those workers cannot call user callbacks or live scene/server code, so this does not relax public older/self dependency guards.

NoiseTexture uses target-only retirement while keeping the public older/self wait guards. Custom sampler callbacks may execute, so recursive generation/disposal and worker disposal of unfinished generation reject before mutation. Owner teardown waits outside the texture gate; built-in snapshots and file-graph leases survive through completion/retirement.

### Caller-owned synchronization

The accepted managed worker foundation also provides sealed `Mutex : ElectronObject` and `Semaphore : ElectronObject`, with managed lifetime replacing reference counting under ADR 0014. `Mutex` uses a recursive .NET monitor: `Lock`, immediate `TryLock` and same-thread `Unlock`; each successful acquisition requires one release. `Semaphore` uses a monitor-backed UInt32 token count initialized to zero: `Post(int count = 1)`, `Wait` and `TryWait`. Positive post validation and overflow reject before publication; wait handles spurious wake-ups. Both operations publish shared managed data through the monitor boundary; they grant no off-owner scene/render access. No named/process-shared handles, async/task policy, cancellation or forced thread termination is exposed.

Callers retain the objects, finish and join participants, then dispose them. Disposal validation rejects observed held/pending mutex acquisitions or semaphore waits before starting this caller's disposal. This validation does not grant permission to race new calls with disposal. Raced semaphore waiters are pulsed and reject disposed state; a raced mutex acquisition releases its monitor before throwing, and an already accepted owner can still Unlock after disposal starts. Failed validation preserves ordinary operation. Browser constructors explicitly reject until shared-memory/threaded runtime bootstrap exists; desktop/mobile .NET is the foundation and only exercised profiles count as verified.

These primitives provide the typed synchronization prerequisite for user worker pipelines and future explicit Thread ownership. Waiting inside a pool consumes a worker slot: callers must guarantee that producers can execute without depending on every blocked worker or the blocked scene owner. Dedicated Thread result/priority/join ownership remains a separate slice.
