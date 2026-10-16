# Node group membership

Node owns literal ordinal group identifiers independently of tree membership.
The first AddToGroup chooses persistence; another add leaves that choice intact.
RemoveFromGroup reports whether a member existed. Removal followed by another add
can choose a new policy. Add rejects empty names; empty lookup/removal/tree queries
have no matches. Whitespace, Unicode and underscore-prefixed identifiers survive
storage. Null identifiers reject. GetGroups produces an independent read-only
snapshot in ordinal order; explicit snapshots allocate.

The scene owner or current processing group may access local group metadata,
including physical nodes within its group. Other callers reject; detached callers
coordinate access. Pack capture rejects writes. Physical transforms and lifetime
keep their owner guards, validated before virtual consumer mutation hooks.
Metadata edits retain pose, RID and sleep. Tree queries observe current depth-first
order, including internal children; moving/detaching nodes changes tree results
without changing the node's memberships or old public snapshots. Group calls
retain current eligibility checks, reverse order and deferred membership lookup.

Only persistent memberships enter PackedScene and file archives. The public-only
--node-groups consumer checks both persistence histories and remove/readd, literal
identifiers, snapshots, tree transfer, group calls, wrong-thread/peer rejection,
actual worker read/write on pure and physical nodes, and a validation override.
Each profile reconstructs a physical hierarchy in a fresh process and verifies
exact groups, backend and motion. After warmup, 128 groups of two real intervals
combine worker membership churn, physical steps and moving-body callbacks at
strict zero owner/all-thread managed bytes; sleeping metadata-only participants
stay asleep. Evidence: bin/physics-node-groups/2026-10-16/.

## Runtime duplication dependency

The pinned Node duplication family is nondeprecated. Its default combines Scripts
and UseInstantiation. The pinned implementation creates a packed hierarchy and
then changes scripts on the existing identities. C# CLR subtype replacement cannot
provide this binding while preserving the accepted object identity. ADR 0091 still
requires a typed live-binding/identity contract. Ordinary Pack/Instantiate also
selects children/resources differently from runtime duplication. No duplicate
method, inert flags, script-dropping substitute or completion claim is added by
this group slice. The duplication rows stay open.

Native/driver allocation budgets, foreign-platform execution, complete inherited
Node API, live script binding, networking and hardware/window performance remain
separate acceptance work. GPU verification here uses pinned software Vulkan.

## Worker context preparation and allocation attribution

Scene processing registers an internal worker-start initializer for its thread-local group context. Every retained pool worker touches that context during startup. Pool construction waits for all initializers and prepares actual shared-gate contention before accepting a job; initialization failure joins every started worker and propagates the failure. Core owns the startup hook/barrier, while Scene registers its own context preparation. No game callback, public initializer, extra configuration switch or captured caller context is introduced.

The previous 128-frame warmup could repeatedly select one worker and leave another worker's first group-context access until a measured frame. A two-worker control measured 32/32 managed bytes on first touch and 0/0 on repeat; startup preparation changes first touch to 0/0. The group fixture's internal pool probe also caught 0/32 owner/all-thread bytes with pool=32, callback=0, both registered Solve samples=0 and no unattributed remainder. Earlier CPU and software-GPU serial failures had the same outside-callback remainder before the pool probe was added. Merely increasing warmup would not guarantee that every eligible worker had prepared its thread-local storage.

A separate NativeAOT 120-byte remainder survived TLS preparation. The internal probe attributed failures both to the owner and to pool execution, with zero callback/Solve samples. A debugger breakpoint on `System.Threading.Lock.CreateWaitEvent` captured the failing worker stack through `Condition.Wait` reacquisition and `WorkerPoolRuntime.Run`; the pinned [.NET 10.0.1 Lock source](https://github.com/dotnet/runtime/blob/v10.0.1/src/libraries/System.Private.CoreLib/src/System/Threading/Lock.cs) creates its per-lock AutoResetEvent lazily under actual contention. A timed monitor wait prepares a different per-thread object, so that candidate was rejected. Sync-table inflation was also absent in a captured failure. Broad allocation breakpoints suppressed the race in 48 runs and do not count as acceptance.

During startup the constructing thread holds the pool gate until every successfully initialized worker has reached that gate and entered `WaitSleepJoin`. The worker publishes its entering flag only after readiness signalling returns, so an initializer/barrier wait cannot satisfy this observation. The gate stays owned until the observation; contention cannot complete early. The constructor then releases the gate and no startup polling enters job/frame execution. This prepares the actual runtime contention path without reflection into runtime internals, extra frame warmup, sleeps chosen by elapsed time or a busy-waiting hot scheduler. A controlled NativeAOT test forced contention on a TLS-only prepared pool and failed with exactly 120 bytes, while a repeated entry on the control gate used zero. The corrected pool must pass the same forced-contention zero-byte assertion.

The public consumer retains separate scalar samples for the actual group callback and each registered numerical Solve. The executable test harness may additionally sample the internal pool counter; the public consumer uses only public engine API. Pool samples already include callbacks, and serial Solve samples overlap the owner, so the remainder avoids double subtraction. Built-in numerical workers are not independently sampled. The internal pool counter publishes successive job-completion intervals, which can include earlier startup/idle bytes when a worker first completes a job late. One accepted zero-total run consequently reported pool=168 and remainder=-168; this is a counter-window carryover, not an allocation count for that measured interval. The independent owner/all-thread GC samples retain their original strict zero assertions. Positive or unknown remainder alone is not automatically attributed to an external/runtime allocator.

Positive controls run **after** the original measured interval: an eight-byte array retained on the owner, actual group callback and each registered Solve must register exactly 32 bytes in Linux x64 CoreCLR/NativeAOT. The original 128 warm cycles, sleep preparation and 128 measured cycles of two real worlds/steps stay intact, including motion, force callback count, RID and sleep assertions. Every warmed allocation budget remains exactly zero. An early calibration attempt before warmup rejected a cold registered interval and was removed; calibration cannot turn cold setup into an unexplained array size or warm away the measured defect.

Before instrumentation, 32 ordinary and 32 CPU-contended parent runs of the unchanged accepted binary (16 CoreCLR and 16 NativeAOT per cohort, each with its existing fresh scene child) did not reproduce the original failure. Contended processes inherited two logical CPUs while two bounded integer-loop processes stayed live throughout. Those controls alone did not explain the anomaly. The later instrumented pool and direct first-touch controls supply the narrower causal evidence; all rejected logs are retained.

The final 12-profile public CPU/software-Vulkan/registered serial/separate CoreCLR/NativeAOT matrix verifies startup preparation, calibrated samples and the original physical workload. Another 42 NativeAOT physical parents (including 16 software-GPU serial and 16 separate profiles) and 32 CPU-contended registered/separate parents (16 CoreCLR and 16 NativeAOT) also retain strict 0/0 bytes, each with its existing fresh scene child. The native probe additionally runs TLS/startup and forced-contention controls; these two control processes are counted separately. Internal tests additionally cover a blocked initializer, constructor readiness, original failure propagation, joined failed startup and two distinct newly created workers. Evidence: bin/physics-group-allocation/2026-10-16/validation.json. Full native/driver allocation, hardware/window performance and complete physics acceptance remain separate. Coverage states stay unchanged.
