# Shape

Last updated: 2026-10-08

**Inherits:** [Resource](Resource.md) · **Inherited By:** [CircleShape](CircleShape.md), [CapsuleShape](CapsuleShape.md), [SegmentShape](SegmentShape.md), [SeparationRayShape](SeparationRayShape.md), [ConvexPolygonShape](ConvexPolygonShape.md), [ConcavePolygonShape](ConcavePolygonShape.md), [RectangleShape](RectangleShape.md)

- **Source:** [Shape.cs](../../src/Scene/Resources/Shape.cs), [Shape.Collision.cs](../../src/Scene/Resources/Shape.Collision.cs)
- **Declaration:** `public abstract class Shape : Resource`
- **Component:** [Collision shapes](../components/physics-shapes.md)

## Description

The reusable 2D collision-geometry role. The caller owns a Shape resource; a [CollisionShape](CollisionShape.md) borrows it for a direct physics-body or [Area](Area.md) parent. [PhysicsShapeQueryParameters](PhysicsShapeQueryParameters.md) can also borrow it for direct shape queries, lazily registering a physics RID that remains stable through edits and is released on disposal. `Changed` invalidates the parent's and borrowed server fixtures before their next fixed step or direct query. Geometry revisions also let attached owners detect an edit when an earlier user `Changed` subscriber throws. Resource duplication of concrete shapes owns independent geometry state. The current profile supports circle, capsule, segment, separation ray, convex polygon, concave segment collection and rectangle geometry; standalone collision and contact queries execute for these families. Canvas drawing and custom solver bias retain their separate [coverage prerequisites](../coverage/classes/Shape2D.md).

## Example

Internally, all built-in shapes supply a borrowed [PhysicsShapeGeometry](PhysicsShapeGeometry.md)
view in scene units. Resources hold authored values; [PhysicsShapeBackend](PhysicsShapeBackend.md)
owns current CPU fixture/query compilation and the weak compiled polygon cache.
Standalone collisions consume the same source geometry without copying contours.
GPUPhysicsBodyStore borrows that view for persistent device geometry and broad-phase
bounds/pairs/contacts; revisions and disposal invalidate its shared geometry records.
This extraction preserves the public resource contract and does not add a second
implemented backend or custom-shape registration.

This complete resource-only snippet requires no SceneTree, body or space:

```csharp
using var circle = new CircleShape { Radius = 10 };
using var box = new RectangleShape { Size = new Vector2(20, 20) };
var boxPose = new Transform(0, Vector2.One, 0, new Vector2(15, 0));
bool touching = circle.Collide(Transform.Identity, box, boxPose);
Vector2[] pairs = circle.CollideAndGetContacts(Transform.Identity, box, boxPose);
```

## API summary

| Member | Contract |
| --- | --- |
| `protected Shape()` | Base construction through a concrete derived shape. |
| `public abstract Rect2 GetRect()` | Returns the local bounding rectangle in scene units. |
| `public bool Collide(Transform localTransform, Shape withShape, Transform shapeTransform)` | Tests two posed resources. |
| `public Vector2[] CollideAndGetContacts(Transform localTransform, Shape withShape, Transform shapeTransform)` | Up to sixteen caller/other boundary pairs. |
| `public bool CollideWithMotion(Transform localTransform, Vector2 localMotion, Shape withShape, Transform shapeTransform, Vector2 shapeMotion)` | Tests independently swept regions. |
| `public Vector2[] CollideWithMotionAndGetContacts(Transform localTransform, Vector2 localMotion, Shape withShape, Transform shapeTransform, Vector2 shapeMotion)` | Boundary pairs of independently swept regions. |

## Method description

<a id="getrect"></a>
### `GetRect()`

Concrete shapes return local-axis bounds (including drawing padding for a separation ray), which may be off-center, independent of the scene transform or a Box2D world. A disposed resource rejects the call. The returned rectangle is a value copy.

<a id="collide"></a>
<a id="collideandgetcontacts"></a>
### `Collide` and `CollideAndGetContacts`

Both resources are evaluated at the supplied global poses, independently of scene borrowers, filters and disabled flags. The same resource may be used twice at different poses. No RID or temporary world is created. Unit scale and zero skew follow the current physics profile; malformed poses reject with ArgumentException, null resources with ArgumentNullException, disposed resources with ObjectDisposedException, and nonfinite motion or unrepresentable transformed geometry with ArgumentOutOfRangeException. The call never edits geometry, emits Changed or moves a scene body.

The boolean includes exact touching. Contact arrays alternate a point on this resource's boundary and a point on the other boundary in global scene units. `(second - first).Normalized()` gives the separating direction and its length the depth. Exact edge touching may return true with no separating pair. Arrays contain at most sixteen pairs; when additional pieces contribute contacts, deeper pairs replace shallower pairs. Empty arrays are shared; nonempty arrays are caller-owned and may be edited. Ordering beyond caller/other point order is not specified.

A full convex resource uses its entire contour even above the native fixture vertex limit. Concave contours remain hollow, with each segment using the accepted short-segment point fallback; two concave resources do not collide. Two separation rays do not collide. Ray contact rejects containment, chooses the closest front-facing surface across pieces and preserves SlideOnSlope's directed contact rule.

<a id="collidewithmotion"></a>
<a id="collidewithmotionandgetcontacts"></a>
### `CollideWithMotion` and `CollideWithMotionAndGetContacts`

