# PhysicsDirectBodyState

Last updated: 2026-09-26

**Inherits:** [ElectronObject](ElectronObject.md)

- **Source:** [PhysicsDirectBodyState.cs](../../src/Servers/Physics/PhysicsDirectBodyState.cs)
- **Declaration:** `public sealed class PhysicsDirectBodyState : ElectronObject`
- **Component:** [Physics server and direct queries](../components/physics-queries.md)

## Description

An owner-thread view of one backend attachment of a scene or server body. Obtain it from PhysicsServer.BodyGetDirectState or the post-solver RigidBody integration hook. It has no public constructor and owns no body or space. The server caches one view per attachment. Access rejects native stepping, wrong threads, disposal, detachment, body release and a replaced attachment; reattachment never revives an old view. Consumer disposal allows a fresh cached view to be created, but disposal during a borrowed callback rejects.

Fields read the live native body. Contact methods read an immutable value snapshot from the last solved step, capped by MaxContactsReported independently of object ContactMonitor. The word local identifies this body; contact positions, normals and velocities use global coordinates. Collision layer/mask changes and direct-space queries may rebuild fixtures without invalidating those stored contact values.

## Example

Partial subclass snippet; keep all borrowed shape resources alive while attached:

```csharp
class ControlledBody : RigidBody
{
    protected override void IntegrateForces(PhysicsDirectBodyState state)
    {
        if (CustomIntegrator) state.IntegrateForces();
        state.LinearVelocity = new Vector2(120, state.LinearVelocity.Y);
    }
}
```

## Property summary

| Signature | Contract |
| --- | --- |
| `public Single AngularVelocity { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Vector2 CenterOfMass { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 CenterOfMassLocal { get;  }` | Live owner-thread field; see descriptions below. |
| `public UInt32 CollisionLayer { get; set; }` | Live owner-thread field; see descriptions below. |
| `public UInt32 CollisionMask { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Single InverseInertia { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single InverseMass { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 LinearVelocity { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Boolean Sleeping { get; set; }` | Live owner-thread field; see descriptions below. |
| `public Single Step { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single TotalAngularDamp { get;  }` | Live owner-thread field; see descriptions below. |
| `public Vector2 TotalGravity { get;  }` | Live owner-thread field; see descriptions below. |
| `public Single TotalLinearDamp { get;  }` | Live owner-thread field; see descriptions below. |
| `public Transform Transform { get; set; }` | Live owner-thread field; see descriptions below. |

## Method summary

