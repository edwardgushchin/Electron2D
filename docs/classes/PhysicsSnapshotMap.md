# PhysicsSnapshotMap

Last updated: 2026-10-14

[Portable joint continuation](../components/physics-snapshots.md#portable-joint-continuation)
now verifies CPU/registered scene/raw sampled frames, silent correction, atomic
policy guards, local replay and real pin/groove/spring response. Warm complete-frame
transfers retain zero managed allocation; wider graphs and GPU/native gates remain open.

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

## Selected fields, exceptions and portable observation

[Portable body roles](../components/physics-snapshots.md#portable-body-roles) verify
CPU/CPU, CPU/GPU and CPU/registered round trips of scene/raw bodies through all four roles, retained
automatic/custom centers, point velocity, actual slot geometry and pending force/
torque consumption. Serial/separate full-frame transfer cycles retain strict zero
managed allocation; general geometry, contacts/joints and hardware/native gates remain
separate.

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

One-way continuation validates before encoding as well as before applying: different bound colliders, enabled shape slots, valid nonnegative pieces and unique unordered pairs. Invalid implementation output rejects capture before destination bytes change; a previous complete packet remains usable.
