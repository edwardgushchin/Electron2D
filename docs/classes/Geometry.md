# Geometry

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Geometry.cs`](../../src/Core/Math/Geometry.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class Geometry`

## Description

Stateless, backend-independent two-dimensional geometry queries on engine-owned [`Vector2`](Vector2.md) and [`Vector2I`](Vector2I.md). This C# static service projects the reference geometry singleton's pure operations without an object to create or dispose. Calls are safe from multiple threads when callers do not mutate their own input values concurrently. Floating calculations use single precision and ordinary IEEE 754 arithmetic; they do not validate finite inputs. Each returned array is owned by the caller.

The current production slice covers grid-line rasterization, nearest points and line/segment intersection. The rest of the reference geometry surface, including polygon boolean operations, offsets, triangulation and atlasing, is still absent; see [reference geometry coverage](../coverage/classes/Geometry2D.md).

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
| [`public static bool IsPointInCircle(Vector2 point, Vector2 circlePosition, float circleRadius)`](#ispointincircle) | Circle inclusion, including the boundary. |
| [`public static Vector2? LineIntersectsLine(Vector2 fromA, Vector2 dirA, Vector2 fromB, Vector2 dirB)`](#lineintersectsline) | Unique intersection of infinite lines, or `null`. |
| [`public static Vector2? SegmentIntersectsSegment(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB)`](#segmentintersectssegment) | Unique intersection of finite segments, or `null`. |

## Method descriptions

### BresenhamLine

Returns integer grid points in traversal order. Equal endpoints return one point. The rasterizer uses the reference tie policy for shallow and steep lines and signed directions. It allocates one array. A requested path longer than `int.MaxValue` elements throws `ArgumentOutOfRangeException` before allocation; a smaller path can still fail on ordinary managed allocation limits.

### GetClosestPointToSegment

Projects `point` onto `s1`–`s2` and clamps to the closed segment. A segment whose squared length is below `1e-20f` returns `s1`.

### GetClosestPointToSegmentUncapped

Uses the same projection without clamping, so the result can lie beyond either endpoint. A degenerate segment returns `s1`.

### GetClosestPointsBetweenSegments

Returns a two-element array, first the point on `p1`–`q1`, then the point on `p2`–`q2`. Both points belong to their closed segments. Degenerate segments are treated as points using the pinned reference's `1e-5f` squared-length threshold. Parallel segments choose one closest pair; intersecting segments return the intersection twice.

### IsPointInCircle

Compares squared distance to squared radius, so the circumference counts as inside and a negative radius has the same result as its positive magnitude.

### LineIntersectsLine

`fromA`/`fromB` are points; `dirA`/`dirB` are directions, not endpoints. Returns `null` for parallel or coincident lines using a `1e-5f` zero-denominator threshold. For a unique crossing, returns its unbounded position.

### SegmentIntersectsSegment

All four parameters are endpoints. Returns a crossing or touching endpoint. Collinear overlaps, parallel, disjoint and zero-length first segments have no unique result and return `null`. Near-parallel rejection uses the pinned reference's `1e-5f` tolerance.

## Dependencies and verification

Only Core math and the .NET base library are used; no scene, renderer, physics or native backend is required. `GeometryTests.Run` checks shallow/steep rasterization, integer extremes, capped/uncapped and degenerate projections, nearest pairs, circle boundaries, crossings, endpoint contact and null intersection cases. These are Linux/.NET 8 managed checks; native, other-platform and exhaustive numeric parity checks remain open.

## Decisions

- [ADR 0004: 2D public API boundary](../decisions/product.md#adr-0004)
- [ADR 0032: engine-owned math vocabulary](../decisions/core-math.md#adr-0032)
- [ADR 0035: public family completeness](../decisions/core-math.md#adr-0035)
