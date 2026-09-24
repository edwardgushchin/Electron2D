# A-star point graph component

Last updated: 2026-09-24

## Scope and owned types

[`AStar`](../classes/AStar.md) is the standalone 2D graph component. It owns points, positions, weights, enablement, outgoing links and search state. It neither reads a navigation map nor creates a SceneTree node.

## Runtime flow

Callers add points with nonnegative 64-bit IDs and finite positions, then connect them in one or both travel directions. A physical segment exists while either direction remains. Updating an existing point changes its position and entry weight without erasing links or disabled state. Removing a point clears all incoming and outgoing links. Clear removes points and resets the next-free cursor while preserving point-map capacity.

`GetIDPath` and `GetPointPath` use A-star with Euclidean cost and estimate by default. Each edge pays the destination's weight. Protected typed hooks customize cost, heuristic and optional neighbor filtering. The priority is lower estimated total cost, then greater traveled cost. A disabled source yields no path. Partial search returns a path to the reached point with the lowest estimate to the requested target, using lower traveled cost to break ties. Returned arrays are independent of later graph edits.

Nearest-point queries break exact distance ties by lowest ID. Nearest-segment projection ignores a link with a disabled endpoint and returns zero if no eligible segment exists. IDs and outgoing connection arrays follow the pinned swap-last graph storage order. Reported capacity follows the pinned power-of-two load threshold even though storage uses a managed dictionary.

## Ownership and failures

The graph follows `ElectronObject` disposal. Missing required points raise `KeyNotFoundException`; invalid IDs, nonfinite coordinates, negative/nonfinite weights and invalid callback costs fail explicitly. Callbacks can read graph state, but mutation, reentrant search and disposal during search fail before graph structure changes. The graph is not synchronized for simultaneous callers. SDL/native allocation and visual/platform acceptance do not apply to this standalone managed algorithm.

## Verification and remaining work

[AStarTests](../../tests/Electron2D.Tests/AStarTests.cs) covers the reference route, weights, directed connection upgrades/removal, closest-point and segment queries, disabled/partial behavior, custom hooks, invalid and failed callbacks, lifetime, capacity thresholds and a long chain. `AStarGrid` implements the separate grid slice; navigation polygons, maps, agents and avoidance require a navigation-server backend and lifetime contract.

## Decision

- [0052: Standalone typed AStar](../decisions/navigation.md#adr-0052)
