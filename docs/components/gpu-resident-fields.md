# Resident GPU Area fields

Last updated: 2026-10-09

## Implemented boundary

GPUPhysicsBodyStore resolves current Area gravity and independent linear/angular
damping on the device before the outer simulation tick. SetAreaFields/RemoveAreaFields
bind a copied field profile to a static sensor owner. StepFields/SimulateFields
accept explicit world defaults; existing Step/Simulate vector-gravity calls remain
zero-default-damping controls and also apply registered Areas. Body policies now
use RigidBody.DampMode.Combine/Replace for each authored damping coefficient.
This implements the internal reduction required by
[ADR 0056](../decisions/physics-fields.md#adr-0056). The [public GPU world](physics-backends.md) now projects scene/server Areas, publishes resolved fields and dispatches common events. Full family conformance and networking remain open. No public declarations or coverage states change.

## Membership and reduction

Authored Area profiles sort by descending integer priority, then stable registration
order. A profile change preserves that order; removal/rebinding gets a new order.
Only profiles with at least one enabled channel enter the device table. The public
adapter retains the common world's stable Area traversal order when registering
mixed scene/server objects. Registration/configuration are CPU authoring data;
there is no host mirror of overlap membership or resolved body fields.

The existing broad/narrow phase computes current sensor contacts. Field membership
uses the Area mask against the receiver layer, irrespective of the reciprocal body
mask. Multiple shapes, manifold points and concave pieces do not multiply one
Area's contribution. Device contact links identify candidate Area ranks for each
receiver; each body selects successive distinct ranks and reduces all three
channels independently. Combine/CombineReplace add; Replace/ReplaceCombine replace;
CombineReplace/Replace stop only their channel. Any unstopped channel receives the
unbounded world fallback; default-world modes/priority are stored input but do not
stop that fallback.

Field contacts allow 0.05 scene-unit separation for ordinary primitives, matching
the current CPU shape-distance tolerance. CPU AABBs include a speculative expansion,
so tight polygon bounds must not reject a smaller valid field gap. Directed rays
retain their actual extent and ray/ray exclusion. Concave sensor pairs test their
pieces; the physical solid concave/concave exclusion remains unchanged. These
boundaries have public CPU comparisons. Sensor margin participates in contact-cache
identity; ordinary contact queries retain their existing default sensor margin.

Directional gravity is unnormalized and independent of Area rotation. Point gravity
transforms its local center by the current Area pose, then uses constant strength
for nonpositive unit distance or inverse-square falloff otherwise. Exact zero
distance gives zero gravity. Default point fields use identity pose and work for
receivers with no geometry. Body gravity scale applies after Area/default reduction;
body damping independently combines with or replaces selected damping.

Reduction happens once per outer tick, before damping and forces. Substeps and CCD
reuse the selected values. Kinematic bodies report fields without automatic motion;
static bodies report zero. Omitted force integration still reports selected values.
A changed resolved vector/coefficient wakes affected sleeping dynamics; unchanged or
fully overridden values do not wake unrelated receivers. Geometry/filter changes,
resource disposal and Area removal/reuse update membership before the next tick.
Zero-time calls and reads retain the previous resolved snapshot.

## Broad-phase distribution

The original index-based mixed-pair ownership made one large sensor query walk the
whole receiver tree. For the measured single-Area workload that serialized the main
spatial work on one GPU invocation. Nonsensor queries now own mixed sensor/body
pairs; sensor queries traverse only sensor-containing branches and own sensor/sensor
pairs by index. Output identities remain canonical and directional mask semantics
are unchanged. This distributes receiver work without dropping pairs or contact
features. Full spatial/shape/contact tests still check complete unordered populations.

Receiver reduction currently scans its linked overlap candidates for each distinct
Area rank: O(links × distinct Areas), without a fixed overlap cap. A marked
implementation comment identifies sorting receiver/rank pairs as the next option
if heavily nested fields dominate measured cost. The current measurements establish
one large Area, not a large nested-Area throughput limit.

## Storage, publication and failure

| Data | Current payload and lifetime |
| --- | --- |
| Area profile | 64 bytes per active reserved device record; CPU dictionary and sorted staging retain authored configuration only. Changed configuration uploads the active sorted table. |
| Resolved fields | 16 bytes per retained body on GPU: scaled gravity xy and final linear/angular damping; copied device-to-device on growth. |
| Membership scratch | 4-byte head + 4-byte Area lookup per retained body; two possible 8-byte links per contact point. Rebuilt on device, never downloaded. |
| Body / command | Unchanged 96 / 176 bytes; damping-mode and initialization bits use existing flags. |
| CPU body slot | 112 bytes, formerly 104, including authored damping modes. No resolved field/pose/velocity mirror. |
| Selected Snapshot | 64 bytes, formerly 48; the added vec4 publishes last resolved gravity/damping and an initialization flag for explicit state consumers. |
| Field status | One 8-byte upload/download and fence for the reduction batch, plus existing membership broad/narrow status. |

At 65,536 slots, core body/center/transient/target/field device payload is 9.5 MiB;
CPU body metadata/command payload is 18,874,368 bytes (18 MiB). Device command/request/
result scratch and transfer payloads are each 16 MiB plus status headers. Field
profiles and membership scratch are additional, with capacity retained after warmup.

Uniform directional defaults with zero default damping and no active Area reuse the
body pass, adding no field dispatch/fence/traffic. FieldMS/FieldWaitMS include current
membership preparation, definition upload and the reduction publication, not just
shader execution. The synchronous status fence exposes nonfinite reduction before
velocity integration; selected field values download only with explicit body reads.

Invalid numeric/enumeration/kind/thread/lifetime authoring rejects before changing
configuration. Uniform default gravity overflow rejects before GPU work and leaves
the store usable. Nonfinite device reduction leaves the store failed, with no CPU
replay. Area owners require static mode and sensor shapes; changing those roles
requires removing field behavior first. Disabled channels retain configuration and
identity but do not launch field reduction by themselves.

## Verification and measurement

`ELECTRON2D_TEST_GPU_FIELDS=1` runs
[GPUPhysicsFieldTests](../../tests/Electron2D.Tests/GPUPhysicsFieldTests.cs) and
existing public CPU PhysicsServerAreaFieldTests. The complete GPU suite includes
the new tests. Comparisons use public PhysicsServer/direct-state results, not
backend arrays or traversal order.

Cases cover five modes in independently shifted channels, priorities/ties, duplicate
Area shapes, 1/4/8 substeps, body damping Combine/Replace, signed coefficients and
gravity scale, omission, rotated point fields and world point fallback. They check
scoped waking, current masks/poses, zero-time semantics, growth, disposal/reuse,
invalid edits and failed device reduction. Boundary cases compare nearby polygons,
directed rays in both operand positions and intersecting concave sensor pieces
against public CPU behavior. Gravity/linear velocity comparisons allow 0.002
scene-unit/s² or scene-unit/s for the specified 0.1-second cases; angular velocity
allows 0.0002 rad/s, damping 0.0001 s⁻¹. Membership/identity is exact and simple
axis-aligned values use exact checks. No application callbacks or public GPU event
acceptance are implied by these internal tests.

The allocation/performance case uses 4,096 dynamic circle receivers plus one large
rectangular Area, all inside it. Receiver masks disable body/body contacts; every
receiver still has a sensor contact and integrates the field. Area gravity alternates
0/1 each tick. Duration is 1/120 s, one substep/four solver iterations, 128 warmup and
128 measured ticks. A full explicit read outside measurement verifies all 4,096
resolved vectors.

Linux/.NET 10.0.1, Vulkan, NVIDIA GeForce RTX 3090 Ti:

| Run | p50 | p95 | p99 | Mean total fence wait | Inclusive field work / field wait |
| --- | ---: | ---: | ---: | ---: | ---: |
| Before mixed-pair redistribution | 8.6192 ms | 9.4876 ms | 9.9858 ms | 8.3987 ms | 4.3140 / 4.1697 ms |
| After redistribution, focused suite | 0.6805 ms | 1.0841 ms | 1.4792 ms | 0.4536 ms | 0.2779 / 0.1830 ms |
| After redistribution, complete GPU suite | 0.6818 ms | 1.0530 ms | 1.6391 ms | 0.4623 ms | 0.2796 / 0.1873 ms |
| Final focused run | 0.6811 ms | 1.0595 ms | 1.6380 ms | 0.4591 ms | 0.2799 / 0.1871 ms |

Every measured run has zero owner-thread managed allocation, 128-byte upload and
64-byte status readback per tick, without body-state downloads. The 64-byte Area
profile changes each tick; status accounts for the remaining traffic. The workload,
population and observed field values are retained across the before/after runs.
This is a same-workload GPU implementation comparison, not a full CPU/GPU benchmark,
real-window FPS, native-allocation or foreign-platform certificate. Public GPU
selection, network snapshots/replay and end-to-end performance acceptance remain open.

### Executed checks

- Release runtime and test-project builds with
  `-p:Electron2DBuildNativeFromSource=true`: passed, zero warnings/errors.
- `ELECTRON2D_TEST_GPU_FIELDS=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`:
  passed, including the final uniform-default-overflow preflight case.
- `ELECTRON2D_TEST_GPU_PHYSICS=1 dotnet tests/Electron2D.Tests/bin/Release/net10.0/linux-x64/Electron2D.Tests.dll`:
  passed for the full resident/stage-hosted GPU suites and renderer/device lifetimes.
  This run preceded only the final preflight exception-classification/assertion edit;
  the focused suite was rerun afterward.
- `python3 -B tools/shaders/check_runtime.py` and `tools/coverage/check.sh`: passed.
- Runtime format verification excluding `src/Vendor`, and test format verification
  scoped to the four changed test files: passed.
- `python3 -B tools/wiki/test_generate.py`: passed. The local wiki candidate is
  generated from the Release assembly snapshot and XML, and checked separately;
  generated output is excluded from the engine commit.

Final Release SPIR-V SHA-256 (`obj/Release/net10.0/shaders/Physics/`):

| Artifact | SHA-256 |
| --- | --- |
| PhysicsResidentFields.comp.spv | `bac622921e81150326a1b602c42107c37470ea1b446bac36ec767e5eac158796` |
| PhysicsResidentBodies.comp.spv | `9361bfd895484a9e0b95bd0886137be7e8a94019f0a7211b8c2187aaad0e0c79` |
| PhysicsResidentContacts.comp.spv | `7293233fcb5f90b75ffdd2468bd3ebc12e8329174deb0fca804221cc66445f70` |
| PhysicsResidentShapes.comp.spv | `512b0c41c29f58748fba91fb0ada5fb36042b14531ea0a8a02ba3dbf922efd05` |
| PhysicsResidentSleep.comp.spv | `07f6dcf492610574ba4ffa68ab7e34beee7e07da86288029e73973001d90bee8` |
