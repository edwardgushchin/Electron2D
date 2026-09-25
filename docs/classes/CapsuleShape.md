# CapsuleShape

Last updated: 2026-09-25

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs)
- **Declaration:** `public sealed class CapsuleShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

A caller-owned vertical capsule resource for rigid, static and area fixtures. Two semicircular ends are separated by a central segment. A direct [CollisionShape](CollisionShape.md) child borrows the resource; local position and rotation place it on its parent. Changes rebuild attached fixtures before the next fixed step. A copied resource has independent dimensions.

## Example

```csharp
using var capsule = new CapsuleShape { Radius = 8, MidHeight = 24 };
var body = new RigidBody();
body.AddChild(new CollisionShape { Shape = capsule });
// Keep capsule alive while the body borrows it.
```

## API summary

| Member | Contract |
| --- | --- |
| `public CapsuleShape()` | Creates radius 10, full height 30 and middle height 10. |
| `public float Radius { get; set; }` | End-cap radius; increasing it can increase Height. |
| `public float Height { get; set; }` | Full height including both caps; reducing it can reduce Radius. |
| `public float MidHeight { get; set; }` | Distance between cap centers, equal to Height minus twice Radius. |
| `public override Rect2 GetRect()` | Returns exact local bounds `(-Radius, -Height / 2, 2 × Radius, Height)`. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent capsule for Resource copying. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies both linked dimensions without publishing intermediate changes. |

## Property descriptions

<a id="radius"></a>
### `Radius`

The default is 10 scene units. It accepts nonnegative finite values whose diameter remains finite. Increasing Radius beyond half Height also raises Height to twice Radius; a repeated equal assignment does not emit `Changed`. Negative, nonfinite and diameter-overflow values throw `ArgumentOutOfRangeException` before mutation. Zero radius produces a line-segment capsule while the middle height is positive.

<a id="height"></a>
### `Height`

The default is 30 scene units and includes both end caps. It accepts nonnegative finite values. Reducing Height below twice Radius also lowers Radius to half Height; a repeated equal assignment does not emit `Changed`. Invalid input throws before mutation. Zero height yields zero radius and point geometry.

<a id="midheight"></a>
### `MidHeight`

The default is 10 scene units. Assigning a nonnegative finite middle height sets full Height to that value plus twice Radius and emits `Changed`, including an equal assignment. Overflow rejects before mutation. Zero middle height has exact circular geometry. The backend also uses a circle for a positive center segment at or below its linear slop, currently 0.5 scene units; the stored dimensions and `GetRect()` remain exact.

## Method and lifecycle descriptions

<a id="getrect"></a>
### `GetRect()`

Returns value-copy local bounds centered on the capsule origin. It does not require an attached body or backend world. A disposed resource throws `ObjectDisposedException`.

The concrete Resource copy hooks preserve Radius and Height independently of the original. A `CollisionShape` borrows either instance and releases its event connection on replacement or disposal. Geometry revisions let an attached body detect a successful edit even if an earlier user `Changed` listener throws. Attached setters are resource operations; scene mutation and physics stepping remain on the tree owner thread.

## Verification and limits

[CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks linked defaults, equal writes, zero/invalid/overflow values, exact bounds, independent duplication, PackedScene borrowing, rotated rigid-body contacts, live resource edits after callback failure, capsule area detection and 64 warmed active-contact/area frames with zero managed allocations on Linux/.NET 8. The small-segment circle substitution follows [ADR 0059](../decisions/physics.md#adr-0059); native allocator, other platforms and owner visual acceptance remain unverified. Inherited standalone shape queries and debug drawing retain their own [coverage](../coverage/classes/Shape2D.md).

Direct [shape queries](PhysicsDirectSpaceState2D.md) use the current capsule, including the sub-slop circle substitution; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) verifies its overlap and contact families.
