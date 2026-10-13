# Portable physics snapshots

Last updated: 2026-10-13

`PhysicsSnapshotMap` connects caller-assigned network identities to one live CPU,
GPU or registered world. `PhysicsSnapshot` owns a bounded reusable byte encoding. Together they
capture and apply authoritative physical state across different worlds, object
creation orders and backends. The [local checkpoint](physics-space-checkpoints.md)
remains the separate exact-history mechanism for local prediction/replay.

## Public use and ownership

Bind every attached collision object and joint with a shared nonzero `ulong` ID and
nonzero `uint` incarnation generation. A map borrows the world and objects; disposing
the world invalidates its maps. First disposal and all operations require the world
owner thread between completed intervals, after physical callbacks have returned.
Snapshots also require their creating thread, but own their bytes independently of
the world. Explicit server spaces advance through `PhysicsServer.SpaceStep`. A
viewport world containing only server colliders must be bound with `FindWorld()`
before expecting its SceneTree to step it. A packet passed to `ReadFrom` is borrowed only during that call.

```csharp
using var identities = new PhysicsSnapshotMap(world.Space);
identities.Bind(100, 1, body.GetRID());
// Bind every other body, Area and joint, using the same IDs on the receiving peer.
var state = new PhysicsSnapshot();
identities.Capture(state);
byte[] packet = new byte[state.GetEncodedSize()]; // Retain/reuse packet storage.
state.WriteTo(packet);
// Send packet through the application's authenticated transport.
var received = new PhysicsSnapshot();
received.ReadFrom(packet);
receiverIdentities.Apply(received);
```

The local RID is never encoded as network identity. Incarnations prevent applying
an old packet to a reused ID when the caller advances the generation. `Unbind`
removes only the association; it never despawns a physical object. Capture/apply
require complete bindings and unchanged attachment lifetimes. Rebind after a local
attachment replacement. Authentication, packet ordering and authority are caller
responsibilities: Apply deliberately allows older ticks for reconciliation.

## Contents and compatibility

A packet carries the source tick, last step duration, compatible authoring signature,
physical poses, velocities, quiet sleep clocks and sleeping/can-sleep state. It also
retains persistent/pending forces, resolved fields, surface velocities, animatable
and server kinematic targets, character platform/slide/floor state, current contact
reports and contact/Area observer history. References inside these records use the
bound network IDs and are resolved to local identities on apply. Joint local frames
are validated and applied in place; authored solver parameters must match. One-way
side decisions survive correction.

Geometry, body roles and physical authoring must already match. This includes
scene/server representation roles, layers/masks, material values, CCD, mass/damping,
monitoring, shapes, exceptions, joint settings and world solver/field policy. Geometry
resources, callbacks, local instance IDs, local RIDs and local attachment revisions
are not serialized. A server can use a headless SceneTree when its clients use scene
bodies; a server-owned body does not currently map to a scene-owned RigidBody.
A change of authoring requires the corresponding change on the receiver before apply.
The signature detects mismatched authoring; it is not a cryptographic authenticator.

Capture synchronizes pending authoring without advancing the tick. Apply parses into
retained scratch, validates identities/generations/authoring/references, and only then
writes incoming physical values. Preparing the destination can flush its own pending
authoring, as normal queries do. A rejected packet leaves the world usable. An
execution/device error after mutation begins makes the world unusable until disposal;
there is no hidden CPU replay or rollback of a failed GPU submission.

Apply emits no gameplay or transform callbacks, resets presentation interpolation,
and preserves existing direct-body view identities. Imported contacts are available
before the receiving world's first step, including when a view did not previously
exist. Subsequent steps continue observer histories without duplicating enters;
normal future events still run. Scene clocks, timers, scripts, input ownership and
custom game state are outside this packet.

The packet replaces private warm-start caches with destination-local reconstruction.
CPU suppresses warm start for the first following interval; GPU rebuilds contacts,
joint caches and sleep connectivity without advancing sleep clocks or waking bodies.
Large convex decomposition is private: the entire convex shape shares one one-way
episode across its CPU fixture pieces. Native solver rotations are transferred as
basis vectors to avoid repeated angle-conversion drift. Cross-backend or cross-platform
bitwise continuation is not promised. Capture a new local checkpoint after correction: replacing sampled joint frames
can invalidate older points under their fixed-configuration contract. Local
checkpoints retain the full backend-private history for subsequent replay.

## Portable body roles

The public-only consumer --portable-roles transfers CPU state to its independent
registered numerical owner and back. --threaded captures separate numerical
execution on both worlds. Each world contains one scene RigidBody and one raw body
with two circle slots at (8, 0)/(-2, 0); local creation order differs. Caller IDs
10/20 with generation 1 resolve to each world's own RIDs. The eight configurations
combine Rigid, Static, Kinematic and RigidLinear with automatic center (3, 0) or
custom center (1, -2). Authored geometry/roles match; corrected poses and velocities
start deliberately different.

