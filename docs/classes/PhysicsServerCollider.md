# PhysicsServerCollider

Last updated: 2026-10-08

**Declaration:** `internal sealed class PhysicsServerCollider` · **Inherits:** System.Object

**Source:** [PhysicsServerCollider.cs](../../src/Servers/Physics/PhysicsServerCollider.cs) · **Component:** [Physics server and queries](../components/physics-queries.md)

## Description and runtime flow

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
