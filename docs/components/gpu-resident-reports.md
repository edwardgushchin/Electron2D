# Resident GPU contact publication

Last updated: 2026-10-27

## Boundary

GPUPhysicsBodyStore can opt into completed-tick contact reports without creating a
CPU solver world or downloading all manifolds. CaptureContactReports defaults to
false. ReadContactReports takes retained caller spans of body handles, per-body
limits and destinations. The [shared public GPU adapter](physics-backends.md) now uses these reports for direct-state contacts and queued scene/server callbacks. Portable snapshots consume this projection; full numerical, network/platform and performance acceptance remain open.

Selected reports carry observed shape indices and generations, world positions,
receiver-facing normal, world-axis impulse, final point velocities and greatest
observed depth. These local identities are not wire IDs or public RIDs. The public
adapter resolves them to retained common identities and logical shape indices,
applies contact-limit assignment invalidation and preserves the existing callback/event
sequence under [ADR 0070](../decisions/physics.md#adr-0070).

## Packed selected result layout

Each selected result uses a 68-byte scalar wire record: four shape index/generation
words, depth, both positions, receiver-facing normal, complete world-axis impulse
and both final point velocities. The preceding record used 80 bytes, including
redundant collider body index/generation and alignment padding. Public projection
already resolves both bodies through the retained shape-owner table and checks
both shape generations before creating immutable contacts. Removed or reused
shape identities therefore retain the same rejection behavior.

The device's 128-byte accumulated FrameContact, incidence/history, cap selection,
solver and public PhysicsBodyContact stay under their existing contracts. Only
the selected transfer/result buffer layout changes. C# uses explicit four-byte
packing and the shader uses scalar members; tests check the exact stride and
numeric field offsets. No numeric precision, body, contact, field, iteration or
wait is omitted. Readback/capacity accounting continues to use the actual record
size, and the existing snapshot copy uses the same typed host record.

The selected-result counter isolates requested buffer payload bytes from body
state, request/count/status traffic. These are SDL download sizes, not measured
PCIe bus transactions. At 16,384 bodies the headless GPU trial reads 6,183,988
selected-result bytes per tick; those same record slots require 7,275,280 bytes
at the preceding stride, a reduction of **1,091,292 bytes per tick**. The 128-byte
device history and all public values retain their original precision.

### Packed-layout qualification (2026-10-27)

The [validation manifest](../../bin/physics-mass-refresh/2026-10-27/validation.json)
accepts 28 complete profiles: 22 functional profiles and six complete massive
scene/window measurements. Functional coverage includes the resident report and
dense joint-degree suites; public CPU/GPU contact frames and populated/empty
checkpoint projection in owner/separate numerical modes; and portable CPU-to-GPU
contact snapshots. Twelve CLR and ten strict full-trim NativeAOT functional
profiles pass. Existing momentum, transient/CCD, depth/cap, generation/reuse,
point-velocity, snapshot, error and strict warm-allocation checks remain active.

Portable PDB/source checks match 2,677 runtime, 1,004 root-test and 292 public
consumer authored files. The baseline is verified against committed 17b240f89;
the candidate and actual Native compiler inputs match the changed sources. Of
231 CLR-embedded shader modules, only PhysicsResidentReports changes. Its exact
validated 68-byte module is present in the CLR assembly and Native executable;
all 17 scalar member offsets match the generated SPIR-V layout.

Every massive trial uses 16,384 awake bodies plus four walls, the same authored
scene/materials/forces, 144 Hz fixed ticks, four substeps, 16 solver iterations,
contact monitoring with cap eight, interpolation, 1,600 warmup and 128 measured
frames. Backend fallback is disabled. CPU headless processes have no usable
GPU ICD or loaded hardware driver. GPU profiles use NVIDIA without software
Vulkan. The two windows use the same GPU renderer and actual native event pump,
scene pose publication, processing and presentation. Requested disabled VSync
reports **Enabled** on this Wayland host; the table records actual window FPS.

| Frozen profile | Complete physics mean / p95, ms | Window FPS | Physics GPU readback, bytes/tick |
| --- | ---: | ---: | ---: |
| Before: CPU headless | 109.913 / 117.888 | — | 0.00 |
| Before: GPU headless | 261.762 / 388.149 | — | 8,530,078.25 |
| Packed: CPU headless | 115.052 / 130.123 | — | 0.00 |
| Packed: GPU headless | 106.761 / 115.409 | — | 7,364,704.25 |
| Packed: CPU physics + GPU window | 120.318 / 130.093 | 4.912 | 0.00 |
| Packed: GPU physics + GPU window | 148.811 / 158.893 | 4.313 | 7,404,640.94 |

All six physics brackets require exactly zero owner/all-thread managed bytes.
Both windows allocate 47,104 managed bytes in injected native input pumping over
128 frames; process/publication/render lanes remain zero. Full-window allocation
acceptance is therefore open. Captured CPU and GPU frames are retained and
inspected; the rendered body count is unchanged.

These are single fresh-process trials under uncontrolled desktop load. Even the
unchanged preparation phase moves from 16.547 to 6.331 ms between GPU trials;
the wall-time difference cannot be attributed to packing. The window GPU trial
is slower than CPU, so the goal of a stable complete-path GPU advantage remains
open. Its solver/wait phase is 75.836 ms versus 37.705 ms headless; queue,
presentation and competing-load costs need further investigation. Readback
reduction does not close that requirement. Native allocator traffic, device
hardware timestamps, broader network/platform and owner acceptance remain open.
API declaration coverage stays **1358/1369 (99.20%)**, with 11 open declarations.
Earlier dated measurements below retain their original 80-byte result layout,
source identity and verification scope.

## Final publication in the numerical command (2026-10-25)

Completed contact records now freeze their final point velocities and reverse
receiver incidence lists in the last numerical command. This runs after the final
position/sleep stages, or after a solve-only interval. No host report count is
needed between the last accumulation pass and finalization: the existing device
summary supplies the actual count, validated against retained record capacity.
Both numerical and report status must succeed before the snapshot becomes ready.
The existing six readonly/eight writable report bindings, buffers and fields stay
unchanged; solver constraints, iteration counts and impulse accumulation remain
unchanged.

A final solver shares its already-recorded report status, removing a second reset
and download. A final position command appends the existing report reset/scan/read
and shares its completion. Idle sleeping publication retains its separate path
that clears old impulses. Zero-time calls still preserve the completed snapshot.
Internal `SeparateReportSubmissions` keeps the same finalization as a control.
An empty final solver batch still records the body-head scan; fewer completions
do not by themselves establish a complete-step throughput improvement.

[GPUPhysicsReportBatchTests](../../tests/Electron2D.Tests/GPUPhysicsReportBatchTests.cs)
warms each control for 128 cycles, then measures 64 complete author/solve/state/
contact-read cycles. All rows retain strict zero owner/all-thread managed bytes:

| Numerical path | Shared/separate submissions | Shared/separate upload bytes | Shared/separate readback bytes |
| --- | ---: | ---: | ---: |
| Fused integration | 320 / 384 | 18,688 / 19,200 | 29,184 / 29,696 |
| Separate integration | 1,344 / 1,408 | 30,976 / 30,976 | 66,048 / 66,048 |
| CCD | 1,408 / 1,472 | 35,584 / 35,584 | 70,656 / 70,656 |
| Solve-only | 320 / 384 | 17,664 / 18,176 | 27,648 / 28,160 |

The first frame also verifies newly generated final-interval records before a
previous host count exists. Every measured cycle checks contact identities, final
point velocities within .001 scene units/second and full normal/friction impulse
against momentum change within .02 scene momentum units. These tolerances allow
float integration/accumulation error without accepting missing impulse channels.
The source-qualified RED has identical 384-completion controls and fails the new
shared-completion assertion after its physical/zero-byte checks pass. Raw
manifests, source/resource maps and receipts remain in
`bin/physics-report-completion/2026-10-25/`. API coverage states are unchanged.

The fresh Linux x64/NVIDIA cohort passes the complete resident/public report
suite, combined state/contact reads and common world checkpoints. Four public
CLR and four strict full-trim NativeAOT `--contact-frames` profiles cover CPU/GPU
with owner/separate numerical threads. Each retains 128 contact cycles and 768
cap/depth frames at zero owner/all-thread managed bytes. Native process maps have
no CoreCLR, GPU maps select NVIDIA without software Vulkan, and the exact report
module is present in the native executable. Both builds match 2,592 runtime and
270 consumer authored inputs and all 201 embedded resources. Pinned SPIR-V and
runtime shader/interface checks pass.

Separate-process network checks pass 600 paced and 900 unpaced ticks with CPU
authority, GPU prediction and a late CPU client. CPU participants verify denied
GPU startup; all participants finish with 39 matching object identities and zero
warmed step/replay allocations. Cold GPU replay remains 2,336 managed bytes before
warm qualification. Packet impairment, correction/replay, ownership and event
confirmation remain active. These functional/source checks do not establish
isolated complete CPU/GPU throughput, massive-window FPS, native allocator cost,
foreign-platform execution or human acceptance.

## Shared completion for body state and contacts

The world completion records configured contact selection in the same command as
the selected body-state gather. Selected state and contact data share one
submission and fence. Changed-body publication retains its separate contact read
and its existing dense/sparse transfer policy. Transfer bounds, report fields,
per-receiver order and cap policy remain unchanged.

`GPUPhysicsBodyStore.ContactRead` carries caller spans on the stack for that
synchronous invocation. Validation and capacity preparation precede recording;
completed metadata is checked before scattering rows, with counts committed last
for alias safety. No asynchronous request lifetime, host numerical mirror or CPU
replay is introduced. Empty contact demand records no extra device work; empty
state demand still services actual contact receivers. Publication errors keep the
existing failed-store policy. Public callback delivery follows both projections.

`ELECTRON2D_TEST_GPU_COMPLETION=1` includes selected/duplicate state, duplicate/aliased report requests, exact fields/caps and
untouched tails, invalid/empty requests and 128 warmed combined reads with strict
zero owner/all-thread managed-byte checks. Full physical/window and platform
acceptance remain separate from this bounded contract.

The selected-read implementation is checked through eight root profiles: combined completion, changed
publication, scene contact workers on owner/separate physics threads, GPU private
checkpoints, report-only bodies/replay, CPU with denied GPU drivers and CPU contact
workers. Four public `--contact-frames` profiles pass in CLR and four in strict
full-trim NativeAOT: CPU/GPU, each with owner/separate physics threads. These use a
mixed scene/raw world, transient peers, signed impulse totals, cap/depth/ties,
private restore and 128 strict zero owner/all-thread managed-byte cycles per run.
Native process maps contain no CoreCLR; GPU maps select NVIDIA and exclude software
Vulkan. The final runtime has the same 2,484 authored source checksums in both builds.
The NativeAOT consumer has 267 verified authored source documents. Native allocator,
foreign-platform and human/window acceptance remain open.

## Selected-read result, final runtime

The final CLR control restores one completed checkpoint before every read and
validates all state/contact bytes afterward. Restore and validation are outside
read latency and inside the strict managed-allocation bracket. Four blocks in
serial/combined/combined/serial order use 32 warmup and 64 measured reads each,
with 16,384 receivers and identical eight-row caps.

| Block | Mean ms | p50 | p95 | p99 | Submissions |
| --- | ---: | ---: | ---: | ---: | ---: |
| Serial 1 | 1.0874 | 1.0636 | 1.2082 | 1.2952 | 2 |
| Combined 2 | 1.0687 | 1.0257 | 1.2674 | 1.4906 | 1 |
| Combined 3 | 1.0452 | 1.0254 | 1.1236 | 1.1472 | 1 |
| Serial 4 | 1.1524 | 1.1437 | 1.2632 | 1.4133 | 2 |

Mean read latency is 1.11990 to 1.05695 ms, a 5.62% reduction in this bounded control. Every read uploads 458,768 bytes and downloads 3,801,104 bytes; one completed submission replaces two. All 256 measured reads and their surrounding restores/byte checks allocate 0/0 owner/all-thread managed bytes. Tail latency does not improve uniformly across blocks. These are selected-read measurements; final whole-frame GPU throughput and window FPS remain open.

Final runtime SHA-256: `766f4f4ed501245c50afebcb123c3aa521e74a0d750c621b83f3aeb0cbc43431`.
Strict NativeAOT public consumer SHA-256: `bacae0cbb7552addaaf33eefdace4cb962e7940f449334bfb115555f55afc349`.

## Coalescing trial, 2026-10-21

The first candidate combined contacts with both changed and selected body reads.
The following frozen CLR cohort used fresh GPU-only processes in A/B/B/A order,
then a separate CPU-only process. A is the original implementation; B is that
candidate, not the final selected-only runtime. Every run retained 16,384 scene
bodies, four walls, 144 Hz, four substeps, 16 sweeps, eight monitored contacts,
interpolation, disabled sleep, 1,600 warmup ticks and 256 measured complete
headless frames. All 1,280 measured ticks passed physical and strict 0/0 managed
allocation gates. This is not window FPS.

| Process | Mean ms | p50 | p95 | p99 | Submissions/tick |
| --- | ---: | ---: | ---: | ---: | ---: |
| GPU A1 | 103.0303 | 102.6135 | 112.2750 | 124.1659 | 19.1016 |
| GPU B1 | 92.6962 | 92.1744 | 96.6802 | 100.1326 | 18.1484 |
| GPU B2 | 90.5160 | 90.1391 | 97.1292 | 102.5634 | 18.0898 |
| GPU A2 | 96.5352 | 96.3193 | 101.6481 | 105.1743 | 19.0273 |
| CPU control | 102.4731 | 102.0798 | 107.8280 | 109.5178 | 0.0000 |

Final states, contact hashes and event counts differ across the full physical
runs. Phase differences also include solver/preparation work outside the changed
completion path. These observations do not isolate a causal whole-frame gain.
A fixed-state control therefore compares serial and combined reads on one store,
restoring the same completed checkpoint for dense changes. It validates all
16,384 snapshots, generations, contact bytes/caps and unchanged destination slots.
Each path uses 32 warmup reads and A/B/B/A blocks of 64 measured reads. It performs
no numerical replay during the timed read; restore and byte validation are outside
the timing but inside the strict allocation bracket.

| Fixed-state path | Serial mean ms | Combined mean ms | Change | Upload/readback bytes |
| --- | ---: | ---: | ---: | --- |
| Dense changes | 0.9321 | 0.9743 | +4.54% | 196,624 / 4,063,248 |
| Selected states | 1.0562 | 0.9930 | -5.98% | 458,768 / 3,801,104 |

Each combined read reduces two submissions to one with identical transfer bytes
and 0/0 managed bytes. Dense changed publication regresses in this control, so
its coalescing code and parameter were removed. Production keeps the original
changed-state stream and batches contacts only with selected body snapshots.
The full-scene B numbers above remain evidence for the rejected broader trial;
they are not performance acceptance for the final implementation.

Artifacts are under `bin/physics-completion/2026-10-21/`: the frozen `baseline`,
`combined` trial, `selected` final payloads, source/PDB checksums, complete process
maps, NVIDIA telemetry, kernel checks and validators. `validation-trial.json`
records the broader cohort; `validation.json` records final selected-only checks.
No new Xid was recorded in the trial; observed maximum GPU temperature/power was
41 C/137.05 W. This does not verify the unchecked power cable or hardware stability.
Native allocation, remaining inherited/API/network conformance, real-window FPS,
foreign platforms and full-physics performance acceptance remain open.

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

## Cold-view pre-step comparison (2026-10-21)

The [cold-only view pass](physics-backends.md#cold-body-view-preparation) removes duplicate owner preparation for a current cached direct-body view. Cold factories still observe their prepared owner, and the normal pass still prepares every body before solving. No numerical feature, solver work or public signature is removed.

The full `PhysicsPipelinePerformance` scene is unchanged: 16,384 awake rigid circles, four walls, 144 Hz fixed ticks, four substeps, sixteen solver sweeps, eight reported contacts, entry/exit handlers and interpolation. Each frozen CLR payload runs CPU then GPU with 1600 warmup and 256 measured ticks. This is the complete manually driven SceneTree physical path without presentation, on Linux/NVIDIA RTX 3090 Ti; it does not measure window FPS. DOTNET_TieredCompilation is zero, with ordinary GC and strict owner/all-thread allocation counters.

| Payload / backend | Complete step mean / p50 / p95 / p99, ms | Initial preparation mean, ms | Managed owner / all threads, B |
| --- | --- | ---: | ---: |
| baseline / CPU | 107.9377 / 107.3403 / 116.4111 / 119.5939 | 6.8891 | 0 / 0 |
| baseline / GPU | 64.3592 / 61.2803 / 76.8683 / 79.1472 | 3.2740 | 0 / 0 |
| candidate / CPU | 102.6460 / 102.0244 / 107.9041 / 110.6472 | 4.2838 | 0 / 0 |
| candidate / GPU | 64.9453 / 62.8860 / 78.0618 / 81.9921 | 2.4878 | 0 / 0 |

CPU mean complete-step time falls 4.90% in this ordered desktop trial. The initial preparation phase falls from 6.8891 to 4.2838 ms on CPU and 3.2740 to 2.4878 ms on GPU. That phase includes view acquisition and ordinary body/Area/joint preparation; it is not an isolated method timer. GPU complete-step mean instead rises from 64.3592 to 64.9453 ms, so this change establishes no whole-GPU speedup. The candidate's CPU/GPU mean comparison still shows a 1.581x GPU advantage on this specific scene. A single ordered trial does not establish repeatable latency changes or their full cause.

All four physical and strict zero-managed-allocation gates pass. CPU final penetration/escape are .297867/.299927 scene units on both payloads; GPU candidate penetration/escape are .083925/.024048, within the unchanged configured bound. All bodies remain active and scene/direct-state poses, velocities, contact identities/caps and bounded energy are checked. Candidate GPU upload/readback is 196888/8744572.5 bytes per tick. Numerical solving and completion/publication remain the larger GPU phases; the initial-preparation saving does not replace that optimization work.

The baseline authored inputs match `cd659def25e19ff08eabc991f3086d0289ddf352`; the candidate changes only two runtime files and the public factory-view test among 4130 captured authored inputs. Portable-PDB proofs match 2453 runtime, 942 root and 267 consumer source documents. Baseline runtime SHA-256 `afe1b06110bdb3cc89b87de13fbb1f41f3cc902f10f5117a98be09bc9674b685`; candidate runtime `0fba20830018e52bb8086d58fca6a423ae6726cb3c2a706f1f8e4df3e82f3d66`; candidate root `610d60189c57667d490b73947a72caa90d43f9c3d51541a951e01fa13f765c0e`. Evidence is local ignored `bin/physics-publication-profile/2026-10-21/`, including baseline/candidate reports, cohort telemetry/maps/kernel receipts, `source-binding.json` and frozen payloads.

Six focused CLR and twelve strict NativeAOT profiles pass, covering factory/view lifetimes, mode preparation and CPU/GPU native drawing. Release, compiled coverage, format and wiki checks pass. The ordinary aggregate runner nevertheless exits -6 after 299.359 seconds at the strict allocation assertion in PhysicsPointContainmentTests. The preceding output identifies GPU/raw/clockwise=true/offset=false/separate=false; the original assertion records no numeric counter deltas. Payloads and sources are unchanged and no new Xid is observed. This is a failed whole-suite gate, not a completed regression.

A fixed instrumented baseline/candidate control runs all 32 CPU/GPU point-query cases once per payload with the calibrated object profiler. Each body has no direct-state view before and after measurement; static receivers with no callbacks/contact demand do not enter the changed view-preparation path. Both controls report 0/0 bytes over 2048 measured frames, no object events in the 32 marked measurement windows, and 128 positive controls at 64 counter bytes/raw object size 61. No recursive profiler events are recorded. These fresh controls do not attribute or fix the original aggregate failure. Original evidence is `full-root/`; paired diagnostic evidence is `point-controls.json`, the probe source, marked traces and decoded events under the same artifact root.

No declaration is promoted: physics coverage remains 1306/1369 (95.40%), with 63 open rows. Historical unattributed allocation failures, coupled/network and foreign-platform acceptance, native/driver allocator totals and complete target-scene performance remain separate gates. The RTX 3090 Ti/Cougar GX-800 power cable is still user-confirmed unchecked; successful monitored trials do not resolve that hardware uncertainty.

## Scene transform publication gate (2026-10-21)

CanvasItem retains its original per-item object gate for transform-cache publication and canvas identity. A System.Threading.Lock trial preserved the four lock scopes: cache publication, invalidation, identity acquisition and retirement. Virtual transform getters and notifications remained outside the gate; owner/current-group guards, volatile revision checks, physical preparation, callback order and interpolation were unchanged. The trial is rejected below. Final source XML and class documentation clarify the existing reentrancy/getter boundary; no runtime behavior changes.

A fifteen-second EventPipe selection during the initial frozen 16,384-body GPU scene warmup records 69,716 samples with no missing stacks. That payload precedes the independent unary-shader integration and binds 2475 runtime documents; it is separate from the controlled pair below. It locates cache invalidation/Monitor.Enter_Slowpath, contact snapshot copies and GPU fence waits. Retained workers contribute many waiting samples; safepoint stacks also affect the distribution. These samples locate paths, not exclusive CPU costs or ordinary performance acceptance. StoreContact's separate CLR disassembly contains one bulk write barrier in 274 code bytes; its contact representation and copying remain unchanged.

The typed-gate trial passes 32 CLR and 28 strict NativeAOT profiles. Public CPU/no-device, NVIDIA GPU and registered worlds exercise shared dirty-parent readers, transform writes, notification failures/reentry, interpolation and real owner/pooled processing groups. Owner and separate numerical modes are represented. Eight included Wayland window profiles inspect transformed physical output with both GPU and compatibility rendering. Existing strict owner/all-thread zero-managed-allocation assertions remain active. These are trial checks, not a final new runtime implementation or complete inherited/native allocation acceptance.

The cold CLR control creates 32,768 objects per gate type outside any physical measurement, with its holder array excluded from the counter interval. Empty reference writes allocate zero; object gates request 786,432 bytes (24 each), Lock gates 1,310,720 bytes (40 each). The trial would add sixteen managed bytes per CanvasItem, about .5 MiB for the target body/shape population. This is a construction-allocation estimate on .NET 10.0.1/Linux x64, not whole-process retained memory or a foreign-runtime result. The rejected replacement adds no production memory cost.

### Complete headless scene comparison

Both payloads retain the same 16,384 awake RigidBody circles, four walls, 144 Hz fixed ticks, four substeps, sixteen sweeps, eight reported contacts, actual entry/exit handlers and interpolation. Each backend receives 1600 warmup and 256 measured complete SceneTree ticks, with ordinary GC and DOTNET_TieredCompilation=0. The first two processes run CPU then GPU; the reverse-order repeat pair runs GPU alone. Report those process histories separately. Their common GPU gate sequence is object/Lock/Lock/object, but the histories differ between the two pairs.

| Process / gate | Backend | Complete step mean / p50 / p95 / p99 / max, ms | Pose-fields / contact-monitor mean, ms | Managed owner / all threads, B |
| --- | --- | --- | --- | --- |
| CPU then GPU / object | CPU | 102.7604 / 101.6564 / 112.6845 / 119.7325 / 120.7742 | 7.5961 / 17.8500 | 0 / 0 |
| CPU then GPU / Lock | CPU | 103.9963 / 103.6761 / 107.7004 / 114.0920 / 116.8608 | 7.7000 / 19.6096 | 0 / 0 |
| CPU then GPU / object | GPU | 65.4095 / 61.3570 / 77.9959 / 83.3627 / 87.6045 | 9.9618 / 8.6699 | 0 / 0 |
| CPU then GPU / Lock | GPU | 65.7030 / 61.9987 / 76.4003 / 78.6580 / 80.9205 | 9.5405 / 8.5258 | 0 / 0 |
| GPU alone / Lock | GPU | 102.6932 / 101.9693 / 109.7620 / 121.5799 / 124.8365 | 14.6979 / 9.2437 | 0 / 0 |
| GPU alone / object | GPU | 99.7370 / 99.3339 / 105.5098 / 108.9509 / 112.6186 | 16.1653 / 8.8439 | 0 / 0 |
| CPU alone, no GPU / object | CPU | 103.8824 / 103.7262 / 108.3385 / 111.0142 / 112.5641 | 7.9912 / 18.9923 | 0 / 0 |

| GPU process / gate | Upload / readback, B per tick | Submissions per tick | GPU wait p50 / p95 / p99 / max, ms |
| --- | --- | ---: | --- |
| CPU then GPU / object | 196888 / 8793959.3125 | 19.05078125 | 17.5308 / 32.5227 / 37.7498 / 38.0557 |
| CPU then GPU / Lock | 196888 / 8720648.6875 | 19.16796875 | 17.5658 / 32.1729 / 35.3608 / 35.8859 |
| GPU alone / Lock | 196888 / 8835296.6250 | 19.21875000 | 40.1344 / 42.2584 / 44.0476 / 60.7433 |
| GPU alone / object | 196888 / 8774054.7500 | 19.02343750 | 39.8609 / 41.4487 / 42.5253 / 44.3714 |

All seven rows (1792 measured ticks) pass unchanged physical/cap/population/energy and zero-managed-byte gates. The final CPU-only process uses dummy video and unavailable Vulkan ICDs; maps confirm no NVIDIA/software-GPU library. The typed gate raises mean CPU time 1.20%, paired GPU .45% and standalone GPU 2.96%. Small pose-subphase improvements do not produce a whole-step benefit. Together with the higher cold memory cost, this rejects the replacement. The contact and state streams remain complete; trajectory-dependent report bytes and submission counts are retained rather than forced equal.

Standalone GPU repeats are much slower than GPU after CPU. Process history and time both changed, so this observation establishes neither one's causality. The paired object's 1.57x GPU/CPU mean ratio must not become a general fresh-process GPU advantage. Separate original-gate CPU/GPU processes give only a 1.04x mean ratio in this ordered desktop trial, with no isolated repeatability proof. Sampling traces, constructor microcontrols and instrumented warmup timings do not resolve that uncertainty; the wider performance gate stays open.

Controlled payloads are built from one captured source tree; only CanvasItem.Skeleton.cs and the CanvasItem class XML remarks differ. Runtime/root/consumer PDB proofs bind 2478/942/267 authored documents. Original native-copy failure, cancelled scratch toolchain work and the timestamp-stale CLR assembly rejected by its PDB checksum gate remain separate receipts. The stale assembly never enters a physical comparison. Evidence is local ignored bin/physics-pose-publication/2026-10-21/; validate.py checks source/payload/native bindings and all accepted results. The reverted private field and trial binaries remain distinct from final documentation changes.

No API declaration is promoted. Historical aggregate allocation failures, complete numerical/extension/network acceptance, native/driver allocation, foreign platforms, full-scene window FPS and the unchecked RTX power cable remain separate limits.

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

## Pure scene completion policy (2026-10-22)

Nonempty scene-only worlds now use the existing mixed-world cost comparison instead of always publishing changed state and reading contact reports separately. Dense moving observers can receive selected snapshots and contacts in one completion. Last observed nonzero linear/angular velocity, mode and sleep are cost hints; either path obtains validated current device state. Stationary awake and sleeping observers retain incremental history. Empty/Area-only scene handling is unchanged. No numerical kernel, contact field, event order or public identity changes.

[PhysicsGPUDensePublicationTests](../../tests/Electron2D.Tests/PhysicsGPUDensePublicationTests.cs) uses 512 circles and one floor, zero friction/bounce, eight-unit spacing outside the two-unit speculative contact margin, and 1/60-second full scene frames. Moving bodies retain velocity (1,0), contacts, cap 2 and monitoring. After 128 warm frames, 64 measured moving frames compare against a frozen pre-change runtime. The control fails the new completion-policy assertion; both paths retain strict 0/0 owner/all-thread managed bytes. Owner/separate candidates also verify stationary awake and sleeping frames, current fields after a zero-motion prediction, an authored pose, actual floor identities, private/portable correction and wake.

| Completion case | Upload / readback B per full frame | Completions per frame | Change publications over 64 frames |
| --- | ---: | ---: | ---: |
| Previous moving scene | 6,424 / 127,856 | 15 | 64 |
| Selected moving scene | 14,632 / 119,648 | 14 | 0 |
| Stationary awake scene | 6,424 / 86,816 | 15 | 64 |
| Sleeping scene | 6,168 / 86,040 | 3 | 64 |

The moving case shifts 8,208 bytes of receiver identity from download to request upload, preserving the 134,280-byte combined total while removing one completion. A first trial used only awake status as the dense hint: stationary awake bodies then unnecessarily read all snapshots. Nonzero observed motion restores their prior incremental traffic without treating that prediction as authority. The selected path reuses existing receiver scratch; its state/handle/backend arrays hold observation results for actual public scene consumers, with capacity growth confined to cold setup. Cold first-positive-frame allocation is reported separately from warmed intervals. Native/driver allocation totals are not measured.

The initial four-unit spacing was on the speculative contact boundary and admitted neighbor reports; the final fixture retains strict floor identity at eight-unit spacing. Historical logs, source proofs and full-scene diagnostic receipts are in `bin/physics-current-mass/2026-10-22/`. On resumption, the frozen runtime DLL/PDB/XML were absent, so the original complete diagnostic payload could not be revalidated. Its interrupted CPU case remains incomplete; these receipts establish no current performance acceptance.

Verification on 2026-10-25 uses a fresh source build and separate frozen payloads in `bin/physics-scene-completion/2026-10-25/`. A committed-source runtime control, with the same test binary and all 201 embedded resources unchanged, fails exactly the dense completion-policy assertion: 15 completions and 64 change publications. The candidate retains the table's 14 completions and zero change publications; both moving intervals allocate 0/0 owner/all-thread managed bytes. Owner/separate candidates also retain the stationary and sleeping traffic above. Cold first-positive-frame owner allocation is 1,632,056/1,521,832 bytes respectively; it is outside the warmed claim.

Thirteen current functional profiles pass: dense owner/separate, common publication, mixed owner/separate, space checkpoint, replication reconfiguration, combined completion, public contact projection on GPU owner/separate and CPU without a device, and real Wayland GPU/compatibility windows with separate GPU physics. Render checks cover pixel, motion and allocation assertions; they do not measure massive-window FPS or establish human acceptance. PDB hashes bind 2,584 runtime, 957 root-test and 270 public-consumer authored inputs. Whole-step speedup, repeated real-window FPS, NativeAOT for this slice and full-goal acceptance remain open; declaration coverage remains 1306/1369 (95.40%), with 63 open rows.

### Full-step admission and network continuation (2026-10-25)

A fresh eight-case comparison pins the compiled authored sources of local `acac7dc93d8ef43faf5bddfdb55c53ecc7bee5fd`, one immutable payload, 16,384 awake monitored bodies, four walls, 144 Hz, four substeps, 16 iterations, 1,600 warm frames and 256 samples. Its GPU/CPU/CPU/GPU order covers headless steps and real windows. Admission retains 30 two-second quiet intervals at most 0.25 other CPU cores, and adds GPU utilization at most 5% plus rejection of foreign non-desktop GPU contexts. Each running case retains the existing one-core foreign-CPU ceiling. Foreign application processes are observed, never stopped by this check. Admission expired after 600.842344 seconds with zero of eight cases started. There is no new step/FPS comparison or speedup claim. The terminal controller is not a pending job to restart.

The same frozen runtime then passed two functional three-process checks. Each starts a CPU authority, an independent GPU client and a later CPU client. Both CPU participants deny actual GPU startup and create no rendering service. Each check observes four CLR processes including its controller, exactly one with NVIDIA mappings and none with software GPU mappings. These runs retain packet loss, delay, duplication and reordering, all 39 portable actors, contacts and joint families, sleep/wake correction, lifecycle/control-epoch changes, prediction/replay and effect confirmation. The requested late-join trigger is tick 310; admission occurs at the observed ticks below.

| Check mode | Final tick | GPU corrections / replayed ticks | Late CPU join / corrections / replayed ticks | GPU warm live / replay samples |
| --- | ---: | ---: | ---: | ---: |
| Coordinated check | 600 | 177 / 2,145 | 339 / 96 / 1,192 | 88 / 1,084 |
| Ordinary session, `--unpaced` | 900 | 125 / 1,480 | 387 / 92 / 1,327 | 154 / 1,015 |

Warm steps in all six participants and the four clients' measured warm replay retain zero owner/all-thread managed bytes. The GPU's complete replay allocation counter still contains 2,336 cold bytes; it is not covered by the warmed claim. The ordinary session has zero check-progress traffic and waits. All participants request separate numerical execution; CPU probes observe that lane directly, while this GPU probe reports device work/traffic and does not independently observe its numerical thread.

GPU cumulative upload/readback is 5,358,904/15,339,348 bytes in the coordinated check and 4,030,444/11,226,332 bytes in the ordinary session. The respective submission/wait totals are 114,424/14,387.9685 ms and 80,445/8,130.8099 ms. These totals include preparation, simulation, corrections and replay under external load; they are diagnostics, not isolated per-step or comparative performance. Reports also retain live/correction percentiles, application traffic and logical command/event history payloads of 524,288/36,864 bytes per participant; these payload counts exclude object headers, other retained state and native/driver memory.

Exact commands, participant reports, maps, telemetry, source/payload hashes and the rejected admission are in `bin/physics-full-comparison/2026-10-25/`; `network-validation.json` separates functional acceptance from performance. No coverage state is promoted. Further comparison requires a new quiet admission; repeated blind attempts or timings from these functional runs cannot close the performance gate.

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
