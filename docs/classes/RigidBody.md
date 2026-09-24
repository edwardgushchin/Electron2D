# RigidBody

Last updated: 2026-09-24

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [RigidBody.cs](../../src/Scene/2D/RigidBody.cs)
- **Declaration:** `public sealed class RigidBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A dynamic 2D scene body backed by the internal fixed-step physics world. A direct CollisionShape child supplies circle or rectangle geometry; without a child the body can still move but cannot collide. It uses scene-unit positions and linear velocity, kilograms for mass, radians for angular velocity, and the world's default downward 980 scene-unit/s² gravity. Game physics callbacks run before the solver step, so a central force or changed velocity applies to that step. Solved transforms and velocities return to the scene before timers, tweens and interpolation capture.

## Example

```csharp
using var geometry = new RectangleShape { Size = new Vector2(20, 20) };
var body = new RigidBody { Position = new Vector2(0, 0), Mass = 1 };
body.AddChild(new CollisionShape { Shape = geometry });
// Add body to a SceneTree and advance its fixed physics frame.
```

## Properties

| Member | Default | Contract |
| --- | --- | --- |
| `public float Mass { get; set; }` | 1 | Positive finite kilograms; scales shape-derived inertia. |
| `public float GravityScale { get; set; }` | 1 | Finite multiplier, including zero and negative values. |
| `public Vector2 LinearVelocity { get; set; }` | (0, 0) | Finite scene units per second. |
| `public float AngularVelocity { get; set; }` | 0 | Finite radians per second. |
| `public float LinearDamp { get; set; }` | 0 | Finite nonnegative linear damping. |
| `public float AngularDamp { get; set; }` | 0 | Finite nonnegative angular damping. |
| `public bool CanSleep { get; set; }` | true | Allows idle sleep. |
| `public bool Sleeping { get; set; }` | false | Reads or changes current awake state. |
| `public bool Freeze { get; set; }` | false | Uses a static backend mode while true; unfreezing resumes dynamic motion. |
| `public bool LockRotation { get; set; }` | false | Locks angular movement in the solver. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| `public RigidBody()` | Creates a detached body with pinned defaults. |
| `public void ApplyCentralForce(Vector2 force)` | Adds a finite center force to an attached body. |
| `public void ApplyCentralImpulse(Vector2 impulse)` | Adds a finite instantaneous center impulse to an attached body. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored first-profile body state to inherited scene descriptors. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates the exact body type for PackedScene. |

## Member descriptions

<a id="mass"></a>
### `Mass`

Zero, negative and nonfinite values throw `ArgumentOutOfRangeException` before mutation. With fixtures present, assignment scales their shape-derived mass and rotational inertia to the requested kilograms; a ratio or inertia that would overflow is rejected before replacing the current mass. A detached value is applied when the body later enters a tree; live fixture edits reapply it. An unshaped body stores mass until geometry exists.

<a id="velocity"></a>
### `LinearVelocity` and `AngularVelocity`

Setters validate finite components and update an attached backend body immediately; detached values initialize its later backend body. Solved values are read back after each fixed step. `LinearVelocity` uses scene units/s, while `AngularVelocity` uses radians/s. Attached reads and writes require the scene owner thread. A frozen body retains assigned velocity for unfreezing.

<a id="damping"></a>
### `LinearDamp`, `AngularDamp` and `GravityScale`

Damping cannot be negative or nonfinite. GravityScale may be negative to reverse gravity. Valid attached assignments reach the current backend body; detached assignments are stored. Area overrides and combine/replace damping modes are not part of the first profile.

<a id="sleepfreeze"></a>
### `CanSleep`, `Sleeping`, `Freeze` and `LockRotation`

CanSleep changes backend sleep eligibility. Sleeping reads the live solver state when attached and requests sleep/awake on assignment. Freeze changes the backend body to static mode without dropping its scene node or stored velocity; unfreezing recreates dynamic movement and refreshes fixtures. LockRotation changes Box2D angular motion locks. Scene entry/re-entry applies the current detached settings.

<a id="forces"></a>
### `ApplyCentralForce` and `ApplyCentralImpulse`

Both take finite scene-unit vectors and wake the attached body. Force is time dependent and should be supplied during each desired physics step; impulse is instantaneous and independent of frame rate. A detached body throws `InvalidOperationException`; an invalid vector throws before backend mutation. Position-offset forces, torque, constant forces and axis-velocity methods remain separate Unimplemented rows.

## Ownership, limits and verification

SceneTree owns the backend world and handle; the body owns no public handle and borrows child collision resources. Node disposal tears down its backend body without disposing borrowed Shape resources. Unit global scale and zero skew are required while active. A failed geometry validation leaves the world reusable after correction. The body can exit and re-enter a tree. Circle/rectangle contacts, masks, central impulse, frozen motion, PackedScene state and warmed zero-allocation frame lanes are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs).

Contact monitor events, `PhysicsDirectBodyState`, other force/torque methods, continuous collision modes, material, area damping and custom integration remain incomplete on [RigidBody2D coverage](../coverage/classes/RigidBody2D.md). [ADR 0054](../decisions/physics.md#adr-0054) records the backend and platform boundary.
