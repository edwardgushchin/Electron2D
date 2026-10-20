# PhysicsSnapshotMap

[Networked crossed capsules and CPU wake preparation](../components/physics-network-example.md#crossed-capsule-correction-and-cpu-wake-preparation-2026-10-20) extend authoritative correction with complete angular/linear contact state and prepare joint storage before later replayed wake.

[Crossed capsule response](../components/physics-contact-policy.md#crossed-capsule-response-and-portable-contact-state) verifies complete paired impulses, off-centre rotation and silent portable contact state across scene/server bodies and selected numerical owners.

Last updated: 2026-10-20

[Publication recovery](../components/physics-registered-adapter.md#failed-motion-publication) distinguishes local rewind from portable preparation. Successful local restore discards superseded motion/pose/mode publication without invoking the failed request. Portable apply first prepares pre-existing local authoring; a preparation failure leaves incoming state unapplied and the tick unchanged, and a later successful apply supplies authoritative state.

[Portable joint continuation](../components/physics-snapshots.md#portable-joint-continuation)
now verifies CPU/CPU, CPU/GPU and CPU/registered scene/raw sampled frames, silent correction, atomic
policy guards, local replay and real pin/groove/spring response. Warm complete-frame
transfers retain zero managed allocation; wider graphs and hardware/native gates remain open.

[Character motion validation](../components/physics-snapshots.md#character-motion-validation)
rejects impossible slide encodings and self/Area/unbound platform references before
packet or world mutation. CPU/registered serial/separate cases preserve previous
state/events and verify real movement after valid platform correction.

[Retained slot history](../components/physics-snapshots.md#capture-after-topology-edits)
keeps sampled public values while rejecting portable capture of retired/reindexed
slots before replacing packet bytes. In-slot replacement and disabled history stay
valid; portable apply remaps private slot lifetimes to destination authoring.

[Same-count authoring validation](../components/physics-snapshots.md#same-count-authoring-validation)
verifies seven geometry/slot-policy mismatches before state or event mutation.
Restoring compatible geometry with independent local resource identities permits
the unchanged packet to apply and continue real contact/slide history.

[Capture after topology edits](../components/physics-snapshots.md#capture-after-topology-edits)
validates retained contact/overlap/slide links before replacing destination bytes.
An authoring edit that removes a referenced logical slot rejects capture while
leaving the previous complete packet available. Refreshing affected histories by
ordinary physics/movement allows recapture; authoring edits themselves are retained.

[Contact and motion history](../components/physics-snapshots.md#portable-contact-and-motion-history)
now validates logical slot ranges before apply for contacts, body/Area pairs and
character slides, while retaining completed history of still-existing disabled
slots. Version 3 carries prepared character query state separately from its solver
pose; local/portable capture supports pending movement without another step.

**Declaration:** `public sealed partial class PhysicsSnapshotMap : IDisposable`

**Source:** [PhysicsSnapshotMap.cs](../../src/Servers/Physics/PhysicsSnapshotMap.cs)

**Component:** [Portable physics snapshots](../components/physics-snapshots.md)

Owner-thread identity bindings and reusable capture/apply scratch for one borrowed
world. Complete collider/joint bindings and matching physical authoring are required.

[Temporary reference lifetime](../components/physics-snapshots.md#temporary-reference-lifetime)
clears pose and body scratch after successful or rejected work, including partial
attachment validation. Unbinding releases its replay entry while reusable scratch
capacity remains available. A shapeless GPU world can apply a packet before its
first simulation interval.

Preparation also releases shape references beyond a reduced current topology,
including when a later schema/budget guard rejects. A live binding keeps only its
current captured shape prefix; the storage suite holds it across collection and
then exercises prepared reuse.

| Member | Contract |
| --- | --- |
| `PhysicsSnapshotMap(RID space)` | Bind to one live scene-owned or server-owned space. |
| `IsDisposed` | True after map or source-world disposal. |
| `Bind(ulong networkID, uint generation, RID objectRID)` | Add a unique nonzero identity/incarnation for an attached collider or joint. |
| `Unbind(ulong networkID)` | Remove only a binding; return whether it existed. |
| `Capture(PhysicsSnapshot)` | Synchronize authoring and capture state/tick without stepping. |
| `Apply(PhysicsSnapshot)` | Validate incoming data, silently correct physical/observer state and rewind the world tick. |
| `Dispose()` | Idempotently release bindings/scratch, leaving borrowed objects alive. |

Mappings do not replicate geometry or lifecycle, select a backend, authenticate
senders or decide whether a received tick is stale. CPU/GPU backends and local
creation order may differ; scene/server roles and physical authoring must match.
A validation failure preserves a usable world; a failure after mutation starts
requires world disposal. See the component for exact state and verification limits.

[CPU-only network profiles](../components/physics-network-example.md#cpu-only-and-separate-numerical-profiles)
exercise portable application and numbered-input replay across three actual
processes with serial/separate numerical execution. Lifecycle recipes and
exact-once confirmed effects remain the application's policy; these checks retain
GPU, native and broader platform/performance limits.

## Pending server geometry

Portable compatibility records the pending kind of an unconfigured raw shape. Pending-to-configured transitions change authored compatibility and older snapshots reject before state writes. No encoded field or hash for live configured geometry changes; pending worlds can capture/apply while their logical slots remain inactive.

## Selected fields, exceptions and portable observation

[Portable body roles](../components/physics-snapshots.md#portable-body-roles) verify
CPU/CPU, CPU/GPU and CPU/registered round trips of scene/raw bodies through all four roles, retained
automatic/custom centers, point velocity, actual slot geometry and pending force/
torque consumption. Serial/separate full-frame transfer cycles retain strict zero
managed allocation; general geometry, contacts/joints and hardware/native gates remain
separate.

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

One-way continuation validates before encoding as well as before applying: different bound colliders, enabled shape slots, valid nonnegative pieces and unique unordered pairs. Invalid implementation output rejects capture before destination bytes change; a previous complete packet remains usable.

[Portable contact and motion history](../components/physics-snapshots.md#portable-contact-and-motion-history)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Imported points/impulses and character slides retain destination peer/slot identity;
raw Area/contact history stays silent during correction and avoids repeated enters.
Malformed indices/platforms preserve state and events, and real movement retains
platform carry. Warm contact cycles require 0/0 bytes; cold motion validation has
no allocation claim. Hardware and wider geometry/joint/network gates remain open.

[Capture topology](../components/physics-snapshots.md#capture-after-topology-edits)
and [same-count authoring validation](../components/physics-snapshots.md#same-count-authoring-validation)
now pass CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Each capture profile retains 24 removal/reindex/readdition cases; each geometry
profile retains 14 incompatible/restored cases across both directions. Rejection
preserves complete packets, sampled values and events; real refresh/continuation
recovers. These cold structural checks have no allocation claim. Hardware and
broader geometry/joint/network acceptance remain open.

[Portable joint continuation](../components/physics-snapshots.md#portable-joint-continuation)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Scene/raw pin/groove/spring frames, atomic policy rejection, private replay and
continued constraints retain their bounds; all six warm cases require 0/0 bytes.
[Off-center spring checks](../components/physics-registered-adapter.md#off-center-spring-anchors-and-portable-joints)
also retain force/moment/cap and stable damping bounds. GPU executes on llvmpipe;
hardware, broader geometry/graphs and network acceptance remain open.


[Network joint continuation](../components/physics-network-example.md#three-joint-families-across-network-correction-2026-10-14)
checks every pin/groove/spring authority/prediction/replay tick after impaired
cross-backend portable correction. Coverage states retain their existing scope;
full hardware/platform/network/performance acceptance remains open.
