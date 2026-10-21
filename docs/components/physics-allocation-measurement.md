# Physics allocation measurement

Last updated: 2026-10-22

## Scope and ownership

This report calibrates allocation evidence for the warmed CPU/GPU contract. It
changes no runtime type, numerical rule, public API or existing zero-byte gate.
The [explicit-sleep report](physics-sleep.md#explicit-sleep-with-automatic-sleep-disabled)
retains its original 24624/24624-byte intervals as unattributed.

## Counter and object-event controls

On Linux x64, CoreCLR .NET 10.0.1, workstation GC and disabled tiered compilation,
a no-inline method reads owner/global allocation counters around SpinWait. A
retained byte[37] allocated before the method establishes its allocation context.
All arrays, reports, mapping and worker startup are outside the measured interval.
An independent no-inline positive control creates byte[37] inside the interval.

The first unprofiled control has 0/256 nonzero quiet intervals and 12/256 nonzero
owner-counter intervals with a collecting worker. A second fixed run uses a
shared native phase tag and calibrated ObjectAllocated callbacks; each sample
keeps its original result, including nonzero counters.

| Profile, 256 empty intervals each | Nonzero owner-counter intervals | Owner object events | Other object events |
| --- | ---: | ---: | ---: |
| Quiet worker | 0 | 0 | 0 |
| Allocating worker | 11 | 0 | 467 |
| Allocating and collecting worker | 53 | 0 | 445 |
| Collecting worker without new allocations | 53 | 0 | 0 |

In the last row, owner and precise global counter deltas agree on every sample.
Their sum is 385640 bytes, despite zero ObjectAllocated events on any thread in
those phase tags. Forty samples report 8136 bytes; the remaining positive samples
report other amounts. Six positive samples have no observed change in the outer
generation-2 collection count, so that count alone is not a validity filter.
Quiet owner/global counters are zero in all 256 samples.

Each of four series also contains 32 empty and 32 positive controls. All empty
owner controls are zero; all 128 positive controls report 64 owner bytes. The
profiler records all 128 positive allocations at the Allocate/Measure stack with
raw object size 61 bytes. Every decoded stack succeeds and the profiler reports
zero recursive callbacks. Thus the empty-window disagreement is independently
observed, rather than inferred from a missing positive allocation signal.

The pinned [CoreCLR counter implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/vm/comutilnative.cpp#L919)
uses thread allocation contexts. Its precise global read suspends the runtime
and subtracts each thread's unused allocation-context tail. The local controls
establish the observed counter/event disagreement; they do not prove a particular
internal transition caused either historical physics interval.

## GC-injected physics diagnostics

The unchanged public consumer assemblies from 285ecd04 were also exercised with
a diagnostic startup hook. A runtimeconfig override enables that hook; it waits
for the first private checkpoint-restore JIT notification, then requests 64 GC
collections. Both collectors actually started and completed. These runs are
instrumented diagnostics, not ordinary functional/allocation acceptance.

The strict first raw cycle fails at 0/22848 owner/all-thread bytes on CPU and
0/86128 on NVIDIA GPU. Their traces contain finalizer-thread allocations from
SharedArrayPool.Trim: GCMemoryInfoData and ConditionalWeakTable enumerators. The
active trace windows contain 250 and 256 such pairs respectively; these are not
asserted to be exact counts inside the counter interval. Collector tracing ends
after its 64 requests, so later process activity is outside that trace window.
The earlier attempt with startup-hook support disabled has ready=0 and zero
collector requests; it is excluded from forced-GC evidence.

Frozen assemblies and PDB source hashes match 2714 authored files in commit
285ecd04177ac961a36bc6363def1e2e29779efa. Temporary parallel rendering edits did
not change those payloads and are not part of this evidence cohort.

## Reading future failures

Keep the strict counters and raw failures. Record runtime, source/payload hashes,
thread identity, measurement boundaries and GC context. Use independently
calibrated object events to locate the producer when counter-only evidence is
ambiguous; distinguish numerical/owner work from unrelated finalizer activity.
Do not subtract a presumed allocation quantum, raise a threshold, disable GC or
finalizers to obtain acceptance, or choose retries until a passing result appears.
A passing control does not retroactively clear a failed engine interval.

Receipts, producer sources, phase-tagged traces and an executable integrity check
are under `bin/physics-allocation-counters/2026-10-21/`; run its `validate.py` from
the repository root. The original 24624-byte intervals and the older separate
64-byte factory interval remain open without their own causal evidence. NativeAOT
measurement calibration, other runtimes/platforms, mass GPU performance and real
window FPS are outside this result.

## Aggregate point-query failure (2026-10-21)

The cold-view preparation candidate's ordinary aggregate runner exits -6 after 299.359 seconds in PhysicsPointContainmentTests, on the inferred GPU/raw/clockwise=true/offset=false/separate=false case. The original assertion preserves no numerical owner/all-thread deltas. No payload/source changes or new Xid are recorded. Its cause remains unattributed.

A fixed paired control copies the public query fixture into a diagnostic test assembly, marks each exact measurement interval through the calibrated profiler's shared state, and checks that static receiver body views remain absent. Both baseline and candidate execute all 32 cases once. Each has 2048 measured frames at 0/0 counter bytes and zero measured object events; all 128 positive controls produce 64 counter bytes and raw ObjectAllocated size 61, with valid stacks and no recursive events. Profiler startup, cold authoring, case transitions and positive controls are outside the query measurements. These instrumented isolated successes do not establish aggregate acceptance or retrospectively explain the failed interval. No threshold, GC/finalizer policy or physical workload is weakened.

Evidence: local ignored `bin/physics-publication-profile/2026-10-21/full-root/check.json`, `point-controls.json`, `point-probe/`, `point-{baseline,candidate}.trace.*` and their decoded JSON. The next allocation investigation needs original-interval counter values and paired object events; new uninstrumented green retries would not close this failure.

## In-fixture point-query tracing (2026-10-21)

PhysicsPointContainmentTests now retains exact owner/all-thread counter deltas, backend/raw/winding/offset/numerical-thread parameters, collection-count deltas, runtime and GC mode when its unchanged zero-byte assertion fails. Successful case lines retain the same parameters and trace tag. GC counts supply context only; they never filter an interval or change its strict gate.

Optional ELECTRON2D_POINT_ALLOCATION_TRACE selects a separately built calibrated Linux CoreCLR profiler library exposing e2d_gc_trace_begin/end. CORECLR_ENABLE_PROFILING=1, the matching CORECLR_PROFILER GUID and CORECLR_PROFILER_PATH, and a fresh E2D_ALLOCATION_TRACE prefix must accompany it. E2D_TRACE_STATE is rejected because this path uses native BEGIN/END markers. Without the optional setting, no profiler library is loaded. Explicit tracing rejects non-Linux or non-dynamic runtimes; normal NativeAOT query execution retains its own gate.

A run reserves a thousand tags starting at 1000000 + 1000 times its invocation index. Case offsets encode raw=8, clockwise=4, offset=2 and separate=1. Positive controls use offsets 100–227, empty controls 300–331, and native-transition warmup 999. Controls and library setup occur before fixture authoring/warmup. Native BEGIN precedes counter reads; END follows both final reads and runs even when a query assertion throws. Event windows therefore bracket the counters, and other-thread events near their edges must not be claimed to exactly equal a counter interval without further evidence.

The first standalone calibration validates all 256 owner positive allocations at 64 counter bytes/raw object size 61 and all 64 empty controls at zero. Its first positive window also sees three finalizer-thread startup objects of raw sizes 112, 32 and 72, with RunFinalizers/GetGCThreadStaticsByIndexSlow/InitializeCurrentThread stacks; that control reports 64 owner/280 global bytes. Those separate events are preserved rather than treating global=64 as a calibration requirement. All 32 complete query windows report 0/0 and no object events; all 354 native boundary pairs balance and profiler recursion is zero. This validates the markers; it does not attribute the older aggregate failure.

The frozen fixture, source/PDB proofs, calibration trace/decoded events and monitored aggregate diagnostic are under local ignored bin/physics-point-allocation/2026-10-21/. Run its validate.py from the repository root. No physics API row is promoted.

## Tagged aggregate timer-worker allocation (2026-10-21)

One aggregate diagnostic on the frozen CLR payload exits -6 after 330.657 seconds. Its GPU/raw/clockwise=true/offset=false/separate=false case reports owner/all=0/32 bytes over 64 warmed complete frames/queries, GC=0/0/0, trace=1002012, .NET 10.0.1 and serverGC=false. This is a failed strict gate, not aggregate acceptance.

That native bracket contains exactly one ObjectAllocated event: raw size 28 (aligned size 32), stackHR=0, OS thread 2263323 named .NET TP Worker; the fixture owner is 2220818. Its CoreLib stack is GetNonGCThreadStaticsByIndexSlow → Lock.EnterAndGetCurrentThreadId → TimerQueue.FireNextTimers → ThreadPoolWorkQueue.Dispatch → WorkerThreadStart → Thread.StartCallback. No physical solver or query frame appears. The observed producer is BCL timer-worker thread-static initialization, and its aligned size agrees with the global delta. Native markers bracket the counter reads rather than timestamping their exact interior; preserve that boundary limit. The scheduling origin and ownership of the timer are not established. A physics-free fresh-Thread Lock control reports 0/0 with no events, so it does not independently reproduce this allocation.

All 345 entered native brackets balance before the abort: two transition warmups, 256 positive/64 empty controls and 23 cases (22 successful, one failed). Each positive owner control retains its 64-byte counter delta and raw size-61 allocation stack; each empty control is zero. The failed process has no profiler SHUTDOWN record; a balanced final END is retained. No payload hash changed, software GPU was absent and no new Xid was recorded.

The frozen runtime PDB binds 2453 authored sources to f0d684b829ff14dc754beb61a8ebdf4125e4ad34; root/consumer PDBs bind the changed fixture separately. Main acquired independent rendering changes while the aggregate process ran: its source receipt records ten changed IO/rendering/tool files, subsequently committed in 3bfe58e46 and 34149e8a4. They did not change the frozen CLR payload. The strict NativeAOT publication instead binds 2475 runtime and 267 consumer documents after that integration, retains its IL/PDB/compiler response file with --warnaserror, and is a separate evidence cohort.

Ordinary unprofiled controls pass once each: CLR CPU+NVIDIA GPU (32 cases, 7.155 seconds), NativeAOT CPU+NVIDIA GPU (32 cases, 27.963 seconds), and NativeAOT CPU with unavailable GPU/dummy display (16 cases, .109 seconds). All 80 case intervals report owner/all=0/0 and GC=0/0/0. Explicit tracing rejects NativeAOT and shared-state overrides before query authoring. These focused passes preserve the failed aggregate gate. They do not attribute the older untraced 299.359-second failure, the original 24624-byte intervals or the separate 64-byte factory interval, and do not establish native allocator, foreign-platform, mass-performance or real-window acceptance.


## Bounded registry probe waits (2026-10-22)

Three cold factory probes used Task.WaitAsync solely to bound a synchronous registry-lock check. A calibrated CoreCLR allocation trace now observes each probe creating a 96-byte TimerQueueTimer through Task.CancellationPromise. The stacks identify PhysicsBackendOwnershipTests.Initialization and the public RunSpaceCreation/RunColliderCreation consumers. These are fixture timers; they are not evidence of a numerical-step allocation.

The probes now use Task.Wait with the same five-second deadline, assert completion, and retain their result/identity checks. They still run the actual registry operation on a worker and fail if the factory holds the registry lock. No runtime implementation, physical operation count, GC/finalizer preparation or allocation threshold changes.

The three original/corrected diagnostic pairs use the identical frozen runtime DLL. Each original probe creates one timer; every corrected probe creates none. A separate physics-free held-worker control expires both forms of a 25 ms deadline and then verifies the exact eventual result. Its asynchronous form creates one timer; the blocking form creates none. The profiler retains the existing native query-window markers and object-stack capture. These controls establish the timer's construction origin and remove an unnecessary scheduling source; they do not by themselves link a particular later TimerQueue.FireNextTimers allocation to that timer or retrospectively explain older aggregate failures.

Twenty-four public CLR/strict NativeAOT profiles pass on CPU without an available GPU, NVIDIA GPU and the independent registered implementation, each on owner/separate numerical lanes. Space creation retains 640 measured complete frames per profile; collider creation retains 128, totalling 9,216 frames at strict 0/0 owner/all-thread managed bytes. The full targeted ownership suite also passes CPU/GPU. The updated profiler passes 128 positive and 32 empty controls plus sixteen strict query cases (1,024 full frames), with 177 balanced native windows and no query allocation events. Together with the six paired diagnostic processes, 33 focused processes pass. Their source/PDB and stable-payload checks are recorded in focused-validation.json; these results do not establish aggregate acceptance.

The paired runtime is from 3fe45c21. Independent integer-graph rendering work entered main during the investigation; integrated CLR and NativeAOT checks bind the later source cohort separately. Evidence, compiler/source checksums, process maps and the aggregate diagnostic's actual status are retained under ignored bin/physics-timer-origin/2026-10-22/. A live or successful focused process is not aggregate acceptance. Physics coverage remains 1306/1369 (95.40%), with 63 open declarations. Historical 32/64/24624-byte aggregate failures and complete native/platform/network/performance gates remain open until their own evidence resolves them.
