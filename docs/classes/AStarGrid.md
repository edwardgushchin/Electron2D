# AStarGrid

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

- **Source:** [AStarGrid.cs](../../src/Navigation/2D/AStarGrid.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class AStarGrid : ElectronObject`
- **Component:** [A-star grid](../components/astar-grid.md)

## Description

A rectangular 2D grid with implicit point IDs, obstacles, entry weights and A-star path search. Set `Region`, optionally configure geometry and call `Update()` before accessing cells. Geometry changes mark the grid dirty; `Update()` rebuilds all cells and resets their weights and solid flags. Cell edits and path-policy changes act immediately.

Paths use ID-space heuristics and configurable diagonal eligibility. Optional jumping emits sparse jump points and ignores per-cell weights, as in the pinned source. Arrays returned by path and region queries belong to the caller. Search callbacks can inspect the grid but cannot mutate, dispose or re-enter it. Calls from different threads require external coordination.

## Example

```csharp
using var grid = new AStarGrid
{
    Region = new Rect2i(0, 0, 8, 8),
    CellSize = new Vector2(16, 16)
};
grid.Update();
grid.SetPointSolid(new Vector2i(3, 3));
Vector2i[] cells = grid.GetIDPath(new Vector2i(0, 0), new Vector2i(6, 6));
```

## Nested enums

| Type | Values and role |
| --- | --- |
| [`CellShape`](AStarGrid.CellShape.md) | `Square=0`, `IsometricRight=1`, `IsometricDown=2`, `Max=3` |
| [`DiagonalMode`](AStarGrid.DiagonalMode.md) | `Always=0`, `Never=1`, `AtLeastOneWalkable=2`, `OnlyIfNoObstacles=3`, `Max=4` |
| [`Heuristic`](AStarGrid.Heuristic.md) | `Euclidean=0`, `Manhattan=1`, `Octile=2`, `Chebyshev=3`, `Max=4` |

## Properties

| Member | Contract |
| --- | --- |
| `Region : Rect2i` | Rectangle of valid cell IDs; setting a different value marks geometry dirty. |
| `Size : Vector2i` | Legacy size projection, preserving `Region.Position`. |
| `Offset : Vector2` | Finite world-space translation; changing it marks geometry dirty. |
| `CellSize : Vector2` | Finite per-cell world size; changing it marks geometry dirty. |
| `Shape : CellShape` | Square or isometric world-position mapping; marks geometry dirty. |
| `Diagonals : DiagonalMode` | Eligibility of diagonal neighbors during search; default `Always`. |
| `DefaultComputeHeuristic : Heuristic` | Default edge-cost function; default `Euclidean`. |
| `DefaultEstimateHeuristic : Heuristic` | Default remaining-cost function; default `Euclidean`. |
| `JumpingEnabled : bool` | Enables sparse jump-point search; false by default. |

`Shape` and `Diagonals` avoid C# member-name collisions with their nested enum types. Enum `Max` values are bounds, not accepted runtime policies.

## Methods and extension points

| Member | Contract |
| --- | --- |
| `AStarGrid()` | Creates an empty, non-dirty grid with unit cells and default policies. |
| `Update()` | Rebuilds cells when dirty; resets all flags and weights. No-op when clean. |
| `Clear()` | Removes cells and resets `Region`; retains current dirty flag. |
| `IsDirty()` | Reports whether changed geometry needs `Update()`. |
| `IsInBounds(int x, int y)` | Checks current region membership, even when dirty. |
| `IsInBoundsV(Vector2i id)` | Vector overload of `IsInBounds`. |
| `SetPointSolid(Vector2i id, bool solid = true)` | Changes obstacle state immediately. |
| `IsPointSolid(Vector2i id)` | Reads obstacle state. |
| `SetPointWeightScale(Vector2i id, float weightScale)` | Changes a finite, nonnegative entry multiplier immediately. |
| `GetPointWeightScale(Vector2i id)` | Reads an entry multiplier, initially one. |
| `FillSolidRegion(Rect2i region, bool solid = true)` | Edits only the intersection with the grid. |
| `FillWeightScaleRegion(Rect2i region, float weightScale)` | Edits weights only in the intersection with the grid. |
| `GetPointPosition(Vector2i id)` | Reads the current world-space position. |
| `GetPointDataInRegion(Rect2i region)` | Returns row-major `(ID, Position, Solid, WeightScale)[]` snapshots. |
| `GetIDPath(Vector2i fromID, Vector2i toID, bool allowPartialPath = false)` | Returns ordered cell IDs for a full or closest-reached partial route. |
| `GetPointPath(Vector2i fromID, Vector2i toID, bool allowPartialPath = false)` | Returns the corresponding current world positions. |
| `OnComputeCost(Vector2i fromID, Vector2i toID)` | Protected virtual unweighted edge-cost hook. |
| `OnEstimateCost(Vector2i fromID, Vector2i endID)` | Protected virtual remaining-cost hook. |
| `ValidateDisposal()` | Rejects disposal during a search callback. |
| `Dispose(bool disposing)` | Releases owned grid storage. |

## Geometry and point state

`Region` and `Size` reject negative dimensions and coordinate ranges that overflow managed 32-bit IDs. `Offset` and `CellSize` accept finite values, including zero or negative cell dimensions. Approximate equality avoids a redundant rebuild for tiny float changes, following the pinned setters. `Shape` changes only the world positions, not neighbor connectivity. `Update()` constructs replacement storage before committing it; an allocation or position-overflow failure leaves the grid dirty and does not partially replace cell data.

Square positions are `Offset + ID * CellSize`. Isometric positions use half-cell spacing and the pinned right/down coordinate transforms. Cell operations reject a dirty grid and out-of-bounds IDs. Region fills clip their input rectangle; negative fill dimensions reject explicitly. `GetPointDataInRegion` returns a named C# value tuple for each clipped cell instead of a dynamic dictionary. It traverses rows from top to bottom, and columns from left to right.

## Search behavior

Normal search considers cardinal directions in top, right, bottom, left order and diagonal directions in top-left, top-right, bottom-right, bottom-left order. `Always` permits any unblocked diagonal destination; `Never` forbids diagonals; `AtLeastOneWalkable` requires either adjacent cardinal cell to be open; `OnlyIfNoObstacles` requires both. The four built-in heuristics operate on absolute integer-ID differences. Protected overrides replace the corresponding default heuristic and must return finite nonnegative costs.

Travel cost is `OnComputeCost(from, to) * destination.WeightScale`. The queue favors a lower estimated total and, on an exact tie, a greater traveled cost. A solid source yields an empty path even when source and target are equal. A solid or unreachable target yields an empty full path; with `allowPartialPath`, the search returns the path to the processed cell with the lowest target estimate, then the lowest traveled cost.

With `JumpingEnabled`, the pinned forced-successor scans can skip intermediate cells. Returned arrays then contain only visited jump points. This mode ignores intermediate cell weights and applies a factor of one to its jump edges. It is opt-in and can produce a different path from normal weighted search.

## Failures and limits

Invalid enum values, nonfinite values and negative weights throw typed argument exceptions before mutation. Missing cells throw `KeyNotFoundException`; reading an initialized cell while geometry is dirty throws `InvalidOperationException`. A callback cannot mutate, re-enter or dispose the grid; callback exceptions release transient search ownership so a later search can proceed. `Dispose()` invalidates subsequent public calls. Very large grids may exhaust managed array memory; calls are not synchronized across threads.

[AStarGridTests](../../tests/Electron2D.Tests/AStarGridTests.cs) verifies the pinned route fixture, geometry, clipping, four diagonal modes, all heuristic policies, weighted/partial routes, jumping, callback failures and lifetime on Linux/.NET. Native rendering, other platforms, large-map performance and owner game acceptance are unverified or not applicable as described in the [component](../components/astar-grid.md).