Both motion vectors are displacements along global axes, independent of each pose's rotation. Ordinary convex shapes contribute the whole region swept from the initial pose through the displacement. These are independently swept regions, without a synchronized time fraction or earliest-impact snapshot: parallel equal motions can collide where their spatial swept regions overlap. Contact points lie on swept-region boundaries.

Pinned special-pair behavior is retained: concave terrain's own motion is ignored in either operand; separation rays extend only by their own positive axial motion and ignore counterpart motion. This differs from direct-space queries whose queried shape is already wrapped as a swept region. Neither call applies recovery, collision masks, Area fields, impulses or scene movement.

Queries can run on any thread while resources remain unchanged and alive. Private per-thread proxy and hull buffers retain only geometry values and grow when a query needs larger capacity. Concurrent mutation/disposal is outside the contract. See [ADR 0069](../decisions/physics.md#adr-0069).

## Limits and verification

Circle and rectangle bounds, validation and copying are checked in [PhysicsBodyTests](../../tests/Electron2D.Tests/PhysicsBodyTests.cs); [CapsuleShapeTests](../../tests/Electron2D.Tests/CapsuleShapeTests.cs) checks the capsule; [SegmentShapeTests](../../tests/Electron2D.Tests/SegmentShapeTests.cs) checks off-center line bounds and fixtures; [ConvexPolygonShapeTests](../../tests/Electron2D.Tests/ConvexPolygonShapeTests.cs) checks compound solid contours; [ConcavePolygonShapeTests](../../tests/Electron2D.Tests/ConcavePolygonShapeTests.cs) checks hollow paired contours. [PhysicsShapeQueryTests](../../tests/Electron2D.Tests/PhysicsShapeQueryTests.cs) checks borrowed RID lifetime, edits and direct query geometry. [ShapeCollisionTests](../../tests/Electron2D.Tests/ShapeCollisionTests.cs) checks static and swept contacts, all ordinary family pairings, rounded corners, contact order/depth, whole convex boundaries, hollow/special pairs, sixteen-deepest-pair retention, invalid/disposed inputs, callback-failure edits and off-thread queries. Sixty-four warmed active, full-contour and empty-contact queries each allocate zero managed bytes on Linux/.NET 10. Successful contact arrays allocate caller-owned output. Native allocation, large-world throughput, other platforms and owner visual acceptance remain unverified. Canvas debug drawing still requires renderer resource identity. Custom solver bias follows the contact policy below. See [ADR 0063](../decisions/physics.md#adr-0063) for direct query ownership.

## Physics identity and server-owned views

| Signature | Contract |
| --- | --- |
| `public override RID GetRID()` | Lazily register and return this geometry's stable physics identity. |
| `protected override void ValidateDisposal()` | Reject separate disposal of a server-owned geometry view before lifetime changes. |

<a id="getrid"></a>
**GetRID:** Returns a nonempty borrowed physics shape RID stable across geometry edits until resource disposal. Scene/server slots and resource query parameters use that same identity. Disposed resources throw ObjectDisposedException; FreeRID rejects a managed-resource-owned identity. A geometry view obtained from a server-backed scene owner shares the caller-owned server shape RID.

<a id="validatedisposal"></a>
**ValidateDisposal:** An owned server RID controls its private geometry view. The view cannot be disposed by a borrower (InvalidOperationException before disposal), including concurrently with owner-controlled retirement; ShapeSetData retires an old held view and FreeRID retires the current view. Other managed shapes retain caller-controlled disposal. For independent data, request ShapeGetData rather than retain a borrowed view across replacement.

[PhysicsServerShapeSlotTests](../../tests/Electron2D.Tests/PhysicsServerShapeSlotTests.cs) verifies identity, query projection, view retirement, callback failures and guarded lifetime under [ADR 0088](../decisions/physics-shape-slots.md#adr-0088). No rendering-resource RID support is implied.

WorldBoundaryShape adds an infinite half-plane special pair. Two boundaries do not
collide; other geometry separates along its normal. Standalone resource motion
ignores boundary displacement and tests the other shape at its endpoint, taking
precedence over separation-ray motion rules. WorldBoundaryTests verifies this
policy; direct-space queries retain their separate swept-query semantics.

## Contact correction and stored policy

<a id="customsolverbias"></a>
`public float CustomSolverBias { get; set; }` defaults to zero (inherit the world).
A single nonzero bias overrides the world; two nonzero biases use their arithmetic
mean. Values must be finite in [0,1]. Disposed resources reject access. Invalid
writes throw ArgumentOutOfRangeException before mutation; equal writes are silent.
Changed policy emits Changed but preserves geometry revision, RID, mass and fixtures.
The policy epoch publishes before callbacks, so a throwing observer cannot hide an
already committed edit. Borrowers observe it on their next preparation and wake
contact neighbours. Queries and sensors do not apply penetration correction.

<a id="getpropertydescriptors"></a>
`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` extends
Resource storage with CustomSolverBias. Concrete geometry descriptors call this base.

<a id="copycustomstateto"></a>
`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource, Resource> copyAlways, Func<Resource, Resource> copyNormally)`
copies inherited policy; each built-in geometry hook calls it and publishes its
geometry revision. Duplicate, in-place copy and .e2dres persistence preserve policy
for all eight built-in shape families. [Contact correction](../components/physics-contact-policy.md)
documents world settings, backend behavior and PhysicsContactPolicyTests.
