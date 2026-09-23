# Geometry

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Geometry.cs`](../../src/Core/Math/Geometry.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class Geometry`

## Description

Stateless, backend-independent two-dimensional geometry queries on engine-owned [`Vector2`](Vector2.md) and [`Vector2I`](Vector2I.md). This C# static service projects the reference geometry singleton's pure operations without an object to create or dispose. Calls are safe from multiple threads when callers do not mutate their own input values concurrently. Vector values are single precision; polygon triangulation uses double area and orientation intermediates. Inputs are not generally checked for finiteness. Each returned array is owned by the caller.

The current production slice covers grid-line rasterization, nearest points, line/segment intersections, polygon predicates, convex hulls, simple-polygon triangulation and segment/circle intersections. The rest of the reference geometry surface, including polygon boolean operations, offsets, Delaunay triangulation and atlasing, is still absent; see [reference geometry coverage](../coverage/classes/Geometry2D.md).

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
| [`public static Vector2I[] BresenhamLine(Vector2I from, Vector2I to)`](#bresenhamline) | Ordered raster points, endpoints included. |
| [`public static Vector2 GetClosestPointToSegment(Vector2 point, Vector2 s1, Vector2 s2)`](#getclosestpointtosegment) | Nearest point on the bounded segment. |
| [`public static Vector2 GetClosestPointToSegmentUncapped(Vector2 point, Vector2 s1, Vector2 s2)`](#getclosestpointtosegmentuncapped) | Projection on the infinite line. |
| [`public static Vector2[] GetClosestPointsBetweenSegments(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)`](#getclosestpointsbetweensegments) | Nearest pair, one point on each segment. |
| [`public static Vector2[] ConvexHull(ReadOnlySpan<Vector2> points)`](#convexhull) | Closed counterclockwise hull. |
| [`public static bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius)`](#ispointincircle) | Circle inclusion, including the boundary. |
| [`public static bool IsPointInPolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)`](#ispointinpolygon) | Odd-even polygon inclusion, including the boundary. |
| [`public static bool IsPolygonClockwise(ReadOnlySpan<Vector2> polygon)`](#ispolygonclockwise) | Cartesian winding test. |
| [`public static Vector2? LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)`](#lineintersectsline) | Unique intersection of infinite lines, or `null`. |
| [`public static bool PointIsInsideTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)`](#pointisinsidetriangle) | Strict oriented-edge triangle test. |
| [`public static float SegmentIntersectsCircle(Vector2 segmentFrom, Vector2 segmentTo, Vector2 circlePosition, float circleRadius)`](#segmentintersectscircle) | First circle crossing fraction, or -1. |
| [`public static Vector2? SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)`](#segmentintersectssegment) | Unique intersection of finite segments, or `null`. |
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

### IsPointInCircle

Compares squared distance to squared radius, so the circumference counts as inside and a negative radius has the same result as its positive magnitude.

### IsPointInPolygon

Returns true for interior points and points on the perimeter. Three vertices are required. The odd-even crossing rule defines the interior of self-intersecting contours. Boundary comparison uses the current vector approximation rule; nonfinite coordinates are outside the documented contract. The method reads a span without retaining it or allocating.

### IsPolygonClockwise

Returns the sign of the reference signed-edge sum for three or more vertices. Its `true` result means clockwise in Cartesian coordinates with positive Y up; in ordinary screen coordinates with positive Y down, that contour appears counterclockwise. Fewer than three vertices and zero signed area return false.

### LineIntersectsLine

`fromA`/`fromB` are points; `dirA`/`dirB` are directions, not endpoints. Returns `null` for parallel or coincident lines using a `1e-5f` zero-denominator threshold. For a unique crossing, returns its unbounded position.

### PointIsInsideTriangle

Compares the signs of three oriented crosses around `point`. The strict sign comparisons preserve the reference behavior for boundaries and degenerate triangles; do not use this method as an inclusive boundary predicate.

### SegmentIntersectsCircle

Solves the segment/circle quadratic and returns the first root in `[0, 1]`; if the start is inside, it returns the exit root. A tangent reports its contact fraction. No root on the segment and zero-length segments return `-1`. Negative radii behave like positive magnitudes because only the squared radius is used.

### SegmentIntersectsSegment

All four parameters are endpoints. Returns a crossing or touching endpoint. Collinear overlaps, parallel, disjoint and zero-length first segments have no unique result and return `null`. Near-parallel rejection uses the pinned reference's `1e-5f` tolerance.

### TriangulatePolygon

Returns three indices into the input contour per triangle, in counterclockwise order, for either input winding. A simple contour with fewer than three points or one that cannot be triangulated returns an empty array. The algorithm first avoids zero-area ears, then permits them if required to finish a contour with aligned or duplicate vertices; self-intersections are not repaired. It reads the input without modifying it. Double intermediates protect area and orientation for finite float coordinates, while index-array size remains subject to managed allocation limits. `CanvasItem.DrawPolygon` uses the same algorithm with reusable scratch buffers and throws on failure before recording.

## Dependencies and verification

Only Core math and the .NET base library are used; no scene, renderer, physics or native backend is required. `GeometryTests.Run` checks raster orientation/endpoints, integer extremes, projections, nearest pairs, circle boundaries, crossings, polygon interior/boundaries/winding, convex hull ordering, triangulation and segment/circle contact. `CanvasPolygonTests.Run` checks drawing reuse and zero-allocation redraw. These managed checks pass on Linux/.NET 8. Native canvas polygon pixel checks pass on Wayland for compatibility and GPU backends, including HLSL/GLSL fixtures; other platforms and exhaustive numeric parity with the reference remain unverified.

## Decisions

- [ADR 0004: 2D public API boundary](../decisions/product.md#adr-0004)
- [ADR 0032: engine-owned math vocabulary](../decisions/core-math.md#adr-0032)
- [ADR 0035: public family completeness](../decisions/core-math.md#adr-0035)
