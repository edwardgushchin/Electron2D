# Portable physics snapshots

Last updated: 2026-10-14

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
registered numerical owner and back. --gpu selects a real GPU recipient; --cpu
provides a CPU/CPU control. --threaded captures separate numerical execution on
both worlds. Actual selection and no startup fallback are asserted. Each world contains one scene RigidBody and one raw body
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
now passes all six CPU/CPU, CPU/GPU and CPU/registered serial/separate profiles.
GPU executes on CPU-only llvmpipe Vulkan without NVIDIA. This verifies the full
eight-case role fixture in both transfer directions, with its original operation
counts and tolerances. It does not establish arbitrary geometry, contacts/joints,
foreign/native, cross-process or hardware/performance acceptance. Current evidence
is retained under bin/physics-public-mass-roles/2026-10-14.

## Portable joint continuation

The public-only --portable-joints consumer transfers pin, groove and damped spring
constraints between CPU and selected CPU/GPU or the independent registered owner,
with both scene-owned and raw joint RIDs. --gpu/--cpu choose the second owner;
--threaded captures both numerical lanes before creation. Actual backend and no
startup fallback are asserted. Each world uses two circle-bearing scene bodies (one static,
one rigid). Creation order and initial rotations differ, so local attachment frames
start different while authored roles/policies match. Caller IDs 10/20/30 identify
the destination's own endpoints and joint. Physical pose and both sampled local
anchor/axis frames transfer without replacing joint identity or emitting transforms.

All six role/ownership cases reject a changed MaxForce profile before complete
state/events change, then accept the same unchanged packet after restoration.
Each destination creates a local checkpoint after correction and repeats a real
central impulse plus twelve .01-second frames. Pose/velocity repeat within .005
scene-unit or unit/s and rotation within .002 rad; this permits single precision
angle reconstruction without requiring independently solved trajectory equality.

The following real simulation checks finite bounded state, pin anchor error below
.3 scene unit, and groove lateral error/endpoint overshoot below .3 for a 20-unit
guide. These are bounded fixture constraints, not arbitrary numerical guarantees.
Frames compare copied origins/bases within .0001 and imported pose/velocity within
.001. Springs retain active elastic/damping motion and local replay; their off-center
point force/damping semantics are checked separately below.

Each serial/separate profile runs 128 warm plus 128 measured cycles for each of six
cases. Each bracket contains 3072 complete frames, 1536 encode/decode/apply transfers
and extra frame-validation captures. All six intervals require strict 0/0 owner/
all-thread managed bytes. No physics features or joint counts are reduced within
the bracket. This same-process circle fixture does not close arbitrary geometry,
world-pin/other recipes, larger joint graphs, hardware/native/cross-platform/network
or final performance acceptance. All six CPU/CPU, CPU/GPU and CPU/registered serial/
separate profiles now pass from the same frozen runtime. GPU uses CPU-only llvmpipe
Vulkan without NVIDIA; retained numerical threads are observed only in separate
profiles. The matching six --spring-anchors profiles retain their original analytic
force/moment/cap/damping bounds; that cold response matrix has no warm allocation
bracket. Current evidence is retained under bin/physics-public-portable-joints/2026-10-14.

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

### Character motion validation

Portable slide records are completed collisions, matching ordinary CharacterBody
movement. Decode requires distinct nonzero moving/collider IDs, nonnegative logical
indices and penetration depth, a true collided flag and ordered fractions satisfying
0 <= safe <= unsafe <= 1. Signed direct-body contact depth is a separate value and
remains supported. Rejected bytes leave the previous complete PhysicsSnapshot intact.

A nonempty character platform ID must resolve to a different bound body; Area,
self and unbound references reject before world state or events change. Capture
checks the same body/self rule in its measurement pass. The empty platform and
valid scene/raw body roles remain permitted. No wire fields/version or API changed.

The public-only --portable-motion fixture starts with a real circle wall slide and
surface platform velocity (0, -4). Eight malformed wire cases preserve the reusable
packet. Three incompatible platform IDs on CPU and selected CPU/GPU/registered
destinations preserve complete encoded state, events and input bytes. All six
serial/separate profiles pass. Each profile checks eight wire and six world-binding
rejections with original bounds, including a GPU receiver without fallback. A valid unchanged packet
applies silently, resolves the destination's own platform and remains point-queryable;
the following actual .01-second frame/movement carries the character upward. The
fixture permits .02 through .1 scene units around expected .04, distinguishing no
carry and large displacement while allowing the contact recovery response.

The reversed-fraction decode and self-platform apply failed before their fixes.
These software Vulkan checks do not establish arbitrary platform recipes, all
geometry/joints, hardware/native, cross-platform or performance acceptance. Existing warm contact
transport retains its strict zero-managed-allocation checks; malformed cold decoding
has no allocation-free claim.

### Same-count authoring validation

The public-only --portable-geometry consumer verifies seven incompatible authored
profiles with unchanged logical shape counts: active circle radius, disabled slot
primitive kind, character slot pose, distant slot enablement, removal/reappend
that reorders active/disabled slots, shared geometry solver bias and raw Area
geometry. CPU and selected CPU/GPU or the independent registered owner alternate
as source/destination. Six serial/separate profiles each retain 14 cases, seven
in each direction; actual backend and no startup fallback are asserted. The registered fixture only solves
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
wire-format change. Producer lifetime/index correspondence is checked separately below; wider object
associations, arbitrary active geometry, hardware/native, cross-process and performance
acceptance remain open. GPU runs on CPU-only llvmpipe Vulkan without NVIDIA. Structural
edits and this cold validation matrix have no zero-allocation claim.

