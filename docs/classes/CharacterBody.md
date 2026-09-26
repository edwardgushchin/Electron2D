# CharacterBody

Last updated: 2026-09-26

**Inherits:** [PhysicsBody](PhysicsBody.md), [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [CharacterBody.cs](../../src/Scene/2D/CharacterBody.cs), [CharacterBody.Motion.cs](../../src/Scene/2D/CharacterBody.Motion.cs)
- **Declaration:** `public partial class CharacterBody : PhysicsBody`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

A caller-driven kinematic body that turns `Velocity` into safe travel and slides along body contacts. [MoveAndSlide](#moveandslide) classifies floor, wall and ceiling in grounded mode; floating mode treats every hit as a wall. The body borrows direct [CollisionShape](CollisionShape.md) or [CollisionPolygon](CollisionPolygon.md) children, shares the SceneTree physics space, and records caller-owned [KinematicCollision2D](KinematicCollision2D.md) snapshots. A call from a physics callback uses that delivered delta; a process callback uses its process delta. An attached call outside a callback uses the last delivered physics delta. Before the first frame it uses 1/60 second.

## Example

Partial snippet inside a character subclass's fixed physics callback:

```csharp
protected override void OnPhysicsProcess(double delta)
{
    Velocity += GetGravity() * (float)delta;
    MoveAndSlide();
}
```

The subclass needs a live collision child and `PhysicsProcessEnabled = true`. Gravity is queried; the character does not add it to `Velocity` automatically.

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public CharacterBody()` | — | Grounded character with zero velocity. |
| `public Vector2 Velocity { get; set; }` | zero | Desired scene units per second; finite with finite length. |
| `public CharacterMotionMode MotionMode { get; set; }` | Grounded | Floor/ceiling classification or floating all-wall motion. |
| `public CharacterPlatformOnLeave PlatformOnLeave { get; set; }` | AddVelocity | Transfer platform point velocity when leaving it. |
| `public float SafeMargin { get; set; }` | 0.08 | Finite nonnegative contact recovery margin, scene units. |
| `public bool FloorStopOnSlope { get; set; }` | true | Stop downward velocity on floor contact. |
| `public bool FloorConstantSpeed { get; set; }` | false | Preserve lateral travel speed over floor slopes. |
| `public bool FloorBlockOnWall { get; set; }` | true | Block forward floor movement at a wall. |
| `public bool SlideOnCeiling { get; set; }` | true | Slide along upward ceiling contact. |
| `public int MaxSlides { get; set; }` | 4 | Positive cap on movement iterations. |
| `public float FloorMaxAngle { get; set; }` | π/4 | Maximum classified floor angle, radians. |
| `public float FloorSnapLength { get; set; }` | 1 | Finite nonnegative floor snap distance, scene units. |
| `public float WallMinSlideAngle { get; set; }` | π/12 | Floating-mode minimum wall slide angle, radians. |
| `public Vector2 UpDirection { get; set; }` | (0, -1) | Finite nonzero normalized grounded up direction. |
| `public uint PlatformFloorLayers { get; set; }` | all bits | Floor platform layers eligible for carry. |
| `public uint PlatformWallLayers { get; set; }` | zero | Wall platform layers eligible for carry. |
| `public bool MoveAndSlide()` | — | Move over current frame delta; return whether a contact was recorded. |
| `public void ApplyFloorSnap()` | — | Snap toward a qualifying floor when not already on one. |
| `public bool IsOnFloor()` / `IsOnWall()` / `IsOnCeiling()` | false | Last movement classification. |
| `public bool IsOnFloorOnly()` / `IsOnWallOnly()` / `IsOnCeilingOnly()` | false | Exclusive last movement classification. |
| `public Vector2 GetFloorNormal()` / `GetWallNormal()` | zero | Last classified global normals. |
| `public float GetFloorAngle(Vector2? upDirection = null)` | — | Positive angle to global up by default, radians. |
| `public Vector2 GetLastMotion()` | zero | Travel of the last slide, scene units. |
| `public Vector2 GetPositionDelta()` | — | Global translation since the last MoveAndSlide began; meaningful after a movement call. |
| `public Vector2 GetRealVelocity()` | zero | Actual displacement over that frame delta, scene units per second. |
| `public Vector2 GetPlatformVelocity()` | zero | Last contacted platform point velocity, scene units per second. |
| `public int GetSlideCollisionCount()` | 0 | Number of recorded motion contacts. |
| `public KinematicCollision2D GetSlideCollision(int index)` / `GetLastSlideCollision()` | — / null | Caller-owned contact snapshots. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores all query options, not transient contact state. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact CharacterBody type in PackedScene. |
| `protected override void OnEnterTree()` | — | Resets transient floor/wall/platform and slide state. |

## Property descriptions

`Velocity` is desired velocity. `MoveAndSlide()` multiplies it by the current frame delta and may remove a vertical component after floor or ceiling contact. It does not apply gravity automatically. Invalid or overflowing values reject before mutation. `MotionMode` takes [CharacterMotionMode](CharacterMotionMode.md); `PlatformOnLeave` takes [CharacterPlatformOnLeave](CharacterPlatformOnLeave.md). Undefined enum values reject. These named enums are separate types because C# cannot use `MotionMode` or `PlatformOnLeave` as both an enum and property identifier.

`SafeMargin` affects initial depenetration. `FloorMaxAngle` and `UpDirection` classify a contact as floor or ceiling; other contacts become walls. `UpDirection` normalizes finite nonzero input, and `GetFloorAngle` defaults to global `(0, -1)`. `FloorSnapLength` sets a minimum downward probe of at least `SafeMargin`. `FloorStopOnSlope`, `FloorConstantSpeed`, `FloorBlockOnWall`, `SlideOnCeiling`, `WallMinSlideAngle` and `MaxSlides` control the grounded/floating branches described below. `MaxSlides` must be at least one; angle settings must be finite, and snap/margin lengths must be nonnegative. Invalid assignments retain the prior value.

`PlatformFloorLayers` and `PlatformWallLayers` gate carry from a touched body's collision layer. A qualifying floor or wall contact stores that body's RID, layer and point velocity. At the next call, the character samples current velocity at its position, carries by velocity times delta while excluding the platform RID, and then performs its own movement. When contact is lost, `PlatformOnLeave` controls how much of the last platform velocity joins `Velocity`. Each of the 32 layer bits is supported.

## Method descriptions

<a id="moveandslide"></a>
### `MoveAndSlide()`

Requires an attached SceneTree and its owner thread. It prepares current body/Area fixtures before replacing the previous contact snapshot, so a preparation failure leaves the prior snapshot and pose intact. It first applies accepted platform carry, then sweeps up to `MaxSlides` times. Grounded mode classifies contacts against `UpDirection`, stops downward motion on a floor when configured, projects remainder along a surface, applies constant-speed slope adjustment and optionally snaps a previously grounded body back to a downward slope. Floating mode treats every hit as a wall and respects `WallMinSlideAngle`. A zero or blocked motion can still report initial recovery when `SafeMargin` reaches a body. The return value is true if any contact was recorded. A missing collision returns false and leaves the slide list empty.

The SceneTree prepares the visible character pose for direct queries immediately after movement. During the following fixed solver step, Box2D receives a kinematic target measured from the preceding solved pose so its contact velocity remains available to other bodies. The character snapshot itself remains scene-owned and is not rewritten by backend integration. `GetPositionDelta()` reads current global position minus the start position of the last call; `GetRealVelocity()` uses the actual delta of that call. With no delivered frame yet, the first call uses 1/60 second; a zero delivered delta yields zero real velocity.

### `ApplyFloorSnap()` and contact getters

`ApplyFloorSnap()` does nothing when already on a floor. Otherwise it probes down by the larger of `FloorSnapLength` and `SafeMargin`, accepts a normal within `FloorMaxAngle`, stores floor/platform state and applies only the permitted up-axis travel. `IsOnFloor`, `IsOnWall`, `IsOnCeiling` and their `Only` variants describe the last completed movement, not future contacts. Floor/wall normals are global and copied. `GetSlideCollisionCount()` reports the stored list length; `GetSlideCollision(index)` rejects an invalid index, while `GetLastSlideCollision()` returns null on an empty list. Returned contact objects are independent caller-owned snapshots; later movement does not overwrite them. Attached reads require the owner thread.

## Lifecycle, verification and limits

Tree entry resets transient collision and platform state while keeping configured options and `Velocity`. The body and its collision child obey the inherited unit-scale, zero-skew physics profile. [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks defaults, validation, PackedScene, floor/wall/ceiling and floating classification, snap, slope speed and angle, ceiling/wall controls, slide cap, floor/wall platform masks, all leave policies, Area gravity, immediate direct-query pose, fixed-lane backend sync, failure recovery and warmed zero-allocation calls on Linux/.NET 8. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) covers the shared initial-contact slope fix. Separation-ray-specific floor participation remains [Partial](../coverage/classes/CharacterBody2D.md) until that shape family exists. Native allocator, other platforms, large-world throughput and owner visual acceptance remain unverified. See [ADR 0067](../decisions/physics.md#adr-0067).
