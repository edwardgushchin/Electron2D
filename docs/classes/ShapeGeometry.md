# ShapeGeometry

Last updated: 2026-10-20

**Declaration:** `public sealed class ShapeGeometry` · **Component:** [Collision shapes](../components/physics-shapes.md)

**Source:** [ShapeGeometry.cs](../../src/Scene/Resources/ShapeGeometry.cs)

## Contract

Immutable exact local geometry supplied by [Shape.GetGeometryCore](Shape.md#procedural-geometry).
It owns copied authoring data and no body, RID, rendering resource or solver handle.
The runtime retains it until the shape publishes a geometry edit. Reuse immutable
instances across calls and duplicates. CPU/GPU compilation, mass, queries, contacts,
bounds, diagnostic drawing and portable schemas share that data.

## API summary

| Member | Contract |
| --- | --- |
| `Type` | Actual represented PhysicsServer.ShapeType; procedural authoring does not falsely report Custom. |
| `Bounds` | Exact local finite bounds; ray bounds include padding and infinite-boundary bounds describe its editing marker. |
| `Circle(float radius)` | Origin-centered positive finite radius with representable diameter. |
| `Capsule(float radius, float height)` | Origin-centered vertical capsule; radius is nonnegative, height is at least its diameter. Zero dimensions retain point geometry. |
| `Segment(Vector2 a, Vector2 b)` | Finite two-sided endpoints, including a coincident point. |
| `Rectangle(Vector2 size)` | Origin-centered finite positive full size. |
| `ConvexPolygon(ReadOnlySpan<Vector2> points)` | Copies a finite convex perimeter in either winding. Arbitrary supported contour length partitions only on CPU; empty input contributes no fixtures. Invalid/crossing/degenerate or out-of-range contours reject. |
| `ConcavePolygon(ReadOnlySpan<Vector2> segments)` | Copies finite paired hollow edges; even endpoint count and finite spans/bounds are required. Empty input clears geometry. |
| `SeparationRay(float length, bool slideOnSlope = false)` | Nonnegative finite directed length in positive local Y, within solver numeric range. |
| `WorldBoundary(Vector2 normal, float distance)` | Infinite solid half-plane; nonzero finite normal is normalized with its signed offset. No finite collision approximation. |

Typed accessors `GetCircleRadius`, `GetCapsuleDimensions`, `GetSegmentEndpoints`,
`GetRectangleSize`, `GetConvexPolygonPoints`, `GetConcavePolygonSegments`,
`GetSeparationRayParameters` and `GetWorldBoundaryPlane` read the matching factory's
complete payload. A mismatched primitive accessor throws InvalidOperationException.
Contour accessors return read-only borrowed spans without copying; boundary access
returns the normalized plane. Shape.GetGeometryData obtains this representation
from any live built-in or caller-defined resource.

Factories reject invalid input before returning data. Contour factories allocate and
validate on authoring paths; warmed use of retained data allocates no managed memory.
Construction does not create a graphics device or world. New collision algorithms
outside these exact geometry families require registered solver implementation under
[ADR 0103](../decisions/physics-extensions.md#adr-0103), which remains open.

## Verification

[Public-only procedural shape checks](../../tests/ShapeExtension.Consumer/Program.cs)
exercise all factories and rejection families, source-array isolation, exact type and
bounds, real scene/raw convex bodies and Areas, mass/inertia, contact identity/impulses,
queries, live kind changes, duplicate/disposed resources, checkpoint and portable state.
[Drawing checks](../../tests/ShapeExtension.Consumer/DrawChecks.cs) use actual window
pixels after geometry changes with both rendering methods. Full backend performance,
native allocations and foreign-platform acceptance are separate gates.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.

The public-only --custom consumer verifies independent annulus/circle resource
sweeps, retained inner holes and true boundary contacts in both argument orders
through registered, CPU and explicitly enabled GPU spaces.
Twelve exact cases, 512 seeded boundary cases and 128 warmed standalone/selected
cycles retain the .0001 scene-unit boundary tolerance and 0/0 managed-byte gate.
See [custom collision geometry](../components/physics-custom-geometry.md) for the
analytic model and remaining numerical-extension limits.


[Shape-pair operand borrowing](../components/physics-shape-pairs.md#operand-borrowing-2026-10-14)
now protects both resources through standalone and selected callbacks, including
inherited copying/reset and owned RID replacement/retirement. Nested reads and
error cleanup retain their existing contract; full numerical/foreign/hardware
acceptance remains open.

[Server data projection](../components/physics-shape-data.md) consumes this immutable
payload for all nine represented kinds, including procedural resources without a
duplicate factory. Ordinary concrete wrappers remain the public copied-data result;
unsupported Custom activation still requires an appropriate selected implementation.

[Hollow pair contacts](../components/physics-custom-geometry.md#hollow-pair-contacts) now exercise the public-only annulus/annulus format through standalone/selected resource pairs and registered scene/raw queries and numerical response. Both surfaces/orders retain strict warmed zero managed allocation; built-in custom attachment, arbitrary algorithms and full native/network/hardware acceptance remain open.
