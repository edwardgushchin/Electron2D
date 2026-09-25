# CollisionObject

Last updated: 2026-09-25

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [PhysicsBody](PhysicsBody.md), [Area](Area.md)

- **Source:** [CollisionObject.cs](../../src/Scene/2D/CollisionObject.cs)
- **Declaration:** `public abstract class CollisionObject : Entity`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

The spatial collision-filter base used by scene physics bodies and areas. Its category and mask are unsigned 32-bit values, retaining every reference bit including bit 32. A direct CollisionShape child contributes the fixture; changing layer or mask marks fixtures for reconstruction before the next fixed step. Bodies use reciprocal filters for contact response. A monitoring Area tests its mask against the other object's layer without requiring the other's mask to include the area. The class does not expose a backend ID or implement mouse picking yet.

## API summary

Direct [CollisionPolygon](CollisionPolygon.md) children now contribute owned solid or hollow fixture sets beside borrowed [CollisionShape](CollisionShape.md) children. The shared internal owner path does not expose the reference's public shape-owner methods or a backend RID.

| Member | Contract |
| --- | --- |
| `protected CollisionObject()` | Initializes layer and mask to bit one. |
| `public uint CollisionLayer { get; set; }` | Category bits; default 1. |
| `public uint CollisionMask { get; set; }` | Accepted category bits; default 1. |
| `public bool GetCollisionLayerValue(int layerNumber)` | Tests one-based layer 1–32. |
| `public void SetCollisionLayerValue(int layerNumber, bool value)` | Changes one layer bit. |
| `public bool GetCollisionMaskValue(int layerNumber)` | Tests one-based mask bit 1–32. |
| `public void SetCollisionMaskValue(int layerNumber, bool value)` | Changes one mask bit. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored filter bits to inherited Entity descriptors. |

## Member descriptions

<a id="collisionlayer"></a>
### `CollisionLayer`

Any 32-bit mask is valid, including zero. Assignment marks attached collision shapes dirty; the next physics step installs the new backend category. This property does not own or dispose shapes.

<a id="collisionmask"></a>
### `CollisionMask`

Any 32-bit mask is valid, including zero. Assignment marks shapes dirty and changes which categories can generate contact response or area detection after the next step.

<a id="bitmethods"></a>
### Layer and mask bit methods

The two getters and two setters take a one-based bit number from 1 through 32. Values outside that range throw `ArgumentOutOfRangeException` without changing the mask. Setters retain all other bits. A body must be attached to a scene to affect live physics, but detached filter state is stored for later attachment and PackedScene capture.

## Limits and verification

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, bit 32, invalid indices, contact filtering and scene storage; [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional area filtering. Shape-owner management, disable modes, collision priority, backend RID and viewport mouse-picking callbacks/events retain distinct [coverage gaps](../coverage/classes/CollisionObject2D.md).
