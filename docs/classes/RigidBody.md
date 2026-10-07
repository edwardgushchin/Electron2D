# RigidBody

Last updated: 2026-10-08

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [RigidBody.cs](../../src/Scene/2D/RigidBody.cs), [RigidBody.Mass.cs](../../src/Scene/2D/RigidBody.Mass.cs), [RigidBody.Freeze.cs](../../src/Scene/2D/RigidBody.Freeze.cs), [RigidBody.Forces.cs](../../src/Scene/2D/RigidBody.Forces.cs), [RigidBody.Contacts.cs](../../src/Scene/2D/RigidBody.Contacts.cs)
- **Declaration:** `public partial class RigidBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Physical skeletal integration

PhysicalBone inherits this complete body contract. Its simulation request controls backend dynamic/static participation without overwriting public Freeze. Configured filters remain authored across noncolliding follower mode.

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
| `public float Mass { get; set; }` | 1 | Positive finite kilograms within the solver range; normalizes automatic shape inertia and retains an explicit override. |
| `public RigidCenterOfMassMode CenterOfMassMode { get; set; }` | Auto | Automatic geometry or configured local center. |
| `public Vector2 CenterOfMass { get; set; }` | Zero | Stored local offset; changed assignment requires Custom. |
| `public float Inertia { get; set; }` | 0 | Stored kg·scene-units²; zero selects automatic geometry. |
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
| `public RigidFreezeMode FreezeMode { get; set; }` | Static | Selects stationary or manually driven kinematic participation while frozen. |
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

Positive kilograms normalize the active native geometry rather than preserving its raw density mass. Solid primitives use actual area-weighted centroid and polar moment; segment-only bodies keep length-weighted thin-rod inertia, and unshaped/point-only automatic bodies have zero inertia. Invalid mass, reciprocal inertia, converted scene-unit inertia or center-relative extents reject before profile/native mutation. Detached configuration applies on entry; pending/live fixture revisions recalculate the profile. Explicit Inertia remains independent of mass scaling. Scene pose and velocity do not change merely because the profile changes. Static participation keeps zero inverse values and restores the configured profile when dynamic behavior returns.

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

`MaxContactsReported=0` is the default and reports no points. Values through 4095 select the deepest current contact points; strictly deeper candidates replace the first shallowest slot, while equal depths retain existing slots. Assigning a limit clears the old point count. `GetContactCount()` reads that last fixed-step count even when `ContactMonitor=false`; `GetCollidingBodies()` and body entry/exit events additionally require monitoring. Body results deduplicate multiple contacting shape pairs. Enabling monitoring while already touching reports a new entry on the next step; disabling it clears the object snapshot immediately without synthesizing exits. A cap outside zero through 4095 rejects before mutation. Attached queries and setters require the scene owner thread. The current typed body array covers `PhysicsBody` scene nodes; tile-map virtual collision bodies remain a Partial coverage gap.

<a id="bodyentered"></a>
<a id="bodyexited"></a>
### `BodyEntered` and `BodyExited`

Callbacks run after the backend step and after the object snapshot commits, before area-monitor events and timers. The argument is the other `Node`. An event fires once per other body even if multiple shapes touch. Disabling `ContactMonitor` inside a contact callback throws without changing it; the callback may remove a collider, which clears the snapshot and queues one exit without another step. Exceptions from one callback are aggregated after later queued events are attempted. The current event payload does not represent absent tile-map virtual collision bodies; per-shape RID/index events require a separate typed identity slice.

<a id="sleepingstatechanged"></a>
### `SleepingStateChanged`

The event carries this RigidBody and fires when the solver changes its sleep state. Assigning `Sleeping` directly does not emit it. Solver sleep transitions are queued before object-level contact transitions; a throwing handler does not prevent later queued contact and area callbacks.

## Ownership, limits and verification

SceneTree owns the backend world and handle; the body owns no public handle and borrows child collision resources. Node disposal tears down its backend body without disposing borrowed Shape resources. Unit global scale and zero skew are required while active. A failed geometry validation leaves the world reusable after correction. The body can exit and re-enter a tree. Circle/rectangle contacts, masks, central impulse, frozen motion, PackedScene state and warmed zero-allocation frame lanes are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs). [PhysicsAreaFieldTests](../../tests/Electron2D.Tests/PhysicsAreaFieldTests.cs) checks signed area/body damping, combination modes and gravity. [RigidBodyForceTests](../../tests/Electron2D.Tests/RigidBodyForceTests.cs) checks offset/center-of-mass actions, unit conversion, persistent force, invalid rollback, packed state and repeated rotation. [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks zero-area segment mass, torque and unshaped movement. [RigidBodyContactTests](../../tests/Electron2D.Tests/RigidBodyContactTests.cs) checks point caps, object entries/exits, multi-shape deduplication, solver sleep, callback mutation/failure, packed state and 64 warmed resting, active and empty contact frames with zero managed allocations on Linux/.NET 8.

Tile-map virtual body reporting and continuous collision modes remain incomplete on [RigidBody2D coverage](../coverage/classes/RigidBody2D.md). [ADRs 0057 and 0058](../decisions/physics.md#adr-0058) record force and contact boundaries.

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

Current scene PhysicsBody identities execute; server-only contacts have no Node payload and are excluded from scene body events. Virtual tile payloads remain Partial; capped-contact priority uses the shared depth selection under [ADR 0058](../decisions/physics-monitoring.md#adr-0058). Handler failure retains the committed snapshot and does not suppress later queued pair transitions. ShapePairEventTests checks real native two-shape contacts, disabled-slot departure and collider removal.

## Disabled processing and physics

Inherited `DisableMode.MakeStatic` makes an effectively disabled body static without setting `Freeze`. Entering static mode clears prior linear/angular velocities for an unfrozen body. Velocity assignments while static remain stored; restoration reapplies them, mass and rotation lock. Freeze changes cannot override a temporary static policy, and enabling does not unfreeze a user-frozen body. Persistent forces and CustomIntegrator remain configured. Remove invalidates attachment-bound direct views; KeepActive continues solving while Node callbacks remain disabled. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

<a id="centerofmassmode"></a>
## `CenterOfMassMode`, `CenterOfMass` and `Inertia`

[RigidCenterOfMassMode](RigidCenterOfMassMode.md) selects Auto/default or Custom. CenterOfMass is a stored local scene-unit offset from body origin, not a global offset. Automatic geometry does not replace this stored vector; use PhysicsServer.BodyGetCenterOfMass or a live direct-body view for the resolved value. A changed center assignment requires Custom, while assigning the current vector is a no-op. Returning to Auto clears the stored center, preserves explicit Inertia, and reports PropertyListChanged after committing the profile. Callback failure leaves that entire profile committed.

Inertia is a nonnegative stored float in kilograms times squared scene units. Zero/default selects automatic polar moment about the selected center; a custom center uses the parallel-axis theorem. A positive override is retained across mass/shape/mode changes and works without collision shapes. The stored zero remains zero after calculation; PhysicsServer.BodyGetInertia reports the resolved moment. Rotation lock masks angular inverse response without discarding the override.

```csharp
using var body = new RigidBody { Mass = 2, CenterOfMassMode = RigidCenterOfMassMode.Custom };
body.CenterOfMass = new Vector2(5, 0);
body.Inertia = 200;
```

Prepared geometry completes deferred backend mass bookkeeping before stepping, including Debug assertion checks; this does not replace configured mass, inertia or custom centre values.

Undefined mode, nonfinite or negative inertia, native reciprocal underflow/overflow and invalid center reject. Attached setters enforce owner thread and reject during solver-owned pose synchronization before replacing stored or native data. PackedScene stores mode before center. PhysicsServer typed mass methods share the same scene profile; BodyResetMassProperties selects Auto, clears stored center/inertia and retains Mass. [PhysicsMassProfileTests](../../tests/Electron2D.Tests/PhysicsMassProfileTests.cs) checks behavior and 64 warmed profile-change/solver frames with zero managed allocation; [ADR 0073](../decisions/physics-mass.md#adr-0073) records automatic geometry and ownership adaptations. Native allocation and other platforms remain unverified.

## Shared force lifetime

Force/impulse operations share the typed [PhysicsServer](PhysicsServer.md) runtime. Scene immediate Apply methods still require attachment; server calls can act on a detached RID. Pending one-step inputs survive disable/remove/static/dormant transitions and apply once on eligible integration; omission discards them there. Dynamic native velocities are captured before removal, preserving an impulse issued since the last solver callback. Persistent positioned calls resolve the current detached profile and rotated center. Creation/rebuild of fixtures applies the complete mass profile once and preserves configured velocity. Invalid inputs/totals/velocities and solver-owned pose mutation reject before partial writes. [PhysicsServerForceTests](../../tests/Electron2D.Tests/PhysicsServerForceTests.cs) verifies these boundaries; [ADR 0074](../decisions/physics-forces.md#adr-0074) owns the shared policy.

<a id="freezemode"></a>
## `FreezeMode`

[RigidFreezeMode](RigidFreezeMode.md) stores Static=0/default or Kinematic=1. It has no effect while Freeze is false. Static manual transforms teleport; Kinematic manual global targets derive native contact velocity over a nonzero fixed frame, allowing the frozen body to push dynamic bodies along its path. Gravity, force response and physical inverse mass/inertia remain disabled. A forced pose query exposes the target immediately; zero delta retains it. Solver history uses the exact native transform, and unchanged targets clear velocity rather than drifting from an approximate angle decoder.

Changing mode/freeze synchronizes pending pose before switching and checks owner/solver access before changing flags. Reentry snapshots the current scene pose; inherited MakeStatic temporarily overrides Kinematic without changing configuration. Unfreezing restores mass, explicit inertia and dynamic rotation lock; that lock does not prevent manually animated frozen rotation. Mode is packed before Freeze. Undefined mode, disposed/off-owner access and role mutation from solver-owned pose callbacks reject.

```csharp
using var body = new RigidBody { Freeze = true, FreezeMode = RigidFreezeMode.Kinematic };
body.Position += new Vector2(2, 0);
```

Current kinematic paths can use several native integration intervals within one fixed frame to avoid skipping crossed dynamic geometry. Dynamic force/torque acts for the full outer duration and integration callbacks still run once. Native angular estimation is approximate: the checked 0.1 rad / 1/60 s target is within 0.1 rad/s of the ideal six; presentation uses a 0.01 rad tolerance. [RigidFreezeModeTests](../../tests/Electron2D.Tests/RigidFreezeModeTests.cs) covers ordinary/fast contacts, idle, query/zero delta, policy and mass restoration, failure boundaries, storage and 64 warmed active subdivided frames with zero managed allocation. [ADR 0075](../decisions/physics.md#adr-0075) owns this contract. Native allocation, broad-scene performance, other platforms and owner acceptance remain unverified.

## Server material and field parameter projection

Typed PhysicsServer body parameters share GravityScale, LinearDamp/AngularDamp and their modes with this scene object. A per-body signed friction/bounce write overrides fixture coefficients without mutating a borrowed PhysicsMaterial; resource assignment/revision/disposal reloads both coefficients. Revision polling recovers missed callback delivery. Leaving approximately zero gravity scale follows the immediate wakeup rule, while changed selected fields retain the existing wake policy. [PhysicsBodyParameterTests](../../tests/Electron2D.Tests/PhysicsBodyParameterTests.cs) and [ADR 0076](../decisions/physics-mass.md#adr-0076) cover ownership, physical contacts, fields and failures.

Contact limits prepare bounded point storage before fixed stepping. Every touching pair is scanned directly, retaining the deepest points without copying raw manifolds; no early pair cap hides later candidates. Rigid monitoring prepares its bounded pair/change collections at configuration time. Solver array compaction keeps the removed reference in the unused slot rather than constructing a replacement; active slots remain distinct. PhysicsSandbox profiles check collision churn and debug contact reads after warmup; native allocations remain outside the managed counter.

Contact monitoring and direct-state values use the same solved-pair traversal. Snapshot collection may run on internal world workers; contact, integration and sync callbacks remain on the scene owner thread. The owner waits for every collector before any game callback can edit bodies or fixtures.

Explicit dynamic Sleeping assignments clear stored velocity, including while
detached. Disabling CanSleep wakes detached configuration. An explicitly sleeping
body reenters asleep even with CanSleep false; server/direct-state setters share
these rules. Existing freeze/disable velocity configuration remains distinct from
actual native velocity. PhysicsServerStateTests verifies these lifecycle cases.
