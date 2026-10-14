# PolygonPathFinder

Last updated: 2026-10-15

**Inherits:** [Resource](Resource.md)

**Inherited By:** —

- **Source:** [PolygonPathFinder.cs](../../src/Navigation/2D/PolygonPathFinder.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PolygonPathFinder : Resource`

## Description

A stored standalone visibility graph inside closed polygon areas. Boundary edges can describe concave contours, holes, separate islands and touching/crossing contours. Membership uses even-odd parity and includes boundaries. Unconnected vertices remain possible waypoints. Reversed duplicate edges collapse in first-authored order; open boundaries, nonfinite points and zero-length edges reject before state changes. Preparation is bounded to 2,048 vertices and 4,096 unique edges.

Path endpoints outside the area project to their nearest boundary; an inside hole endpoint is outside the area. Route cost combines Euclidean length and each entered authored vertex's finite nonnegative penalty. Equal choices follow point order. Empty areas and disconnected islands return no route. Equal direct endpoints retain two entries. Intersections follow authored edge order and retain duplicate shared-vertex hits; collinear overlap has no unique crossing.

Setup copies arrays and resets penalties. Returned arrays are independent snapshots. Calls synchronize per resource; change observers run after publication outside the gate and use inherited Resource failure semantics. Geometry is immutable; copies share it safely and copy penalties. A private typed stored descriptor preserves geometry and penalties through duplication, CopyFromResource, ResetState, resource files and PackedScene. The resource retains no renderer, scene or native owner.

## Example

```csharp
using var area = new PolygonPathFinder();
area.Setup([new(0, 0), new(100, 0), new(100, 100), new(0, 100)],
    [0, 1, 1, 2, 2, 3, 3, 0]);
Vector2[] route = area.FindPath(new(10, 10), new(90, 90));
```

## Constructors

| Member | Description |
| --- | --- |
| `PolygonPathFinder()` | Empty stored area. |

## Methods

| Member | Description |
| --- | --- |
| `Setup(ReadOnlySpan<Vector2>, ReadOnlySpan<int>)` | Atomically copies geometry, prepares visibility and resets penalties. |
| `FindPath(Vector2 from, Vector2 to)` | Projected minimum-cost route or an empty snapshot. |
| `GetBounds()` | Bounds of every authored point, including isolated vertices. |
| `GetClosestPoint(Vector2)` | Nearest boundary even for an interior query; zero for no edges. |
| `GetIntersections(Vector2 from, Vector2 to)` | Unique crossings per authored edge, including endpoint/shared-vertex hits. |
| `IsPointInside(Vector2)` | Closed even-odd area membership. |
| `GetPointPenalty(int)` | Authored vertex entry penalty. |
| `SetPointPenalty(int, float)` | Nonnegative finite penalty, notifying after a changed value. |

## Protected resource integration

GetPropertyDescriptors supplies the private stored byte-array schema. OnResetState clears only transient search workspace. CreateDuplicateInstance, CopyCustomStateTo and Dispose preserve executable copies, independent penalties and deterministic managed cleanup. See [polygon paths](../components/polygon-paths.md) and [ADR 0106](../decisions/navigation.md#adr-0106) for the decision and verification boundaries.
