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
