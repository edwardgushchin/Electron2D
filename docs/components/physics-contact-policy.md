# World and shape contact correction

Last updated: 2026-10-09

## Authored policy

[ADR 0098](../decisions/physics-contacts.md#adr-0098) defines Shape.CustomSolverBias
and typed PhysicsServer world getters/setters for default bias and allowed penetration.
[PhysicsContactSettings](../classes/PhysicsContactSettings.md) validates the shared
policy. New worlds sample ProjectSettings feature overrides: bias 0.8 and slack
0.3 scene units. Existing worlds retain their settings. Zero shape bias inherits
the world, one nonzero shape wins, and two nonzero biases use their arithmetic mean.
World zero disables correction. Slack suppresses correction of shallow penetration
without suppressing contact reporting, physical velocity constraints or materials.

Bias requests a fraction of excess penetration per outer tick. Each prepared interval
uses `1 - (1 - bias)^(interval / tick)`. Coupled constraints, correction-speed caps,
float precision and CCD clipping of already prepared velocities can delay convergence;
this is not a promise of exact geometric displacement for arbitrary contact networks.
Changing tick duration does not multiply the nominal fraction for an isolated contact.

World changes wake dynamics and reset quiet timers; equal writes preserve them.
Shape changes publish a separate policy epoch before Changed observers. Borrowers
observe a committed edit even if a callback throws. Geometry revisions, fixture/RID
identity, mass data and one-way contact history remain unchanged. CPU fixture tags
retain a weak authored resource reference, update only policy and wake touching
bodies. CollisionShape geometry uses the existing revision path instead of treating
every Changed event as a fixture rebuild. Copies publish geometry revisions; all
eight concrete shapes preserve inherited policy through duplicate, in-place copy
and built-in .e2dres storage.

## Solver execution

CPU scalar overflow and SIMD contacts cache the mixed bias at contact update and
prepare correction against the outer tick duration. Correction velocity is relaxed
out before physical momentum publication. Rotation locks mask solver inverse inertia
as well as final angular velocity; unlock restores authored inertia. This avoids
losing normal response into a forbidden rotational degree of freedom.

Virtual linear/angular surface velocity contributes contact-point displacement to
separation during position/relaxation stages. The surface pose remains fixed, but
relaxation no longer cancels the physical impulse transferred to a departing body.
CCD clips the initial restitution interval even when position correction separates
the bodies, so the outgoing bounce velocity advances during the remaining tick.

The older CPU-host/GPU-stage solver packs the computed contact correction rate/slack
into existing contact-input padding. Its step uniform is 80 bytes (formerly 64),
including elapsed surface displacement time. CPU remains the owner of that path.

Independent GPU shapes store bias in an existing padding word of the 80-byte shape
record; policy changes upload metadata without vertices or mass recompilation.
The device mixes shape/world biases and applies the interval exponent. The shared
resident solver uniform is 80 bytes (formerly 64); all four consuming kernels use
the same layout. Its correction channel also keeps inherited joint bias separate
from the contact default. Unspecified diagnostic arguments use the captured contact
policy and project constraint bias; an explicit correctionFactor retains the internal
diagnostic override for both. No host physical pose or contact calculation is added.
Public independent-GPU world binding remains open.

## Verification and limits

PhysicsContactPolicyTests covers seven finite/directed body geometries against an
analytic boundary, shape bias mixing, zero/full bias, slack, live edits and failed
observers, resource copies/storage, shared CPU scene/server state, phase/thread
validation and momentum/contact reporting. A 12-plane case exercises overflow and
dense incidence. Isolated penetration is checked within 0.003 scene units, dense
correction within 0.01; CCD-interval correction allows 0.004. One/four/eight resident
substeps and 0.01/0.04-second ticks check fractional policy. Surface and CCD regression
suites retain physical-response and frame-impulse assertions. GPU joint policy tests
verify that contact bias does not change inherited joint correction.

The allocation probe alternates one shared shape's bias before every active full
1/60-second step: 256 warmup, 128 measured samples. It measures policy propagation,
solver work and reporting storage, with no pose readback in the measured loop. It is
an overhead probe, not evidence of GPU speedup at large body counts. Native allocation,
other platforms/devices, window FPS, networking and public GPU selection remain
outside this acceptance. The CPU collider suite, full GPU suite and resource archive suite passed;
these checks do not establish owner visual acceptance.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti; one active contact:

| Execution path | p50 / p95 / p99, ms per edited step | All-thread managed bytes over 128 samples |
| --- | --- | --- |
| Public CPU, dummy video | 0.0048 / 0.0049 / 0.0052 | 0 |
| CPU host/GPU stages | 0.2230 / 0.2562 / 0.3785 | 0 |
| Independent resident GPU | 2.8037 / 3.4005 / 4.3771 | 0 |

The resident loop uploads 472 B of buffers and 27,604 B of uniforms and reads
200 B of status per step; included waits average 1.4645 ms. Logs:
`/tmp/electron2d-contact-policy-collider.log` and
`/tmp/electron2d-contact-policy-gpu-suite.log`. This deliberately tiny case exposes
submission overhead; it is not a large-world CPU/GPU performance comparison.

## Solver iterations

SpaceGetSolverIterations and SpaceSetSolverIterations use a positive integer, sampled
from Physics2DSolverIterations (default sixteen). Existing worlds retain their count
when project defaults change. Invalid/equal values preserve prior state; a changed
count wakes dynamics and resets quiet timers. An explicit sleep written afterward
wins. Owner/solver/lifetime validation applies to both accessors.

The count controls complete contact and joint sweeps within each time substep.
CPU repeats correction-enabled solving and relaxation separately; the independent
GPU repeats its coupled physical/position constraint passes. Integration, force
consumption, restitution and nominal joint/CCD budgets do not repeat per sweep.
Different algorithms need not converge equally fast at the same count.

CPU keeps one descriptor per stage/color and reuses it for all sweeps. Block claims
and cancellation use 64-bit atomics with a 48-bit phase ordinal, avoiding wrap at
65,536 phases without a policy clamp or per-iteration storage. Raw private backend
worlds keep one sweep for diagnostic controls; public world creation applies the
captured setting. Resident optional per-call iterations remain diagnostic overrides
and do not change the stored world value or its getter.

PhysicsSolverIterationTests runs the same eight-body contact and pin-chain cases on
CPU, stage GPU and resident GPU. Initial momentum is 60 kg*u/s and the converged
common speed is 7.5 u/s. After a 1/60-second tick, 128 sweeps must reduce RMS velocity
error below 40% of the one-sweep result and below 0.5 u/s; this latter bound is under
7% of the equilibrium speed. Momentum error is below 0.03 kg*u/s. A separate free
body moving at 40 u/s advances 0.8 u over 0.02 seconds within 0.00003 u at 1/16/128
sweeps, checking that iteration count does not multiply time. The same free-body
check accepts int.MaxValue sweeps: worlds without contacts/joints skip the empty
iteration loop entirely. The prior attempted
32-sweep common convergence budget was insufficient for the resident Jacobi chain
(1.601325 u/s contact RMS); the convergence test uses 128 on every path instead of
assuming CPU and GPU convergence rates match.

A CPU-only 256-body/128-pair check executes 8193 sweeps per each of four substeps,
with multiple graph work blocks and more than 65,535 phases. Storage prepared at
one sweep remains sufficient, and the measured high-count step allocates zero
managed bytes. Worker failure cancellation is covered separately by PhysicsParallelTests.

Small-world measurements use 256 active bodies in 32 eight-circle rows, shared
4 u/s velocity, no gravity/friction/correction, four substeps, 128 warmup and 128
samples per count. These are controlled iteration-cost probes with all bodies and
contacts retained, not large-world GPU speedup or window-FPS acceptance.

Linux/.NET 10 Release, Vulkan/NVIDIA GeForce RTX 3090 Ti, 256-body probe:

| Path | Sweeps | p50 / p95 / p99, ms | Managed bytes / 128 ticks |
| --- | --- | --- | --- |
| Public CPU, dummy video | 1 | 0.1136 / 0.1450 / 0.1550 | 0 |
| Public CPU, dummy video | 16 | 0.3670 / 0.4000 / 0.4055 | 0 |
| Public CPU, dummy video | 32 | 0.6443 / 0.6900 / 0.7388 | 0 |
| CPU host/GPU stages | 1 | 0.4677 / 0.7663 / 0.9880 | 0 |
| CPU host/GPU stages | 16 | 2.0894 / 2.4477 / 2.5976 | 0 |
| CPU host/GPU stages | 32 | 3.9832 / 4.3127 / 4.4319 | 0 |
| Independent resident GPU | 1 | 1.6085 / 2.2604 / 3.2013 | 0 |
| Independent resident GPU | 16 | 2.1984 / 2.8653 / 3.7316 | 0 |
| Independent resident GPU | 32 | 2.8541 / 3.5612 / 4.5762 | 0 |

Resident upload/readback remain 160/160 B per tick at all counts, with no pose reads
in the measurement loop. Uniform traffic is 7,480 / 17,080 / 27,320 B and included
mean wait is 1.0334 / 1.3143 / 1.6409 ms at 1/16/32 sweeps. CPU's 8193-sweep step
completed in 64.065 ms with zero managed bytes. Contact/pin RMS error at 128 sweeps:
CPU and stage GPU about 0.000002 u/s, resident GPU 0.071448 / 0.028477 u/s. These
figures establish convergence and expose small-workload dispatch cost; equal counts
do not promise equal numerical work or GPU acceleration. Logs:
`/tmp/e2d-iterations-final-cpu.log`, `/tmp/e2d-iterations-final-stages.log`,
`/tmp/e2d-iterations-final-resident.log`. CPU/GPU broad suites and parallel failure
checks passed; focused checks were repeated after the empty-iteration optimization. Native allocation,
other devices/platforms, real-window FPS, public GPU selection and network acceptance
remain unverified by this iteration-setting slice.
