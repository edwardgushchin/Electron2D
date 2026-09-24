# Navigation domain

Last updated: 2026-09-24

## Responsibility

Navigation owns typed 2D pathfinding and, in later slices, map, polygon and avoidance services. Its first executable component is a standalone point graph that games can use without opening a SceneTree, renderer or native host.

## Component inventory

| Component | Responsibility | State |
| --- | --- | --- |
| [A-star point graph](../components/astar-graph.md) | Directed weighted points, nearest-segment queries and full/partial path search | Implemented and verified in managed execution |

## Public surface

[`AStar2D`](../classes/AStar2D.md) derives from `ElectronObject`. It owns caller-added points and directed links, mutable position/weight/disabled state, exact point and connection queries, capacity reservation, and typed virtual cost, estimate and neighbor-filter hooks. `GetIDPath` and `GetPointPath` return caller-owned ordered arrays. The graph owns no scene nodes or external resources.

## Dependencies and invariants

- Navigation depends on Core `Vector2`, `ElectronObject` lifetime, and .NET collections; it has no SDL, Box2D.NET, GPU or physics-server dependency.
- Point IDs are nonnegative 64-bit integers. Positions, weights and callback costs must be finite, and costs/weights nonnegative. Invalid writes fail before graph mutation.
- Links are directional for travel and undirected for segment existence. A path pays the destination point's weight on each edge. Disabled points are excluded from path expansion and closest-segment queries unless a point query explicitly includes them.
- A path callback may inspect graph state, but search re-entry, mutation and disposal are rejected. Concurrent callers coordinate access externally.
- AStarGrid2D and NavigationServer2D remain separate prerequisites; this domain does not expose placeholder maps, RIDs, agents or avoidance callbacks.

## Verification and limits

[AStar2DTests](../../tests/Electron2D.Tests/AStar2DTests.cs) checks the pinned four-point weighted route, direction changes, disabled and partial paths, lowest-ID ties, storage/capacity behavior, virtual hooks, failure/re-entry/lifetime guards and a 1,024-point chain. These are local managed checks. Large-map performance, cross-thread coordination and owner game acceptance have not been established.

## Decisions

- [0052: Standalone typed AStar2D](../decisions/navigation.md#adr-0052)
- [0003: Managed engine lifetime](../decisions/core-object-runtime.md#adr-0003)
- [0004: Strict 2D public API](../decisions/product.md#adr-0004)
