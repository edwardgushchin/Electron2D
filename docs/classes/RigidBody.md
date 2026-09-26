# RigidBody

Last updated: 2026-09-26

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [RigidBody.cs](../../src/Scene/2D/RigidBody.cs), [RigidBody.Forces.cs](../../src/Scene/2D/RigidBody.Forces.cs), [RigidBody.Contacts.cs](../../src/Scene/2D/RigidBody.Contacts.cs)
- **Declaration:** `public partial class RigidBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A dynamic 2D scene body backed by the internal fixed-step physics world. A direct CollisionShape child supplies circle, capsule, segment, convex polygon, concave segment collection or rectangle geometry; without a child the body can still move but cannot collide. A child can select a one-way contact side for its fixtures. It uses scene-unit positions and linear velocity, kilograms for mass, radians for angular velocity, and a world gravity default of 980 scene-unit/s² downward unless typed project settings change it. Overlapping [Area](Area.md) fields can change its gravity and damping. Game physics callbacks run before the solver step, so forces and changed velocity apply to that step; stored constant force and torque apply every step until cleared. Solved transforms, velocities and contact snapshots return to the scene before contact, sleep and area callbacks, timers, tweens and interpolation capture.

## Example

A direct [CollisionPolygon](CollisionPolygon.md) child supplies owned solid or hollow fixtures. Solid convex pieces carry mass and inertia; hollow segments use the existing zero-area body policy.

```csharp
using var geometry = new RectangleShape { Size = new Vector2(20, 20) };
var body = new RigidBody { Position = new Vector2(0, 0), Mass = 1 };
body.AddChild(new CollisionShape { Shape = geometry });
// Add body to a SceneTree and advance its fixed physics frame.
```

## Properties

| Member | Default | Contract |
| --- | --- | --- |
| `public float Mass { get; set; }` | 1 | Positive finite kilograms within the solver range; scales shape-derived or zero-area inertia. |
| `public float GravityScale { get; set; }` | 1 | Finite multiplier, including zero and negative values. |
| `public Vector2 LinearVelocity { get; set; }` | (0, 0) | Finite scene units per second. |
| `public float AngularVelocity { get; set; }` | 0 | Finite radians per second. |
| `public float LinearDamp { get; set; }` | 0 | Finite signed linear damping. |
| `public float AngularDamp { get; set; }` | 0 | Finite signed angular damping. |
| `public DampMode LinearDampMode { get; set; }` | Combine | Adds to or replaces area/world linear damping. |
| `public DampMode AngularDampMode { get; set; }` | Combine | Adds to or replaces area/world angular damping. |
| `public bool CanSleep { get; set; }` | true | Allows idle sleep. |
| `public bool Sleeping { get; set; }` | false | Reads or changes current awake state. |
| `public bool Freeze { get; set; }` | false | Uses a static backend mode while true; unfreezing resumes dynamic motion. |
| `public bool LockRotation { get; set; }` | false | Locks angular movement in the solver. |
| `public PhysicsMaterial? PhysicsMaterialOverride { get; set; }` | null | Borrows a surface material for every child fixture. |
| `public Vector2 ConstantForce { get; set; }` | (0, 0) | Persistent center force in scene units times kilograms/s². |
| `public float ConstantTorque { get; set; }` | 0 | Persistent torque in kilograms times squared scene units/s². |
| `public bool ContactMonitor { get; set; }` | false | Enables body entry/exit reports when the contact cap is positive. |
| `public int MaxContactsReported { get; set; }` | 0 | Bounds reported contact points per fixed step. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| `public RigidBody()` | Creates a detached body with pinned defaults. |
| `public enum DampMode` | Two modes: `Combine = 0`, `Replace = 1`; see [values](RigidBody.DampMode.md). |
| `public void ApplyCentralForce(Vector2 force)` | Adds a finite center force to an attached body. |
| `public void ApplyCentralImpulse(Vector2 impulse = default)` | Adds a finite instantaneous center impulse to an attached body. |
| `public void ApplyForce(Vector2 force, Vector2 position = default)` | Adds a one-step force at a world-axis offset from origin. |
| `public void ApplyImpulse(Vector2 impulse, Vector2 position = default)` | Adds a one-time impulse at a world-axis offset from origin. |
| `public void ApplyTorque(float torque)` | Adds one-step torque. |
| `public void ApplyTorqueImpulse(float torque)` | Adds a one-time angular impulse. |
| `public void AddConstantCentralForce(Vector2 force)` | Accumulates center force across later steps. |
| `public void AddConstantForce(Vector2 force, Vector2 position = default)` | Accumulates force and its current offset moment across later steps. |
| `public void AddConstantTorque(float torque)` | Accumulates persistent torque. |
| `public void SetAxisVelocity(Vector2 axisVelocity)` | Replaces only the velocity component along the supplied axis. |
| `public int GetContactCount()` | Returns reported contact points from the last step. |
| `public Entity[] GetCollidingBodies()` | Returns a caller-owned array of currently reported scene bodies. |
| `public event Action<Node>? BodyEntered` / `BodyExited` | Reports object-level contact transitions after the solver step. |
| `public event Action<RigidBody>? SleepingStateChanged` | Reports a solver-driven sleep-state transition. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored body motion, material and persistent-force state to scene descriptors. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the exact body type for PackedScene. |

## Member descriptions

<a id="mass"></a>
### `Mass`

Zero, negative and nonfinite values throw `ArgumentOutOfRangeException` before mutation. With fixtures present, assignment scales their shape-derived mass and rotational inertia to the requested kilograms; a ratio or inertia that would overflow is rejected before replacing the current mass. A detached value is applied when the body later enters a tree; live fixture edits reapply it. A body with only zero-area segments distributes its requested mass by segment length and uses thin-rod inertia around their length-weighted center. An unshaped body has the requested mass and zero inertia, so it can move without colliding. A mass or inertia whose reciprocal exceeds the finite solver range rejects before backend mutation.

<a id="velocity"></a>
### `LinearVelocity` and `AngularVelocity`

Setters validate finite components and update an attached backend body immediately; detached values initialize its later backend body. Solved values are read back after each fixed step. `LinearVelocity` uses scene units/s, while `AngularVelocity` uses radians/s. Attached reads and writes require the scene owner thread. A frozen body retains assigned velocity for unfreezing.

<a id="damping"></a>
### `LinearDamp`, `AngularDamp`, their modes and `GravityScale`

The body damping values are finite and may be negative, which increases speed. The [DampMode](RigidBody.DampMode.md) properties default to `Combine`; `Replace` ignores resolved area and world damping for that channel. On each fixed step, areas resolve from greatest priority to least, then world defaults fill any unstopped channel, and body damping combines or replaces. The step multiplies velocity by `max(0, 1 - delta * totalDamp)` before force integration. GravityScale multiplies the resolved vector after area/world reduction and may be negative. An attached `GetGravity()` returns the last resolved scaled vector. Field changes wake a sleeping body; detached assignments initialize its later body. Nonfinite damping and invalid mode values reject before mutation.

<a id="sleepfreeze"></a>
### `CanSleep`, `Sleeping`, `Freeze` and `LockRotation`

CanSleep changes backend sleep eligibility. Sleeping reads the live solver state when attached and requests sleep/awake on assignment. Freeze changes the backend body to static mode without dropping its scene node or stored velocity; unfreezing recreates dynamic movement and refreshes fixtures. LockRotation changes Box2D angular motion locks. Scene entry/re-entry applies the current detached settings.

<a id="forces"></a>
### `ApplyCentralForce` and `ApplyCentralImpulse`

Both take finite scene-unit vectors and wake the attached body. Force is time dependent and should be supplied during each desired physics step; impulse is instantaneous and independent of frame rate. `ApplyCentralImpulse()` defaults to a zero vector. A detached body throws `InvalidOperationException`; an invalid vector throws before backend mutation. Pending shape and pose edits synchronize before the action.

<a id="positionedactions"></a>
<a id="applyforce"></a>
<a id="applyimpulse"></a>
<a id="applytorque"></a>
<a id="applytorqueimpulse"></a>
### `ApplyForce`, `ApplyImpulse`, `ApplyTorque` and `ApplyTorqueImpulse`

`ApplyForce` and `ApplyImpulse` use a world-axis `position` offset from the body's current backend origin, not a rotated local offset. A force acts for the current fixed step; an impulse is instantaneous. Offset actions may rotate a body according to its current center of mass and inertia. `ApplyTorque` is time dependent, while `ApplyTorqueImpulse` changes angular motion once; both require nonzero inertia from a live shape. Force/linear impulse convert scene units to meters by 0.01, and torque/angular impulse use 0.0001 for squared units. Inputs and resulting point/moment must be finite. Actions require an attached body and wake it. Pending shape/pose edits synchronize first, so an action before the first fixed step uses the attached shape's current mass.

<a id="constantforce"></a>
<a id="constanttorque"></a>
<a id="persistentactions"></a>
<a id="addconstantcentralforce"></a>
<a id="addconstantforce"></a>
<a id="addconstanttorque"></a>
### `ConstantForce`, `ConstantTorque`, `AddConstantCentralForce`, `AddConstantForce` and `AddConstantTorque`

Both properties default to zero and replace their stored totals. The three methods add to those totals, including when the body is detached. `AddConstantForce` adds its force and computes a constant moment from the supplied world-axis origin offset and current center of mass at the time of addition; later motion does not recompute it. A centered addition changes force without torque. Persistent totals apply before each solver step and remain stored while frozen; clearing both stops new acceleration without erasing velocity. Nonfinite inputs or accumulated totals throw before either total changes. Detached totals and their PackedScene copy retain typed state; caller-triggered changes wake an eligible body.

<a id="setaxisvelocity"></a>
### `SetAxisVelocity`

The supplied vector gives both axis direction and replacement speed. The method subtracts the old velocity projection along the normalized axis and adds the supplied vector, preserving perpendicular velocity. A zero vector leaves velocity unchanged; nonfinite inputs or a nonfinite result throw before mutation. Detached bodies store the new velocity; attached calls reach and wake the backend body.

<a id="physicsmaterialoverride"></a>
### `PhysicsMaterialOverride`

The optional borrowed [PhysicsMaterial](PhysicsMaterial.md) sets friction, bounce and their mixing modifiers for every child fixture. Without an override, friction is one and bounce is zero. Assigning a disposed material throws before changing the current override; disposing the borrowed material resets the property to null. Assignment or a resource edit rebuilds fixtures before the next fixed step. The caller owns the resource.

<a id="contactmonitor"></a>
<a id="maxcontactsreported"></a>
<a id="getcontactcount"></a>
<a id="getcollidingbodies"></a>
### Contact monitoring and queries

`MaxContactsReported=0` is the default and reports no points. A positive value caps the sum of current touching manifold points. `GetContactCount()` reads that last fixed-step count even when `ContactMonitor=false`; `GetCollidingBodies()` and body entry/exit events additionally require monitoring. Body results deduplicate multiple contacting shape pairs. Enabling monitoring while already touching reports a new entry on the next step; disabling it clears the object snapshot immediately without synthesizing exits. A negative cap rejects before mutation. Attached queries and setters require the scene owner thread. The current typed body array covers `PhysicsBody` scene nodes; tile-map virtual collision bodies and exact reference contact selection when over the cap remain Partial coverage gaps.

<a id="bodyentered"></a>
<a id="bodyexited"></a>
### `BodyEntered` and `BodyExited`

Callbacks run after the backend step and after the object snapshot commits, before area-monitor events and timers. The argument is the other `Node`. An event fires once per other body even if multiple shapes touch. Disabling `ContactMonitor` inside a contact callback throws without changing it; the callback may remove a collider, which clears the snapshot and queues one exit without another step. Exceptions from one callback are aggregated after later queued events are attempted. The current event payload does not represent absent tile-map virtual collision bodies; per-shape RID/index events require a separate typed identity slice.

<a id="sleepingstatechanged"></a>
### `SleepingStateChanged`

The event carries this RigidBody and fires when the solver changes its sleep state. Assigning `Sleeping` directly does not emit it. Solver sleep transitions are queued before object-level contact transitions; a throwing handler does not prevent later queued contact and area callbacks.

## Ownership, limits and verification

SceneTree owns the backend world and handle; the body owns no public handle and borrows child collision resources. Node disposal tears down its backend body without disposing borrowed Shape resources. Unit global scale and zero skew are required while active. A failed geometry validation leaves the world reusable after correction. The body can exit and re-enter a tree. Circle/rectangle contacts, masks, central impulse, frozen motion, PackedScene state and warmed zero-allocation frame lanes are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs). [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks signed area/body damping, combination modes and gravity. [RigidBodyForceTests](../../tests/Electron2D.Tests/RigidBodyForceTests.cs) checks offset/center-of-mass actions, unit conversion, persistent force, invalid rollback, packed state and repeated rotation. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks zero-area segment mass, torque and unshaped movement. [RigidBodyContactTests](../../tests/Electron2D.Tests/RigidBodyContactTests.cs) checks point caps, object entries/exits, multi-shape deduplication, solver sleep, callback mutation/failure, packed state and 64 warmed resting, active and empty contact frames with zero managed allocations on Linux/.NET 8.

Tile-map virtual body reporting, exact capped-contact selection, custom center of mass/inertia, continuous collision modes remain incomplete on [RigidBody2D coverage](../coverage/classes/RigidBody2D.md). [ADRs 0057 and 0058](../decisions/physics.md#adr-0058) record force and contact boundaries.

A [SeparationRayShape](SeparationRayShape.md) sensor contributes zero inertia and is excluded from the segment-only thin-rod fallback. Ordinary dynamic ray impulses and contact reporting remain incomplete; the class coverage records the required solver manifold integration.

## Custom integration

`public bool CustomIntegrator { get; set; }` defaults false and is packed. True omits automatic gravity, damping and accumulated forces while preserving impulses and native contacts. `protected virtual void IntegrateForces(PhysicsDirectBodyState state)` runs after each active native step with synchronized scene pose and solved contacts. RigidBody is extensible so subclasses can override this hook. State queries are permitted in the hook; scene pose and velocity caches synchronize again after it. A callback exception retains committed edits and does not suppress other body callbacks. Callback references are transient and subclass scene factories follow the inherited typed packing contract.

See [the direct state class](PhysicsDirectBodyState.md) for exact units, lifetime, force semantics and manual gravity-before-damping integration. [PhysicsBodyStateTests](../../tests/Electron2D.Tests/PhysicsBodyStateTests.cs) verifies ordinary/custom behavior, packing, failure recovery and warmed zero managed allocation under [ADR 0070](../decisions/physics.md#adr-0070).

## Body shape-pair events

| Signature | Contract |
| --- | --- |
| `public event Action<RID, Node, int, int>? BodyShapeEntered` | Retained collider RID, scene Node, collider global slot index, local global slot index. |
| `public event Action<RID, Node, int, int>? BodyShapeExited` | Retained departed pair and sampled global indices. |

<a id="bodyshapeentered"></a>
<a id="bodyshapeexited"></a>
These events require ContactMonitor and a positive MaxContactsReported. The capped touching contact set is reduced to logical slot pairs, so duplicate manifold points and compound fixtures do not repeat entries. Object entry precedes its first pair entry; object exit precedes its last retained pair exit. A remaining shape pair keeps the object's monitor snapshot alive. Removal uses committed RID/indices rather than already-destroyed native shape IDs and delivers exits without another solver step. Disabling ContactMonitor inside either object or shape callback rejects.

Current scene PhysicsBody identities execute; server-only contacts have no Node payload and are excluded from scene body events. Virtual tile payloads and exact capped-contact priority remain Partial under [ADR 0058](../decisions/physics-monitoring.md#adr-0058). Handler failure retains the committed snapshot and does not suppress later queued pair transitions. ShapePairEventTests checks real native two-shape contacts, disabled-slot departure and collider removal.

## Disabled processing and physics

Inherited `DisableMode.MakeStatic` makes an effectively disabled body static without setting `Freeze`. Entering static mode clears prior linear/angular velocities for an unfrozen body. Velocity assignments while static remain stored; restoration reapplies them, mass and rotation lock. Freeze changes cannot override a temporary static policy, and enabling does not unfreeze a user-frozen body. Persistent forces and CustomIntegrator remain configured. Remove invalidates attachment-bound direct views; KeepActive continues solving while Node callbacks remain disabled. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).
