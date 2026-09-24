# Electron2D navigation decisions

Last updated: 2026-09-24

This bounded document owns standalone 2D pathfinding decisions. The [decision index](index.md) routes other domains.

<a id="adr-0052"></a>
## ADR 0052: Standalone typed AStar2D graph before navigation servers

Last updated: 2026-09-24

- Status: Accepted
- Scope: `AStar2D` point graph and path queries

### Context

The current 2D renderer and SceneTree can display and move game objects, but the Navigation2D API is absent. The pinned 4.7.2 `AStar2D` contract is a self-contained directed point graph with caller-controlled cost, heuristic and neighbor filtering. It does not require a navigation mesh, physics world, SDL host or renderer. `AStarGrid2D`, `NavigationServer2D` and navigation agents have distinct graph, map or avoidance contracts and remain future work.

### Decision

- Add `AStar2D : ElectronObject` in the Navigation domain. The managed lifetime projects the reference `RefCounted` role under ADR 0003. The class exposes all own applicable methods and the neighbor-filter property; C# `long` represents 64-bit point IDs, and caller-owned `long[]`/`Vector2[]` replace packed arrays. Acronyms remain uppercase in method names under ADR 0045.
- Retain the pinned directed-segment rules, endpoint weight multiplication, nearest-point lowest-ID tie, swap-last point and neighbor order, partial route to the closest reached point, A-star priority by lower estimated total and then greater traveled cost, and observable power-of-two point-map capacity. A custom protected `OnComputeCost`, `OnEstimateCost` or `OnFilterNeighbor` overrides the corresponding virtual source hook without `Variant` or string dispatch.
- Validate nonnegative IDs and finite positions, weights and callback costs before they can corrupt graph/search state. Missing required points raise typed `KeyNotFoundException`; absent connection queries return false. A callback cannot mutate, dispose or re-enter the graph during search. Calls are not internally synchronized; applications coordinate concurrent use, as path queries own transient search state.
- The runtime uses .NET collections and no new package. An explicit reservation above managed collection limits fails before mutation. Path arrays are caller-owned snapshots. Other Navigation2D services do not appear as inert public types.

### Consequences

Games can compute directed, weighted and partial paths without native backend setup. The graph has deterministic managed lifetime and API validation; returned paths stay valid after later graph edits. The chosen managed representation maintains the pinned public capacity values even though its private dictionary has a different allocation layout. Search and output arrays allocate by design; it is not a per-frame renderer primitive.

`AStarGrid2D` requires its own complete grid, cell-shape, diagonal and jump-point slice. Navigation maps, polygon baking, agents and avoidance require the first full navigation-server backend and lifetime contract. This decision does not classify those domains as implemented.

### Rejected alternatives

- Expose only a simplified shortest-path method: it omits directed links, disabled points, weights, partial paths, callbacks and capacity behavior already applicable to the reference API.
- Route the point graph through NavigationServer2D: it adds a missing map/RID lifetime dependency to a standalone graph and delays usable game pathfinding.
- Add a general graph framework or a second managed dependency: the typed point graph and .NET collections cover this contract directly.