Every case checks capture/encode/decode/apply, the imported world tick, actual
inverse mass/inertia, global/local center offsets, origin-relative point velocity
and both logical-slot point queries. It advances real complete .01-second frames
before/after correction. Dynamic roles receive a pending (4, -2) force and torque 6
with mass 2; the following interval must change velocity by (.02, -.01) and angular
velocity by torque times inverse inertia times .01. Reverse transfer then another
frame must not revive the consumed interval. An incompatible mass profile rejects
before changing the destination pose/tick, and restoring compatible authoring
allows the same packet to apply.

The 128-warm/128-measured bracket includes 5120 complete frames, 2048 portable
transfers and 32768 actual point queries per bracket across the two worlds. Both
serial/separate profiles require strict 0/0 owner/all-thread managed bytes. Motion/
center tolerance is .002 scene unit, unit/s or radians; inverse values and force
increments use .0001 in their respective units. These bounds accommodate single
precision pose/angle conversion while distinguishing the full force increment
and its accidental repetition. This same-process, collision-free circle fixture
does not establish arbitrary geometry, contacts/joints, foreign/native or GPU
transfer/replay/performance acceptance. Evidence is retained under
bin/physics-portable-roles/2026-10-13.

## Encoding, budget and cost

Version 3 is explicit little-endian data with bounded counts, finite scalars, strict
booleans and exact-length validation. Unknown versions, truncated/trailing data and
over-budget packets reject before replacing a snapshot's previous complete payload.
`maxBytes` is immutable, from 48 bytes through 64 MiB; the default is 64 MiB. It bounds
one encoded payload, not total world/history/transport memory. Buffers grow as needed
and retain their peak capacity. Use an application-appropriate lower packet budget.

Capture on GPU explicitly reads current body state in one batch, because coalesced
presentation changes omit quiet-clock-only changes. It also reads the bounded one-way
history. Apply does not read the old client poses; it uploads incoming body state and
one-way decisions, rebuilds device structures and publishes resulting state/statistics
for the common public views. Those transfers and completed-submission waits are real
network publication costs, not ordinary-step performance.

`PhysicsSnapshotTests` exercises CPU/CPU, CPU/GPU, GPU/CPU and GPU/GPU transfers with
reordered local creation, contacts, scene/server overlaps, partially elapsed sleep
clocks, sleeping, forces, kinematic targets,
character state, rotated pin/groove/spring joints and denied one-way episodes, including
large convex decomposition, indexed concave pieces and CCD. Validation includes every truncated prefix, unknown version, nonfinite fields,
byte budgets, wrong IDs/generations/authoring and duplicate one-way history. The suite
also verifies a real GPU arithmetic failure during apply, failed-world rejection
and disposal, and measures warmed capture/encode/decode/apply allocations and latency.

Full separate-process authoritative play, transport integration, input acknowledgement,
lifecycle/late join, confirmed-event reconciliation, remote interpolation and adverse
network tests remain open in the [contract audit](physics-contract-audit.md). This
portable state primitive does not close those integration requirements. No new window
FPS, cross-platform or owner acceptance is claimed here.

## Measured publication cost

Linux x64, Ryzen 7 5700X, RTX 3090 Ti (driver 615.71.09), .NET SDK 10.0.101,
Release runtime based on `63676f19` plus this slice. Unrelated CPU numerical
work was active on the host: these are observed diagnostic latencies, not isolated
throughput acceptance or a claim of a GPU solver speedup. The CPU and GPU initial
worlds use the same 1,024 active circles, floor and 90 settling ticks; their resulting
contact reports differ, which explains the different packet lengths. CPU→GPU uses
the CPU authority's packet. There is no simulation step inside the measured cycle.
After 96 capture/write/read/apply warmups, 64 cycles measured:

| Capture → apply | Payload bytes | p50 / p95 / p99 ms | GPU upload / readback bytes per cycle | GPU wait ms / submissions per cycle | Managed owner / all threads |
| --- | ---: | --- | --- | --- | --- |
| CPU → CPU | 460,844 | 6.6087 / 8.9932 / 10.9979 | 0 / 0 | 0 / 0 | 0 / 0 B |
| GPU → GPU | 486,236 | 8.4249 / 10.9632 / 12.2064 | 196,840 / 147,656 | 0.6458 / 8 | 0 / 0 B |
| CPU → GPU | 460,844 | 7.2703 / 8.2734 / 8.9428 | 180,432 / 82,048 | 0.4630 / 7 | 0 / 0 B |

Commands: `ELECTRON2D_TEST_PHYSICS_SNAPSHOT=gpu ELECTRON2D_SNAPSHOT_BODIES=1024
dotnet run --project tests/Electron2D.Tests -c Release`. The source/receiver each own
one retained snapshot, plus a reusable transport byte array; each encoded snapshot
here requires at least its listed payload size. Map scratch and world/device storage
are additional memory. A bounded multi-tick history is not implemented by these two
classes. A full CPU packet at 60 updates/s would already carry 27,650,640 payload
bytes/s before transport framing; the future network controller must explicitly
budget publication frequency and history rather than assuming free per-frame sync.

## Selected preparation and observation

