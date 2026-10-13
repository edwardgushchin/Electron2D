# PhysicsServerCollider

Last updated: 2026-10-13

[Retained slot history](../components/physics-snapshots.md#capture-after-topology-edits)
keeps sampled public values while rejecting portable capture of retired/reindexed
slots before replacing packet bytes. In-slot replacement and disabled history stay
valid; portable apply remaps private slot lifetimes to destination authoring.

**Declaration:** `internal sealed partial class PhysicsServerCollider` · **Inherits:** System.Object

**Source:** [PhysicsServerCollider.cs](../../src/Servers/Physics/PhysicsServerCollider.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

Body/fixture IDs and current-space ownership now reside in [PhysicsColliderBackend](PhysicsColliderBackend.md). The server collider retains authored shape slots, body mode, motion/field policy and public RID routing. Scene bodies and Areas use the same creation/rebuild/destruction implementation.

Attachment now supplies PhysicsBodyConfiguration and scene-unit pose to the shared
adapter. RigidLinear starts with its rotation lock; stored kinematic/static virtual
velocities and delayed targets retain their existing application policy.
Live pose, contact velocity, sleep and target-motion access now use the same
engine-valued adapter as scene bodies; the server collider retains detached
configuration and pending-target policy.

Attached dynamic/kinematic GetTransform reads the adapter's full physical pose.
GPU returns its current qualified resident basis without angle reconstruction;
CPU preserves its published-angle convention. Authored policy invalidation and
the public CPU/GPU live-edit checks are described in
[host preparation](../components/gpu-host-preparation.md).

SpaceRID is projected from the actual backend attachment rather than retained separately. Scene and raw space operations now use the same guarded transition; partial target attachments are retired before restoration. [Common space bindings](../components/physics-registered-adapter.md#common-space-bindings) covers failure and lifetime behavior.

## Description and runtime flow

The shared collider carries authored identity, slots and selected operations;
CPU body/shape handles belong exclusively to the actual CPU owner/attachment.
Native probes stay in the owning implementation and tests.

Caller-owned body/Area configuration with indexed shape entry, local pose, disabled and body one-way fields. Getter/replacement/clear operate on logical slots; native tags retain logical indices across compound fixtures. Same-value writes are inert, real edits rebuild the one native world body and mass profile. Typed scene slots use their existing CollisionObject owner store instead of this class.

Consumer examples and complete public operations are on [PhysicsServer](PhysicsServer.md#shape-slots). Scene group and raw body/Area slots share native geometry, queries, one-way pre-solve/motion, and mass calculation. Shared shape mutation/free checks all related active worlds before changes. Structural work may allocate; indexed reads and unchanged settings reuse state.

## Internal geometry operations

| State/operation | Contract |
| --- | --- |
| `PhysicsServerCollider(RID rid, bool isArea)` | Detached indexed body or sensor configuration. |
| RID, space, native body/fixture IDs and motion/filter fields | Shared native owner, logical shape tags and collider policy. |
| ShapeSlot resource/pose/disabled/one-way/margin/direction | One resource entry per logical index, independent of compound pieces. |
| `AddShape`, `GetShape`, `GetShapeTransform`, `SetShape`, `SetShapeTransform`, `SetShapeDisabled`, `SetShapeOneWay` | Indexed geometry/policy; malformed input rejects before replacement. |
| `RemoveShapeAt`, `RemoveShape`, `ClearShapes`, `UsesShape` | Reindex/removal and shared shape lifetime membership. |
| `ReplaceSlot` | Same-value no-op or native rebuild with stored-slot rollback on failure. |
| `PrepareBackend`, `RebuildShapes`, `AttachBackend`, `DetachBackend` | Apply pending geometry/material/mass and manage one native attachment generation. |
| `AppendMassGeometry` | Effective active slot primitives and poses for mass resolution. |

## Lifetime and verification

PhysicsServer owns the registry entry; colliders borrow shapes and do not release them. Owned geometry cannot be disposed outside its RID owner. Shape data replacement preserves identity and retires a held view; free removes users and unregisters even after a disposal observer failure. World removal retains collider configuration. The cached body runtime retains its collider owner; RID release marks that runtime released before removing its registry entry, so retained direct views still reject access. [PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies scene/server geometry, policy, reindexing, ownership and native response with zero managed bytes over 64 warmed read/unchanged-write/solver frames on Linux/.NET 10. Native allocation, structural-edit budgets, other platforms and owner acceptance remain unverified. [ADR 0088](../decisions/physics-shape-slots.md#adr-0088) owns this integration.

Static linear/angular velocity now feeds the shared stationary-surface channel.
Attachment restores it; detach preserves configuration. A switch to Static or
Kinematic clears velocity, while a switch to Rigid transfers configured velocity
to real motion. PhysicsSurfaceVelocityTests checks the state and point-query path.

## Body state and kinematic targets

Linear/angular getters return live combined contact velocity while attached.
Static/kinematic assignments configure virtual surface motion, separate from pose
integration. The first kinematic transform initializes the pose; later writes keep
only the latest target until a nonzero active step. PrepareMotion derives actual
travel velocity; CompleteMotion consumes the target after solving. Idle motion is
zeroed without reconstructing a decoded angle. Detach preserves a pending target.
Explicit dynamic sleep clears velocity and survives detach/reentry alongside
CanSleep; disabling automatic sleep wakes. PhysicsServerStateTests covers these
paths on CPU/GPU, including real path contacts and zero warmed managed allocation.

After an internally enabled GPU backend fails, body/area/joint teardown preserves
owner/stepping guards but skips individual raw graph destruction and partial-motion
capture. Managed bindings/views are released; the failed space reclaims raw storage
in bulk. Queries and further simulation remain rejected. See the
[GPU island graph failure contract](../components/gpu-physics.md#gpu-contact-driven-island-graph-2026-10-08).

[Directional filter mutation](../components/physics-filters.md) shares scene/server/tile metadata and query rules across CPU/GPU; it updates existing fixtures, preserving shape/resource identity. Body assignments wake the body and contact neighbors, including unchanged bits; Area snapshots update on the next step.

The query-result boundary also reads each raw logical slot's disabled state without exposing native fixtures. Direct-space extension results must identify a current active slot in the queried world and satisfy its sampled filters. See [typed extension validation](../components/physics-space-extensions.md).

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.


Raw collider release/transfer preflight checks the live backend world reference, including the interval before Attach completes. A borrowed implementation hook cannot retire or retarget that partial attachment; [identity tests](../components/physics-registered-adapter.md#initial-collider-metadata) verify body and Area cleanup and retry.


Releasing prevents rebind or recursive free from departure callbacks. Detach attempts motion capture and numerical retirement independently; errors cannot skip the latter. Shared shape removal marks retained authoring dirty before preparation, so a failed numerical update retries at the next query/step.


EnsureAuthoringAccess retains release and geometry-borrow checks without treating filter metadata as a hierarchy change. EnsureGeometryLifetimeAccess additionally rejects borrowed extension hierarchy mutation; logical body filter setters use the common body write guard and this authoring preflight. Ordinary body-state forwarding remains available, with numerical hooks rejected before mutation.