### Capture after topology edits

Retained contacts, body/Area pairs and collided character slides now sample both
logical-slot lifetimes as well as their indices. Each scene/raw slot owns a small
private lifetime token, created only on structural addition. In-slot resource,
pose, disabled or policy edits retain the token. Removing and readding identical
geometry creates a new lifetime; moving a surviving slot to another index also
makes its old indexed history unsuitable for portable capture.

The capture measurement pass requires the original slot lifetimes at both sampled
indices before changing destination storage. Retired/reindexed history rejects
with InvalidOperationException and preserves the previous complete encoding.
Public sampled contact/slide values remain unchanged; capture does not discard
history or deliver events. Preparation may flush existing authoring edits and does
not roll them back. A real physics interval and character movement refresh the
retired histories so capture can succeed again.

The public-only --portable-capture fixture starts with real contacts/slides and
performs twelve removal/clear/reindex/readdition cases on CPU and selected
CPU/GPU/registered owners. Six serial/separate profiles retain 24 cases each,
12 per owner, with actual selection and no fallback asserted. The five original removals are joined
by remove/reappend of mover, peer, character and raw Area slots and an identical
active-slot removal/readdition. The unchanged-count CPU case failed before the
fix. Two additional local/remote contact cases disable the body monitor, so direct
contact validation rejects independently of observer-pair validation. Completed disabled-slot history and ordinary in-slot body/Area resource
replacement remain transferable in --portable-contacts.

Tokens hold no resource/owner reference and never enter wire format/schema data.
Apply seeds history with the destination's existing slot lifetimes; private replay
copies preserve them. Selected contact projections keep the initially sampled
tokens, avoiding resampling an old cached contact as current. Common CPU/GPU/
registered producers share this rule; both GPU capture and destination validation
now execute on CPU-only llvmpipe Vulkan. The separate warm contact-transfer fixture
retains strict 0/0 owner/all-thread managed-byte checks; these structural/cold
validation matrices have no zero-allocation claim.
Structural addition may allocate. Broader collider/object associations, topology
lifecycle, active geometry/joints, hardware/native/network and performance acceptance
remain separate gates. Current matrices are retained under
bin/physics-public-portable-topology/2026-10-14.

The public-only --portable-contacts consumer creates real CPU contacts, a character
slide and raw Area history, then transfers them to selected CPU/GPU or the
independent registered owner and back. --gpu/--cpu select the second owner;
--threaded captures both numerical lanes before world creation. Actual backend
and no fallback are asserted. Disabled slot zero precedes enabled slot one; the character hits
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
and collided character slides before restoring the world. Sixteen negative cases per profile
(eight fields on CPU and the selected receiver) compare complete before/after
encodings and observer counters. A completed history may refer to a disabled slot
that still exists; native fixture counts or current enabled state cannot replace
logical-index validation.

The 128-warm/128-measured complete contact/step/transfer/slide/query cycles require
0/0 owner/all-thread managed bytes in serial/separate modes. This bounded circle
fixture now passes all six CPU/CPU, CPU/GPU and CPU/registered serial/separate
profiles. Each measured bracket retains 640 complete frames, 256 transfers,
128 character movements and 256 actual point queries at 0/0 owner/all-thread bytes.
GPU processes load CPU-only llvmpipe without NVIDIA; CPU/registered load no graphics
driver, and retained numerical threads are observed only in separate profiles.
The corresponding six --portable-motion profiles also pass exact wire/platform
guards and real carried movement; that cold validation fixture has no warm
allocation bracket. These results do not establish general geometry, joint
correction, hardware/native/foreign/cross-process/network-performance acceptance.
Current evidence is retained under bin/physics-public-portable-history/2026-10-14.


[Software Vulkan validation](physics-backends.md#software-vulkan-functional-checks) now executes
the resident GPU modules, point-range/failure checks and common public physics suites
without physical-GPU load. A separate CPU authority/GPU prediction/late CPU client
run covers the existing impaired-network scenario. Hardware GPU execution, performance
and full-goal acceptance remain open; software timing is not hardware evidence.

## Authored sensor pose publication (2026-10-14)

Ordinary mixed-world completion no longer requests complete GPU snapshots for
scene Areas. Sensor poses are authored static inputs; their collider reuses the
existing pose cache across numerical epochs. Initial scene attachment and explicit
pose writes populate it. Selected device reads and restored snapshots replace its
values. Body caches retain the existing epoch policy, and explicit portable capture
still reads complete sensor state when needed. No new host solver or pose array is
introduced; resident geometry remains authoritative for actual sensing and queries.

PhysicsGPUDemandPublicationTests exercises 64 scene sensors, scene/raw bodies and
a raw sensor through 128 warm/128 measured edited full frames at strict 0/0 owner/
all-thread managed bytes. The completed observer readback drops by exactly 4,096
bytes per step, equal to 64 unused 64-byte sensor records. Public pose reads, moved/
rotated scene and raw sensing, raw detach/reattach, resident point queries, local
checkpoint and portable Area restoration retain identities and avoid duplicate
user effects. Serial/separate numerical profiles retain these same operations.
The fixture reports full-frame p50/p95/p99, uploads, readback, mean wait and submission
count. Its CPU-only Vulkan values are transfer/function evidence, not hardware
speedup, native allocator totals, real-window FPS or final performance acceptance.

GPU collider replay also retains the compact activity flag, validity and epoch. This is pre-step callback/force eligibility metadata, independent of full Snapshot and authored sensor pose caches. Restore keeps cache epochs aligned with restored space state; explicit edits invalidate activity before subsequent use.