The map validates bindings and sends a borrowed collider span to the selected world. GPU handles/state batch scratch belongs to that owner; common map/entry state uses engine fields and identities. Preparation and batch reads borrow context, so failure permits recapture without membership loss. The exact bound GPU object set is still read explicitly for authoritative state; ordinary unobserved steps retain demand-driven publication. Selected ownership preserves existing field/schema meanings; the current wire version is described above. See [ownership and verification](physics-backends.md#selected-observation-and-collision-policy).

One-way continuation validates during capture and apply. Self/unbound identities, disabled or unavailable logical slots, invalid pieces and duplicate unordered pairs reject before packet publication or world mutation. A failed capture preserves any previous complete encoding. Registered providers receive a span bounded by the current declared count, even when retained capacity is larger; the external consumer verifies errors, alternate local IDs and numerical continuation.

Version 3 retains the scene server-mode state introduced in version 2 and adds a
four-byte strict query-pose boolean per CharacterBody. A character's current query
pose and separately retained solver pose already have distinct records; the flag
preserves how the next interval consumes its pending movement without inferring
state from rounded transforms. Versions 1 and 2 reject before replacing a stored
payload; both endpoints must use the same supported version.

## Portable contact and motion history

### Same-count authoring validation

The public-only --portable-geometry consumer verifies seven incompatible authored
profiles with unchanged logical shape counts: active circle radius, disabled slot
primitive kind, character slot pose, distant slot enablement, removal/reappend
that reorders active/disabled slots, shared geometry solver bias and raw Area
geometry. CPU and the independent registered owner alternate as source/destination;
serial/separate execution covers 28 cases. The registered fixture only solves
circles/annuli, so the rectangle-kind case deliberately uses a disabled slot.

Each source supplies real contact impulse, body/raw Area overlap and character
slide history. Apply rejects incompatible authoring with InvalidDataException;
complete destination bytes, history/transform events and the input packet remain
unchanged. Restoring the full profile, including inactive one-way margin/direction,
allows that same packet to apply silently. Independent local shape RIDs match by
geometry data, not resource identity. Actual mover/character point queries, a real
following interval without repeated enters, and ordinary separation exits verify
continued physical use. Stored impulse values copy exactly; subsequent solver
continuation does not require bitwise equality.

These checks exercise existing authoring-schema guards, with no runtime/API or
wire-format change. They do not establish producer history correspondence after
same-count edits, geometrically indistinguishable slot/owner replacement, arbitrary
active geometry, GPU/native, cross-process or performance acceptance. Structural
edits and this cold validation matrix have no zero-allocation claim.

### Capture after topology edits

Capture now uses the same logical-range predicate as apply for contact, body/Area
observer and collided character-slide references. Its measurement pass validates
both endpoints and the moving-body identity before destination storage is changed.
After an authoring edit removes a referenced slot, capture rejects with
InvalidOperationException and preserves the previous complete encoding. Preparation
may still flush the caller's existing authoring edits; it does not undo that edit.

The public-only --portable-capture fixture begins with real contacts and character
slides, then removes the mover's slot, the peer's distant slot, the character's
slot, the raw Area's slot or all peer slots. Five cases on CPU and registered owners
pass in serial/separate modes. They verify rejection, preserved bytes and no history
events during capture. A real physics interval and character movement refresh the
retired histories; capture then succeeds again. Existing --portable-contacts warm
cycles continue to require 0/0 owner/all-thread bytes with the added preflight.
Same-count replacement/reindex correspondence, wider topology, GPU/native and full
network/performance acceptance remain separate gates.

The public-only --portable-contacts consumer creates real CPU contacts, a character
slide and raw Area history, then transfers them to the independent registered
owner and back. Disabled slot zero precedes enabled slot one; the character hits
the same peer's distant slot two. Completed normal/tangent impulses agree with
actual mass-times-velocity change within .1 kg-scene-unit/s. Immediate imported
points and impulses retain their encoded values while peer RIDs, objects and
instance IDs resolve to the destination. Apply stays silent for contact, Area and
transform observers; the next real interval does not repeat enters, and separation
still emits ordinary exits.

The initial query-prepared character capture failed despite retaining its solver
and query state. Local replay now captures that mutable state, and portable version
3 explicitly restores it. The fixture restores a local checkpoint after movement
and tests actual point-query geometry at the corrected character pose before the
next simulation interval. A nonboolean query-state field rejects while preserving
the previous complete packet.

Another initial packet with local contact index 999 was accepted. Apply now checks
local and remote logical slot ranges for direct contacts, body/Area observer pairs
and collided character slides before restoring the world. Sixteen negative cases
(eight fields on CPU and registered receivers) compare complete before/after
encodings and observer counters. A completed history may refer to a disabled slot
that still exists; native fixture counts or current enabled state cannot replace
logical-index validation.

The 128-warm/128-measured complete contact/step/transfer/slide/query cycles require
0/0 owner/all-thread managed bytes in serial/separate modes. This bounded circle
fixture does not establish general geometry, joint correction or current GPU/
native/foreign/network-performance acceptance. Evidence is retained under
bin/physics-portable-contacts/2026-10-13.