| Signature | Contract |
| --- | --- |
| `public Void AddConstantCentralForce(Vector2 force = default)` | Guarded live operation or retained contact lookup. |
| `public Void AddConstantForce(Vector2 force, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void AddConstantTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyCentralForce(Vector2 force = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyCentralImpulse(Vector2 impulse)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyForce(Vector2 force, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyImpulse(Vector2 impulse, Vector2 position = default)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `public Void ApplyTorqueImpulse(Single impulse)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetConstantForce()` | Guarded live operation or retained contact lookup. |
| `public Single GetConstantTorque()` | Guarded live operation or retained contact lookup. |
| `public RID GetContactCollider(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public UInt64 GetContactColliderID(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public CollisionObject GetContactColliderObject(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactColliderPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactColliderShape(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactColliderVelocityAtPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactCount()` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactImpulse(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalNormal(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Int32 GetContactLocalShape(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetContactLocalVelocityAtPosition(Int32 contactIndex)` | Guarded live operation or retained contact lookup. |
| `public PhysicsDirectSpaceState GetSpaceState()` | Guarded live operation or retained contact lookup. |
| `public Vector2 GetVelocityAtLocalPosition(Vector2 localPosition)` | Guarded live operation or retained contact lookup. |
| `public Void IntegrateForces()` | Guarded live operation or retained contact lookup. |
| `public Void SetConstantForce(Vector2 force)` | Guarded live operation or retained contact lookup. |
| `public Void SetConstantTorque(Single torque)` | Guarded live operation or retained contact lookup. |
| `protected override Void ValidateDisposal()` | Guarded live operation or retained contact lookup. |

## Property descriptions

<a id="velocity"></a>
### Velocity, pose and sleep

LinearVelocity uses global scene units/s; AngularVelocity uses radians/s. Assignments require finite values and wake a dynamic body. Sleeping changes native awake state. Transform is a global translated/rotated unit-scale, zero-skew pose; invalid input rejects before applying the pose. A scene body's pose is applied to scene and backend immediately. Node velocity caches are synchronized around integration callbacks.

<a id="mass"></a>
### CenterOfMass, CenterOfMassLocal, InverseMass and InverseInertia

CenterOfMass is the offset from body origin to center along global axes; CenterOfMassLocal is that offset before body rotation. Both use scene units. InverseMass uses reciprocal kilograms; InverseInertia uses reciprocal kg times squared scene units and reports zero for a rotation lock. Static/kinematic native inverse mass and inertia are zero. These are actual native solver mass values, including child offsets and the existing shape/mass policy.

<a id="fields"></a>
### Step, TotalGravity, TotalLinearDamp and TotalAngularDamp

Step is the last nonzero space step in seconds, zero before the first step. TotalGravity is the last selected world/Area gravity including scene body gravity scale. Damping includes current field and body combine/replace policy. Server bodies now participate in scene Area gravity/damping reduction and field-change wakeup using the same masks and shape-overlap kernel; server-only Area field configuration remains a separate gap.

<a id="filters"></a>
### CollisionLayer and CollisionMask

All 32 category bits are preserved through uint values. Scene filtering uses the existing body properties; server filtering rebuilds its indexed fixtures. The contact snapshot remains from the solved step until the next capture.

## Method descriptions

<a id="forces"></a>
### Force, impulse and persistent-force operations

Position arguments are offsets from the body origin along global axes, not rotated local positions. Force uses scene units times kg/s²; impulse uses scene units times kg/s; torque and angular impulse use kg times squared scene units per s² and s respectively. Central operations act at the actual mass center. Positioned operations include the center-relative moment. Nonfinite inputs/totals or resulting impulse velocities reject before native mutation. Static/kinematic native response follows their motion mode.

Apply*Force and ApplyTorque accumulate for the next native solver step. Impulses change velocity immediately. SetConstantForce/Torque replaces the persistent value; AddConstant* accumulates it. Positioned constant-force torque is recorded when added. Scene RigidBody constants share its public stored state; other bodies retain constants in the server runtime record across attachments. Constant writes wake a dynamic body.

<a id="integrateforces"></a>
### IntegrateForces()

Adds TotalGravity * Step to linear velocity, then applies max(0, 1 - Step * damping) to linear/angular velocity. Every call applies another equivalent tick and wakes a dynamic body. It includes no force/torque accumulators. This explicit gravity-before-damping order differs from the ordinary native-step force integration order. With CustomIntegrator, velocities written in the post-solver hook affect the next solved motion. Impulses and native contact response remain active.

<a id="contacts"></a>
### Contact methods

GetContactCount is capped by the body's configured contact limit, zero by default. Each zero-based index yields both direct shape indices, collider RID/instance identity, global contact points, global outward normal and global-axis point velocities. GetContactColliderObject returns the live scene CollisionObject or null for server-only or released objects. Virtual tile collider identity remains Partial until typed tile-body integration. Contact snapshots survive fixture edits and queries during callbacks.

GetContactImpulse uses scene units times kg/s. The normal component is accumulated across four solver substeps; the tangential component currently describes the final substep. **Whole-step tangential aggregation remains Partial** until the backend exposes that contact accumulator. No aggregate compatibility claim is made for that component.

<a id="getspacestate"></a>
### GetSpaceState() and GetVelocityAtLocalPosition(...)

GetSpaceState returns the same cached direct-query view as PhysicsServer.SpaceGetDirectState. Queries are legal in the post-solver callback window. GetVelocityAtLocalPosition treats its argument as a finite global-axis offset from the body origin and includes rotation about the actual center. Malformed offsets or unrepresentable point velocity reject.

## Lifecycle, errors and verification

The server creates views on demand or during step preparation. Borrowed callback disposal is rejected; off-callback disposal invalidates only that wrapper. Detachment clears the cached wrapper while external old wrappers remain invalid. Old contacts contain values and RIDs, not owning scene references. Native world execution and recursive SpaceStep/world disposal remain forbidden; body/fixture mutations are legal after solver completion. Callback failures are aggregated after remaining callbacks and committed edits are retained.

Invalid contact indices throw ArgumentOutOfRangeException. Disposed or ended attachments throw ObjectDisposedException; thread/phase violations throw InvalidOperationException. Pose/motion/force validation uses ArgumentException/ArgumentOutOfRangeException as applicable. Public fields and methods do not expose native backend types.

[PhysicsBodyStateTests](../../tests/Electron2D.Tests/PhysicsBodyStateTests.cs) covers scene/server fields, center offsets, inertia units, force/impulse paths, Area reduction/wakeup, custom integration, callback ordering/userdata, actual solved contacts, fixture-query edits, hierarchy mutation, exceptions, stale/disposed/off-thread views, packing and 64 warmed active callback frames without managed allocations on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. See [ADR 0070](../decisions/physics.md#adr-0070).

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).
