# Geometry

Last updated: 2026-09-26

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Geometry.cs`](../../src/Core/Math/Geometry.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class Geometry`

## Description

Stateless, backend-independent two-dimensional geometry queries on engine-owned [`Vector2`](Vector2.md) and [`Vector2i`](Vector2i.md). This C# static service projects the reference geometry singleton's pure operations without an object to create or dispose. Calls are safe from multiple threads when callers do not mutate their own input values concurrently. Vector values are single precision; convex hulls and polygon triangulation use double orientation intermediates. Polygon clipping and offsets use Clipper2 at five decimal digits of internal precision and reject nonfinite or out-of-range coordinates. Other inputs are not generally checked for finiteness. Each returned array is owned by the caller.

The class covers grid-line rasterization, nearest points, line/segment intersections, polygon predicates, convex hulls and decomposition, simple-polygon and Delaunay triangulation, atlas layout, segment/circle intersections, polygon boolean operations and offsets. The public name is `Geometry`; [`Geometry2D` coverage](../coverage/classes/Geometry2D.md) identifies the reference source only.

## Example

```csharp
using Electron2D;

Vector2? crossing = Geometry.SegmentIntersectsSegment(
    new Vector2(0, 0), new Vector2(10, 0),
    new Vector2(5, -5), new Vector2(5, 5));
// crossing is (5, 0).
```

## Methods

