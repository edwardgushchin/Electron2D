# Portable physics snapshots

Last updated: 2026-10-10

`PhysicsSnapshotMap` connects caller-assigned network identities to one live CPU or
GPU world. `PhysicsSnapshot` owns a bounded reusable byte encoding. Together they
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

## Encoding, budget and cost

Version 1 is explicit little-endian data with bounded counts, finite scalars, strict
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
