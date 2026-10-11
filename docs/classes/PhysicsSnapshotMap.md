# PhysicsSnapshotMap

Last updated: 2026-10-11

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

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).
