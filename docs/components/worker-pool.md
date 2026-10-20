# Worker execution component

Last updated: 2026-10-20

## Scope and ownership

[ADR 0104](../decisions/jobs.md#adr-0104) owns typed worker execution. [WorkerThreadPool](../classes/WorkerThreadPool.md) is a permanent retained ElectronObject service, registered by Engine and protected against caller disposal/removal, whose public operations are static. Its lazily started [WorkerPoolRuntime](../classes/WorkerPoolRuntime.md) owns dedicated background threads, fixed prepared records and priority queues. Callback state remains borrowed until completion is waited; every accepted task/group ID must be waited even if its completion was already queried.

Regular Action callbacks execute once. Action<int> groups distribute every index exactly once and report finished attempts. Group failures continue remaining work; completion wait rethrows the lowest-index failure. Regular failures preserve their original exception and stack. Concurrent registered waiters safely share completion; final retirement clears callback/failure/description references and returns the record to prepared storage. IDs never repeat and reject wrong-kind or retired access.

High priority takes precedence among eligible queued jobs without preemption. Ordinary jobs use a configured subset of workers, preserving a high-priority slot in multi-worker pools. Worker waits help newer work, restore nested caller context and reject unfinished self/older dependencies. External waiters execute no callbacks. Groups accept all negative runner counts as the whole pool, cap positive counts by workers/elements, complete zero elements immediately, and reject zero runners for a nonempty group.

## Preparation and integration

ProjectSettings.WorkerPoolMaxThreads, WorkerPoolLowPriorityThreadRatio and WorkerPoolMaxPendingTasks are sampled with feature overrides at first validated submission. Successful repeated submit/query/dispatch/wait reuses startup storage; exceeding pending capacity fails atomically. Game callback allocations and failure capture are separate boundaries. No per-job Task.Run, closures or ExecutionContext capture are introduced by dispatch. Callback-created ambient state remains the callback author's responsibility.

The public-only [WorkerRoutes example](../../examples/WorkerRoutes/README.md) owns one AStarGrid per route, computes the initial routes in indexed callbacks and submits four marker computations each rendered frame. Prebuilt publication delegates enter SceneTree.Defer; only the scene owner updates drawing state. Each bounded group is waited before the next frame or grid disposal. The example and tests exercise real worker/scene/render integration, not a headless renderer substitute.

## Verification and remaining dependencies

WorkerThreadPoolTests checks validation and capacity recovery; regular/group caller IDs; parallel callbacks and progress; priority/reserved slots; negative/oversized runner limits; single-worker nested regular/group waits and failure context restoration; older/self rejection; FIFO preservation while helping; concurrent waiters; failure draining and deterministic lowest-index failure; callback target release; caller context suppression; named service/static access; draining shutdown; and public independent routes with owner publication.

After 128 warm cycles, 1024 regular/group submit/query/wait cycles allocate zero managed bytes on the sender and within measured worker dispatch. WorkerRoutesRenderingTests executes Linux Wayland GPU and compatibility independently: 88 groups, 352 deferred publications, endpoint pixel checks and cleanup. Each backend's final 64 active frames after 24 warm frames allocates zero managed bytes on the scene owner. Worker/owner managed counters do not measure native/OS allocations. These rendered profiles have no foreign-target, threaded-browser, NativeAOT or human acceptance; targeted NativeAOT startup/physical evidence follows below. Browser submissions explicitly reject until their threaded prerequisite exists.

ResourceLoader threaded requests now implement staged graph ownership, progress, cancellation, owner publication and actual dependency parallelism; see [threaded resource loading](threaded-resource-loading.md). Baking and other asynchronous domains still need their concrete consumers. Public explicit Thread/Mutex/Semaphore APIs remain separate slices over this foundation.

Invalid submission arguments are checked before lazy startup. Completion/progress/wait calls with no pending runtime reject their ID without starting workers or freezing startup settings. The cold public API test checks both boundaries before a later valid configured submission.

Internal loader waits select only the dependency group. Older root-task waits verify the active worker stack and retain a non-consuming preparation wait until publication retires its owner ticket. Public WorkerThreadPool wait/older guards retain their behavior and warmed allocation budget.

## Caller-owned synchronization

[Mutex](../classes/Mutex.md) and [Semaphore](../classes/Semaphore.md) now provide recursive protected data and counted worker signals. See [synchronization](synchronization.md) for ownership, blocking dependencies, failures and verified public scene consumers. These primitives grant no off-owner scene/native access and do not replace the requirement to wait every accepted task ID.

## Ready workers and scene context

Validated pool creation captures engine thread initializers and starts every worker with suppressed caller context. Each worker prepares its engine-local state and signals readiness before accepting jobs. Construction holds the shared gate until all signals have arrived and every prepared worker has actually blocked on that gate, preparing runtime contention state before dispatch. Entering flags are published after readiness signalling; only then does the constructor observe WaitSleepJoin while it still owns the gate. This cold observation adds no polling to frame execution. An initializer failure returns on the constructor thread after all started workers join. Shutdown releases the retained readiness barrier after joining. Scene's assembly initializer registers only thread-local group preparation, preserving Core's dependency direction and lazy sampling of worker settings. Existing pools cannot gain late game/plugin initializers; no public registration API is added.

The group-context control exposes the former 32-byte first touch on a newly selected worker even after many frames had warmed another worker. Startup preparation makes both distinct workers' first/repeated accesses zero. A forced-contention NativeAOT control also exposes the former 120-byte lazy gate event; prepared contention must use zero bytes. Blocking/failing startup tests, the worker pool suite and the [physical group attribution profile](node-groups.md#worker-context-preparation-and-allocation-attribution) verify readiness, cleanup and zero warmed execution. Hardware/native/foreign acceptance remains separate.

The job retains only the internal [factory construction scope](../classes/ElectronObject.md#factory-construction-provenance), installs/restores it through dispatch and nested helping, and clears it on retirement. User ExecutionContext and SynchronizationContext remain uncaptured. A closed scope releases all object references and ceases to block scene activation.
