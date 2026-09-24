# Area

Last updated: 2026-09-24

**Inherits:** [CollisionObject](CollisionObject.md), [Entity](Entity.md), CanvasItem, Node, ElectronObject

- **Source:** [Area.cs](../../src/Scene/2D/Area.cs)
- **Declaration:** `public sealed class Area : CollisionObject`
- **Component:** [Physics areas](../components/physics-areas.md)

## Description

A nonresponding 2D sensor region. Direct [CollisionShape](CollisionShape.md) children supply borrowed circle or rectangle geometry. After each nonzero fixed physics step, the area records overlapping `PhysicsBody` and other `Area` nodes; moving a node or editing a filter does not immediately change the snapshot. Events are delivered after body synchronization and before physics timers and tweens. The area's `CollisionMask` tests the other object's `CollisionLayer`; the other object's mask can be zero. Other areas also need `Monitorable=true` to be reported. The area can monitor even when its own `Monitorable` is false.

## Example

```csharp
using var region = new RectangleShape { Size = new Vector2(80, 80) };
var trigger = new Area();
trigger.AddChild(new CollisionShape { Shape = region });
trigger.BodyEntered += body => Console.WriteLine(body.Name);
// Add trigger to a SceneTree, then advance a fixed physics frame.
```

## API summary

| Member | Default | Contract |
| --- | --- | --- |
| `public Area()` | — | Creates a detached monitoring, monitorable area. |
| `public bool Monitoring { get; set; }` | true | Enables this area's detection after the next fixed step. |
| `public bool Monitorable { get; set; }` | true | Makes this area detectable by another area. |
| `public Area[] GetOverlappingAreas()` | — | Returns a caller-owned copy of the latest area snapshot. |
| `public Entity[] GetOverlappingBodies()` | — | Returns a caller-owned copy of the latest body snapshot. |
| `public bool HasOverlappingAreas()` | — | Tests the area snapshot without creating an array. |
| `public bool HasOverlappingBodies()` | — | Tests the body snapshot without creating an array. |
| `public bool OverlapsArea(Node? area)` | — | Tests membership; null and unrelated nodes return false. |
| `public bool OverlapsBody(Node? body)` | — | Tests membership; null and unrelated nodes return false. |
| `public event Action<Area>? AreaEntered` / `AreaExited` | — | Reports another area entering or leaving. |
| `public event Action<Entity>? BodyEntered` / `BodyExited` | — | Reports a physics body entering or leaving. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | — | Stores monitoring flags for PackedScene. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | — | Restores the exact area type in PackedScene. |
| `protected override void OnEnterTree()` / `OnExitTree()` | — | Joins or leaves this SceneTree's physics world. |
| `protected override void Dispose(bool disposing)` | — | Releases backend fixtures and overlap snapshots. |

## Property descriptions

<a id="monitoring"></a>
### `Monitoring` and `Monitorable`

Both default to true and are stored with the scene. `Monitoring=false` clears this area's overlaps on its next nonzero physics step and emits exits. `Monitorable=false` removes this area from other areas' snapshots on their next step, while this area's own detection continues. A body's mask and an area's own `Monitorable` do not gate this area's body detection. The inherited layer and mask use all 32 bits.

## Method descriptions

<a id="overlaps"></a>
### Overlap queries

The six query methods read the most recently committed snapshot, even after a node moves. Area and body arrays are fresh caller-owned arrays; `HasOverlapping*` and `Overlaps*` avoid array allocation. `GetOverlappingBodies()` returns the implemented `PhysicsBody` family as `Entity` values, preserving the spatial role for future collider owners. Detached queries return empty results. Attached queries require the SceneTree owner thread. Removing an object from its tree clears it from other areas' snapshots without waiting for another frame.

## Event descriptions

<a id="events"></a>
### Area and body entry/exit events

Object-level events are raised once per other node even when multiple shape pairs overlap. The argument is the other area or spatial body. Physics backend stepping and body synchronization finish before callbacks; event handlers may remove or reparent nodes. A throwing handler does not replay the committed transition on the next frame, and other queued transitions still run before an aggregate exception reaches the physics-frame caller. Zero-delta frames do not change snapshots or emit events. Shape-index events require typed physics identity and shape-owner APIs and remain separate coverage rows.

## Lifecycle, errors and verification

The area owns its backend sensor fixtures, while its child shapes borrow caller-owned Shape resources. A moving body passes through without collision response. Active area and shape transforms require unit scale and zero skew; an invalid transform fails the step before replacing geometry, and a corrected step can continue. Filter, disabled-state and shape changes take effect on the next step. Exiting or disposing removes backend state; re-entry creates new fixtures. [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional filtering including body mask zero, area monitorability, multi-shape deduplication, moving-body passage, live geometry, packing, tree removal, callback failure and mutation, and 64 warmed steady and empty frames each with zero managed allocations on Linux/.NET 8. Native allocation, other platforms and large-scene performance remain unverified.

The body-result and body-event methods currently cover `PhysicsBody` nodes; tile-map virtual collision bodies are not yet integrated. Gravity and damping overrides, priority, audio-bus routing and shape-index events are incomplete on [Area2D coverage](../coverage/classes/Area2D.md). [ADR 0055](../decisions/physics.md#adr-0055) records this sensor profile.
