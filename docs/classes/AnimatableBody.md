# AnimatableBody

Last updated: 2026-10-09

**Inherits:** [StaticBody](StaticBody.md), [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [AnimatableBody.cs](../../src/Scene/2D/AnimatableBody.cs)
- **Declaration:** `public sealed class AnimatableBody : StaticBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A manually moved 2D body for moving platforms and doors. Direct [CollisionShape](CollisionShape.md) children provide borrowed collision geometry and may select a one-way contact side; the inherited [PhysicsMaterialOverride](StaticBody.md#physicsmaterialoverride) controls surface friction and bounce. The body has zero backend mass and cannot be displaced by contacts or external forces. Its scene target creates linear and angular velocity over the next nonzero fixed step, affecting dynamic bodies it touches. With synchronization enabled, the scene presents that target after the solver step; otherwise the scene changes immediately.

## Example

The inherited collision owner also accepts a direct [CollisionPolygon](CollisionPolygon.md) child; its convex pieces or hollow edges move with the kinematic target.

```csharp
using var platformShape = new RectangleShape { Size = new Vector2(120, 20) };
var platform = new AnimatableBody();
platform.AddChild(new CollisionShape { Shape = platformShape });
// In a physics callback, set platform.Position; the next fixed step moves its collider.
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public AnimatableBody()` | — | Creates a detached kinematic body. |
| `public bool SyncToPhysics { get; set; }` | true | Delays scene presentation of manual motion until the next nonzero fixed step. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores the synchronization flag with inherited body and spatial state. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact body type in PackedScene. |
| `protected override void OnEnterTree()` / `OnExitTree()` | — | Registers/clears kinematic motion and transform notification state. |
| `protected override void OnNotification(int what)` | — | Captures synchronized transform edits and restores the previous visible pose. |

## Member descriptions

<a id="synctophysics"></a>
### `SyncToPhysics`

True by default. Attached local transform writes, including Position and Rotation, send their global target to the kinematic body but immediately restore the last solved scene pose. A nonzero fixed physics step derives backend velocity from the target and step duration, resolves contact, then presents the resulting pose. A zero-length frame leaves the target pending. Several writes before a step use the latest target. The body's transform does not change under contact forces. When false, the scene transform changes immediately and the backend moves to it on the next step. Turning synchronization off while a target is pending presents that target immediately. Attached reads and writes require the scene owner thread.

The target must have unit global scale, zero skew and finite components. An invalid active target restores the prior pose and throws `InvalidOperationException`; the base Entity rejects nonfinite components before mutation. A throwing user local-transform handler leaves a valid target pending. Reentry captures the current pose and clears old targets. The flag survives PackedScene copying. Synchronization controls presentation and does not implement collision sweeps or character movement.

## Inherited surface and lifecycle

The StaticBody material override, PhysicsBody gravity query, CollisionObject filter and Entity spatial API remain available. `GetGravity()` returns zero because the kinematic body has no gravity response. A direct CollisionShape borrows its resource; the scene tree owns backend lifetime. The body supports unshaped motion, geometry edits, filter changes, scene exit/reentry and disposal. The inherited StaticBody conveyor properties remain separate incomplete rows; this subclass gains contact velocity from its actual movement.

## Verification and limits

[AnimatableBodyTests](../../tests/Electron2D.Tests/AnimatableBodyTests.cs) checks a rigid rider on a moving platform, deferred and immediate modes, rotation, zero delta, invalid rollback, callback failure, owner-thread access, reentry, PackedScene and 64 warmed stationary and moving contact frames without managed allocations on Linux/.NET 8. [ADR 0060](../decisions/physics.md#adr-0060) defines the kinematic target policy. CharacterBody now follows accepted floor and wall platform layers under ADR 0067; native allocation, other platforms and owner visual acceptance remain unverified.

## Disabled processing and physics

Inherited `DisableMode.MakeStatic` temporarily replaces the kinematic native type with static. Manual targets retain the existing fixed-step synchronized presentation, but teleport without derived contact velocity. Enable/KeepActive restores the kinematic role; exit/reentry applies the effective inherited policy. Remove omits fixtures while retaining stored scene state and shape owners. See [CollisionObject.DisableMode](CollisionObject.md#disablemode) and [ADR 0072](../decisions/physics.md#adr-0072).

## Idle and fast kinematic paths

Synchronized solved poses stop their derived velocity on unchanged frames without rebuilding a target from the native angle decoder. Current scene/server kinematic roles can subdivide world integration when travel would otherwise cross a dynamic body between collision updates; outer callback/event timing remains one fixed frame. [RigidFreezeModeTests](../../tests/Electron2D.Tests/RigidFreezeModeTests.cs) checks synchronized sibling idle rotation and [ADR 0075](../decisions/physics.md#adr-0075) defines the shared integration policy and performance/precision limits.

Inherited `ConstantLinearVelocity` and `ConstantAngularVelocity` add virtual surface
motion to contact and point-query velocity. They never add displacement to the
manual target. Kinematic subdivision uses actual target travel only.
PhysicsSurfaceVelocityTests verifies linear/angular surface state, target motion
and character platform carry on CPU/GPU.


[Shared public CPU/GPU scene checks](../components/physics-backends.md#public-scene-motion-conformance)
now run AnimatableBodyTests with explicit backend selection. The linked record
separates verified motion/lifetime/allocation cases from remaining physics coverage.
