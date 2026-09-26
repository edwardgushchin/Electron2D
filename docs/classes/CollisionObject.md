# CollisionObject

Last updated: 2026-09-26

**Inherits:** [Entity](Entity.md), CanvasItem, Node, ElectronObject · **Inherited By:** [PhysicsBody](PhysicsBody.md), [Area](Area.md)

- **Source:** [CollisionObject.cs](../../src/Scene/2D/CollisionObject.cs), [CollisionObject.ShapeOwners.cs](../../src/Scene/2D/CollisionObject.ShapeOwners.cs)
- **Declaration:** `public abstract class CollisionObject : Entity`
- **Component:** [Scene physics bodies](../components/physics-bodies.md)

## Description

The spatial collision-filter base used by scene physics bodies and areas. Its category and mask are unsigned 32-bit values, retaining every reference bit including bit 32. A direct CollisionShape child contributes the fixture; changing layer or mask marks fixtures for reconstruction before the next fixed step. Bodies use reciprocal filters for contact response. A monitoring Area tests its mask against the other object's layer without requiring the other's mask to include the area. The class does not expose a backend ID or implement mouse picking yet.

## API summary

Direct [CollisionPolygon](CollisionPolygon.md) children contribute owned solid or hollow fixture sets beside borrowed [CollisionShape](CollisionShape.md) children. The shared internal owner path exposes a stable opaque RID, and its public owner registry groups borrowed shapes independently of scene children.

| Member | Contract |
| --- | --- |
| `protected CollisionObject()` | Initializes layer and mask to bit one. |
| `public uint CollisionLayer { get; set; }` | Category bits; default 1. |
| `public uint CollisionMask { get; set; }` | Accepted category bits; default 1. |
| `public RID GetRID()` | Stable server identity from construction to disposal. |
| `public bool GetCollisionLayerValue(int layerNumber)` | Tests one-based layer 1–32. |
| `public void SetCollisionLayerValue(int layerNumber, bool value)` | Changes one layer bit. |
| `public bool GetCollisionMaskValue(int layerNumber)` | Tests one-based mask bit 1–32. |
| `public void SetCollisionMaskValue(int layerNumber, bool value)` | Changes one mask bit. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds stored filter bits to inherited Entity descriptors. |

## Member descriptions

<a id="getrid"></a>
### `GetRID()`

Returns the collider's opaque [RID](RID.md) even while detached. Fixture rebuild and scene exit/reentry retain the value; disposal removes the server registration, and later server lookups reject the held RID. A query result also carries this RID and shape-owner index. The method throws after object disposal. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) verifies identity across rebuild and free.

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

[PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs) checks defaults, bit 32, invalid indices, contact filtering and scene storage; [AreaTests](../../tests/Electron2D.Tests/AreaTests.cs) checks directional area filtering. [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) checks stable RID identity. Public shape-owner management, collision priority and viewport mouse-picking callbacks/events retain distinct [coverage gaps](../coverage/classes/CollisionObject2D.md).

## Shape-owner example

Complete resource/configuration snippet; keep the borrowed shape alive while the body uses it:

```csharp
using var circle = new CircleShape { Radius = 12 };
using var body = new StaticBody();
uint ownerID = body.CreateShapeOwner(null);
body.ShapeOwnerAddShape(ownerID, circle);
body.ShapeOwnerSetTransform(ownerID, new Transform(0, Vector2.One, 0, new Vector2(40, 0)));
int globalIndex = body.ShapeOwnerGetShapeIndex(ownerID, 0);
```

## Shape-owner API summary

| Signature | Contract |
| --- | --- |
| `public UInt32 CreateShapeOwner(ElectronObject owner)` | Current owner/slot registry operation. |
| `public Vector2 GetShapeOwnerOneWayCollisionDirection(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Single GetShapeOwnerOneWayCollisionMargin(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public UInt32[] GetShapeOwners()` | Current owner/slot registry operation. |
| `public Boolean IsShapeOwnerDisabled(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Boolean IsShapeOwnerOneWayCollisionEnabled(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Void RemoveShapeOwner(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public UInt32 ShapeFindOwner(Int32 shapeIndex)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerAddShape(UInt32 ownerID, Shape shape)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerClearShapes(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public ElectronObject ShapeOwnerGetOwner(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Shape ShapeOwnerGetShape(UInt32 ownerID, Int32 shapeIndex)` | Current owner/slot registry operation. |
| `public Int32 ShapeOwnerGetShapeCount(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Int32 ShapeOwnerGetShapeIndex(UInt32 ownerID, Int32 shapeIndex)` | Current owner/slot registry operation. |
| `public Transform ShapeOwnerGetTransform(UInt32 ownerID)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerRemoveShape(UInt32 ownerID, Int32 shapeIndex)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerSetDisabled(UInt32 ownerID, Boolean disabled)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerSetOneWayCollision(UInt32 ownerID, Boolean enable)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerSetOneWayCollisionDirection(UInt32 ownerID, Vector2 direction)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerSetOneWayCollisionMargin(UInt32 ownerID, Single margin)` | Current owner/slot registry operation. |
| `public Void ShapeOwnerSetTransform(UInt32 ownerID, Transform transform)` | Current owner/slot registry operation. |

