# CollisionPolygon

Last updated: 2026-10-09

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [CollisionPolygon.cs](../../src/Scene/2D/CollisionPolygon.cs)
- **Declaration:** `public sealed class CollisionPolygon : Entity`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

An editable collision contour that must be a direct child of a [PhysicsBody](PhysicsBody.md) or [Area](Area.md) to contribute geometry. `BuildMode=Solids` partitions a valid concave or convex contour into solid convex fixtures; `Segments` closes the vertex list and contributes only hollow edges. The node owns its generated resources, so no Shape resource needs to be retained by the caller. Local position and rotation place all pieces; active scale/skew are rejected. Body fixtures can be one-way, while an Area still senses either edge side. Edits reach the physics world before the next nonzero fixed step.

## Example

```csharp
var ground = new StaticBody();
ground.AddChild(new CollisionPolygon
{
    Polygon = [new(-80, 0), new(80, 0), new(80, 20), new(-80, 20)]
});
// Add ground to a SceneTree; the polygon creates its own solid fixtures.
```

## API summary

| Member | Contract |
| --- | --- |
| `public CollisionPolygon()` | Creates an empty solid polygon with no fixtures. |
| `public PolygonBuildMode BuildMode { get; set; }` | Chooses solid interior or closed hollow edges; Solids by default. |
| `public Vector2[] Polygon { get; set; }` | Copied local vertices; empty by default. |
| `public bool Disabled { get; set; }` | Removes fixtures while retaining the contour; false by default. |
| `public bool OneWayCollision { get; set; }` | Selects the side of body contact; false by default. |
| `public float OneWayCollisionMargin { get; set; }` | Maximum accepted one-way recovery depth; one scene unit by default. |
| `public Vector2 OneWayCollisionDirection { get; set; }` | Local pass-through direction; `(0, 1)` by default. |
| `public override string[] GetConfigurationWarnings()` | Reports an absent direct owner, empty/invalid geometry or ineffective Area one-way setting. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores mode, contour, disabled and one-way state. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores this node type in PackedScene. |
| `protected override void OnNotification(int what)` / `OnEnterTree()` | Creates/removes the owner group at parenting/unparenting and synchronizes configuration on tree entry. |
| `protected override void Dispose(bool disposing)` | Removes the slot and disposes internally generated resources. |

## Property descriptions

<a id="buildmode"></a>
### `BuildMode`

[`PolygonBuildMode.Solids`](PolygonBuildMode.md) is the default and uses convex decomposition for the filled region, including valid concave contours. `Segments` joins each vertex to the next and the last to the first; a body wholly inside the outline does not touch it. Fewer than three solid vertices or two segment vertices yield no fixtures and a warning. An undefined enum value throws `ArgumentOutOfRangeException` without changing mode or geometry. A valid mode change rebuilds fixtures before the next step and survives PackedScene.

<a id="polygon"></a>
### `Polygon`

Gets and sets caller-owned copies of vertices in this node's local coordinate space. Null throws `ArgumentNullException`; nonfinite coordinates or overflowing overall bounds throw `ArgumentException` before mutation. Empty, degenerate or undecomposable solid contours remain editable but contribute no fixtures. A valid assignment replaces generated resources and marks an attached owner for rebuilding even if a configuration-warning listener throws afterward. An Area with solid mode detects overlap in every convex piece; segment mode detects only touching edges.

<a id="disabled"></a>
### `Disabled`

True prevents this node from contributing fixtures without clearing its contour or mode. Changing it marks only the owning object's fixture set dirty; the next physics step applies the change. False by default and stored in PackedScene.

<a id="onewaycollision"></a>
### `OneWayCollision`

On a physics-body child, true uses the first contact's side to allow the face opposite `OneWayCollisionDirection` and permit travel through the other face. The decision stays with the fixture pair until separation. It applies to every generated solid or segment fixture and rebuilds on change. An Area ignores response filtering, continues two-sided sensing and reports a configuration warning. False by default and stored in PackedScene.

<a id="onewaycollisiondirection"></a>
### `OneWayCollisionDirection`

The pass-through direction rotates with this node and its parent body. It defaults to `(0, 1)`; finite nonzero input is normalized, zero rejects all contacts while one-way mode is enabled, and nonfinite input throws `ArgumentOutOfRangeException` before mutation. Changing it rebuilds active body fixtures before the next step. It is stored in PackedScene.

<a id="onewaycollisionmargin"></a>
### `OneWayCollisionMargin`

The default one scene unit bounds accepted recovery depth against this polygon's one-way body fixtures. Body motion uses at least its own safe margin; deeper initial overlap is ignored for one-way recovery. The property is finite and nonnegative, rejects invalid writes before mutation, rebuilds active fixtures before the next query or step and survives PackedScene. Area sensors ignore it.

## Lifecycle, errors and limits

The node creates its group under a direct CollisionObject parent at parenting, synchronizes transform/policy on tree entry, and removes the group at unparenting or disposal. Tree exit alone retains it. Generated ConvexPolygonShape or ConcavePolygonShape resources are private and owned by this node; callers own only the scene node and vertex arrays they pass. Polygon resources are regenerated before replacement, so invalid numeric writes retain the former state. The owning body or Area rejects active scale/skew before destroying its existing fixtures, and a corrected transform allows a later step. Scene mutation follows Node's owner-thread rule. All own [coverage rows](../coverage/classes/CollisionPolygon2D.md) now have executable behavior; inherited body and Shape gaps remain separate.

[CollisionPolygonTests](../../tests/Electron2D.Tests/CollisionPolygonTests.cs) checks defaults, copies, malformed contour, errors, solid concavity, hollow edges, direct mixed owners, one-way traversal, live rebuild after callback failure, PackedScene and 64 warmed contact frames without managed allocation on Linux/.NET 8. [PhysicsMotionTests](../../tests/Electron2D.Tests/PhysicsMotionTests.cs) verifies its one-way margin in recovery. Native allocator counts, other platforms and owner visual acceptance remain unverified. See [ADR 0066](../decisions/physics.md#adr-0066).

The [CollisionObject owner registry](CollisionObject.md#createshapeowner) now supplies logical shape slots for both child and manual groups. Query/contact indices identify global slots, while ShapeFindOwner returns the distinct group ID; removal shifts later indices. Motion owner accessors resolve weak configured objects as well as child nodes. See [ADR 0071](../decisions/physics.md#adr-0071).

[Physics canvas diagnostics](../components/physics-debug.md) describes this node's retained geometry, live redraw, ordinary canvas behavior and CPU/GPU/native checks.
