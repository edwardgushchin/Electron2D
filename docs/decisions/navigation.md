# Electron2D navigation decisions

Last updated: 2026-09-24

This bounded document owns standalone 2D pathfinding decisions. The [decision index](index.md) routes other domains.

<a id="adr-0052"></a>
## ADR 0052: Standalone typed AStar graph before navigation servers

Last updated: 2026-09-24

- Status: Accepted
- Scope: `AStar` point graph and path queries

### Context

The current 2D renderer and SceneTree can display and move game objects, but the server-backed Navigation2D API is absent. The pinned 4.7.2 `AStar2D` contract is a self-contained directed point graph with caller-controlled cost, heuristic and neighbor filtering. It does not require a navigation mesh, physics world, SDL host or renderer. The separate `AStarGrid` is implemented under ADR 0053; `NavigationServer2D` and navigation agents retain distinct map or avoidance contracts.

### Decision

- Map the pinned `AStar2D` to public `AStar : ElectronObject` in the Navigation domain. The 2D suffix is redundant in Electron2D under ADR 0004. The managed lifetime projects the reference `RefCounted` role under ADR 0003. The class exposes all own applicable methods and the neighbor-filter property; C# `long` represents 64-bit point IDs, and caller-owned `long[]`/`Vector2[]` replace packed arrays. Acronyms remain uppercase in method names under ADR 0045.
- Retain the pinned directed-segment rules, endpoint weight multiplication, nearest-point lowest-ID tie, swap-last point and neighbor order, partial route to the closest reached point, A-star priority by lower estimated total and then greater traveled cost, and observable power-of-two point-map capacity. A custom protected `OnComputeCost`, `OnEstimateCost` or `OnFilterNeighbor` overrides the corresponding virtual source hook without `Variant` or string dispatch.
- Validate nonnegative IDs and finite positions, weights and callback costs before they can corrupt graph/search state. Missing required points raise typed `KeyNotFoundException`; absent connection queries return false. A callback cannot mutate, dispose or re-enter the graph during search. Calls are not internally synchronized; applications coordinate concurrent use, as path queries own transient search state.
- The runtime uses .NET collections and no new package. An explicit reservation above managed collection limits fails before mutation. Path arrays are caller-owned snapshots. Other Navigation2D services do not appear as inert public types.

### Consequences

Games can compute directed, weighted and partial paths without native backend setup. The graph has deterministic managed lifetime and API validation; returned paths stay valid after later graph edits. The chosen managed representation maintains the pinned public capacity values even though its private dictionary has a different allocation layout. Search and output arrays allocate by design; it is not a per-frame renderer primitive.

`AStarGrid` has its own complete grid, cell-shape, diagonal and jump-point slice under ADR 0053. Navigation maps, polygon baking, agents and avoidance require the first full navigation-server backend and lifetime contract. This decision does not classify those domains as implemented.

### Rejected alternatives

- Expose only a simplified shortest-path method: it omits directed links, disabled points, weights, partial paths, callbacks and capacity behavior already applicable to the reference API.
- Route the point graph through NavigationServer2D: it adds a missing map/RID lifetime dependency to a standalone graph and delays usable game pathfinding.
- Add a general graph framework or a second managed dependency: the typed point graph and .NET collections cover this contract directly.

<a id="adr-0053"></a>
## ADR 0053: Standalone typed 2D grid search

Last updated: 2026-09-24

- Status: Accepted
- Scope: `AStarGrid` geometry, cell data and path queries

### Context

The point graph from ADR 0052 requires callers to build every edge. The pinned 4.7.2 `AStarGrid2D` is an independent rectangular grid with automatic neighbors, four diagonal policies, four heuristics and optional jump-point search. It requires no navigation-server map or native backend. Its dictionary-array cell query needs a typed C# representation under ADR 0001.

### Decision

- Map the pinned `AStarGrid2D` to public `AStarGrid : ElectronObject`; Electron2D has no 3D grid pathfinding sibling. Its nested `CellShape`, `DiagonalMode` and `Heuristic` enums retain all pinned numeric identities. The `Shape` and `Diagonals` properties avoid name collisions with their nested enum types. `GetIDPath` and `IsInBoundsV` retain uppercase acronyms under ADR 0045.
- `Region`, legacy `Size`, `Offset`, `CellSize` and `Shape` mark geometry dirty; `Update` atomically rebuilds positions and resets cell solidity and weights. Solidity and weight writes act immediately. `Clear` resets the region and points while retaining the pinned dirty flag. A row-major `GetPointDataInRegion` returns caller-owned arrays of named `(ID, Position, Solid, WeightScale)` tuples instead of dynamic dictionaries.
- Keep the pinned cardinal/diagonal neighbor order, entry-weight multiplication, custom protected cost/estimate hooks, closest-reached partial path and A-star priority. Jumping uses the pinned forced-successor algorithm, emits only jump points, and does not apply intermediate cell weights. It remains opt-in, as in the reference.
- Reject negative or overflowing region bounds, nonfinite geometry or weights, invalid enum values and out-of-bounds cell access with typed exceptions. Large grid allocations may fail before state replacement. Callback results must be finite and nonnegative. Search callbacks may inspect but cannot mutate, dispose or re-enter the grid. Calls are not synchronized across threads.

### Consequences

Games can query full or partial cell paths without constructing point-edge graphs. The grid is a managed standalone algorithm with no renderer, physics, SDL or native platform verification requirement. Returned paths and cell data are snapshots. Large grid performance and owner game acceptance remain separate verification work; navigation-server maps, regions and avoidance retain their own backend trigger.
