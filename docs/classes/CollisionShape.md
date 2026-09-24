# CollisionShape

Last updated: 2026-09-25

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [CollisionShape.cs](../../src/Scene/2D/CollisionShape.cs)
- **Declaration:** `public sealed class CollisionShape : Entity`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

Places one borrowed [Shape](Shape.md) as a direct child of a [PhysicsBody](PhysicsBody.md) or [Area](Area.md). Only a direct collision-object parent contributes a physics fixture. Area fixtures are nonresponding sensors; body fixtures participate in collision response. Local position and rotation offset that fixture; active scale/skew are rejected. The caller owns the Shape resource. Assignment, resource changes, disabling and tree transitions update fixtures before the next physics step. A missing parent or shape is reported through configuration warnings.

## Example

```csharp
using var geometry = new RectangleShape { Size = new Vector2(20, 20) };
var body = new RigidBody();
body.AddChild(new CollisionShape { Shape = geometry });
// Retain geometry while the body borrows it.
```

## API summary

| Member | Contract |
| --- | --- |
| `public CollisionShape()` | Detached node with null shape and enabled collision slot. |
| `public Shape? Shape { get; set; }` | Borrowed geometry, null by default. |
| `public bool Disabled { get; set; }` | Excludes its fixture when true; false by default. |
| `public override string[] GetConfigurationWarnings()` | Reports missing direct collision-object parent and/or live geometry. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored Shape and Disabled descriptors to spatial state. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Restores this exact node type in PackedScene. |
| `protected override void OnEnterTree()` / `OnExitTree()` | Registers or unregisters the direct body or area slot. |
| `protected override void Dispose(bool disposing)` | Disconnects borrowed resource events and removes the slot. |

## Property descriptions

<a id="shape"></a>
### `Shape`

Accepts a live CircleShape, CapsuleShape, RectangleShape or null. A disposed shape throws before assignment. The old resource's change/disposal listeners are removed and the new one's installed; the node does not dispose either. Successful assignment marks its parent's fixtures dirty and refreshes configuration warnings. A Changed callback failure from a warning observer may propagate after assignment.

<a id="disabled"></a>
### `Disabled`

Changing it marks the parent fixture set dirty. A disabled child remains in the scene and retains its borrowed resource, but contributes no Box2D geometry. The next fixed step applies the change.

## Lifecycle, errors and limits

The child registers during tree entry and unregisters before its exit finishes; scene disposal disconnects resource listeners. A disposed borrowed Shape becomes ineligible at the next step. A scale or skew change on an active shape fails before replacing the parent's existing fixtures; correction permits a later step. Owner-thread scene mutation follows Node and Entity. One-way collision and debug color are unfinished on [CollisionShape2D coverage](../coverage/classes/CollisionShape2D.md).

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks parent/shape diagnostics, live shape edits, disablement, PackedScene restoration and borrowed lifetime. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks area ownership and sensor fixture edits. [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks borrowed capsule edits, rotation and area sensors. [ADRs 0055 and 0059](../decisions/physics.md#adr-0059) record the area and capsule extensions.
