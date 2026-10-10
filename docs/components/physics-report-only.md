# Contacts without impulse response

Last updated: 2026-10-10

## Contract

An overlapping kinematic/static or kinematic/kinematic pair can report contacts
when either endpoint has a positive BodySetMaxContactsReported limit. This also
applies to a frozen RigidBody in Kinematic freeze mode, including its ordinary
object/shape events. Two static bodies do not create a pair, even when reporting
is configured. Admission follows either directional mask; exceptions, disabled
geometry, joint vetoes and shape contact policies still apply.

The pair has real narrow-phase geometry, collider/owner and logical shape indices,
normals, positions and point velocities. It applies zero impulse and no positional
correction. Each receiver retains its own cap and current snapshot. Enabling
reporting immediately exposes a kinematic receiver as active; its next completed
step discovers the pair even after an earlier prepared step. Disabling the final
receiver clears it. Quiet reported kinematics remain active, so moving a static
neighbor updates the snapshot and queued departures. Reporting metadata follows
existing checkpoint and attachment identities, and is reset on backend slot reuse.
Both scene and server cap setters synchronize logical reporting eligibility.
Local checkpoints retain their existing requirement for unchanged authored
configuration; they do not undo arbitrary contact-cap edits.

[ADR 0102](../decisions/physics-filters.md#adr-0102) owns the admission policy;
[ADR 0058](../decisions/physics-monitoring.md#adr-0058) owns scene event delivery.
The pinned body-pair implementation permits reporting without dynamic response;
its broad phase excludes static/static pairs. Those are separate conditions.

## Implementation

CPU reads current static/kinematic tree candidates only for reporting receivers,
uses existing native manifold functions and appends the observations to retained
frame contact storage. Ordinary impulse accumulation and capped projections share
that storage. Report-only pairs never join native constraint/sleep islands, so
reporting cannot hold a supported dynamic body awake. The adapter exposes reported
kinematics as active for callback/public state while native solver dormancy stays
independent. Geometry is refreshed after each completed discrete world interval. Report-only
receivers participate in kinematic subdivision even without a dynamic body.
Contacts crossed during earlier internal intervals remain in the completed outer
frame snapshot and clear on the following idle frame, under ADR 0070.

GPU bodies carry one eligibility bit in resident flags. A sparse existing body
command updates it; the existing spatial uniform enables conservative role
traversal. Narrow phase and reports remain on GPU. Zero-mass rows have no incident
solver constraints. The existing capped report read transfers public contact
results without downloading complete body state or manifolds. First-impact advancement still observes report-only geometry. Once the pair is
already touching and neither endpoint can respond dynamically, CCD no longer
subdivides to bound a residual closing velocity that zero-impulse rows cannot
remove. This preserves the prescribed destination and avoids exhausting the
interval budget. No buffer layout, full CPU mirror or new reporting fence is
introduced.

## Verification

`ELECTRON2D_TEST_REPORT_ONLY=cpu` and `=gpu` run PhysicsReportOnlyTests through
public PhysicsServer and scene operations. The role/receiver matrix tests each
static/kinematic combination, both attachment orders and either reporting endpoint.
Both receivers also enable one-point caps together; each retains its own snapshot
and completed pair statistics count the shared physical pair exactly once.
Counts, identity, zero impulse and unchanged stationary poses are exact checks.
Lifecycle checks cover already prepared worlds, both mask directions, reverse
exceptions, joint veto/release, receiver disable/re-enable, reused slots and quiet
static departures and immediate public activity after re-enabling a quiet
kinematic receiver through either cap setter. A circle and a directed ray report against an infinite boundary
at X=20000, outside its finite debug marker, from either receiver. Surface point
velocity is checked within 0.001 scene units/second; bounded debug markers are
observed separately, including zero markers at an exactly touching circle pair
whose contact remains reported. Dynamic bodies must sleep on a reported idle AnimatableBody
with either SyncToPhysics value and wake when that platform moves.
Frozen scene tests verify object/shape identity, one entry/exit and 121 consecutive
state-sync callbacks after a 120-tick quiet interval. A -60 to +60 sweep past a
static circle verifies transient contact retention with zero impulse, a destination
tolerance of 0.001 scene units and clearing on the next idle tick. Marker checks
enable SceneTree.DebugCollisionsHint and respect its sampled project limit.

Prepared full-step measurements use two rectangles, dt=1/60, four substeps and
16 iterations. Blocking collection precedes 128 warmup and 128 measured frames.
Both continuous overlap and alternating overlap/separation measure owner/all-thread
managed bytes. GPU counters include upload/readback and wait costs of the complete
public step and subsequent capped view access. This tiny scene measures dispatch
and reporting costs; it does not prove a massive-scene GPU benefit or window FPS.

Checkpoint, broader shape/joint/CCD, failure, networking and native/platform
acceptance retain their corresponding executable suites and full-goal gates.

The broader public-world fixture exercises frozen rotation with the accepted
ADR 0075 presentation tolerance of 0.01 radians. Subdivision exposed its former
0.001-radian assertion as stricter than that decision (CPU observed 0.40108532
for a 0.4 target). It still verifies manual rotation while LockRotation is
authored and restoration of the dynamic lock after unfreezing. The resident CCD
fixture now rejects both mask directions for its pass-through case and keeps a
zero wall mask for its responding/friction case, under ADR 0102.

## Local measurements, 2026-10-10

Linux x64, .NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti. The complete
report-only fixture above uses identical CPU/GPU authoring and cadence; its
publication and capped direct-view access remain inside the measured bracket.

| Backend and membership | p50 / p95 / p99, ms | Upload / readback, B per frame | GPU wait, ms per frame | Owner / all-thread managed B |
| --- | --- | --- | --- | --- |
| CPU, persistent | 0.0024 / 0.0024 / 0.0025 | 0 / 0 | 0 | 0 / 0 |
| CPU, alternating | 0.0024 / 0.0025 / 0.0026 | 0 / 0 | 0 | 0 / 0 |
| GPU, persistent | 3.9015 / 4.8911 / 6.0361 | 744 / 1236 | 2.1077 | 0 / 0 |
| GPU, alternating | 3.5819 / 4.2382 / 5.6250 | 664 / 826 | 1.5591 | 0 / 0 |

The broader unchanged PhysicsGPUSpaceTests workload measured 4096 active circles
plus one floor, four substeps, 768 warmup and 128 samples on one assembly:
CPU p50/p95/p99 6.9880/8.3067/9.2043 ms; GPU 9.4703/10.3882/11.1224 ms,
240/328440 B upload/readback plus 63276 B uniforms and 4.6879 ms wait per frame.
Both measured zero owner/all-thread managed bytes. GPU is slower on this
workload; the full-goal performance gate remains open. These are local physics
measurements, not window FPS, native allocation or cross-platform acceptance.
The broader regression ran before the final CPU debug-only zero-separation
guard; the table's report-only measurements and contact diagnostics exercised
that final guard.

Local ignored evidence: `bin/physics-report-only-validation/2026-10-10/`,
including `report-cpu.log`, `report-gpu.log`, `contact-debug.log`,
`gpu-world-before-tangency.log`, separate network JSON files and the measured
assembly digests.
