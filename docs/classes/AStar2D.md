# AStar2D

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** Caller-defined graph types

- **Source:** [AStar2D.cs](../../src/Navigation/2D/AStar2D.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class AStar2D : ElectronObject`

## Description

A standalone directed, weighted graph for 2D point paths. It works without SceneTree, rendering, SDL or a navigation server. Points have nonnegative 64-bit IDs, finite positions, nonnegative entry weights and an enabled flag. Directed edges govern travel; either direction creates a physical segment for nearest-segment queries. A-star search uses Euclidean cost/estimate unless a derived type overrides protected typed hooks. The neighbor hook runs only when enabled.

The implementation follows the pinned graph's ID cursor, point/connection storage order, capacity thresholds, weighted path priority and closest-reached partial path. It returns caller-owned arrays. Managed lifetime and typed exceptions replace manual reference counting and engine diagnostics. The graph is not internally synchronized; concurrent callers must coordinate access. A callback may read points but cannot mutate, dispose or re-enter the graph during a path search.

## Example

```csharp
using var graph = new AStar2D();
graph.AddPoint(1, new Vector2(0, 0));
graph.AddPoint(2, new Vector2(0, 20));
graph.AddPoint(3, new Vector2(30, 20));
graph.ConnectPoints(1, 2);
graph.ConnectPoints(2, 3);
long[] route = graph.GetIDPath(1, 3); // 1, 2, 3
```

## Constructor

| Member | Contract |
| --- | --- |
| [`public AStar2D()`](#constructor) | Creates an empty graph with reported capacity sixteen. |

## Property

| Member | Contract |
| --- | --- |
| [`public bool NeighborFilterEnabled { get; set; }`](#neighborfilterenabled) | Enables the protected neighbor-filter callback; false by default. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public void AddPoint(long id, Vector2 position, float weightScale = 1f)`](#addpoint) | Adds or updates a point without changing existing links or enablement. |
| [`public bool ArePointsConnected(long id, long toID, bool bidirectional = true)`](#arepointsconnected) | Tests any physical segment or a specific travel direction. |
| [`public void Clear()`](#clear) | Removes all points and links while retaining capacity. |
| [`public void ConnectPoints(long id, long toID, bool bidirectional = true)`](#connectpoints) | Adds one or both travel directions. |
| [`public void DisconnectPoints(long id, long toID, bool bidirectional = true)`](#disconnectpoints) | Removes one or both travel directions. |
| [`public long GetAvailablePointID()`](#getavailablepointid) | Finds the next unused ID from the last-freed cursor. |
| [`public long GetClosestPoint(Vector2 toPosition, bool includeDisabled = false)`](#getclosestpoint) | Finds the nearest eligible point, breaking ties by lowest ID. |
| [`public Vector2 GetClosestPositionInSegment(Vector2 toPosition)`](#getclosestpositioninsegment) | Projects to the nearest enabled segment. |
| [`public long[] GetIDPath(long fromID, long toID, bool allowPartialPath = false)`](#getidpath) | Returns a full or closest-reached partial ID route. |
| [`public long GetPointCapacity()`](#getpointcapacity) | Reports the graph's pinned power-of-two backing capacity. |
| [`public long[] GetPointConnections(long id)`](#getpointconnections) | Returns outgoing neighbor IDs. |
| [`public long GetPointCount()`](#getpointcount) | Returns registered point count. |
| [`public long[] GetPointIDs()`](#getpointids) | Returns IDs in graph storage order. |
| [`public Vector2[] GetPointPath(long fromID, long toID, bool allowPartialPath = false)`](#getpointpath) | Returns the matching position route. |
| [`public Vector2 GetPointPosition(long id)`](#getpointposition) | Reads a registered point's position. |
| [`public float GetPointWeightScale(long id)`](#getpointweightscale) | Reads the entry weight. |
| [`public bool HasPoint(long id)`](#haspoint) | Tests point existence. |
| [`public bool IsPointDisabled(long id)`](#ispointdisabled) | Reads pathfinding eligibility. |
| [`public void RemovePoint(long id)`](#removepoint) | Removes a point and all incident edges. |
| [`public void ReserveSpace(long numNodes)`](#reservespace) | Reserves positive point capacity. |
| [`public void SetPointDisabled(long id, bool disabled = true)`](#setpointdisabled) | Enables or disables a point for paths. |
| [`public void SetPointPosition(long id, Vector2 position)`](#setpointposition) | Changes a point's position. |
| [`public void SetPointWeightScale(long id, float weightScale)`](#setpointweightscale) | Changes the entry weight. |
| [`protected virtual float OnComputeCost(long fromID, long toID)`](#oncomputecost) | Supplies unweighted edge cost. |
| [`protected virtual float OnEstimateCost(long fromID, long endID)`](#onestimatecost) | Supplies remaining-cost heuristic. |
| [`protected virtual bool OnFilterNeighbor(long fromID, long neighborID)`](#onfilterneighbor) | Optionally excludes an outgoing neighbor. |
| [`protected override void ValidateDisposal()`](#validatedisposal) | Rejects disposal during search. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases owned graph references. |

## Member descriptions

<a id="constructor"></a>
### `public AStar2D()`

Creates an empty graph with capacity sixteen and available ID zero. The caller owns its deterministic disposal.

<a id="neighborfilterenabled"></a>
### `public bool NeighborFilterEnabled { get; set; }`

False initially. When true, the path solver calls `OnFilterNeighbor` before computing the edge cost. Changing it from a search callback throws `InvalidOperationException`.

<a id="addpoint"></a>
### `public void AddPoint(long id, Vector2 position, float weightScale = 1f)`

IDs must be nonnegative, positions finite and weights finite/nonnegative. A duplicate ID updates position and weight while retaining links, enabled state and storage order. Each entered edge multiplies its computed cost by the destination's weight. Invalid input rejects before mutation.

<a id="arepointsconnected"></a>
### `public bool ArePointsConnected(long id, long toID, bool bidirectional = true)`

With the default true, returns true if either direction exists between the IDs, even if one endpoint is disabled. False checks travel specifically from `id` to `toID`. Missing points or edges return false.

<a id="clear"></a>
### `public void Clear()`

Removes every point and edge and resets the available-ID cursor to zero. Reported point capacity remains available for reuse.

<a id="connectpoints"></a>
### `public void ConnectPoints(long id, long toID, bool bidirectional = true)`

Both points must exist and differ. The default adds travel in both directions; false adds only `id` to `toID`. An existing direction is unchanged; adding the reverse upgrades a one-way segment. Self-links and absent endpoints throw before mutation.

<a id="disconnectpoints"></a>
### `public void DisconnectPoints(long id, long toID, bool bidirectional = true)`

Both points must exist. The default removes both directions; false removes only travel from `id` to `toID`, preserving any reverse edge. An absent edge is a no-op.

<a id="getavailablepointid"></a>
### `public long GetAvailablePointID()`

Starts at the last freed ID, initially zero, and scans upward if occupied. Reaching an occupied `long.MaxValue` throws `InvalidOperationException` instead of overflowing.

<a id="getclosestpoint"></a>
### `public long GetClosestPoint(Vector2 toPosition, bool includeDisabled = false)`

Returns the nearest enabled point ID, or includes disabled points when requested. Exact squared-distance ties choose the lowest ID. The pinned scan starts with a `1e20` squared-distance bound, so it returns minus one when every point is at or beyond that bound. A nonfinite query position throws `ArgumentException`.

<a id="getclosestpositioninsegment"></a>
### `public Vector2 GetClosestPositionInSegment(Vector2 toPosition)`

Projects the finite query to any connected segment whose endpoints are enabled, independent of travel direction. Returns zero when no eligible segment exists. A nonfinite query throws `ArgumentException`.

<a id="getidpath"></a>
### `public long[] GetIDPath(long fromID, long toID, bool allowPartialPath = false)`

Both points must exist. Returns IDs from source through target when reachable; a disabled source returns an empty array even when source and target are equal. When partial routing is allowed, an unreachable or disabled target returns the route to the popped point with the lowest estimated remaining cost, then lowest traveled cost. Returned arrays belong to the caller. A callback failure or invalid cost leaves graph structure intact and propagates.

<a id="getpointcapacity"></a>
### `public long GetPointCapacity()`

Reports sixteen initially and power-of-two capacity after reserve/growth. Clear and point removal preserve it. A pre-allocation reservation can select a smaller capacity; later insertion grows at the pinned 75-percent load threshold. Managed collection limits reject extreme reservations before state mutation.

<a id="getpointconnections"></a>
### `public long[] GetPointConnections(long id)`

Returns a fresh array of outgoing neighbors in graph storage order. A missing point throws `KeyNotFoundException`; disabling the point does not erase links.

<a id="getpointcount"></a>
### `public long GetPointCount()`

Returns the current registered point count as a 64-bit integer.

<a id="getpointids"></a>
### `public long[] GetPointIDs()`

Returns a fresh array in graph storage order. Removing a point swaps the last stored ID into the removed slot; later insertions append.

<a id="getpointpath"></a>
### `public Vector2[] GetPointPath(long fromID, long toID, bool allowPartialPath = false)`

Uses the same solver and edge rules as `GetIDPath`, returning current `Vector2` positions in path order. Both endpoints must exist; the array is caller-owned. This operation is not internally synchronized with other graph users.

<a id="getpointposition"></a>
### `public Vector2 GetPointPosition(long id)`

Returns a registered point's position. A missing ID throws `KeyNotFoundException`.

<a id="getpointweightscale"></a>
### `public float GetPointWeightScale(long id)`

Returns the nonnegative entry weight. A missing ID throws `KeyNotFoundException`.

<a id="haspoint"></a>
### `public bool HasPoint(long id)`

Returns false for an absent ID, including a negative ID, without changing the graph.

<a id="ispointdisabled"></a>
### `public bool IsPointDisabled(long id)`

Returns whether a registered point is excluded from pathfinding. A missing ID throws `KeyNotFoundException`.

<a id="removepoint"></a>
### `public void RemovePoint(long id)`

Removes the point, every incoming and outgoing direction, and makes that ID the next available-ID cursor. Other points remain; the final stored ID swaps into the removed storage slot. A missing point throws `KeyNotFoundException`.

<a id="reservespace"></a>
### `public void ReserveSpace(long numNodes)`

Requires a positive request within managed collection limits. Reported capacity rounds up to a power of two; an allocated graph never shrinks. Invalid requests throw `ArgumentOutOfRangeException` without changing reported capacity.

<a id="setpointdisabled"></a>
### `public void SetPointDisabled(long id, bool disabled = true)`

Toggles pathfinding eligibility without deleting the point or its links. The default disables. A missing ID throws `KeyNotFoundException`.

<a id="setpointposition"></a>
### `public void SetPointPosition(long id, Vector2 position)`

Updates a registered point's finite position without changing topology or weight. A missing point throws `KeyNotFoundException`; nonfinite coordinates throw `ArgumentException` before mutation.

<a id="setpointweightscale"></a>
### `public void SetPointWeightScale(long id, float weightScale)`

Updates a registered point's finite nonnegative entry weight. Zero is allowed; invalid values throw `ArgumentOutOfRangeException` before mutation.

<a id="oncomputecost"></a>
### `protected virtual float OnComputeCost(long fromID, long toID)`

Returns Euclidean distance by default. The solver calls it for a traversable outgoing edge before applying the destination weight. Overrides may inspect graph state and must return finite nonnegative values.

<a id="onestimatecost"></a>
### `protected virtual float OnEstimateCost(long fromID, long endID)`

Returns Euclidean distance by default. A-star calls it for candidate ranking and closest-reached partial selection. Overrides must return finite nonnegative values; an overestimating heuristic can change optimality.

<a id="onfilterneighbor"></a>
### `protected virtual bool OnFilterNeighbor(long fromID, long neighborID)`

Returns false by default. Called only when `NeighborFilterEnabled` is true; returning true skips the neighbor before cost computation. The callback may read but cannot mutate or re-enter graph search.

<a id="validatedisposal"></a>
### `protected override void ValidateDisposal()`

Preserves inherited lifetime validation and rejects disposal during an active search before owned graph state changes.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Clears owned point and edge references, then completes inherited disposal. Later graph operations throw `ObjectDisposedException`.

## Verification and limits

[AStar2DTests](../../tests/Electron2D.Tests/AStar2DTests.cs) checks defaults, all own method families, the pinned four-point route and weighted alternative, directed edge transitions, disabled/partial routes, nearest queries, custom hooks, invalid inputs, callback/re-entry/disposal failure, capacity, array ownership and a 1,024-point path. The test runs in the managed executable harness. No SDL/native backend is involved; concurrent use and very large graphs require caller coordination and separate performance acceptance. [Coverage](../coverage/classes/AStar2D.md) records all declarations and their status.

## Decision

- [0052: Standalone typed AStar2D](../decisions/navigation.md#adr-0052)
