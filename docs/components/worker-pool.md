# Worker execution component

Last updated: 2026-10-10

## Scope and ownership

[ADR 0104](../decisions/jobs.md#adr-0104) owns typed worker execution. [WorkerThreadPool](../classes/WorkerThreadPool.md) is a permanent retained ElectronObject service, registered by Engine and protected against caller disposal/removal, whose public operations are static. Its lazily started [WorkerPoolRuntime](../classes/WorkerPoolRuntime.md) owns dedicated background threads, fixed prepared records and priority queues. Callback state remains borrowed until completion is waited; every accepted task/group ID must be waited even if its completion was already queried.

Regular Action callbacks execute once. Action<int> groups distribute every index exactly once and report finished attempts. Group failures continue remaining work; completion wait rethrows the lowest-index failure. Regular failures preserve their original exception and stack. Concurrent registered waiters safely share completion; final retirement clears callback/failure/description references and returns the record to prepared storage. IDs never repeat and reject wrong-kind or retired access.

High priority takes precedence among eligible queued jobs without preemption. Ordinary jobs use a configured subset of workers, preserving a high-priority slot in multi-worker pools. Worker waits help newer work, restore nested caller context and reject unfinished self/older dependencies. External waiters execute no callbacks. Groups accept all negative runner counts as the whole pool, cap positive counts by workers/elements, complete zero elements immediately, and reject zero runners for a nonempty group.

## Preparation and integration

ProjectSettings.WorkerPoolMaxThreads, WorkerPoolLowPriorityThreadRatio and WorkerPoolMaxPendingTasks are sampled with feature overrides at first validated submission. Successful repeated submit/query/dispatch/wait reuses startup storage; exceeding pending capacity fails atomically. Game callback allocations and failure capture are separate boundaries. No per-job Task.Run, closures or ExecutionContext capture are introduced by dispatch. Callback-created ambient state remains the callback author's responsibility.

The public-only [WorkerRoutes example](../../examples/WorkerRoutes/README.md) owns one AStarGrid per route, computes the initial routes in indexed callbacks and submits four marker computations each rendered frame. Prebuilt publication delegates enter SceneTree.Defer; only the scene owner updates drawing state. Each bounded group is waited before the next frame or grid disposal. The example and tests exercise real worker/scene/render integration, not a headless renderer substitute.

## Verification and remaining dependencies

WorkerThreadPoolTests checks validation and capacity recovery; regular/group caller IDs; parallel callbacks and progress; priority/reserved slots; negative/oversized runner limits; single-worker nested regular/group waits and failure context restoration; older/self rejection; FIFO preservation while helping; concurrent waiters; failure draining and deterministic lowest-index failure; callback target release; caller context suppression; named service/static access; draining shutdown; and public independent routes with owner publication.

After 128 warm cycles, 1024 regular/group submit/query/wait cycles allocate zero managed bytes on the sender and within measured worker dispatch. WorkerRoutesRenderingTests executes Linux Wayland GPU and compatibility independently: 88 groups, 352 deferred publications, endpoint pixel checks and cleanup. Each backend's final 64 active frames after 24 warm frames allocates zero managed bytes on the scene owner. Worker/owner managed counters do not measure native/OS allocations. Foreign targets, threaded browser bootstrap, AOT and human acceptance remain unverified; browser submissions explicitly reject until that prerequisite exists.

ResourceLoader threaded requests remain blocked on resource request/status/progress ownership, cooperative cancellation, cache/graph lifetime, owner callback delivery and actual parallel dependency loading across the synchronous load gate. Baking and other asynchronous domains still need their concrete consumers. Public explicit Thread/Mutex/Semaphore APIs remain separate slices over this foundation.

Invalid submission arguments are checked before lazy startup. Completion/progress/wait calls with no pending runtime reject their ID without starting workers or freezing startup settings. The cold public API test checks both boundaries before a later valid configured submission.
