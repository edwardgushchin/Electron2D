# Resident GPU contact publication

Last updated: 2026-10-21

## Boundary

GPUPhysicsBodyStore can opt into completed-tick contact reports without creating a
CPU solver world or downloading all manifolds. CaptureContactReports defaults to
false. ReadContactReports takes retained caller spans of body handles, per-body
limits and destinations. The [shared public GPU adapter](physics-backends.md) now uses these reports for direct-state contacts and queued scene/server callbacks. Portable snapshots consume this projection; full numerical, network/platform and performance acceptance remain open.

Reports carry observed shape/body indices and generations, world positions,
receiver-facing normal, world-axis impulse, final point velocities and greatest
observed depth. These local identities are not wire IDs or public RIDs. The public
adapter resolves them to retained common identities and logical shape indices,
applies contact-limit assignment invalidation and preserves the existing callback/event
sequence under [ADR 0070](../decisions/physics.md#adr-0070).

## Frame accumulation

Each solved interval contributes its applied normal and signed friction impulse,
including warm start and subsequent solver deltas. Their sum is the final physical
constraint accumulator; positional correction is excluded. The device converts
each interval's contribution to world axes before summing, so changing normals do
not reinterpret older tangential impulses. CCD intervals and ordinary substeps
share one outer frame. Separated transient contacts remain available until the
next frame; integration-only Step does not claim contact solving and clears reports.
Zero-time work preserves the preceding completed snapshot.

A generation/feature/piece-qualified hash table merges contacts across intervals.
The incoming-point chain also merges duplicate keys within one batch without
floating-point atomics. A separate phase resolves immutable old keys and incoming
point tokens; it does not spin waiting for another invocation to publish a record.
Each record belongs to both endpoint incidence lists. The last geometry and maximum
depth are retained. Final publication reverses each prepend list into encounter
order and samples point velocities from final poses/COM/actual-plus-surface motion.
Later body/geometry writes cannot alter those contact values.

Selection runs one invocation per requested receiver over its incident list.
It fills slots in encounter order, then replaces the first shallowest only for a
strictly greater depth. Ties retain the selected slot. Both endpoints report equal
and opposite impulses without accumulating the shared constraint twice. Sensor
points and joint rows are not solid contact reports. Speculative solid points
inside the current contact margin remain reportable even with zero impulse, as in
the public CPU path; tiny positive separation must not drop a sleeping contact. Body generation checks prevent
reuse from inheriting publication; newly added bodies outside the published range
return zero contacts. All body requests and limits (0 through 4095) validate before
submission.

The retained record population is independent of the read cap. Capping accumulation
would lose early contributions when a formerly shallow contact later becomes a
selected deep contact. Worlds without a reporting consumer skip every report pass
and allocation. Reporting worlds currently collect all eligible solid pairs once;
selective endpoint authoring is a potential measured optimization, not an API limit.

## Storage and synchronization

| Storage | Payload |
| --- | --- |
| Frame record | 128 device bytes per retained feature: identities, endpoint links, geometry, summed impulse, final velocities. |
| Hash table / incoming heads | Two 4-byte arrays at a retained power-of-two table capacity, at most half full. |
| Incoming links | 8 bytes per candidate point; scratch reused each interval. |
| Receiver heads | 4 bytes per retained body. |
| Selected request / row | 12 uploaded bytes (body, generation, cap) and 8 downloaded bytes (count, packed offset) per requested receiver. |
| Selected result | 80 bytes per conservative packed slot bound. Only returned row counts identify valid entries. |
| Status | 8 bytes uploaded/downloaded per interval, sharing the existing solver fence; final publication adds one 8-byte exchange and fence. |

Before each solver submission, capacity reserves the worst possible previous-frame
records plus this interval's points. Growth copies existing records device-to-device;
it never reruns the solver or truncates a candidate population. The completed count
comes back with status, without a CPU contact mirror. Every interval rebuilds the
hash from retained frame records. All capacities survive warmup.

The final publication fence makes frozen contact positions/velocities available to
post-solver consumers. Each explicit batched read adds one fence and transfers only
requested capped segments plus counts/status. Unused capacity within those segments
is not a valid contact; this bounded transfer avoids another count-discovery fence.
An unchanged sleeping frame retains contact geometry and clears applied impulses
without rerunning constraint solving. Invalid device publication fails the store;
there is no CPU replay.

Selection is O(receiver candidates × limit), the same rule as current direct-state
selection. A marked comment identifies a stable min-heap if large reporting limits
become a measured bottleneck. This implementation makes no cross-device promise for
parallel backend encounter order; cap selection within the published order is exact.

## Packed selection and one-completion readback (2026-10-21)

Selection first counts eligible incident records up to each requested cap, then reserves that exact row size with one device atomic. Its ordinary encounter-order/deepest-cap selection writes into that private packed row. Per-receiver field values and order are unchanged; global packed row placement is not public identity. The host validates the complete header and all count/offset ranges before scattering valid records into the original caller segments. Counts commit last, preserving aliased limit/count spans. Unused caller slots and destination tails remain untouched.

A frame record has at most two endpoint receivers. With R retained records, L the sum of requested limits and M the greatest multiplicity of one requested body, the download bound is B = min(L, 2 × R × M). Two retained Int32 arrays at body-slot capacity count that multiplicity by invocation epoch; they store no numerical body or manifold mirror. Epoch rollover clears the marker array without allocation; retirement releases these scratch references. The result buffer grows to B rather than L. Metadata costs 12 bytes uploaded and 8 bytes downloaded per receiver; the selected payload costs 80 × B bytes plus the eight-byte status header. Counters retain actual transfer sizes and retained device/transfer capacities.

This bound is available before submission. It preserves duplicate requests, arbitrary caps and receiver subsets without a second count-first GPU wait. Some bound padding can remain when only one endpoint or a subset is requested; reading exactly the final selected count would require another host dependency with the current copy API. Selection, metadata and result download share the existing single submission/completion. Numerical state and complete frame records remain resident. Invalid status/ranges retain terminal failure and do not publish caller output or replay on CPU.

The sparse controlled test rejects the former 8,340-byte read, preserves duplicate/zero-cap requests, aliased spans, all valid report fields and segment/tail boundaries, and requires 64 warmed reads at zero owner/all-thread managed bytes. Same-scene timings and the complete verification receipt are recorded after their final frozen runs below; earlier measurements retain their original revisions.

### Frozen monitored-scene pair

The candidate is based on main `61d04d474`, with runtime DLL SHA-256 `3463a7685e85f9a7bdec2c8e27db4ffcd03b5de97db61e7bad4466bb6f33d494`. CPU then GPU execute in one process with identical inputs: 16,384 awake scene rigid circles, four static walls, 144 Hz fixed steps, four substeps, sixteen sweeps, eight reported contacts, both entry/exit handlers and interpolation. There are 1,600 warmup and 256 measured ticks; DOTNET_TieredCompilation is zero and the NVIDIA Vulkan driver is explicit. Physical invariants, active population, contact identities/caps and strict zero owner/all-thread managed bytes pass independently for both backends. This pair measures the complete scene path without presentation.

| Backend | Complete step mean / p50 / p95 / p99 / max, ms | Managed owner / all threads, B |
| --- | --- | --- |
| CPU | 106.2166 / 105.4193 / 112.8819 / 119.1222 / 123.7379 | 0 / 0 |
| GPU | 66.4093 / 63.3225 / 81.0603 / 89.9739 / 93.7898 | 0 / 0 |

Within this same runtime, GPU mean/median improve 1.599x/1.665x over CPU. GPU upload/readback is 196888.000/8867639.375 bytes per tick; waits p50/p95/p99/max are 18.4171/35.1346/40.7779/46.6033 ms. Completion pose/contact/event means are 10.2848/8.6493/1.4036 ms.

The pre-change pair on `08182ca4` recorded GPU mean 66.6677 ms and 11863137.688 readback bytes/tick. Observed readback falls 25.25%; the complete-step time remains close. This is not evidence of a repeatable whole-step speedup from packing: the two checkpoints also include the intervening scene/resource commit, and shared-desktop scheduling remains uncontrolled. Each CPU/GPU comparison uses its own identical frozen runtime; no physical feature or contact receiver is removed. The raw sparse counter control separately rejects 8,340 bytes and passes at 352 bytes.

Artifacts are under `bin/physics-contact-selection/2026-10-21/`, including both paired JSON reports, process/library/telemetry receipts, source manifests and PDB proofs. The `performance/` payload retains the measured root and runtime. A subsequent test-only assertion additionally checks the aliased floor segment. Four archived source overrides plus pinned `61d04d474` reproduce the measured authored inputs; all performance modes retain that same payload. Final integration uses the later main source tree and stronger test assembly in `verified/`, separately from these timings. Broad platform, coupled/network workload, hardware timing and owner acceptance remain open.

### Frozen real-window pair

The identical performance payload also presents all 16,384 bodies in an actual 1152 × 800 Wayland/NVIDIA window, with the same 1,600 warmup and 256 measured fixed ticks. One physical tick accompanies each rendered frame; requested Disabled VSync resolves to the driver's reported Enabled mode. The observed FPS measures wall-time frame throughput, not the configured 144 Hz step duration.

| Backend | Complete physical step mean / p50 / p95 / p99 / max, ms | Frame p50 / p95 / p99 / max, ms | Actual FPS | Physical / whole-loop owner/all-thread managed bytes |
| --- | --- | --- | ---: | --- |
| CPU | 107.8433 / 107.0714 / 113.5717 / 119.0868 / 161.5311 | 175.7514 / 186.8863 / 192.4414 / 228.5684 | 5.6504 | 0/0 / 0/0 |
| GPU | 88.4106 / 87.5035 / 99.1462 / 112.4714 / 150.9721 | 150.6275 / 165.9835 / 180.7269 / 217.5267 | 6.5693 | 0/0 / 0/0 |

GPU window throughput improves 1.163x in this pair. Both physical quality/cap/identity/population checks pass. This scene remains far below 144 ticks per wall second; the same renderer and its host publication remain substantial frame costs. CPU/GPU captures are retained and were visually inspected for the populated, contained piles, separately from counting the population through actual physics counters.

This is explicitly the frozen `61d04d474` candidate. During the run, live main advanced to an unrelated window-input commit. The monitor's previously loaded live-source assertion rejected that changed working tree after both processes had succeeded; `frozen-validation.json` independently verifies every original input from the pinned commit/four archived overrides and every unchanged binary. Its accepted scope is that immutable performance checkpoint, not later live main. Current integration tests use the later source tree separately.

### Scoped native request profile

A separate run uses the identical frozen GPU performance payload and 16,384-body scene, 1,600 warmup ticks and 64 measured ticks under the calibrated Linux glibc interposer. Its startup calibration requires exactly four allocations, 131 requested bytes, three frees and one reallocation. During complete PhysicsFrame intervals across all process threads it records 16,697 successful allocation requests, 68,462,968 requested bytes, 16,703 frees, 0 reallocations and 0 failures. This averages 260.891 requests and 1069733.875 requested bytes per measured tick. Physical quality and 0/0 owner/all-thread managed-byte gates pass.

These are scoped libc request counts, not retained heap, driver-private allocation totals, mapped/device memory or net memory growth. Frees may retire earlier requests. Interposed timings are excluded from the ordinary latency/FPS comparisons. The probe path/hash, calibration, environment, payload, source proof and hardware receipt are retained under `candidate-native-allocations-16384/`.

### Current integration checkpoint

Source-built integration uses main `85fbef258` plus this change, separately from the frozen performance payload. PDB document checks match every expected authored input: 2,427 runtime, 921 root-test and 262 public-consumer sources, with no missing, extra or mismatched hashes. Runtime DLL/PDB SHA-256 values are `82b5ce9f213b2e1248230a56ff1156c1993df160862ffb2bd615f55a64507adf` / `2a928caff26f5df0923687dacab4a7af19fa5b2b9141f715579e58f8b6c3e17a`; root-test DLL/PDB values are `b2ff430cb0652aa65450fb8b15a24ddcd5c838ef749f107337b0bb5b1d707725` / `108b525322ca4039b87646710acfcac13026d5f987e34154689f33f80feb523b`.

The stronger aliased-floor assertion, sparse packing control and complete resident/public GPU contact selector pass on this payload. Strict Linux x64 NativeAOT publication treats trim/AOT warnings as errors; five profiles pass: CPU contacts with unavailable GPU, GPU contacts on owner/separate numerical threads, and portable GPU contacts on both numerical hosts. All measured warmed contact/snapshot intervals retain the strict zero owner/all-thread managed-byte gates. Runtime/root/consumer format verification, Release runtime build, compiled API coverage, wiki tests, generation and generation-check all pass locally.

An earlier ordinary root run at `9a90dd665` aborted after 630.642 seconds in the unchanged malformed-scene fixture: it expected archive version 6 although current main already writes version 7. The fixture now expects 7, preserving its actual corrupt-field/role/reference rejection cases; its isolated managed profile passes in 3.749 seconds. A concurrent main update interrupted the first rebuild's source enumeration; rebuilding from the complete current tree produced the exact PDB proof above. Original failure/build receipts remain under the artifact root.

The current ordinary root run aborts after 469.079 seconds in GPU/separate-thread PhysicsScriptFactoryTests with 0 owner / 64 all-thread managed bytes. CPU owner/separate and GPU owner variants passed immediately before it. That fixture has one moving body and no contact partner, so it bypasses packed contact selection. Its isolated identical GPU/separate profile passes in 1.483 seconds with 128 full frames/queries at 0/0 bytes. This does not attribute or fix the earlier 64 bytes: complete-suite allocation acceptance remains open, with the original failed interval preserved in `integrated-root/` and the isolated receipt in `integrated-factory-threaded/`. No threshold, assertion or physical workload is weakened.

### Script-factory allocation investigation

The failed 0/64-byte interval above remains open. Diagnostic runs preserve the
original runtime and test binaries, whose hashes are listed in the integration
checkpoint, at committed source checkpoint `b3eb6d426`. No runtime behavior,
allocation threshold, frame count or query workload changes in this investigation.

A private CoreCLR object-allocation recorder is calibrated against one escaping
64-byte object, two 32-byte objects, and one 64-byte object on a dedicated worker.
It distinguishes the worker's 0 owner / 64 all-thread counter delta and resolves
the allocating thread and managed stack. The final recorder stores at most 4,096
off-owner objects of at most 64 bytes in preallocated native storage, then resolves
metadata after the region. Calibration and the exercised factory traces have no
overflow, incomplete record, stack error or recursive callback. Its library/source
SHA-256 values are `1a751ee900424306cb341737b441365b97f35ea055abb13f59bb76e7c5b6042f`
and `1f5db6dd7682ca28f95139770245291468bf73f103431b6e65e19ccf8dac37a3`.

| Diagnostic | Observed result | Boundary |
| --- | --- | --- |
| Five fresh unmodified script/factory sequences | All 30 cases pass | CPU/GPU/registered, owner/separate hosts; 128 full frames and queries per measured case, strict 0/0 bytes. |
| Full root suite with native interval markers | Process passes in 668.439 s; no allocation events in any of six measured factory intervals | Diagnostic test assembly only: 921 pinned authored inputs plus the marker helper, with 922 exact PDB source hashes. Original runtime retained. |
| Full root suite with unmodified binaries and JIT-triggered recording | Passes in 598.740 s; all six factory intervals 0/0 bytes | Captures setup, warmup and measurement together. Profiler timings are diagnostic, not performance comparisons. |
| Full root suite with unmodified binaries and deferred metadata recording | Process passes in 658.731 s; all six factory intervals 0/0 bytes | 1,473 small off-owner objects across setup, warmup and measurement; 2,577,114 raw trace bytes. Those records cannot be attributed to a measured interval. |
| Prepared-worker/ambient-context sequence | Passes in 4.236 s; all six factory intervals 0/0 bytes | Adds the existing worker-pool suite before scripts; this is an additional stress condition, not the default root order. |

Both full suites with marker/deferred recording finish with unchanged payload
hashes but their live-source guards reject concurrent unrelated main edits. Their
immutable input manifests qualify the frozen diagnostic checkpoint only; neither
run certifies later main. The unmodified eager/deferred prefix profiles also pass.
In total, 12 diagnostic profiles retain 72 successful factory cases; the earlier
failed interval is retained separately and is not replaced by these passes.

Cold/setup traces contain 64-byte solver-buffer delegates and allocator objects;
one prefix also records a 64-byte string on the socket finalizer. No trace places
these in the original failed interval. A separate forced-finalization control
also includes runtime GC callbacks and larger totals, so it does not identify the
original 64 bytes. There is no causal fix or complete-suite allocation acceptance.
The remaining requirement is to capture a failing measured interval and establish
its source before changing the runtime or fixture.

Local evidence is under `bin/physics-script-allocation/2026-10-21/`: calibrated
native source/library, original and instrumented payloads, frozen sources, PDB
proofs, per-process traces, hardware maps and receipts. Run its `validate.py` to
check 12 profile receipts, input hashes, six factory cases per profile, trace
integrity and the retained original failure. `validation.json` states the open
gate explicitly. The large eager prefix trace and decoded JSON are losslessly
compressed; `trace-compression.json` retains original/decompressed hashes.

### Separate network processes on the integration payload

Two headless 900-tick impaired-delivery checks pass on the same current payload, with owner/separate numerical hosts. Each launches an unavailable-GPU CPU authority, an NVIDIA resident-GPU client and a late unavailable-GPU CPU client. All three processes converge to the authority's 30-object final topology, identities, control epochs, poses, velocities and sleep states under the existing 0.002 final-correction tolerance. Late clients join at ticks 329/327 respectively after the tick-310 admission gate. The existing checks retain all three joint families, contacts, crossed-capsule/rotating-ray corrections, lifecycle, ownership, event confirmation, prediction/replay, and positive warmed sample counts; step/replay owner/all-thread managed bytes are zero in every report.

The owner attempt overlapped the ordinary suite and timed out after 143.049 seconds at authority tick 408. It is a failed receipt. The same binary/inputs pass separately in 37.685 seconds; the separate-thread profile passes in 37.534 seconds after the ordinary process exits. These are functional process checks, not controlled timing comparisons. Reports/logs, hardware maps and immutable payload/source hashes are retained under `integrated-network-owner-isolated/` and `integrated-network-threaded/`; the failed overlap is retained under `integrated-network-owner/`. Broad platform, owner and complete-suite acceptance remain open.

## Verification

The dedicated entry point is `ELECTRON2D_TEST_GPU_REPORTS=1`; it runs
[GPUPhysicsReportTests](../../tests/Electron2D.Tests/GPUPhysicsReportTests.cs) and
public GPU PhysicsContactImpulseTests. The complete GPU runner includes
the report suite; the ordinary root runner checks public CPU/GPU contact impulses.
The earlier report below records its own executed checks and limits separately.

## Numerical regression found by publication tests

A four-substep CCD collision of masses 2 and 3, initially moving at +600/-600,
exposed a real pre-existing warm-start problem: a remaining interval of
0.0000010840595 s was followed by 0.025 s. Scaling the cached impact by 23061.465
created a large velocity followed by cancellation. The second body's final X
velocity became 360.67355 instead of approximately 360; published impulse 2880
correctly disagreed with the erroneous velocity-derived 2882.0208.

Contact warm-start scale is now min(1, newDuration/previousDuration). The same
unbounded scale existed in joint warm starts. A pin connecting unequal masses,
with initial +600/-600.7 velocities, reproduced drift from -120.41998 to -120.125
when duration grew from 0.000001 to 0.025 s. Joint reuse now uses the same bounded
scale. Shorter intervals still scale down; larger intervals obtain any additional
impulse through fresh solver iteration. Reporting tolerances were not widened to
hide either defect.

An initial test also incorrectly demanded identical one-tick CPU/GPU impact
velocities. The public CPU solver produced (-207.46976, -396.24414) impulse while
the independent solver produced (-240, -360), each matching its own body's momentum
change. That ungrounded cross-solver equality was replaced by the required shared
momentum and impulse-sign invariant; no production code changed for that assertion.

## Measured publication cost

Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti. The same retained world has
1,024 active mass-2, rotation-locked 2×2 boxes on 1,024 static 2×2 floors, placed
10 units apart. Gravity is (0,10); automatic sleep is disabled. Each tick is 1/120 s,
four substeps and four iterations. Each variant warms 96 ticks and measures 64.
The reported variant reads every dynamic body at limit 4 in one batch. All 1,024
bodies are checked for two retained points and summed support impulse (0,-20/120).
The population and physical features are unchanged when capture is disabled.

| Operation | p50 | p95 | p99 | Buffer upload / download |
| --- | ---: | ---: | ---: | ---: |
| Simulation, capture disabled | 1.8175 ms | 2.4252 ms | 3.5890 ms | 160 / 160 B |
| Simulation including publication | 1.9283 ms | 2.4894 ms | 4.1520 ms | 200 / 200 B |
| Explicit 1,024-body report read | 0.0980 ms | 0.1064 ms | 0.1098 ms | 16,392 / 331,784 B |

The disabled variant's mean fence wait is 1.1465 ms. Enabled simulation plus read
waits average 1.2613 ms, of which the read is 0.0658 ms. Four interval status exchanges
share existing solver fences; final publication adds the remaining exchange/fence.
The read downloads 4,096 reserved 80-byte slots, 1,024 four-byte counts and eight
status bytes. Only 2,048 returned contacts are valid; spare capped slots avoid a
separate count-discovery readback/fence. This cost is attributed to explicit public
contact consumers, not mandatory full-world state mirroring.

Warmed owner-thread allocation is zero for both simulation variants and the batched
read. Report-specific retained device payload is 962,568 bytes; upload/download
transfer capacity is 557,056 bytes. Driver bookkeeping/native allocations are not
measured. These measurements compare report overhead on one GPU implementation;
they are not whole CPU/GPU acceptance, window FPS or network snapshot/replay cost.

The first population fixture used a 6-unit pitch. Its upper floors were inside the
2-unit speculative contact margin, so the assertion of exactly two points was
incorrect (receiver 352 also observed floor 640). Spacing was corrected before
both baseline and enabled measurements. No population or reportable feature was
removed from one side of the measured comparison.

### Assertions and limits

GPU impact momentum checks allow 0.02 mass·scene-unit/s; the same invariant through
public CPU direct state allows 0.08. CCD checks allow 0.03 against each body's
momentum and 0.002 for equal/opposite receiver sums. The rotating-normal case allows
0.04 for accumulated angular/linear float arithmetic. These bounds are small
relative to the tested impulses (hundreds to thousands), and caught the 2.02-unit
CCD regression. They do not require equal one-tick solver trajectories.

Pin interval-change velocity tolerance is 0.003 scene-unit/s after 64 iterations;
point velocities allow 0.001. The four-iteration steady support case allows 0.003
mass·scene-unit/s per tick (expected 1/6). Counts, identities, cap selection/ties,
zero impulses and snapshot retention are exact where asserted. Tests additionally
cover 1/4/8 substeps, full-shape CCD separation, zero-time work, reporting enable in
an inactive world, sensors, filtering, wrong-thread/foreign/stale/disposed access,
invalid limits/spans and post-publication body growth/reuse. Public CPU proximity
checks retain zero-impulse speculative geometry at a 1-unit gap and reject a
3-unit gap under the current 2-unit contact margin.

The dedicated GPU/public-CPU contact suite and complete GPU suite passed. The
complete run covers prior independent body/geometry/contact/joint/field/sleep/CCD
and filtering/lifetime suites, as well as the existing stage-hosted prototype and
both renderer/device-lifetime checks. No real-window, networking, foreign-GPU,
native-allocation or public independent-GPU acceptance is claimed.

### Final local gates

Release runtime/test builds use `-p:Electron2DBuildNativeFromSource=true` and pass
with zero warnings/errors. Runtime formatting (excluding `src/Vendor`) and formatting
of the three changed test files pass verification. `tools/coverage/check.sh`,
`python3 -B tools/shaders/check_runtime.py`, `python3 -B tools/wiki/test_generate.py`
and local wiki generation/check pass. Public declarations and coverage states are
unchanged; generated wiki/SPIR-V output is excluded from the engine commit.

Final Release SPIR-V SHA-256:

| Artifact | SHA-256 |
| --- | --- |
| PhysicsResidentReports.comp.spv | `50e0f6562ed396a50328e22575674a77c454f6539eb0c8c00834ae00c1391f62` |
| PhysicsResidentJoints.comp.spv | `09cf14036fb3b8d957088f121b683f5dafa2ed6f775fe63d79ef141558d33208` |

[Report-only contact verification](physics-report-only.md) now covers nonresponding kinematic/static and kinematic/kinematic pairs, current receiver caps, identity, lifecycle and restoration on both public backends. Existing contact storage and publication are reused; no CPU body-state mirror is added to GPU physics.
