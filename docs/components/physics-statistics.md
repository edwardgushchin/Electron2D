# Completed physics statistics

Last updated: 2026-10-10

[PhysicsServer.GetProcessInfo](../classes/PhysicsServer.md#statistics) and
[Performance.GetMonitor](../classes/Performance.md) read the same completed-step
samples from CPU and independent GPU worlds. [ADR 0089](../decisions/physics-activity.md#adr-0089)
owns sampling and counter semantics; [ADR 0095](../decisions/singleton-services.md#adr-0095)
owns retained static services.

## Counts and lifetime

| Count | Meaning |
| --- | --- |
| Active objects | Awake nonstatic bodies, including stationary kinematic bodies. Excludes Areas and static virtual surfaces. |
| Collision pairs | Filtered backend collision candidates, including sensor pairs. Not contact points or event counts. |
| Island count | Active dynamic constraint groups containing contacts or joints. Excludes unconstrained bodies and sensor work. |

Each active registered world contributes its latest completed positive step once,
even when multiple viewports share it. Publication precedes body/event callbacks.
New worlds start at zero; an empty positive step clears their sample. Local inactive
worlds are excluded immediately, and reactivation exposes the retained sample.
Global suspension and zero-time steps retain samples. Freeing a world removes it.
A failed unpublished interval retains its previous sample; a failing observer sees
the already completed step. Queries and authored edits alone do not publish a sample.
Individual reads are coherent; several separate calls are not a combined transaction.

Counts reflect the actual backend. CPU contact candidates come from the retained
contact pool; sensor candidates use its existing spatial trees with directed masks
and sensor/sensor deduplication. GPU candidates come from its filtered resident tree.
Geometry decomposition and broad-phase padding can yield different pair totals.
CPU may defer splitting a disconnected island until sleep becomes possible. This
is a workload diagnostic, not a promise of identical private graphs across solvers.

## Runtime cost

Publication and aggregation use the existing PhysicsServer registry lock. Reads
never acquire another world's owner lane, touch a device or invoke application code.
They sum cached values without managed allocation. Undefined selectors reject;
integer aggregate overflow throws rather than wrapping.

CPU samples awake solver sets/islands and queries each sensor's fat AABB against
the native broad phase. It does not rebuild a graph or scan every body pair.
GPU reuses its resident sleep graph: incident constraints mark bodies, then the
existing final pass counts awake bodies and active constrained roots. Two uints
join the existing final status download: **8 extra bytes per simulated outer tick**,
with no additional submission or fence. Skipped idle ticks and ordinary state/query
reads do not pay this transfer. The skip guard remains distinct: moving static
surfaces keep simulation alive but do not count as active objects.

## Verification

`ELECTRON2D_TEST_PHYSICS_STATISTICS=1` runs
[PhysicsStatisticsTests](../../tests/Electron2D.Tests/PhysicsStatisticsTests.cs)
on the public CPU and GPU APIs: resting contacts, joining/removing joints, sleep and
wake, moving surfaces, stationary kinematics, directed sensors, shared/mixed worlds,
local/global suspension, empty/zero steps, callback failure, terminal GPU failure,
foreign-thread reads, and permanent named service identity. The full GPU suite
includes this fixture.

On Linux x64/.NET 10, 64 warmed measured steps with sensors and 1,024 cached reads
allocate **0 process-wide managed bytes** on each backend. Reading statistics leaves
GPU submission and readback counters unchanged. A separate CPU-only process passes
with DISPLAY/WAYLAND_DISPLAY removed, an unavailable GPU driver and SDL dummy video.
Native/device allocation, other platforms and rendered FPS are separate acceptance
requirements. The complete GPU physics and network goal remains open.

## Complete public-path measurement

Measured on this change over parent `507e7949`, Linux x64, .NET 10.0.1,
Ryzen 7 5700X and Vulkan/NVIDIA GeForce RTX 3090 Ti. The same
`ELECTRON2D_TEST_GPU_SPACE=1` fixture constructs circles plus a floor through the
public API, disables sleep, runs 4 substeps at 1/60 s with world defaults, warms
768 steps and measures 128 complete steps. Scene sizes and physical work remain
unchanged between backend selections. Statistics publication is included.

| Bodies + floor | Backend | p50 / p95 / p99 (ms) | Managed bytes, owner / all threads | Upload / readback (B/step) | Uniforms (B/step) | Mean device wait (ms) |
| --- | --- | --- | --- | --- | --- | --- |
| 1,024 | CPU | 2.5116 / 2.7246 / 2.8589 | 0 / 0 | 0 / 0 | 0 | 0 |
| 1,024 | GPU | 4.2494 / 8.9611 / 10.8055 | 0 / 0 | 184 / 82,112 | 17,820 | 2.6893 |
| 4,096 | CPU | 6.5864 / 7.4496 / 7.9317 | 0 / 0 | 0 / 0 | 0 | 0 |
| 4,096 | GPU | 4.9793 / 5.7956 / 6.7035 | 0 / 0 | 184 / 327,872 | 18,184 | 2.7609 |

This run favors CPU at 1,024 bodies and GPU by about 1.32 times at 4,096 bodies
in median complete-step time. It does not establish a universal crossover or
rendered-window FPS, nor isolate the timing cost of statistics against a
pre-change binary. Device waits and active body publication still dominate the
GPU path. The resident role/policy, force and kinematic traffic regressions verify
104 B status readback per full tick versus their previous 96 B budget, with
unchanged uploads and zero warmed allocation. The full GPU conformance suite and
runtime shader checks pass on this device.

Kinematic/static and kinematic/kinematic report-only pairs contribute once to
collision-pair statistics when their current geometry touches and either endpoint
requests a snapshot. Native-dormant CPU kinematics with reporting enabled count
as logically active without holding a dynamic sleep island awake. Two static
bodies remain ineligible, and report-only pairs add no dynamic constraint island.
See [contact reporting](physics-report-only.md).
