# ConvexPolygonShape

Last updated: 2026-10-08

**Inherits:** [Shape](Shape.md), [Resource](Resource.md)

- **Source:** [ConvexPolygonShape.cs](../../src/Scene/Resources/ConvexPolygonShape.cs)
- **Declaration:** `public sealed class ConvexPolygonShape : Shape`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

A caller-owned solid convex polygon resource. It stores perimeter vertices independently of the caller's array and can generate a convex hull from a point cloud. A direct [CollisionShape](CollisionShape.md) child borrows the resource and applies its local position and rotation. Bodies and areas keep one resource association even when a large polygon needs several internal fixtures. An empty resource contributes no fixture.

## Example

```csharp
using var hull = new ConvexPolygonShape();
hull.SetPointCloud([new Vector2(-20, 10), new Vector2(20, 10),
    new Vector2(0, -15), new Vector2(0, 0)]);
var body = new RigidBody();
body.AddChild(new CollisionShape { Shape = hull });
// Keep hull alive while the body borrows it.
```

## API summary

| Member | Contract |
| --- | --- |
| `public ConvexPolygonShape()` | Creates an empty polygon. |
| `public Vector2[] Points { get; set; }` | Gets or sets a caller-owned array of convex perimeter vertices. |
| `public void SetPointCloud(ReadOnlySpan<Vector2> pointCloud)` | Replaces Points with the closed convex hull of an unordered cloud. |
| `public override Rect2 GetRect()` | Returns exact local-axis bounds, or default for an empty contour. |
| `protected override Resource CreateDuplicateInstance()` | Creates an independent polygon resource. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies authored vertices without publishing intermediate changes; compiled pieces belong to the backend cache. |

## Member descriptions

<a id="points"></a>
### `Points`

The default is empty. Reads return a new array; writes copy the input, so a caller cannot mutate stored geometry through an old array. Clockwise and counterclockwise contours are accepted, with an optional repeated first vertex closing the list. A successful assignment publishes `Changed`, including an equal assignment; empty input clears all fixtures at the next fixed step. A nonempty contour must contain at least three finite, noncollinear perimeter points and form a convex polygon without crossing edges. Invalid input throws `ArgumentException` before state changes; null throws `ArgumentNullException`. The backend's minimum polygon size is checked before commit.

<a id="setpointcloud"></a>
### `SetPointCloud(ReadOnlySpan<Vector2> pointCloud)`

Uses [Geometry.ConvexHull](Geometry.md) to discard interior points and assign the resulting closed contour through `Points`. Input order is irrelevant and caller storage is not retained. Fewer than three noncollinear hull points or an invalid coordinate throws before changing the resource.

<a id="getrect"></a>
### `GetRect()`

Returns a value-copy tight bounding rectangle around stored vertices. Its origin need not be the polygon origin; an empty resource returns the default rectangle. The query needs no SceneTree and rejects use after disposal.

## Physics behavior and verification

The public polygon remains one borrowed resource. The backend accepts at most eight vertices per fixture, so longer contours use adjacent convex fan pieces; body and area snapshots still deduplicate contacts by owner. Each piece contributes to dynamic mass and inertia. Local CollisionShape rotation and position transform every piece. Existing circle, capsule, segment and rectangle resources continue to append one fixture each. Resource duplication owns independent authored vertices. PhysicsShapeBackend retains compiled hulls in a weak resource-keyed cache, compiling a duplicate on first use and replacing successful edits before their Changed notifications.

The shared/copy regression checks two borrowers, an independent duplicate, rejected
small contours and live point/shape query results. Alternating between the two
compiled resources for 128 warmed queries allocates zero managed bytes on the
checked Linux/.NET 10 runtime.

[ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks winding, copied arrays, cloud hulls, invalid and crossing contours, twelve-vertex body/area integration including a probe in a later piece, rotated fixtures, live edits despite a throwing `Changed` listener, removal of all fixtures, PackedScene borrowing and 64 warmed unchanged multi-fixture frames without managed allocations on Linux/.NET 8. [ADR 0062](../decisions/physics.md#adr-0062) records the compound-fixture boundary. Native allocator, other platforms, owner visual acceptance, concave decomposition and inherited drawing and custom solver bias remain unverified or incomplete.

Direct [shape queries](PhysicsDirectSpaceState.md) test all convex pieces while returning one shape-owner hit per collider; [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) covers a twelve-vertex query.

Inherited [Shape collision methods](Shape.md#collide) now test posed resources and independently swept regions without a SceneTree. ShapeCollisionTests verifies this family under [ADR 0069](../decisions/physics.md#adr-0069), including caller/other boundary-point ordering, lifetime and the sixteen-pair cap.

Inherited [Shape.CustomSolverBias](Shape.md#customsolverbias) is stored and copied
alongside geometry. CopyCustomStateTo calls the Shape base and publishes the changed
geometry revision, including in-place copying into a resource with existing borrowers.
PhysicsContactPolicyTests checks duplication, in-place copy and .e2dres round trips.

<a id="getpropertydescriptors"></a>
`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` combines
inherited resource/policy descriptors with this shape's authored geometry for storage.
The built-in resource file registry constructs this concrete shape on load.
