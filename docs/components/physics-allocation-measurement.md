# Physics allocation measurement

Last updated: 2026-10-24

## Scope and ownership

### Stationary character measurement preparation (2026-10-24)

Ordinary macOS x64 job `120808086157`, run `40070173409` at `780e574a5`,
aborts in `CharacterBodyTests.VerifyFloorSnapAndPlatform` at 07:43:58 UTC.
Its 64 stationary CPU `MoveAndSlide` calls report 5,848 all-thread managed
bytes after the unchanged 64-call warmup. The original interval did not record
owner bytes or native physical boundaries, so its allocation producer remains
unattributed. The complete suite exits 134 without its completion marker.

The stationary interval and its sibling reusable-collision getters now use the
shared calibrated double collection/finalization preparation before their
original warmups. Both retain their numerical/lifetime checks and strict zero
limits. Getter work remains 128 warmup/128 measured pairs; stationary work
remains 64 warmup/64 measured calls. Each interval records owner/all-thread
counters and a native tag before asserting, closing the physical bracket in
`finally`. Domain `5UL << 32` uses scenario 2 for getter pairs and 3 for stationary
slides; the existing lifecycle-idle interval retains scenario 1 and its original
preparation and workload.

The existing complete CPU/GPU character selectors run in both ordinary and
calibrated modes in the macOS diagnostics, retaining their logs and traces.
Preparation and successful controls cannot explain the older unmarked delta;
complete ordinary target execution remains required for release acceptance.

Local Linux ordinary and calibrated CPU/GPU selectors pass the complete
character cohorts. All six physical intervals retain 0/0 owner/all-thread bytes
and no object events. The two traces have 978 balanced brackets, 768 positive
controls, 192 empty controls, successful stacks and zero recursive callbacks.
The first CPU positive control also records three objects on `.NET Finalizer`
(112, 32 and 72 bytes) before measurement preparation: its 64 owner bytes and
280 all-thread bytes reconcile with the aligned native objects. These events
remain in the evidence; they occurred outside the physical character intervals
and do not identify the producer of the older macOS failure.

The complete ordinary Linux suite also passes in 584.3 seconds, with one final
completion marker and all frozen payload hashes unchanged. Its test source
matches the preparation change. This remains local evidence; complete ordinary
macOS execution on the revised source is still required.

### Runtime lock initialization (2026-10-23)

