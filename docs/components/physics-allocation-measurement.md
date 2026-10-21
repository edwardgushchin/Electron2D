# Physics allocation measurement

Last updated: 2026-10-21

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
