# Resident GPU one-way contacts

Last updated: 2026-10-09

## Boundary and side lifetime

GPUPhysicsBodyStore now implements the rigid-contact side policy of
[ADR 0065](../decisions/physics.md#adr-0065) independently of the CPU solver.
SetShapeOneWay/GetShapeOneWay retain enablement, normalized shape-local direction
and finite nonnegative recovery margin. Defaults are disabled, direction Down and
margin 1. Nonfinite direction/margin and negative margin reject before mutation;
large finite directions normalize with the existing double-precision helper.
A zero enabled direction accepts no contact side. Sensor response ignores these
settings. The direction follows both shape-local and body rotation.

The first generated contact normal selects the permitted side of each pair of
geometry pieces. That accepted or rejected decision remains stable while the
piece pair continues to produce contact geometry. Rejected episodes must be kept
even though they publish no solver points. OneWayPairCount reports both kinds.
Concave segments have separate piece identities, so one logical pair can contain
an accepted piece and a rejected piece simultaneously. If both shapes are
one-way, both side tests must accept.

Contact generation retains only observed episodes in the next device history.
Separation retires the decision; shape generation/revision and geometry revision
prevent reuse after deletion, replacement or edits. Body pose/velocity edits do
not alone erase the decision while the pieces still overlap, matching the
existing CPU contact lifetime. Explicit body exceptions or sensor conversion
remove the solid episode. Policy edits wake the owner through the existing shape
edit path; the device sleep graph handles prior neighbours.

Recovery margin has the same role as on CPU: it does not select a rigid-contact
side. [GPU body-motion/recovery queries](gpu-resident-motion-queries.md) now apply
the margin and permitted direction independently; the public scene/server world
adapter remains unfinished. This internal stage adds no public declarations or coverage
state upgrades, and does not establish a selectable complete GPU backend.

## Device pipeline and continuous collision

History is a double-buffered list of 48-byte records: shape IDs/generations,
piece IDs plus decision, and both shape/resource revisions. A retained hash table
indexes the last completed list. Each broad-phase-pair thread processes its pieces
in order, records the first side decision once per observed piece pair, and emits
only accepted physical contact points. Sensors retain their ordinary overlap
points. A following clear/build pair of dispatches publishes the next hash table.
The old history remains immutable until the entire batch fits and passes status
validation. Capacity overflow reports complete contact and episode counts; the
retry grows output storage and repeats geometry without advancing bodies.

Ray and full-shape CCD share the same history lookup and rotated-side test. A
rejected translational overlap between convex pieces can be skipped. Rotation
requires more care: a rejected episode can separate and later meet the solid side
within the same interval. Conservative sweep advancement therefore publishes its
separation boundary, beyond the current contact-generation margin, before a
later impact. The intervening manifold pass retires the old decision. Returning
only the later impact would incorrectly carry the rejected decision into it.
The existing iteration/interval limits still fail the world explicitly if a
sweep cannot converge; no unverified trajectory is committed through CPU replay.

| Storage or transfer | Cost and reason |
| --- | --- |
| Device shape | 80 bytes, formerly 64; includes one vec4 for normalized direction/margin. A sparse shape edit is 96 bytes. |
| Episode history | Two retained buffers, 48 bytes per record each; no CPU episode list or decisions. |
| Episode hash | Two power-of-two tables, 4 bytes per slot each, at least twice their corresponding record capacity. |
| Extra contact status | 8-byte reset and 8-byte count readback when the store contains enabled one-way shapes; shares the existing contact submission/fence. |
| Contact uniforms | 64 bytes per dispatch. One-way worlds additionally clear/build the next table in two dispatches. |
| CCD uniforms | 48 bytes, including history bounds and the last contact margin. Existing 8-byte fraction/status readback and fence are reused. |

Body state, contact geometry and side history remain on the device. Shape policy
uploads occur only on authoring edits; unchanged ticks upload status/settings
only. The small count readback permits complete capacity recovery and publishes
the new history bounds. No extra one-way-specific fence is introduced. Empty
history buffers reserve 64 records and 128 hash slots on first contact/CCD use;
their combined payload plus status is 7,176 bytes. Buffer/driver overhead and
native allocations are not inferred from these payload counts.

## Verification

`ELECTRON2D_TEST_GPU_RESIDENT_ONE_WAY=1` runs
[GPUPhysicsOneWayTests](../../tests/Electron2D.Tests/GPUPhysicsOneWayTests.cs)
and the existing public CPU OneWayCollisionTests. The full GPU runner includes
the new resident suite. The checks exercise default/normalized/zero directions,
invalid rollback, owner thread, accepted and rejected latches, separation,
shape reuse, live policy/geometry changes, concave piece independence, sensors,
explicit exceptions and all three platform roles. Child and body rotation are
checked separately. A moving kinematic platform must push its supported rider.

CCD checks use speed 3,000 units/s for 0.01 seconds against a 0.2-unit platform:
pass-side travel ends at -20 ± 0.002 units, while a solid-side approach stops above
the platform with speed below 0.1. A half-turning beam additionally starts in a
rejected overlap, separates, then strikes from its solid side in the same step.
The focused run recorded 17 CCD boundaries at zero contact margin and 15 at
the standard margin 2, with a nonzero physical response in both cases. The final
review caught and corrected a missing contact-margin value in the CCD uniform;
these two cases verify that the actual configured margin is used. Direction normalization tolerance is 1e-6; identities, filtered counts
and mutation rollback are exact. Discrete 40-unit/s approach checks use 60 frames
at 1/120 second and bound the final radius-one center between 0.5 and 1.5 units.

The residency case has 8,192 bodies and 4,096 independent one-way piece episodes,
half accepted and half rejected. It checks all 2,048 published manifolds and all
4,096 retained episodes after capacity recovery and measurement. After 128 warmup
ticks, 128 measured ticks (one substep, four iterations, zero gravity/correction)
allocate zero owner-thread managed bytes and transfer 48 bytes in each direction
per tick, excluding uniforms. No policy/body-state/contact/history array is
transferred during those ticks.

The focused Vulkan / NVIDIA GeForce RTX 3090 Ti run recorded p50 0.5611 ms,
p95 1.0228 ms, p99 1.8709 ms and mean fence wait 0.3400 ms. Evidence is in
`/tmp/electron2d-resident-oneway-focused2.log`. This is an internal regression
workload on a shared Linux/.NET 10 desktop, not a full public CPU/GPU comparison,
window FPS, native allocator or network acceptance result.


The final focused run, including second-shape and dual one-way policy checks,
passed with public CPU OneWayCollisionTests; evidence is in
`/tmp/electron2d-resident-oneway-focused3.log`. Portable replay checkpoints will
need to preserve side episodes alongside ordinary contact/joint state; no capture,
restore or network API for that state is exposed by this change.

Source-generated shader SHA-256:

| Resource | SHA-256 |
| --- | --- |
| PhysicsResidentContacts.comp.spv | `f09ebe216918cf704c17ac27fcc83d0528f9626c4875c99d82bdd13a09132bda` |
| PhysicsResidentCCD.comp.spv | `80f4f1862bae3204dbbe197763b63392af516b5d11a28bab75618cef7f2e47dc` |

Temporary logs, generated bytecode and generated wiki files are not committed.


Final checks on 2026-10-09 passed: Release runtime/test source-native builds,
focused CPU/GPU one-way runner, complete GPU runner including both renderer
compute lifetimes, runtime/touched-test formatting, shader delivery, compiled API
coverage and wiki tests/generation/check. The final full GPU log is
`/tmp/electron2d-resident-oneway-final-gpu.log`. Coverage remains at 11,348 accounted
declarations with zero unmapped; no public API state is advanced by this internal
stage. Platform execution is Linux/Vulkan only. Public-world selection, callbacks,
queries, portable state/replay and full performance/network acceptance remain open.
