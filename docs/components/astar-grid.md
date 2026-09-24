# A-star grid component

Last updated: 2026-09-24

## Scope and owned type

[`AStarGrid2D`](../classes/AStarGrid2D.md) owns a rectangular cell array and the transient state for 2D grid pathfinding. It neither creates graph-edge objects nor uses navigation maps or native backends.

## Runtime flow

Set `Region` and optionally `Size`, `Offset`, `CellSize` and `Shape`, then call `Update()`. Geometry changes mark the grid dirty; cell reads, writes and searches reject a dirty grid. `Update()` replaces its cells and resets every solid flag and weight. `SetPointSolid`, `SetPointWeightScale` and the two region fills change initialized cells immediately. Region queries intersect caller bounds and return row-major typed snapshots.

Search tests cardinal neighbors in top, right, bottom, left order, then eligible diagonals in top-left, top-right, bottom-right, bottom-left order. Four diagonal policies govern whether blocked cardinal neighbors permit a diagonal step. The selected default cost and estimate heuristics use grid IDs, not world positions; protected hooks can replace each. Normal search multiplies entering-cell weight into edge cost. A disabled source yields no path, and partial search selects the reached cell with the lowest remaining estimate, then the lowest traveled cost.

Optional jump-point search follows the pinned forced-successor scans and returns sparse jump cells. It does not apply individual cell weights. The mode is chosen before each search and does not invalidate geometry. World positions are computed for square and the two isometric cell shapes during `Update`.

## Ownership, errors and verification

The grid follows `ElectronObject` disposal. Invalid region dimensions, nonfinite geometry and weights, invalid enum values, and absent cell IDs throw typed exceptions; failed geometry builds leave the previous storage uncommitted and the grid dirty. Callbacks may inspect but cannot mutate, dispose or re-enter the grid. Concurrent callers coordinate access externally.

[AStarGrid2DTests](../../tests/Electron2D.Tests/AStarGrid2DTests.cs) covers defaults, invalid/dirty state, geometry transforms, cell fills/snapshots, path policies, jumping and callback/lifetime failures. Backend and visual checks do not apply to this managed algorithm. Large-map performance and owner game acceptance remain unverified.

## Decision

- [0053: Standalone typed 2D grid search](../decisions/navigation.md#adr-0053)