## Shape-owner method descriptions

<a id="createshapeowner"></a>
<a id="removeshapeowner"></a>
<a id="getshapeowners"></a>
<a id="shapeownergetowner"></a>
### Group creation, identity and lifetime

CreateShapeOwner accepts a live ElectronObject or null and stores a weak object identity. Empty registries start at ID zero; subsequent IDs are one above the largest current ID, so deleting the highest group permits its reuse. GetShapeOwners returns an ascending caller-owned uint array including empty/disabled groups. ShapeOwnerGetOwner returns null when the identity is absent, collected or disposed; identity lifetime never removes group shapes. RemoveShapeOwner clears its shapes and configuration. Groups retain borrowed resources until removed or collider disposal, and never dispose caller resources.

Manual groups are transient runtime configuration rather than additional PackedScene properties. Direct CollisionShape/CollisionPolygon children create groups at parenting; their existing node configuration is stored and reconstructs groups when packed. Tree exit/entry preserves group IDs while the parent relationship remains. Unparenting/disposal removes the child's group. The child transform/policy is resynchronized on tree entry; detached transform notifications remain inactive, and active local transform notifications update only the transform. Public owner overrides do not rewrite child node properties; subsequent child edits update only their corresponding field.

<a id="shapeowneraddshape"></a>
<a id="shapeownergetshape"></a>
<a id="shapeownergetshapecount"></a>
<a id="shapeownergetshapeindex"></a>
<a id="shapeownerremoveshape"></a>
<a id="shapeownerclearshapes"></a>
<a id="shapefindowner"></a>
### Group-local shapes and global indices

ShapeOwnerAddShape borrows a live Shape and appends one global logical slot, regardless of owner ID ordering. Each group retains its own local insertion order. ShapeOwnerGetShape preserves resource identity; ShapeOwnerGetShapeCount includes disabled/disposed resources. GetShapeIndex maps a group-local index to the global index returned by queries/motion/contacts. ShapeFindOwner reverses that mapping. Removing any slot shifts all later global indices, including other groups; clear removes all group shapes while preserving ID/configuration. Compound native fixtures from one resource share its single logical index. A CollisionPolygon solid can own several generated convex resources, each with its own logical slot; its accepted hollow mode owns one ConcavePolygonShape.

A disposed borrowed resource removes fixture activity on the next prepare/step but keeps its logical slot/index until explicitly removed. Geometry revision polling reaches fixtures even if an earlier Changed listener throws. Global indices survive resource/transform/filter rebuilds but do not survive structural reindexing; retained collision results describe the indices at their sampled step. KinematicCollision.GetLocalShape/GetColliderShape resolves the current slot's weak owner object, including arbitrary configured identities.

<a id="shapeownersettransform"></a>
<a id="shapeownergettransform"></a>
<a id="shapeownersetdisabled"></a>
<a id="isshapeownerdisabled"></a>
### Transform and disabled policy

Manual groups default to identity transform and enabled state. Local poses are relative to this CollisionObject and require finite translation, unit scale and zero skew. Public invalid transforms reject before mutation; source child scale/skew validation remains a pre-rebuild gate. Disabled groups contribute no body response, direct query fixture or Area sensing, while counts and indices remain unchanged. Changes reach the current native world before a subsequent query/step; an owner controls both scene and manually added resource fixtures through the same path.

<a id="shapeownersetonewaycollision"></a>
<a id="isshapeowneronewaycollisionenabled"></a>
<a id="shapeownersetonewaycollisionmargin"></a>
<a id="getshapeowneronewaycollisionmargin"></a>
<a id="shapeownersetonewaycollisiondirection"></a>
<a id="getshapeowneronewaycollisiondirection"></a>
### One-way policy

New manual groups default to false, zero margin and local down direction. Body setters configure the shared one-way side decision and recovery-depth limit. Margin is finite/nonnegative scene units; direction is finite and normalized, preserving zero. Group rotation transforms the local direction into body coordinates. Child groups copy their configured default margin one. Area one-way setters have no effect, even though a child keeps its own stored options and configuration warning; Area groups retain false/zero/down defaults.

Owner IDs and slot indices are distinct. Missing owner IDs, negative/absent local or global indices throw ArgumentOutOfRangeException. Null/disposed shape additions throw ArgumentNullException/ObjectDisposedException. Malformed numeric values reject before mutation. All attached reads and writes require the SceneTree owner thread; writes also obey scene capture/disposal guards. GetShapeOwners allocates its caller-owned result; unchanged fixture/solver work reuses prepared capacity.

[ShapeOwnerTests](../../tests/Electron2D.Tests/ShapeOwnerTests.cs) verifies defaults, ID reuse, weak identity, interleaved global/local ordering, removals/copies/errors, manual motion/query/solver fixtures, arbitrary owner results, transform/disabled/one-way policies, Area sensing, revision updates after callback failure, parenting/tree/packing lifecycle, resource disposal and 64 warmed solver frames with zero managed allocations on Linux/.NET 10. Native allocation, other platforms and owner visual acceptance remain unverified. See [ADR 0071](../decisions/physics.md#adr-0071).
