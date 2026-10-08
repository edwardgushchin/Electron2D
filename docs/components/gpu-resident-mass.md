# Resident GPU mass profiles

Last updated: 2026-10-08

## Executing boundary

[GPUPhysicsBodyStore](../classes/GPUPhysicsBodyStore.md) now retains configured and
resolved mass profiles under [ADR 0073](../decisions/physics-mass.md#adr-0073).
BodyDefinition.Inertia=0 selects automatic geometry; nullable CenterOfMass selects
automatic centroid, while an explicit vector selects a custom local center.
SetMassProfile changes these values without moving the body origin or changing
its current linear/angular velocity. GetMassProfile returns the configured values;
GetMassProperties resolves pending geometry and returns kilograms, scene-unit polar
inertia and local center without a GPU readback.

This is an internal GPU component. Public backend selection, live body-mode/freeze
adapters, sleep, CCD, complete queries/events and network snapshots/replay remain
open. Existing public CPU mass operations continue through their current adapter.
The feature is not evidence of a complete selectable GPU world or window FPS.

## Shared authoring policy

The [PhysicsMass](../classes/PhysicsMass.md) Geometry scratch object reuses the
existing primitive mass calculation and CPU shape compiler during authoring. It
creates no CPU physics world and retains no evolving pose, velocity, contact or
joint state. Normalized mass properties are functions of authored geometry/settings;
they do not require per-step GPU simulation results. This reuse also preserves
convex decomposition, capsule/segment tolerance, area weighting and thin-rod policy.

Solid areas take precedence over zero-area segments. Sensors and separation rays
contribute no mass; disposed resources are ignored. With no solid area, segment
lengths weight centers and rod inertia. Point-only/empty automatic inertia is zero.
A custom center uses the parallel-axis theorem; an explicit positive inertia stays
independent of mass and shape changes. Local placement, geometry/resource revision,
sensor conversion, attachment removal and disposal invalidate affected profiles.
Material or collision-mask-only changes do not trigger a mass recalculation.
Revision polling handles resource changes whose subscriber threw.

Mass preflight runs before device submission. Invalid authored configuration or
geometry can be corrected and retried; it does not invalidate an unstarted GPU step.
After execution begins, the existing failed-world/no-CPU-replay policy remains.
Sparse mass edits invalidate contact and joint warm history through body edit epochs.

The initial CPU/GPU geometry comparison exposed a rotated hollow-line centroid
mismatch: (11.056936, 21.3757) versus (11.072337, 21.37115). Detached mass proxies
used an approximate rotation helper while segment fixtures used the authored
rotation. Common mass proxies and rectangle/polygon fixture authoring now use the same
ordinary sine/cosine rotation from the authored basis as other
families. Public CPU tests now verify analytic rotated centers before/after
attachment. This fixes authoring consistency; it changes no native integrator
rotation policy or public parameter units.

## Device motion and constraints

The local center lives in a separate 8-byte-per-slot device buffer; the hot body
record remains 80 bytes. Integration advances the world center by its linear
velocity, rotates the body basis, then reconstructs the body origin:

`worldCenter = origin + rotation * localCenter`

`newOrigin = worldCenter + delta * velocity - newRotation * localCenter`

The same rule applies to positional correction scratch. Static poses remain fixed;
kinematic integration ignores forces; dynamic and RigidLinear roles retain their
existing force and rotation policies. Contact and joint preparation subtract the
local center from their moment arms. Contact reports/anchors remain body-local
coordinates relative to the origin, and joint geometry stays in its sampled frames.
The iterative impulse kernels need no center-buffer binding.

Central/angular impulse commands capture mass/inertia at invocation by encoding
velocity deltas from the resolved authoring profile. Thus an impulse queued before
a later mass/geometry edit retains its original effect. The actual velocity update
still runs on GPU; a later explicit velocity assignment supersedes earlier queued
impulses as before. Constant forces/torque use device inverse values during each
step. No solved velocity is copied to CPU for these operations.

## Storage and traffic

| Payload | Current size / consumer |
| --- | --- |
| Device body | 80 bytes per retained slot, unchanged hot layout. |
| Device local center | 8 bytes per slot; integration and contact/joint preparation read it. Growth copies it GPU-to-GPU. |
| CPU body slot | 64 bytes: identity/attachment/edit routing, authored role/profile and resolved immutable geometry values. No live pose or velocity. |
| Pending body command | 128 bytes, including optional mass/center updates and captured impulse delta. Consumed entries are cleared. |
| Body state result | 32 bytes pose/velocity, unchanged; only explicitly requested bodies are downloaded. |

At 65,536 slots, bodies plus centers use 5.5 MiB. Authored body/command array payload
is 12 MiB, measured by AuthoredBodyCapacityBytes; it excludes object headers, shape
and joint metadata, dirty-index capacity, proxy scratch and driver allocations.
Unchanged warmed ticks add no mass upload, readback or wait. A real profile edit
coalesces into the existing body command. GetMassProperties reads the authoring
cache and queues a changed device profile; subsequent physics/read submission
applies it. Authoring geometry preparation is CPU work, not a GPU solver stage.

A first candidate embedded a padded center in every 96-byte body record. The final
separate buffer saves eight device bytes per slot and keeps the center out of hot
iteration loads. The same 65,536-body response test gave 23.2542 ms versus 18.5652 ms
medians in the two runs, but desktop clocks/load were not pinned and contact order
is unspecified; this is candidate evidence rather than a controlled speedup claim.

## Verification and numerical scope

Run `ELECTRON2D_TEST_GPU_RESIDENT_MASS=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`
after a Release source-native build. The full GPU runner includes this suite, while
`ELECTRON2D_TEST_COLLIDER_BACKEND=1` covers the affected public CPU path.

The checks include automatic/custom centers, explicit/automatic inertia, actual
force/torque response, origin/velocity preservation, free rotation around a shifted
center, normalized compound area, thin rods and all current shape families compared
with public CPU getters. A circle/rectangle/capsule/polygon/line/ray set covers
rotated placement and custom centers. A collision, world pin and spring at an
offset computed center verify that the solver creates no spurious torque. Mass-only
edits invalidate warm impulses. Impulse/profile/geometry ordering, sensor changes,
throwing observers, disposal, reused identities, numeric rollback and recoverable
invalid geometry are exercised.

Analytic circle/parallel-axis values allow 0.001 kg·scene-unit²; compound moment
allows 0.01. CPU/GPU centers allow 0.005 scene unit, and decomposed moments allow
max(0.005, 0.0005×expected), covering float summation/unit conversion. Pure rotation
keeps its center within 0.001 scene unit after 120 steps at 120 Hz. Actual force/
torque velocity checks allow 0.00001 in scene units/s or rad/s. Centered contact
and spring response allow 0.001; the rotating pin allows 0.02-unit anchor error
and 0.001-rad/s angular error after one second with four substeps/32 iterations.

The active-edit test alternates kilograms and custom centers for 128 warmup and
128 sampled iterations, each with a force update, step, explicit body read and
mass getter. It reports zero managed owner-thread allocation. The normal 65,536-body
integration/contact populations and 4,096-pin regression also retain zero warmed
allocation. Native allocator totals and foreign devices/platforms remain unmeasured.


## Measured scope

Linux/.NET 10.0.1, Vulkan, RTX 3090 Ti, 2026-10-08. The active-edit test reports
**0 B managed**, **0.1049 ms mean** per edit+step+explicit-read, **152 B upload**
and **40 B readback** per iteration. These include the existing status/request
payloads and one coalesced 128-byte body command; unchanged ticks send no command.

The final separate-center-buffer run retains the contact workload from the earlier
report (gravity 980, 1/120 s, four substeps, sixteen iterations, 384 warmup/256
samples). All final states are checked outside the sampling window:

| Dynamic bodies | Tick p50 | Tick p95 | Tick p99 | Mean wait | Managed bytes/tick |
| --- | ---: | ---: | ---: | ---: | ---: |
| 4,096 circles | 3.1649 ms | 3.8572 ms | 4.7384 ms | 2.3114 ms | 0 |
| 65,536 circles | 18.5652 ms | 19.8489 ms | 20.3467 ms | 16.8844 ms | 0 |

Both exchange 128 B each way per tick; mean uniforms are 12,840 / 13,616 B. All
circles remain inside their pit, with final maximum Y 126.0420 / 510.2630 below
floor tops 127 / 511 and negative total mechanical-energy change. This workload
still omits sleep/CCD, public publication and rendering; 60 FPS is not established.
The integration-only control is 0.0483 ms p50 with 4 B each direction and 32 B
uniforms. It is not a full-physics result.

The CPU collider aggregate, full GPU suite (including both renderer lifetimes),
and dedicated mass suite passed. Logs: `/tmp/electron2d-resident-mass-final-cpu.log`,
`/tmp/electron2d-resident-mass-centers-gpu.log`,
`/tmp/electron2d-resident-mass-final-profile.log`. The earlier failing geometry
comparison is in `/tmp/electron2d-resident-mass-tests.log`.

Generated SPIR-V SHA-256, untracked build outputs:

- Bodies: `f3e5735e476ebda0be0e23ade496e7ed382e7b4e8fe1e2b1f3ff356563697160`.
- Contact preparation: `7e2c6bd6aab022d478d950b877982791e7a7c7160e6b7521792c4013c683f106`.
- Joint preparation: `b07c33319cdad81756b6503e586ab9c18eed68bc569ac0289cd666dee42f9270`.
