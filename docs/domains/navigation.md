# Navigation domain

Last updated: 2026-10-10

## Responsibility

Navigation owns typed 2D pathfinding authored map/polygon/region services and future baking slices. Its first two executable components are standalone point and grid searches that games can use without opening a SceneTree, renderer or native host.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [A-star point graph](../components/astar-graph.md) | Directed weighted points, nearest-segment queries and full/partial path search | Implemented and verified in managed execution |
| [Authored maps](../components/navigation-maps.md) | World map ownership, scene regions, convex portal routes and deferred publication | Implemented profile; further capabilities retain coverage dependencies |
| [A-star grid](../components/astar-grid.md) | Rectangular cells, obstacles, weights, diagonal and jump-point search | Implemented and verified in managed execution |

## Public surface

[`AStar`](../classes/AStar.md) derives from `ElectronObject`. It owns caller-added points and directed links, mutable position/weight/disabled state, exact point and connection queries, capacity reservation, and typed virtual cost, estimate and neighbor-filter hooks. `GetIDPath` and `GetPointPath` return caller-owned ordered arrays. The graph owns no scene nodes or external resources.

[`AStarGrid`](../classes/AStarGrid.md) also derives from `ElectronObject`. Its region defines implicit cell IDs and eight potential neighbors. Geometry settings rebuild cells on `Update`; obstacle and weight edits act immediately. Paths use configurable diagonal and heuristic policies, optional sparse jump-point search, and typed virtual cost and estimate hooks. Region data, path IDs and path positions are caller-owned snapshots.

## Dependencies and invariants

- Navigation depends on Core `Vector2`, `ElectronObject` lifetime, and .NET collections; it has no SDL, Box2D.NET, GPU or physics-server dependency.
- Point IDs are nonnegative 64-bit integers. Positions, weights and callback costs must be finite, and costs/weights nonnegative. Invalid writes fail before graph mutation.
- Links are directional for travel and undirected for segment existence. A path pays the destination point's weight on each edge. Disabled points are excluded from path expansion and closest-segment queries unless a point query explicitly includes them.
- A path callback may inspect graph state, but search re-entry, mutation and disposal are rejected. Concurrent callers coordinate access externally.
- Grid region and cell geometry are finite and bounded to managed arrays. Jumping intentionally ignores individual cell weights, matching its separate search mode. Both graph types reject search re-entry, mutation and disposal from callbacks.
- NavigationServer supplies owned map/region RIDs and immutable authored-polygon iterations. World/scene integration uses the same storage; baking and debug retain their own exact prerequisites.

## Verification and limits

[AStarTests](../../tests/Electron2D.Tests/AStarTests.cs) checks the pinned four-point weighted route, direction changes, disabled and partial paths, lowest-ID ties, storage/capacity behavior, virtual hooks, failure/re-entry/lifetime guards and a 1,024-point chain. These are local managed checks. Large-map performance, cross-thread coordination and owner game acceptance have not been established.

[AStarGridTests](../../tests/Electron2D.Tests/AStarGridTests.cs) checks update/reset state, square and isometric positions, region clipping, every diagonal and heuristic identity, weighted and partial paths, sparse jumping, callback failures and disposal. These are local managed checks; large-map performance, cross-thread coordination and owner game acceptance remain unverified.

## Decisions

- [0097: Typed planar navigation maps](../decisions/navigation.md#adr-0097)
- [0052: Standalone typed AStar](../decisions/navigation.md#adr-0052)
- [0053: Standalone typed 2D grid search](../decisions/navigation.md#adr-0053)
- [0003: Managed engine lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0004: Strict 2D public API](../decisions/product.md#adr-0004)

## Executable authored maps and regions

[Navigation maps](../components/navigation-maps.md) now supply real RID ownership, World maps, convex region topology, deferred publication, projection and copied routes with a native scene path-following consumer. Baking/async/debug capabilities remain exact separate dependencies.

NavigationLink and owned server links now provide directed/bidirectional off-surface travel, finite strict-radius attachment and weighted routes. [The map contract](../components/navigation-maps.md#links-across-separated-surfaces) records transforms, source storage, publication, typed query metadata and remaining raster/search equivalence.

[Typed query objects](../components/navigation-maps.md#typed-query-objects-and-transition-metadata) now supply copied filters, real search/output controls, transition provenance and completion. The native consumer performs a metadata-driven link action; agent debug and bake producers retain separate prerequisites.

[NavigationAgent](../classes/NavigationAgent.md) now supplies getter-driven Entity-parent movement targets, typed waypoint/link events, target completion, real server agent map membership and fresh-process source settings. [Its contract](../components/navigation-maps.md#agent-path-following) separates managed checks and actual GPU/compatibility rendered consumers from pending debug prerequisites.

[Reciprocal avoidance](../components/navigation-maps.md#reciprocal-agent-and-obstacle-avoidance) now connects NavigationAgent controls and typed velocity events, NavigationObstacle moving disc/oriented contour geometry, static server state and the physics lane. Managed/source/oracle/allocation and actual GPU/compatibility games are verified separately. Bake exclusion/carving and complete profiling/debug producers retain operation-specific dependencies.

Committed [raster topology and surface sampling](../components/navigation-maps.md#geometry-and-routes) now provide cell/scale consumers, directed margin pathways, per-region versions and complete process counters. Quantized ownership follows two-owner grouping; independent pinned-runtime fixtures and real GPU/compatibility actors verify the connected slice. Broad search-tie/fallback, baking, async and debug/global activation remain separate obligations.
