# PhysicsBackendCollider

Last updated: 2026-10-11

**Declaration:** `internal abstract class PhysicsBackendCollider` · **Source:** [PhysicsBackendCollider.cs](../../src/Servers/Physics/PhysicsBackendCollider.cs) · **Component:** [Registered solver adapter](../components/physics-registered-adapter.md)

## Description

Defines one selected body/Area numerical attachment. Creation is claimed exactly once. Initial and integration policy, force intent, logical shape spans, fields, monitoring policy, mass, motion, surfaces, contact output and portable motion carry values without scene/server runtime storage.

The engine retains authoring, logical identity, owner-thread/solver guards and observer timing. Backend-private simulation state stays with the implementation. Borrowed spans expire on return. Factories and hooks cannot transfer/release their current world or attachments. Solver/history objects reject repeated claims; retiring attachments preserve matching replacements.

## Internal operation family

| Declaration | Contract |
| --- | --- |
| `internal void Claim()` | Permanently claims one implementation context; repeated ownership rejects before use. |
| `internal abstract int ShapeCount { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void Detach()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool HasMotionMode(PhysicsServer.BodyMode mode)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetMotionMode(PhysicsServer.BodyMode mode)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetContactReporting(bool enabled)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareContactPolicy()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetAreaFields(in PhysicsAreaFieldConfiguration configuration)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetAreaMonitoring(bool monitoring, bool monitorable)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void InvalidateCollisionExceptions()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void RefreshIdentity(ulong objectID, ulong canvasID)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetCollisionPriority(float value)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void UpdateFilter(uint layer, uint mask, bool wakeBody)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract (Vector2 Position, float Rotation) GetPose()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Transform GetTransform()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Transform SampleJointLocalFrame(Vector2 point, float angle)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Vector2 LinearVelocity { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract float AngularVelocity { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool IsAwake { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetPose(Vector2 position, float rotation)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetTargetPose(Transform pose, double delta)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SavePose()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void RestorePose()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetLinearVelocity(Vector2 velocity)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetAngularVelocity(float velocity)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ClearVelocity()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetAwake(bool awake)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetCanSleep(bool canSleep)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetRotationLocked(bool locked)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetGravityScale(float scale)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyCentralForce(Vector2 force, bool wake)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyTorque(float torque, bool wake)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract bool RotationLocked { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Vector2 CenterOfMass { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Vector2 CenterOfMassLocal { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract float InverseMass { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract float InverseInertia { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void WakeTouching()` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Vector2 GetPointVelocity(Vector2 offset)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Vector2 GetWorldPointVelocity(Vector2 point)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyImpulse(Vector2 impulse, float moment)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void SetSurfaceVelocity(Vector2 linear, float angular)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract int ReadContacts(Span<PhysicsBodyContact> contacts)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract Transform PortablePose { get; }` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping, Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |
| `internal abstract void ValidatePortablePiece(int slot, int piece)` | Selected implementation operation; units and roles follow the parameter/result types. Adapter callouts enforce owner/lifetime guards. |

## Verification and limits

[Independent solver probe](../../tests/Electron2D.Tests/PhysicsRegisteredBackendTests.cs) exercises scene/raw integration, ordinary views, queries, Area events, contacts, spring policy, local/portable restore, guarded lifetime and terminal solve failure. The test solver implements a bounded circle/spring model; it is not a production backend or a public-only consumer. Unexercised operation families and complete public registration/custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103).
