# PhysicsShapeBackend

Last updated: 2026-10-08

**Declaration:** `internal static class PhysicsShapeBackend`

**Source:** [PhysicsShapeBackend.cs](../../src/Servers/Physics/PhysicsShapeBackend.cs)
**Component:** [Collision shapes](../components/physics-shapes.md)

## Responsibility

The current CPU geometry adapter converts the borrowed [PhysicsShapeGeometry](PhysicsShapeGeometry.md)
view to Box2D fixtures and query proxies. PhysicsColliderBackend still owns body
and fixture lifetimes, logical slot tags, filters and material settings. Shape
resources retain authored values and no longer construct backend objects or store
backend hull arrays themselves. Public APIs and geometry units are unchanged.

| Operation | Contract |
| --- | --- |
| `AppendToBody` | Apply a slot's local pose and definition, append its concrete fixtures, preserve sub-tolerance capsule/segment fallback and separation-ray sensor metadata. |
| `AppendQueryProxies` | Append each local convex or hollow query piece in backend units; callers retain transform, margin, exclusions and mass policy. |
| `ValidateAndCachePolygon` | Validate and compile the entire candidate before replacing its cached hulls. The resource commits its copied points immediately afterward, before Changed notifications. |
| `ToBackend` | Convert scene-unit vectors at backend call sites without coupling the Shape base class to vendor vectors. |

Only convex resources need retained compiled pieces. A ConditionalWeakTable maps
resource identity to those arrays without keeping resources alive. Successful
Points assignment replaces them; invalid input leaves both authored and compiled
geometry intact. A duplicated resource owns copied authoring points and compiles
independently on first use. Compiled data is not serialized and contains no owner
reference. Local rectangle/polygon fixture rotations use ordinary sine/cosine, matching
other shape families and shared mass authoring instead of an approximate angle
helper. This preserves rotated centroids across detached/attached mass resolution.
Structural authoring may allocate; warmed geometry reads reuse storage.

Contour validation and the current minimum-size policy still compile CPU hulls
during authoring, even without a world. This moves the existing compiler/cache out
of resources; it does not remove CPU query/mass dependencies or supply GPU geometry
ownership. Those obligations remain in the [physics audit](../components/physics-contract-audit.md).

## Verification

`ELECTRON2D_TEST_COLLIDER_BACKEND=1` includes all current shape families, logical
slots/owners, material/mass, body/Area lifetimes and contacts, direct queries,
motion queries, standalone collisions and their warmed allocation assertions.
ConvexPolygonShapeTests checks one edited resource shared by two bodies against an
independent duplicate, preserves the prior small contour after an invalid edit,
and exercises alternating query resources without warmed managed allocations.