Profiled macOS ARM64 run `40031605911` at `426798799` fails the complete suite's
GPU SegmentShape/CapsuleShape cast interval at owner/all-thread `0/64 B`.
Artifacts `12535108294` and `12535661639` retain the physical trace, calibrated
recorder and matching CoreLib. Tag `281479272679959` has one 64-byte allocation
on worker thread `58378`, while the measured owner is `57792`. Its successful
stack resolves to `System.Threading.LowLevelLock..cctor`, `WaitAndAcquire`,
`WorkerThreadStart` and `Thread.StartCallback`; the allocated type is
``System.Func`2``. Both interval boundaries and `STACK_END 0` are present. The
assertion abort leaves no process-wide shutdown record.

The desktop test entry point now runs that static constructor before selectors,
fixture creation or allocation intervals. It retains the existing physical
workloads, warmup counts, positive controls and strict zero-byte assertions.
The lookup fails explicitly if the runtime no longer exposes the recorded type.
No production code depends on this internal CLR type. Corrected native execution
remains required. A local .NET 10.0.13 probe measures 64 B for the first constructor
call and 0 B for 128 repeated calls. The prepared executable passes the compressed
CPU audio and GPU compound selectors locally, retaining their zero-byte checks.
This evidence does not explain the earlier idle-Ogg owner
`3360 B` or separate-thread joint-policy `2712/19264 B` failures.

The selected-backend Area fixture now uses the calibrated collection/finalization
helper both before and after its 64 warm full-frame/audio/field cycles. Its former
private collector did not wait for finalizers after the second collection. The
controlled second-finalization probe verifies that preparation completes before
measurement. The 64 measured cycles still require zero owner/all-thread bytes.
Optional physical markers use domain `6UL << 32`, and both backend selectors run
before the complete macOS suite. The original untraced ARM64 run 39968402941
failed at 0/720 bytes; its allocation producer remains unattributed until a
matching physical trace is available. Preparation alone does not explain that
failure or establish target acceptance.

The Linux .NET 10.0.13 controls reject the former collector while its controlled
second finalizer is blocked and accept the shared helper. The four ordinary and
profiled CPU/GPU Area processes pass at 0/0 bytes. Each profiled process retains
163 balanced native boundaries, including its physical interval, with no object
events in that interval, successful stacks and zero recursion. Evidence is in
ignored `bin/release-audit/area-prepared/`; foreign execution remains separate.

The current ordinary macOS ARM64 aggregate at `c847dc38d` passes its complete
suite. The separate profiled aggregate passes the Area interval at 0/0 bytes,
then fails `AudioCompressedTests.VerifyBoundaries` during idle compressed mixing.
Its original assertion retained no byte delta or format tag and had no physical
audio bracket. The fixture now records owner-byte deltas and MP3/Ogg active/idle
tags through the existing calibrated recorder, using domain `7UL << 32` and
scenario offsets one through four. Each native boundary closes in `finally`.
The 50 active/20 idle warm blocks, 64 measured blocks, 512-frame buffer and strict
owner-only zero-byte check remain unchanged; no collection or finalizer policy
is added. `ELECTRON2D_TEST_AUDIO_COMPRESSED=cpu` selects the same non-native host
mode as the aggregate, while `1` retains the complete native-output checks.
The macOS diagnostic captures both the standalone audio intervals and their
later aggregate execution. The older failure still needs its own matching
numeric/physical evidence; successful controls do not identify its producer.

The next profiled cohort, run 39987153000 at `3fa842b13`, passes the complete
ARM64 suite. Both architectures record four zero-byte audio intervals with no
physical object events; each root trace has 5,108 balanced boundaries. ARM64 ends
with `SHUTDOWN recursive:0`. The x64 root has no shutdown and its complete suite
fails later in separate-thread `PhysicsJointPolicyTests.VerifyLifetime`, at
2,784/19,336 owner/all-thread bytes. That interval had no physical marker, so its
producer remains unattributed. The fixture now brackets its original counter
interval through the existing recorder, domain `8UL << 32`, and logs backend,
host-GPU and separate-thread roles before asserting the same zero-byte limits.
Its 128 warm/128 measured frames, edits, thread/lifetime checks and collection
policy are unchanged. The macOS workflow retains the existing full separate-thread
CPU selector in ordinary and traced standalone controls before its full suite.

Ordinary x64 run 39991319732 at `4a80378d0` later fails the same separate-thread
joint-policy interval at 0/9,024 owner/all-thread bytes. Profiled x64 run
39996059802 at `87042214a` stops earlier, in the public GPU contact-policy interval
at 0/64 bytes, before reaching its newly marked late joint-policy interval.
Neither failed interval has a matching physical marker. Contact-policy fixtures
now use domain `9UL << 32` around their original 128 measured shape-bias edits
and full steps after 256 warmups, including the existing owner/all-thread reads.
Markers close in `finally`; role and tag accompany the original counter deltas.
The physical workload, timings, collection policy and zero thresholds remain
unchanged. CPU/GPU standalone corrections are diagnostic controls, not an
explanation of either failed full-process interval.

Local Linux x64 controls pass all four contact-policy roles, each ordinarily
and profiled at 0/0 bytes. Every trace retains 163 balanced boundaries,
128 positive and 32 empty controls, successful stacks and zero recursion;
its physical interval contains no object events. CPU/GPU calibration controls
also account for foreign finalizer objects separately from that physical interval.
These receipts do not resolve either macOS aggregate failure.

The resting-body fixture drains the existing calibrated collection/finalization
warmup before its first 64-frame counter interval. Its failure records both
owner and all-thread bytes; the zero-byte assertions and physical frames remain.
An isolated CPU scene profile passed while an initial aggregate run failed this
interval. A second aggregate passed the same interval before continuing; those
controls do not attribute the original counter change to engine allocations.

FontTests uses the same collection/finalization boundary after its sixteen
prepared font/layout/recording cycles. Its sixty-four measured rebuilds retain
their strict zero owner-thread-byte assertion. The Linux ARM64 aggregate's
2,416-byte counter failure is retained as evidence, without assigning its cause
or treating local controls as corrected target acceptance.

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

## Current aggregate after probe isolation (2026-10-22)

The corrected frozen integration payload passes all 32 CPU/GPU point-containment cases in the aggregate process: 2,048 measured complete frames/queries at owner/all-thread 0/0 bytes and GC=0/0/0. All 354 native control/query boundaries balance, no query object event occurs and no TimerQueueTimer construction is observed. This includes the previously failing tag 1002012. It is current interval evidence; it does not retrospectively attribute the older untraced 24,624-byte or separate 64-byte failures.

The process later exits -6 after 2,391.773 seconds because TileTerrainTests.SaveFreshTerrain times out while waiting 30 seconds for a fresh terrain-scene child. Payload hashes remain unchanged; no new Xid is recorded. The fresh-child timeout remains unresolved and aggregate acceptance is failed. The result is not the earlier observer's 25-minute command-limit stop. Receipts and decoded events are in bin/physics-timer-origin/2026-10-22/aggregate-integrated-result.json and aggregate-integrated-fast/. The newer lattice suite uses its own later rendering/source cohort and does not replace this failed full run.

## Terrain child investigation (2026-10-22)

The original frozen terrain suite passes in isolation both without the profiler (94.263 seconds) and with it (170.722 seconds), each including CPU/GPU parent consumers and two fresh CPU children. Replaying the identical preserved 492 KiB scene directly through the same frozen root runner also passes without/with the profiler in 9.363/14.439 seconds. Those are diagnostic wall times, not physics throughput measurements. The failed aggregate's first terrain child has a profiler shutdown record; its later child has only initialization. Neither maps a GPU driver. The earlier receipt did not retain child stdout, environment, scene or progress, so these controls cannot establish the original timeout's cause or attribute it solely to profiling.

The fixture now reports both child streams, PID, elapsed time and exit code on success, failure and timeout. It keeps the 30-second execution deadline, kills the timed-out process tree and bounds termination cleanup to five seconds. Archive-load, instantiate and consumer-attachment progress distinguishes cold stages before real queries and re-painting. No physical operation, allocation threshold or production behavior changes. The current complete terrain fixtures pass without/with profiling in 146.201/125.676 seconds; overlapping functional controls make those totals unsuitable for a speed comparison. Six successful isolated profiles and three expected-failure child controls validate the receipts. The timeout control retains both streams after termination at 30.157 seconds; nonzero exit 17 and exit zero without the success marker also reject with both streams intact.

Isolated passing runs do not close the failed aggregate gate. Current source/PDB proofs bind 943 root-test and 2,501 runtime documents. Frozen payloads, preserved scenes, process samples, controlled child failures and validation.json are retained under ignored bin/physics-terrain-child/2026-10-22/. Physics remains 1306/1369 (95.40%), with 63 open declarations.

## Wide-coordinate point-field aggregate failure (2026-10-22)

The subsequent ordinary aggregate on that frozen 2,501-document runtime exits -6
after 3,655.258 seconds. Both fresh terrain children complete within the retained
deadline, and all CPU/GPU point-containment cases pass at 0/0 bytes. The later
CPU point-gravity fixture passes its positive-coordinate warmed interval at 0/0,
then fails the negative-coordinate interval. Its original assertion reports
neither owner/global byte counts nor collection deltas, so the allocation's size,
thread and cause are unknown. No new Xid or changed payload is observed. This is
a failed aggregate; successful numerical checks and terrain children do not close
its allocation gate or explain the preceding child timeout.

Both point-gravity allocation brackets now retain exact owner/all-thread bytes,
GC generation deltas, backend/case, runtime and server-GC policy on failure.
Their 128 warmup and 128 measured complete frames, authored resets, tolerances
and strict zero-byte assertions are unchanged. The existing optional native
begin/end tracer is shared as PhysicsAllocationTrace with point containment,
including its native lifetime/error guards and positive/empty controls. Within
each run's tag block, offsets 32/33 identify positive/negative wide coordinates
and 34 the authored-reset field interval; query offsets and calibration ranges
retain their prior values. Markers surround counter reads and the complete
physical loop; they do not timestamp the exact interior of those reads.

The diagnostic source cohort includes the later matrix-graph integration:
2,509 runtime and 946 root-test documents. Its runtime differences from the
failed cohort are twelve rendering/resource files and eight new numeric graph
types, recorded separately from these test-only changes. No production physical
implementation, public signature or coverage state changes. Original receipts
and the new frozen source/compiler evidence are under ignored
`bin/physics-point-gravity-allocation/2026-10-22/`. Aggregate attribution and
complete native/platform/network/performance acceptance remain open.

Twelve focused profiles pass: four ordinary CLR and four strict NativeAOT
CPU/GPU owner/separate point-field consumers, two calibrated CLR field consumers
and two CLR containment regressions. All 6,912 measured complete frames retain
0/0 owner/all-thread managed bytes: 3,840 field frames and 3,072 containment
frames. The two calibrated profiles retain 256 positive controls at 64/64 bytes,
64 empty controls at 0/0, 328 balanced native windows and zero object events in
the physical intervals. All 256 positive allocations resolve with raw size 61
(aligned 64) and successful stacks; recursion counts are zero. This validates
the observation path and focused intervals, not attribution of the failed run.

Frozen payload manifests and source/PDB inputs match the preserved build inputs. The
four-document native probe includes the same three field/tracer files and its
explicit entry point; its runtime matches the CLR source cohort. CPU processes
map no GPU driver; GPU profiles map NVIDIA without a software GPU, and native
processes map no CoreCLR. Release/API/wiki and scoped formatting/identity checks
pass. `validation.json` records the focused profiles and preserves the original
failed aggregate separately. The complete calibrated process now has the failed
terminal result below. Physics stays 1306/1369 (95.40%), with 63 open.

## Finalizer events in the wide-coordinate diagnostic (2026-10-22)

The same complete process exits -6 after 4,242.435 seconds at the negative
wide-coordinate CPU point-field gate. Its 128 warmed complete frames report
owner/all=5,520/10,512 bytes, GC=0/0/0, trace=1003033, .NET 10.0.1 and
serverGC=false. The positive wide-coordinate interval passes at 0/0. This is a
failed allocation gate; no physical implementation or threshold has changed.

The failed native bracket contains 29 successful ObjectAllocated stacks on OS
thread 763448, named .NET Finalizer; the fixture owner is 763440. Fifteen
System.GCMemoryInfoData objects of 288 bytes and fourteen
ConditionalWeakTable enumerators of 48 bytes total 4,992 bytes. Fourteen pairs
run through SharedArrayPool.Trim and Gen2GcCallback.Finalize; the remaining
memory-info object runs through PortableThreadPool.OnGen2GCCallback. These are
observed finalizer callbacks, even though collection counts do not change in
the measured interval.

The 4,992 observed bytes equal the all-minus-owner counter difference. No owner
ObjectAllocated event explains its 5,520-byte delta. The calibrated profiler does
not thereby prove a counter defect or absence of engine allocations: ownership
and accounting of that unmatched delta remain open. Native markers surround the
counter reads rather than timestamping their exact interior. Do not subtract
these events, discard the interval or claim aggregate acceptance.

All 517 entered native brackets balance before abort: three transition warmups,
384 positive controls, 96 empty controls, 32 containment cases and two wide
point-field cases. Every positive owner/all control reports 64/64 bytes with one
raw-size-61 allocation and a successful stack; every empty control is 0/0. The
32 containment intervals and positive wide-field interval report 0/0 with no
object events. The abort has no profiler SHUTDOWN record. Payload hashes remain
unchanged and no new Xid is recorded.

The runtime/root PDBs still bind 2,509/946 authored documents to preserved inputs
before the contact-range prototype and later rendering integration. The trace,
terminal receipt and scenes remain in
`bin/physics-point-gravity-allocation/2026-10-22/current-aggregate-trace/`;
`bin/physics-report-ranges/2026-10-22/aggregate-source-proof.json`,
`aggregate-decoded.json` and `aggregate-analysis.json` retain the source binding,
decoded stacks and unresolved difference. The older untraced failures retain
their own limits. Complete allocation, native/platform/network and performance
acceptance remain open; the declaration census is unchanged.

## Owner-counter boundary diagnostics (2026-10-22)

Engine-free controls do not reproduce the unmatched owner delta. Four array
entry points (ordinary, uninitialized, pinned and reflection-created byte arrays)
each retain eight 5,520-byte owner/global counter deltas and eight corresponding
ObjectAllocated events. Eight small positive controls retain 64-byte deltas/raw
size 61; eight empty controls are 0/0 without events. These controls establish
coverage of those allocation paths, not every runtime allocation source.

A separate 512-window owner-only/owner-plus-precise control retains zero owner
bytes during 1,366 explicit nonblocking generation-2 collection requests. Four
paired processes repeat 512 windows each with/without fourteen seeded ArrayPools
and with/without profiling, using the identical executable. All 2,048 owner
deltas remain zero. The seeded profiled process observes 38,858 finalizer events,
including the same memory-info/enumerator producers as the failed aggregate.
Neither collection requests nor those finalizers alone reproduce its owner
delta. These controls do not resolve or replace the failed physical gate.

Optional point-field tracing now retains 129 owner-counter samples around the
unchanged 128 complete authored-reset/step intervals. A failed byte gate reports
the initial counter-read boundary, each nonzero frame delta, and the final
boundary after native END. The reusable sample array is created before fixture
authoring/warmup. Untraced execution creates no tracer or sample array. No
collection preparation, physical operation count, retry or allocation threshold
changes. Tag offset 400 calibrates exactly 64 bytes before frame zero, 64 bytes
in frame 63 and 64 bytes after the last frame, with all other frame deltas zero.

The local framed profiler retains the existing begin/end ABI and also writes its
cumulative recursion count at each END, so an aborted process need not rely on
SHUTDOWN for that diagnostic. The control projects, immutable receipts, decoded
events and profiler source/compiler command are retained under ignored
`bin/physics-report-ranges/2026-10-22/`. These are observation tools; attribution,
aggregate acceptance and full-goal readiness remain open.

Six CLR profiles pass 4,608 complete CPU/GPU field/containment frames at strict
0/0 owner/all-thread managed bytes. They retain 864 balanced native marker pairs,
five successful frame-boundary calibrations, no physical object events and zero
reported profiler recursion. CPU profiles have no GPU driver; GPU profiles map
NVIDIA without a software GPU. The frozen working-tree cohort binds 2,526
runtime, 946 root and 269 public-consumer source documents, based on 53b2bd674;
it includes the separately pending contact-range prototype and does not accept
that optimization. Scoped formatting, Release/API/wiki and identity checks pass.
NativeAOT remains separate; the complete aggregate result below fails. Successful
focused windows do not attribute the historical 5,520-byte owner delta.


## Framed aggregate: owner delta in frame 77 (2026-10-22)

The same frozen aggregate process exits -6 after 3,594.691 seconds, before its
5,400-second diagnostic deadline. The CPU negative wide-coordinate point-field
case reports 3,184 owner and 8,176 all-thread bytes over its unchanged 128 warmed
complete frames, with collection deltas 0/0/0. Tag 1003033 brackets the failure.
The new 129 counter samples locate the entire owner delta in frame 77; the
initial and final boundaries both report zero. This is a failed strict physical
gate, not aggregate acceptance or a successful allocation fix.

The bracket contains 29 ObjectAllocated callbacks on the separate .NET Finalizer
thread: fifteen 288-byte GCMemoryInfoData objects and fourteen 48-byte
ConditionalWeakTable enumerators. Their stacks again identify
PortableThreadPool.OnGen2GCCallback and SharedArrayPool.Trim through
Gen2GcCallback.Finalize. The 4,992 observed bytes equal all-minus-owner. There is
no owner-thread ObjectAllocated callback in the bracket. The 3,184-byte owner
delta remains unattributed; finalizer callbacks do not explain it.

All 520 entered native brackets balance. Their END records retain zero
cumulative profiler recursion, including the aborting bracket. All 384 positive
controls preserve their 64-byte owner counter and raw size-61 callback; all 96
empty controls remain zero. Three frame-boundary controls locate their
64/64/64-byte prefix/frame63/suffix allocations correctly. Thus the recorded
reentrant-callback drop counter does not account for the missing owner event.
This does not prove that every runtime allocation route has been observed.

The [v10.0.1 counter implementation](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/vm/comutilnative.cpp)
reads alloc_bytes + alloc_bytes_uoh minus unused current allocation-context
space. It does not enumerate ObjectAllocated callbacks. That source distinction
provides no causal explanation for this failure and changes no byte threshold.
The next allocation investigation must distinguish an actual unobserved owner
producer from allocation-context accounting; repeating the same full run without
stronger observation would not resolve that distinction.

The payload is unchanged, NVIDIA remains available, and no new Xid is recorded.
The 2,526 runtime, 946 root and 269 public-consumer source bindings are checked
through preserved overrides after the later texture-boundary fix. Raw/decoded
traces and assertions are in local ignored
bin/physics-report-ranges/2026-10-22/framed-aggregate-trace/ and
framed-aggregate-analysis.json. The owner result differs from the earlier
5,520-byte failure; neither result is discarded or attributed from the other.
Physics coverage remains 1306/1369 (95.40%), with 63 open declarations.


## Optional native allocation-context snapshots (2026-10-22)

PhysicsAllocationTrace also accepts ELECTRON2D_POINT_CONTEXT_TRACE while ordinary
CoreCLR allocation tracing is enabled. Its caller-supplied helper is built from
[PhysicsAllocationContextProbe.cpp](../../tests/Electron2D.Tests/Fixtures/PhysicsAllocationContextProbe.cpp):

`g++ -std=c++17 -O2 -fPIC -shared -Wl,--no-undefined -o libContextProbe.so tests/Electron2D.Tests/Fixtures/PhysicsAllocationContextProbe.cpp`

This is a bounded Linux x64 observation profile. Initialization follows the
known counter entry stub, validates its complete native instruction sequence,
and obtains that implementation's TLS accessor/descriptor. A different counter
implementation rejects before field authoring. It never patches runtime code
or writes the allocation context. The short snapshot call suppresses the GC
transition and copies TLS identity, allocation pointer/limit and SOH/UOH byte
counters into storage created before fixture authoring/warmup. Failed frame
reports include both native boundaries alongside the existing owner delta.
Ordinary untraced execution loads neither diagnostic library; NativeAOT tracing
remains explicitly unavailable.

The standalone probe validates sixteen allocations of each of four array sizes
(64, 3,184, 5,520 and 100,024 counter bytes), plus sixteen empty controls. All 80
native formulas agree with the managed counter on the observed .NET 10.0.1
runtime; null and wrong-function initialization reject. The ordinary frame-63
calibration additionally requires agreement at all 129 native boundaries.
There is no subtraction, counter replacement or changed zero-byte assertion.
These observations do not yet reproduce or explain the aggregate owner delta.
Native helper code, source/PDB/payload proofs and direct controls are retained
under ignored bin/physics-report-ranges/2026-10-22/allocation-context-probe/ and
context-source-proof.json.


The --point-context-lifetime root-runner mode accepts an incorrect helper and a
correct helper path. [PhysicsAllocationContextInvalid.cpp](../../tests/Electron2D.Tests/Fixtures/PhysicsAllocationContextInvalid.cpp)
supplies deliberate zero samples for the negative path. Calibration failure
releases both libraries before propagating the original error; the same process
checks that the helper mapping disappears, opens a correct tracer and verifies
normal disposal. Both backend labels exercise this setup/retirement contract
without creating a numerical world. The first incorrect-snapshot control was
RED because Open released only its base profiler reference; the corrected
cleanup and in-process recovery are GREEN. Numerical GPU evidence remains the
separate field/query profiles, not those enum-label checks.


Four initial CLR context-enabled CPU/GPU owner/separate profiles, the original
trace ABI and ordinary CLR/strict NativeAOT query consumers pass 8,064 measured
physical frames. Those retained source cohorts precede the cleanup correction
and the independent sampled-image graph integration. The final integrated main
cohort repeats CPU/GPU context-enabled field intervals and strict NativeAOT CPU
queries, adding 1,792 frames. All 9,856 physical intervals retain 0/0 owner/all
managed bytes. Two root-runner lifecycle processes additionally verify failure
cleanup and same-process recovery; they are excluded from physical-frame totals.
Unsupported counter code and NativeAOT tracing reject before authoring.

The final cohort binds 2,537 runtime, 946 root and 269 public-consumer documents
at e820cbe20 plus this fixture change and the separately pending contact-range
prototype. It does not accept that optimization. The older graph/texture source
versions remain preserved and checked independently. Source/payload proofs,
strict publisher arguments and context-initial-validation.json/context-validation.json
record these separate scopes. Successful focused checks do not close the failed
framed aggregate, the unattributed owner delta, native allocator, foreign-platform,
network, mass-throughput, window-FPS or owner-acceptance gates. No API row changes.

## Background allocation-context retirement (2026-10-22)

The installed workstation CLR build fad253f51 maps through its source manifest to
runtime commit 138d5fe8. Its per-thread counter adds SOH/UOH bytes and subtracts
the unused allocation-context tail. At the end of background marking, the GC
voids context pointers before sweeping. This can remove that subtraction without
allocating an object or starting another collection inside the measured interval.
Collection-count deltas alone therefore cannot exclude this counter transition.

A standalone CPU control keeps a 500,000-object graph alive, starts background
collection before the interval, allocates its seed before tracing, then only
sleeps. Sixteen of sixteen calibrated background intervals report 8,136 counter
bytes: exactly the prior unused tail. TLS, SOH and UOH stay unchanged while both
pointers become zero. All three collection-count deltas are zero. Sixteen paired
blocking-collection controls report zero and retain their contexts. Positive
controls before and after report 64 counter bytes and raw ObjectAllocated size 61;
the 32 empty intervals have no object event. All 34 native brackets balance with
zero recursion. These controls contain no physics frames or GPU work.

PhysicsPointGravityRangeTests now finishes two blocking full collections and
their pending finalizers before each 128-frame warmup, for both the ordinary and
wide-coordinate measurements. AllocationWarmupCheck verifies completion of the
second finalization pass. Collection remains outside both warmup and measurement;
the same 128 complete frames, native tracing and strict owner/all-thread zero-byte
assertions remain. No bytes are subtracted, no interval is retried or filtered,
and GC is not disabled. The standalone control does not retrospectively attribute
the older 3,184/5,520-byte owner failures; their absent native context snapshots
remain a limit. The already-running frozen aggregate retains its original code.

The exact CLR sources, build mapping, controls, decoded events and hashes are
retained under ignored bin/physics-report-ranges/2026-10-22/runtime-source-audit/.

Six prepared CLR field profiles pass: ordinary CPU/GPU plus context-traced CPU/GPU
on owner and separate numerical threads. All 2,304 measured complete frames retain
owner/all-thread 0/0 bytes. The four traced profiles retain 660 balanced brackets,
524 positive object events and no physical-interval object event. Their frozen
cohort binds 2,541 runtime, 946 root and 269 consumer documents at 24e906cb4 plus
the pending contact-range prototype. That prototype, the older failed aggregates,
NativeAOT, native allocation totals and performance remain separately unaccepted.

## Aggregate context-retirement witness (2026-10-22)

The original context-enabled aggregate terminates at the negative wide-coordinate
CPU case after 3,757.413 seconds, before the preparation correction is part of its
frozen payload. Tag 1003033 reports owner/all-thread 3,184/5,440 bytes. Frame 83
has an unused native context tail of exactly 3,184 bytes; frame 84 has zero pointer
and limit, the same TLS identity and unchanged SOH/UOH totals. Both native formulas
equal the saved owner counters. This directly identifies the owner counter jump
as the independently reproduced background-context retirement, with no owner
ObjectAllocated event.

The bracket contains twelve finalizer-thread objects: seven GCMemoryInfoData at
288 bytes and five ConditionalWeakTable enumerators at 48 bytes, totaling 2,256
bytes, exactly the all-thread remainder. Eleven stacks pass through
SharedArrayPool.Trim and one through PortableThreadPool.OnGen2GCCallback; all
reach Gen2GcCallback.Finalize. Counter interiors are still bracketed rather than
individually timestamped. All 520 native brackets balance with zero recursion;
the payload is unchanged. The raw trace and decoded evidence are retained in
context-aggregate-trace/ and context-aggregate-analysis.json under the same ignored
report directory.

This attributes the current failed interval; it does not turn its zero-byte gate
into a pass or retrospectively certify the older intervals without native context
snapshots. The current-main ordinary aggregate below completes the separate
unprofiled verification; this failed interval itself remains rejected.

## Prepared ordinary aggregate (2026-10-22)

The unprofiled frozen runtime at c1af6d3a6a2a9b738c6cd9362151a1f0b3bc9459
contains the blocking field-fixture preparation. Its six CPU/GPU point-field
gates pass 768 measured complete frames at strict 0/0 owner/all-thread bytes
inside the ordinary aggregate process. This is a separate source cohort from
the shared pair/contact completion change.

The aggregate then exits -6 after 4168.539 seconds at
IntegerSourceTextureTests.RunManaged: libElectron2DKTX.so is absent from the
frozen native payload. Payload hashes remain unchanged and no Xid is recorded.
This dependency failure kept that cohort's full-suite gate open; its point-field
success does not retrospectively clear older failed intervals. Receipts and preserved
source bindings are in local ignored
`bin/physics-report-ranges/2026-10-22/current-pair/main-aggregate-analysis.json`
and `post-contact-batch-source-overrides.json`.

## Current-main ordinary aggregate (2026-10-22)

The complete default CLR test aggregate at
`c17f173af98febab1fc3c05b8843c6e2ca0413e9`, including shared pair/contact
completion, exits 0 after 4391.535 seconds with the final
`Electron2D checks passed.` marker. This is an ordinary, unprofiled process with
no test-scope environment filters. All six CPU/GPU point-field allocation gates
pass 768 measured complete frames at exactly 0/0 owner/all-thread bytes.
The existing force-overflow failure, selection/publication and retirement checks
also pass; no assertion or allocation threshold was relaxed.

The Release build supplies the existing local native KTX bridge through
`Electron2DKTXNativeLibrary`. Its integer/compressed/block image tests now pass
inside the full aggregate, beyond the previous missing-dependency stop. The
frozen payload remains unchanged. Runtime/source/PDB proofs bind 2552 runtime,
946 root-test and 269 public-consumer documents to this revision: 3766 match Git,
and the sole extra is a preserved, uncommitted fresh-contact checkpoint regression.
Two unaccepted runtime report-range prototype files are excluded using verified
committed-source substitutions. This is not acceptance of that prototype.

Runtime SHA-256:
`0c6dfbc36f18aa18884e4fadeef727657453d175122fb33c04f54ecd535f324f`.
Root test SHA-256:
`bf0b38a162030d289c53560d37dc3b76abd637bf3151f2450caeb54cfb4ef078`.
Process maps observe the NVIDIA backend and no software GPU; a fresh kernel-log
read confirms no new Xid. The 2149 telemetry samples reach 43 C and 115.98 W.
These observations do not verify the unchecked power cable or sustained hardware
stability.

Receipts, payload/source/shader proofs and the validator are under local ignored
`bin/physics-pair-contact-batch/2026-10-22/`: `main-aggregate-analysis.json`,
`main-source-proof.json`, `main-shader-proof.json`, `main-cohort-manifest.json`
and `validate-main-aggregate.py`. The validator also checks its six-gate parser
against the earlier rejected KTX cohort, without requiring gate-log order.
The successful current aggregate does not retrospectively accept older failed
intervals or prove all native, network, foreign-platform or massive-scene
performance requirements. Physics declaration coverage stays 1306/1369 (95.40%),
with 63 open rows and 92 excluded.

## Default CLR aggregate at d90d89c1c (2026-10-22)

The later frozen default CLR suite at
`d90d89c1c989fdb4a98243d53000f29f18144fbb` exits 0 in 4740.632 seconds with
`Electron2D checks passed.` The ordinary process has no test-scope filters or
profiler attached. All six point-field gates pass 768 measured complete frames at
strict 0/0 owner/all-thread managed bytes. Payload hashes remain unchanged;
process maps observe NVIDIA without a software GPU, and the fresh kernel read
contains no new Xid.

The source/PDB proof covers 2554 runtime, 947 root-test and 269 consumer documents:
3769 match the pinned commit, plus the same explicitly preserved extra checkpoint
test. The two unaccepted report-range runtime prototypes remain excluded through
verified committed-source overrides. Runtime SHA-256 is
`d20e79e032c31d87ef04eb3d4fa3c3329ee518dc7ec3787dd07085e28a61cec4`;
root-test SHA-256 is
`2bbb5d0060baa7479a663d3f3bd2379908c8e2439c0859e90248d4c61fd9d636`.
The receipt SHA-256 is
`846eb79bfb69fe96b63a822f4946029dea7335dffe6e8d30c993c4850f0e883a`;
its log is `6b80582c40b23dd3ef1b6b62a0867ba711a90ba0076a63228c3404f305aff333`.

Local evidence is `d90-aggregate-analysis.json`, `d90-source-proof.json`,
`d90-cohort-manifest.json`, `d90-aggregate/check.json` and
`validate-d90-aggregate.py` under the existing pair/contact evidence directory.
This completes that exact older aggregate. It does not certify later rendering
or cached-color command changes, the rejected contact-copy experiment, mass-scene
speedup, real-window FPS or the complete physics goal. The API census remains
1306/1369 (95.40%), with 63 open and 92 Excluded.

## Shared pin-joint measurement preparation (2026-10-23)

The point-field test's existing two blocking collections and pending-finalizer
waits now live in `AllocationWarmupCheck.Collect`. Both point-field profiles keep
their original calls and calibration; `PinJointTests` also calls it before its
40-frame warmup. The following 64 complete moving-joint frames still require
exactly zero owner-thread bytes on both CPU and GPU. Preparation does not run
inside a measured interval, subtract bytes, retry failures or disable GC.

During release validation the ordinary aggregate reported 5,152 owner bytes in
the CPU moving pin-joint interval after the GPU pipeline checks. A fresh focused
joint-policy run passed unchanged. That failed interval has no native object or
allocation-context trace, so its cause remains unattributed. The shared
preparation applies the already calibrated isolation against background context
retirement; it does not retrospectively accept or explain the failed interval.

The focused CPU/GPU joint contracts pass with this preparation. The shared
second-finalization calibration and both point-field runs also pass: six
128-frame measured intervals retain 0/0 owner/all-thread bytes. A fresh ordinary
aggregate remains required; these focused runs do not replace it.

## Contact-history and capsule measurement preparation (2026-10-23)

`PhysicsContactPersistenceTests` and `CapsuleShapeTests` now use the same
`AllocationWarmupCheck.Collect` helper after their existing 256/64 warmup frames
and before both allocation baselines. The contact sample array is allocated before
preparation. Their original 128/64 complete measured frames, numerical assertions
and strict zero owner/all-thread byte limits remain unchanged. No measured frame
is skipped, no result is retried and GC remains enabled.

An independent Linux x64 .NET 10.0.13 positive control runs the actual shared helper
before allocating either one 64-byte object or two 32-byte objects. Both counters
still report 64 bytes. A worker control reports owner/all-thread/worker bytes of
0/64/64. The native prototype records all four objects with successful stack
snapshots and zero recursive callbacks; receipts are retained under ignored
`bin/release-audit/managed-allocation-prototype/`. This verifies continued detection
of later real allocations on that runtime. It does not attribute the earlier
macOS 64-byte failures or certify target execution; those intervals lack object
and allocation-context traces. The corrected target aggregate remains required.

## General-cast allocation diagnostics on macOS (2026-10-23)

An untraced macOS x64 aggregate at 23fed8a78 reports 0/64 owner/all-thread bytes
in the public general-cast interval. That fixture already finishes its two
collection/finalization passes after 128 warmup queries. The failed 128-query
interval has no object trace and remains unattributed and rejected.

A later untraced x64 aggregate at 16a65ea38 reports the same 0/64-byte delta in
the 64-cycle dense contact snapshot/transport/replay interval. It also remains
unattributed.

GeneralCasts now opens the existing optional PhysicsAllocationTrace around each
unchanged interval. It retains ten shape combinations, all numerical assertions
and the strict 0/0 byte gate. A separate tag domain distinguishes this consumer
assembly from root point-field controls. Dense contact measurements use another
scenario domain around their unchanged 64 complete cycles. Assembly domains
prevent collisions between independently compiled copies of the shared helper. Begin/end markers close on exceptions;
geometry labels and counter deltas are printed after the interval. Ordinary runs
leave profiling disabled. macOS supports object/stack tracing; native allocation-
context snapshots retain their Linux-only runtime guard.

The manual macOS workflow's allocation_trace input builds a matching native
profiling library and self-contained calibration executable before a diagnostic
full run. Calibration distinguishes one 64-byte object, two 32-byte objects and a
worker's 0/64/64 owner/all-thread/worker bytes; two empty brackets, successful
managed stacks, seven balanced native brackets and zero recursive callbacks are
required. The shared second-finalization preparation is checked as well. The
profiler records object sizes, thread identities, module/method tokens and stacks,
without object contents. Callback output is flushed and I/O failure rejects the
process. Native interface and library references are released by their owners.

Trace, calibration, native-library and relevant managed-assembly receipts are
uploaded separately from ordinary logs. They do not replace an untraced target
gate, retrospectively attribute the 64 bytes or establish native allocator totals.
The local Linux calibration and traced/untraced CPU/GPU compound controls provide
helper evidence; matching macOS execution remains required.

The completed ARM64 untraced runs at 23fed8a78 and 16a65ea38 pass the earlier
physics intervals but fail FrameMonitorContract's 256 prepared frames with
0/6672 and 0/4416 owner/all-thread bytes respectively. Those failures are not
explained by a successful x64 interval. The optional profiler now brackets that
same 256-frame timing/navigation interval in its own scenario domain. Begin/end
callbacks surround the original counters; frame count, warmup and strict zero-byte assertions remain unchanged. Its
existing second collection now waits for finalizers before the 128 warm frames. Ordinary portability consumers
supply no callbacks. The diagnostic workflow uploads focused logs before starting
the longer full run so native URI failures can be inspected independently.

## Prepared button diagnostics (2026-10-23)

Untraced focused macOS run 39943239574 at fa022b225 rejects a 6,304-byte owner
counter delta on both RIDs: en_US on x64 and automatic language on ARM64. The
regional-language fix and earlier successful runs do not explain this failure.
ButtonTests now brackets the same 64 changed-text/toggle/layout/recording cycles
with an optional native object trace. The four language roles, 64 warmups and
strict zero-byte owner assertion remain unchanged. The diagnostic workflow runs
this focused trace separately after native calibration, then keeps the ordinary
focused checks and complete diagnostic run with their original outcomes.

The completed x64 b055b6fc diagnostic records 20 general-cast and 16 dense-contact
intervals with 0/0 counters and no object events inside their exact tags. The
calibrated trace is retained with the matching managed modules. This does not
attribute the earlier untraced 64-byte deltas. That diagnostic instead stops in
a fresh terrain child after 30.354 seconds (exit 137, loading archive). ARM64's
same diagnostic reaches the later native URI failure. Neither is a complete
untraced platform pass.

The early x64 object trace in run 39945830574 catches the en-US failure: the
owner counter increases by 4,496 bytes. Seventeen recorded objects total 2,976
bytes on native thread 22795, while begin/end identify owner 22721. Matching
CoreLib metadata resolves their stacks to GC.RunFinalizers, Gen2GcCallback.Finalize,
SharedArrayPool.Trim, GC.GetGCMemoryInfo and PortableThreadPool.OnGen2GCCallback.
The trace records no object on the owner; these other-thread events do not by
themselves explain its counter delta. The four ARM64 traced intervals are zero.
The button fixture now uses the existing calibrated AllocationWarmupCheck.Collect
after all 64 warm frames and before measuring the unchanged 64 active frames.
This drains both finalization passes; it does not change the zero-byte assertion.
Target untraced execution remains required. The earlier two-RID 6,304-byte
failures remain retained separately.

## Prepared idle character interval (2026-10-23)

The untraced macOS x64 aggregate at ed0c3f4ef passes its focused GUI checks and
CPU/GPU CCD and exception checks, then fails the CharacterBody idle all-thread
allocation assertion. That assertion did not record its byte delta. The fixture
now uses the existing calibrated double collection/finalization preparation after
64 warm calls, records both counters over the same 64 measured MoveAndSlide calls,
and requires both to be zero. Optional native tracing uses a separate character
tag domain and closes on exceptions. Standalone CPU/GPU entry points let the
macOS workflow exercise the complete character suite before the full aggregate.
This preparation does not attribute the old failure; ordinary target acceptance
and any matching failing trace remain required.

## Shape-owner and joint-policy measurement preparation (2026-10-24)

The ordinary macOS x64 job `120766028047`, run `40056127660` at `5135f740d`,
fails its separate-thread CPU joint-policy interval at owner/all-thread
`2,256/18,808 B`. It retains 128 warmup and 128 edit-active measured frames.
The calibrated macOS ARM64 run `40063421412` instead aborts earlier in the CPU
`ShapeOwnerTests.VerifyLifetimeAndAllocation` interval at `5,816/13,216 B`.
That interval contains 64 complete measured frames after 120 settling frames
and 64 warmup frames. It had no native physical marker, so the process-wide
profile cannot attribute those particular bytes. Neither failure is accepted
or explained by a successful focused control.

Both fixtures now call the existing calibrated `AllocationWarmupCheck.Collect`
before their unchanged warmup loops. Both blocking collection/finalization
passes finish outside warmup and measurement. This isolates the counters from
the documented background context-retirement and pending-finalizer effects;
it does not identify the producers in either older failed interval. The same
physical work, numerical checks and strict zero owner/all-thread byte assertions
remain. GC stays enabled; no bytes are subtracted and no interval is retried.

The complete CPU/GPU shape-owner suite now opens the existing native recorder
in domain `10UL << 32`. Its 64-frame physical bracket closes on exceptions and
prints its backend, counters and tag before the assertion. The existing
owner/disable-contract selector exercises the complete CPU and GPU families;
the macOS diagnostic workflow retains both its profiled and ordinary logs and
the dedicated trace. Positive/empty controls still precede physical tracing.
These diagnostics do not replace complete ordinary target CI or attribute an
older interval that has no matching object/context evidence.