| Signature | Result |
| --- | --- |
| [`public static Vector2i[] BresenhamLine(Vector2i from, Vector2i to)`](#bresenhamline) | Ordered raster points, endpoints included. |
| [`public static Vector2 GetClosestPointToSegment(Vector2 point, Vector2 s1, Vector2 s2)`](#getclosestpointtosegment) | Nearest point on the bounded segment. |
| [`public static Vector2 GetClosestPointToSegmentUncapped(Vector2 point, Vector2 s1, Vector2 s2)`](#getclosestpointtosegmentuncapped) | Projection on the infinite line. |
| [`public static Vector2[] GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)`](#getclosestpointsbetweensegments) | Nearest pair, one point on each segment. |
| [`public static Vector2[] ConvexHull(ReadOnlySpan<Vector2> points)`](#convexhull) | Closed counterclockwise hull. |
| [`public static Vector2[][] DecomposePolygonInConvex(ReadOnlySpan<Vector2> polygon)`](#decomposepolygoninconvex) | Counterclockwise convex parts, or an empty array. |
| [`public static Vector2[][] ClipPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)`](#polygon-boolean-operations) | Region A minus region B. |
| [`public static Vector2[][] ClipPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)`](#polygon-boolean-operations) | Open line portions outside the polygon. |
| [`public static Vector2[][] ExcludePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)`](#polygon-boolean-operations) | Exclusive-or of two regions. |
| [`public static Vector2[][] IntersectPolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)`](#polygon-boolean-operations) | Common region. |
| [`public static Vector2[][] IntersectPolylineWithPolygon(ReadOnlySpan<Vector2> polyline, ReadOnlySpan<Vector2> polygon)`](#polygon-boolean-operations) | Open line portions inside the polygon. |
| [`public static bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius)`](#ispointincircle) | Circle inclusion, including the boundary. |
| [`public static bool IsPointInPolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)`](#ispointinpolygon) | Odd-even polygon inclusion, including the boundary. |
| [`public static bool IsPolygonClockwise(ReadOnlySpan<Vector2> polygon)`](#ispolygonclockwise) | Cartesian winding test. |
| [`public static Vector2? LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)`](#lineintersectsline) | Unique intersection of infinite lines, or `null`. |
| [`public static (Vector2[] Points, Vector2i Size) MakeAtlas(ReadOnlySpan<Vector2> sizes)`](#makeatlas) | Tile origins and the occupied atlas size. |
| [`public static Vector2[][] MergePolygons(ReadOnlySpan<Vector2> polygonA, ReadOnlySpan<Vector2> polygonB)`](#polygon-boolean-operations) | Union of both regions. |
| [`public static Vector2[][] OffsetPolygon(ReadOnlySpan<Vector2> polygon, float delta, PolyJoinType joinType = PolyJoinType.Square)`](#polygon-offsets) | Expanded or contracted polygon contours. |
| [`public static Vector2[][] OffsetPolyline(ReadOnlySpan<Vector2> polyline, float delta, PolyJoinType joinType = PolyJoinType.Square, PolyEndType endType = PolyEndType.Square)`](#polygon-offsets) | Stroked polygon contours for an open or joined line. |
| [`public static bool PointIsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)`](#pointisinsidetriangle) | Strict oriented-edge triangle test. |
| [`public static float SegmentIntersectsCircle(Vector2 segmentFrom, Vector2 segmentTo, Vector2 circlePosition, float circleRadius)`](#segmentintersectscircle) | First circle crossing fraction, or -1. |
| [`public static Vector2? SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)`](#segmentintersectssegment) | Unique intersection of finite segments, or `null`. |
| [`public static int[] TriangulateDelaunay(ReadOnlySpan<Vector2> points)`](#triangulatedelaunay) | Delaunay triangle index triples, or an empty array. |
| [`public static int[] TriangulatePolygon(ReadOnlySpan<Vector2> polygon)`](#triangulatepolygon) | Counterclockwise triangle index triples, or an empty array. |

## Method descriptions

### BresenhamLine

Returns integer grid points in traversal order. Equal endpoints return one point. The rasterizer uses the reference tie policy for shallow and steep lines and signed directions. It allocates one array. A requested path longer than `int.MaxValue` elements throws `ArgumentOutOfRangeException` before allocation; a smaller path can still fail on ordinary managed allocation limits.

### GetClosestPointToSegment

Projects `point` onto `s1`–`s2` and clamps to the closed segment. A segment whose squared length is below `1e-20f` returns `s1`.

### GetClosestPointToSegmentUncapped

Uses the same projection without clamping, so the result can lie beyond either endpoint. A degenerate segment returns `s1`.

### GetClosestPointsBetweenSegments

Returns a two-element array, first the point on `p1`–`q1`, then the point on `p2`–`q2`. Both points belong to their closed segments. Degenerate segments are treated as points using the pinned reference's `1e-5f` squared-length threshold. Parallel segments choose one closest pair; intersecting segments return the intersection twice.

### ConvexHull

Copies and lexicographically sorts the input before constructing a monotone hull. The returned points are counterclockwise in Cartesian coordinates, omit collinear interior points, and repeat the first vertex at the end for two or more input points. Empty input returns an empty array; one input point returns one point. The input is unchanged. Nonfinite coordinates have no defined hull ordering.

### DecomposePolygonInConvex

Returns caller-owned, counterclockwise convex contours for a simple polygon in either winding direction. A convex polygon stays whole; a concave polygon is triangulated and neighboring triangles are merged where both joins remain convex. The number and order of parts are not an optimality guarantee. Fewer than three vertices, zero signed area, or failed triangulation return an empty array. Self-intersections are not repaired, and nonfinite coordinates have no defined result. The input is unchanged.

### Polygon boolean operations

`ClipPolygons`, `ExcludePolygons`, `IntersectPolygons`, and `MergePolygons` respectively compute A minus B, exclusive-or, intersection, and union. `ClipPolylineWithPolygon` and `IntersectPolylineWithPolygon` return the outside and inside portions of an open line. Polygon results may include multiple disconnected boundaries and holes; holes have opposite winding from their surrounding boundary. Open-line results are arrays of separate open segments. The result contour order is not guaranteed. Inputs are read without mutation and outputs are caller-owned.

These methods apply the even-odd fill rule and use five-decimal-place internal clipping precision before conversion back to single-precision `Vector2`. Points must be finite and within the backend coordinate range; otherwise `ArgumentOutOfRangeException` is thrown. An internal clipping failure throws `InvalidOperationException`. Self-intersecting or degenerate input is governed by the clipping backend's fill rule, not repaired as a simple polygon.

### Polygon offsets

`OffsetPolygon` uses positive `delta` to expand and negative `delta` to contract a closed contour. `OffsetPolyline` strokes an open or joined line on both sides; negative `delta` and `PolyEndType.Polygon` return an empty result. Polygon offsets can split a contour or remove it entirely. Join and cap shapes are selected by the enums below. The miter limit is 2; round corners use an arc tolerance of 0.25 coordinate units. The same finite-coordinate and bounded-range validation applies as for boolean operations. Invalid join or end enum values and nonfinite/out-of-range distances throw `ArgumentOutOfRangeException`.

## Enumerations

| Type | Values | Meaning |
| --- | --- | --- |
| `Geometry.PolyBooleanOperation` | `Union = 0`, `Difference = 1`, `Intersection = 2`, `XOR = 3` | Region operations used by the polygon methods. |
| `Geometry.PolyJoinType` | `Square = 0`, `Round = 1`, `Miter = 2` | Offset corner and line-join shape. |
| `Geometry.PolyEndType` | `Polygon = 0`, `Joined = 1`, `Butt = 2`, `Square = 3`, `Round = 4` | Closure or endpoint cap of an offset line. |

### IsPointInCircle

Compares squared distance to squared radius, so the circumference counts as inside and a negative radius has the same result as its positive magnitude.

### IsPointInPolygon

Returns true for interior points and points on the perimeter. Three vertices are required. The odd-even crossing rule defines the interior of self-intersecting contours. Boundary comparison uses the current vector approximation rule; nonfinite coordinates are outside the documented contract. The method reads a span without retaining it or allocating.

### IsPolygonClockwise

Returns the sign of the reference signed-edge sum for three or more vertices. Its `true` result means clockwise in Cartesian coordinates with positive Y up; in ordinary screen coordinates with positive Y down, that contour appears counterclockwise. Fewer than three vertices and zero signed area return false.

### LineIntersectsLine

`fromA`/`fromB` are points; `dirA`/`dirB` are directions, not endpoints. Returns `null` for parallel or coincident lines using a `1e-5f` zero-denominator threshold. For a unique crossing, returns its unbounded position.

### MakeAtlas

Truncates each finite tile size to integer pixels and tries power-of-two strip widths through 4096 pixels. Tiles are processed by descending width; equal widths retain input order. The returned `Points` array restores input order, and `Size` reports actual occupied bounds, which need not be powers of two. The chosen candidate has the smallest ratio of power-of-two-rounded width and height; equal ratios keep the first candidate. Empty input throws `ArgumentException`. Nonpositive dimensions after truncation, widths above 4096, nonfinite or out-of-range values, and layouts exceeding 32-bit atlas height throw `ArgumentOutOfRangeException`. The input is not modified, and the returned array belongs to the caller.

### PointIsInsideTriangle

Compares the signs of three oriented crosses around `point`. The strict sign comparisons preserve the reference behavior for boundaries and degenerate triangles; do not use this method as an inclusive boundary predicate.

### SegmentIntersectsCircle

Solves the segment/circle quadratic and returns the first root in `[0, 1]`; if the start is inside, it returns the exit root. A tangent reports its contact fraction. No root on the segment and zero-length segments return `-1`. Negative radii behave like positive magnitudes because only the squared radius is used.

### SegmentIntersectsSegment

All four parameters are endpoints. Returns a crossing or touching endpoint. Collinear overlaps, parallel, disjoint and zero-length first segments have no unique result and return `null`. Near-parallel rejection uses the pinned reference's `1e-5f` tolerance.

### TriangulateDelaunay

Returns consecutive triples of indices into the original point array. An incremental empty-circumcircle algorithm triangulates the point set without modifying it. Fewer than three points or a collinear set return an empty array. Cocircular inputs can admit multiple triangulations; input order determines ties. Nonfinite coordinates have no defined result.

### TriangulatePolygon

Returns three indices into the input contour per triangle, in counterclockwise order, for either input winding. A simple contour with fewer than three points or one that cannot be triangulated returns an empty array. The algorithm first avoids zero-area ears, then permits them if required to finish a contour with aligned or duplicate vertices; self-intersections are not repaired. It reads the input without modifying it. Double intermediates protect area and orientation for finite float coordinates, while index-array size remains subject to managed allocation limits. `CanvasItem.DrawPolygon` uses the same algorithm with reusable scratch buffers and throws on failure before recording.

## Dependencies and verification

Only Core math, the .NET base library and internally compiled [Clipper2 1.5.4](../../src/Vendor/Clipper2/UPSTREAM.txt) are used; no scene, renderer, physics or native backend is required. `GeometryTests.Run` checks raster orientation/endpoints, integer extremes, projections, nearest pairs, circle boundaries, crossings, polygon interior/boundaries/winding, convex hull ordering and decomposition, atlas layout and limits, both triangulation methods, segment/circle contact, all polygon boolean operations, holes, open lines, offsets, caps and invalid inputs. `CanvasPolygonTests.Run` checks drawing reuse and zero-allocation redraw. These managed checks pass on Linux/.NET 8. Native canvas polygon pixel checks pass on Wayland for compatibility and GPU backends, including HLSL/GLSL fixtures; other platforms and exhaustive numeric parity with the reference remain unverified.

## Decisions

- [ADR 0004: 2D public API boundary](../decisions/product.md#adr-0004)
- [ADR 0032: engine-owned math vocabulary](../decisions/core-math.md#adr-0032)
- [ADR 0035: public family completeness](../decisions/core-math.md#adr-0035)

The convex hull implementation also supplies an internal span-writing path for standalone Shape collision regions. Public ConvexHull retains its caller-owned closed contour and does not mutate the input. The full executable checks cover the shared builder under ShapeCollisionTests.
