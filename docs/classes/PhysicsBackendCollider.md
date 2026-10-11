# PhysicsBackendCollider

Last updated: 2026-10-11

**Declaration:** `public abstract class PhysicsBackendCollider` · **Source:** [PhysicsBackendCollider.cs](../../src/Servers/Physics/PhysicsBackendCollider.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Defines one selected body/Area numerical attachment. Creation is claimed exactly once. Initial and integration policy, force intent, logical shape spans, fields, monitoring policy, mass, motion, surfaces, contact output and portable motion carry values without scene/server runtime storage.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

## Protected implementation hooks

| Declaration | Contract |
| --- | --- |
| `protected internal abstract int ShapeCount { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void Detach()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool HasMotionMode(PhysicsServer.BodyMode mode)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetMotionMode(PhysicsServer.BodyMode mode)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetContactReporting(bool enabled)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareContactPolicy()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetAreaFields(in PhysicsAreaFieldConfiguration configuration)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetAreaMonitoring(bool monitoring, bool monitorable)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void InvalidateCollisionExceptions()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void RefreshIdentity(ulong objectID, ulong canvasID)` | After Attach and before shape preparation, receives retained object/canvas IDs; later edits publish again. Zero clears; UInt64 IDs are exact. |
| `protected internal abstract void SetCollisionPriority(float value)` | Receives retained finite positive priority after initial identity and on later edits. Initial default is 1. |
| `protected internal abstract void UpdateFilter(uint layer, uint mask, bool wakeBody)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract (Vector2 Position, float Rotation) GetPose()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Transform GetTransform()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Transform SampleJointLocalFrame(Vector2 point, float angle)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Vector2 LinearVelocity { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract float AngularVelocity { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool IsAwake { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetPose(Vector2 position, float rotation)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetTargetPose(Transform pose, double delta)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SavePose()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void RestorePose()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetLinearVelocity(Vector2 velocity)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetAngularVelocity(float velocity)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ClearVelocity()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetAwake(bool awake)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetCanSleep(bool canSleep)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetRotationLocked(bool locked)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetGravityScale(float scale)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyCentralForce(Vector2 force, bool wake)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyTorque(float torque, bool wake)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract bool RotationLocked { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Vector2 CenterOfMass { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Vector2 CenterOfMassLocal { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract float InverseMass { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract float InverseInertia { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract PhysicsMassProperties ApplyMassProfile(float mass, float inertia, Vector2? center)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void WakeTouching()` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Vector2 GetPointVelocity(Vector2 offset)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Vector2 GetWorldPointVelocity(Vector2 point)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyImpulse(Vector2 impulse, float moment)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void SetSurfaceVelocity(Vector2 linear, float angular)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract int ReadContacts(Span<PhysicsBodyContact> contacts)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract Transform PortablePose { get` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping, Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |
| `protected internal abstract void ValidatePortablePiece(int slot, int piece)` | Selected implementation operation; engine units, owner-thread borrowing and validated result publication apply. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The test solver implements a bounded circle/spring model; it is a separate public-only test consumer, not a production backend. Arbitrary custom geometry and unexercised operation acceptance remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).


## Attachment metadata

The registered adapter calls `Attach`, `RefreshIdentity` and `SetCollisionPriority`
in that order before exposing shape queries. Values authored while detached reach
the first attachment; transfer creates a fresh attachment and republishes the same
retained values. Hooks borrow the world context. Object/canvas/priority bindings
cannot change, and the live collider cannot be retired or transferred from within
those hooks. A failed initial hook retires the fresh attachment; detached
authoring remains available for retry.

Object IDs and canvas IDs are engine-owned metadata. Object associations are weak;
existing result snapshots retain sampled IDs when a target is replaced or disposed.
The implementation receives numeric identities, never ownership of the target.
Facade getters project the retained metadata. Numerical use of collision priority
belongs to the implementation; the bounded external fixture verifies its delivery,
not a general third-party solver's collision-priority response.

The public-only `BackendChecks.IdentityChecks` consumer exercises detached/live
body and Area assignment, two-world transfer, clear/disposed objects, exact 64-bit
canvas filtering through actual point queries, scene CanvasLayer membership, failed
initial hooks, owner/borrow guards and 64 warmup/128 edited query cycles with zero
owner/all-thread managed bytes. Native allocation and foreign platforms are unverified.

Body/Area shape slots, filters and shared server-shape data/lifetime cannot change from a borrowed hook, including partial initial attachment. Rejection precedes common authoring mutation; guarded logical reads remain available. See [borrowed shape authoring](../components/physics-registered-adapter.md#borrowed-shape-authoring).

Body force, mass, integration, motion, sleep, CCD and callback authoring now
reject before mutation during selected numerical hooks, including initial
attachment. Direct-body setters/forces use the same qualified numerical guard.
Guarded authored reads, ordinary post-solver callbacks and caller-created
body-state forwarding remain available; hierarchy/resource-lifetime guards still
cover every extension scope. See [body authoring](../components/physics-registered-adapter.md#borrowed-body-authoring).

[Registered Area fields](../components/physics-registered-adapter.md#registered-area-fields) now exercise the complete typed profile/default shortcut through the selected hooks, including independent override/point/mask response and zero warmed edited full frames. Numerical-hook writes reject before mutation; authored reads and ordinary Area callback edits retain their guards and overlap history. Generic solver/extension acceptance remains open.
