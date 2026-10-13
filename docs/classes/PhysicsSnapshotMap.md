# PhysicsSnapshotMap

Last updated: 2026-10-13

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
CPU/registered round trips of scene/raw bodies through all four roles, retained
automatic/custom centers, point velocity, actual slot geometry and pending force/
torque consumption. Serial/separate full-frame transfer cycles retain strict zero
managed allocation; general geometry, contacts/joints and GPU/native gates remain
separate.

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

One-way continuation validates before encoding as well as before applying: different bound colliders, enabled shape slots, valid nonnegative pieces and unique unordered pairs. Invalid implementation output rejects capture before destination bytes change; a previous complete packet remains usable.
