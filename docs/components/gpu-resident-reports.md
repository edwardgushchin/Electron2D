# Resident GPU contact publication

Last updated: 2026-10-09

## Boundary

GPUPhysicsBodyStore can opt into completed-tick contact reports without creating a
CPU solver world or downloading all manifolds. CaptureContactReports defaults to
false. ReadContactReports takes retained caller spans of body handles, per-body
limits and destinations. This is an internal prerequisite for the shared public
world/direct-state adapter; public PhysicsSpace still uses CPU. Scene/RID event
projection, callback delivery and portable network snapshots remain open.

Reports carry observed shape/body indices and generations, world positions,
receiver-facing normal, world-axis impulse, final point velocities and greatest
observed depth. These local identities are not wire IDs or public RIDs. The future
adapter must resolve them to retained common identities and logical shape indices,
apply contact-limit assignment invalidation and preserve the existing callback/event
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
| Selected request / count | 16 uploaded bytes and 4 downloaded bytes per requested receiver. |
| Selected result | 80 bytes per requested slot. Only each returned count identifies valid entries. |
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

## Verification

The dedicated entry point is `ELECTRON2D_TEST_GPU_REPORTS=1`; it runs
[GPUPhysicsReportTests](../../tests/Electron2D.Tests/GPUPhysicsReportTests.cs) and
existing public CPU PhysicsContactImpulseTests. The complete GPU runner includes
the new suite. The report below records executed checks and their limits separately.

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
