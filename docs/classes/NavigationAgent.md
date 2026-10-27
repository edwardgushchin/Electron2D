# NavigationAgent

Last updated: 2026-10-26

- Source: [NavigationAgent.cs](../../src/Navigation/2D/NavigationAgent.cs), [NavigationAgent.Debug.cs](../../src/Navigation/2D/NavigationAgent.Debug.cs)
- Component: [Navigation maps and agents](../components/navigation-maps.md#agent-path-following)

## Description

A getter-driven path follower for its direct Entity parent, with retained query/result versions, optional typed waypoint/link metadata, target progression, real map membership and node-owned RID.

Inherits [Node](Node.md), not Entity: the parent supplies spatial movement. Set TargetPosition, call GetNextPathPosition from parent physics processing, and move the parent yourself. Path following, link actions and retained path diagnostics execute through their owning services. Avoidance controls execute through the shared ORCA kernel.

Equal target assignments reset the request. Map/layer changes repath active targets. Other query settings take effect at the next query. Desired thresholds accept finite signed values and use strict comparisons; reachability is inclusive, off-path reload is inclusive. Empty routes remain unfinished; an unreachable final waypoint finishes without reaching the requested target. Completed requests do not repeat events. Internal snapshots avoid public array copies during warmed following updates.

The agent retains a borrowed result whose consumer disposal rejects while the agent is alive. External result setters retain copied arrays; replacing its path requests a fresh query and missing metadata is safe. Lifecycle/World rebinding from callbacks aborts stale progression and invalidates after the outer update. Waypoint transitions advance before delivery. Every subscriber is attempted; callback failures aggregate afterward. Explicitly stored scene settings and targets reload through ResourceFileTypes; RIDs/results/maps/indexes are runtime state.

## Example

Parent physics consumer; parent is an attached Entity and agent is its direct child.

```csharp
agent.TargetPosition = new(80, 20);
agent.LinkReached += details =>
{
    if (details.LinkExitPosition is Vector2 exit)
        parent.GlobalPosition = exit;
};
// Each physics tick:
Vector2 next = agent.GetNextPathPosition();
Vector2 offset = next - parent.GlobalPosition;
parent.GlobalPosition += offset.LimitLength((float)delta * 100);
```

## Constructors

| Member | Contract |
| --- | --- |
| [`public NavigationAgent()`](#constructor) | Creates a detached agent with default path thresholds and all waypoint metadata enabled. |

## Properties

| Member | Contract |
| --- | --- |
| [`public bool DebugEnabled { get; set; }`](#debugenabled) | Draws the current path on the parent canvas without advancing navigation. |
| [`public bool DebugUseCustom { get; set; }`](#debugusecustom) | Selects the custom color and waypoint size. |
| [`public Color DebugPathCustomColor { get; set; }`](#debugpathcustomcolor) | Finite custom line/waypoint color, initially white. |
| [`public float DebugPathCustomLineWidth { get; set; }`](#debugpathcustomlinewidth) | Always-selected finite line width, initially -1; zero omits the line. |
| [`public float DebugPathCustomPointSize { get; set; }`](#debugpathcustompointsize) | Custom square size, initially 4; zero suppresses all waypoints. |
| [`public uint NavigationLayers { get; set; }`](#navigationlayers) | Gets or changes the 32-bit navigation mask; changing it repaths an active request. |
| [`public float PathDesiredDistance { get; set; }`](#pathdesireddistance) | Gets or changes the finite strict waypoint distance threshold; nonpositive values never advance. |
| [`public float PathMaxDistance { get; set; }`](#pathmaxdistance) | Gets or changes the finite inclusive distance from the active path segment that triggers a repath. |
| [`public NavigationPathQueryParameters.PathMetadataFlags PathMetadataFlags { get; set; }`](#pathmetadataflags) | Gets or changes the metadata selection applied on the next path query. |
| [`public NavigationPathQueryParameters.PathPostProcessing PathPostprocessing { get; set; }`](#pathpostprocessing) | Gets or changes the output mode applied on the next path query. |
| [`public float PathReturnMaxLength { get; set; }`](#pathreturnmaxlength) | Gets or changes the finite maximum output length clamped to zero; zero disables clipping. |
| [`public float PathReturnMaxRadius { get; set; }`](#pathreturnmaxradius) | Gets or changes the finite maximum output circle radius clamped to zero; zero disables clipping. |
| [`public float PathSearchMaxDistance { get; set; }`](#pathsearchmaxdistance) | Gets or changes the finite search distance clamped to zero; zero disables the limit. |
| [`public int PathSearchMaxPolygons { get; set; }`](#pathsearchmaxpolygons) | Gets or changes the processed polygon limit; nonpositive values are unlimited. |
| [`public NavigationPathQueryParameters.PathfindingAlgorithm PathfindingAlgorithm { get; set; }`](#pathfindingalgorithm) | Gets or changes the search algorithm applied on the next path query. |
| [`public float SimplifyEpsilon { get; set; }`](#simplifyepsilon) | Gets or changes the finite simplification epsilon clamped to zero, applied on the next path query. |
| [`public bool SimplifyPath { get; set; }`](#simplifypath) | Gets or changes the point simplification applied on the next path query. |
| [`public float TargetDesiredDistance { get; set; }`](#targetdesireddistance) | Gets or changes the finite strict target threshold, inclusive for reachability; nonpositive values never reach. |
| [`public Vector2 TargetPosition { get; set; }`](#targetposition) | Gets or submits a finite world-space target; even equal assignments request a new path. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<NavigationWaypoint> LinkReached`](#linkreached) | Occurs after WaypointReached for a waypoint whose selected type metadata identifies a link. |
| [`public event Action NavigationFinished`](#navigationfinished) | Occurs after reaching the target or the final waypoint of an unreachable target. |
| [`public event Action PathChanged`](#pathchanged) | Occurs after a new path is published, including an empty path. |
| [`public event Action TargetReached`](#targetreached) | Occurs when the parent is strictly within TargetDesiredDistance of the requested target. |
| [`public event Action<NavigationWaypoint> WaypointReached`](#waypointreached) | Occurs once for each reached waypoint, before LinkReached for a link. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#createsceneinstancefactory) | Typed inherited lifecycle/discovery hook. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Typed inherited lifecycle/discovery hook. |
| [`public float DistanceToTarget()`](#distancetotarget) | Returns the current parent-to-requested-target distance without advancing the path. |
| [`public override string[] GetConfigurationWarnings()`](#getconfigurationwarnings) | Typed inherited lifecycle/discovery hook. |
| [`public Vector2[] GetCurrentNavigationPath()`](#getcurrentnavigationpath) | Returns a copied current world-space path without advancing navigation. |
| [`public int GetCurrentNavigationPathIndex()`](#getcurrentnavigationpathindex) | Returns the current waypoint index without advancing navigation. |
| [`public NavigationPathQueryResult GetCurrentNavigationResult()`](#getcurrentnavigationresult) | Returns the retained result used by this agent. |
| [`public Vector2 GetFinalPosition()`](#getfinalposition) | Updates navigation and returns its final reachable path position. |
| [`public bool GetNavigationLayerValue(int layerNumber)`](#getnavigationlayervalue) | Returns a navigation layer bit using one-based indices. |
| [`public RID GetNavigationMap()`](#getnavigationmap) | Returns the explicit override, attached direct Entity parent's World map, or empty. |
| [`public Vector2 GetNextPathPosition()`](#getnextpathposition) | Updates navigation and returns the next waypoint or current parent position for an empty path. |
| [`public float GetPathLength()`](#getpathlength) | Returns the current published path length without advancing navigation. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Typed inherited lifecycle/discovery hook. |
| [`public RID GetRID()`](#getrid) | Returns the stable node-owned agent identity. |
| [`public bool IsNavigationFinished()`](#isnavigationfinished) | Updates navigation and returns completion of the current request. |
| [`public bool IsTargetReachable()`](#istargetreachable) | Updates navigation and tests the final point against the inclusive target reachability threshold. |
| [`public bool IsTargetReached()`](#istargetreached) | Returns cached target completion without advancing the path. |
| [`protected override void OnEnterTree()`](#onentertree) | Typed inherited lifecycle/discovery hook. |
| [`protected override void OnExitTree()`](#onexittree) | Typed inherited lifecycle/discovery hook. |
| [`protected override void OnNotification(int what)`](#onnotification) | Typed inherited lifecycle/discovery hook. |
| [`public void SetNavigationLayerValue(int layerNumber, bool value)`](#setnavigationlayervalue) | Changes a navigation layer bit and repaths an active request. |
| [`public void SetNavigationMap(RID map)`](#setnavigationmap) | Changes the live map override; empty restores the parent's selected World map. |
| [`protected override void ValidateDisposal()`](#validatedisposal) | Typed inherited lifecycle/discovery hook. |
| [`protected override void ValidateMutation()`](#validatemutation) | Typed inherited lifecycle/discovery hook. |

## Member descriptions

<a id="constructor"></a>
### `public NavigationAgent()`

Creates a detached agent with default path thresholds and all waypoint metadata enabled.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="navigationlayers"></a>
### `public uint NavigationLayers { get; set; }`

Gets or changes the 32-bit navigation mask; changing it repaths an active request. Default 1u; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathdesireddistance"></a>
### `public float PathDesiredDistance { get; set; }`

Gets or changes the finite strict waypoint distance threshold; nonpositive values never advance. Default 20f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathmaxdistance"></a>
### `public float PathMaxDistance { get; set; }`

Gets or changes the finite inclusive distance from the active path segment that triggers a repath. Default 100f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathmetadataflags"></a>
### `public NavigationPathQueryParameters.PathMetadataFlags PathMetadataFlags { get; set; }`

Gets or changes the metadata selection applied on the next path query. Default NavigationPathQueryParameters.PathMetadataFlags.IncludeAll; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathpostprocessing"></a>
### `public NavigationPathQueryParameters.PathPostProcessing PathPostprocessing { get; set; }`

Gets or changes the output mode applied on the next path query. Default NavigationPathQueryParameters.PathPostProcessing.CorridorFunnel; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathreturnmaxlength"></a>
### `public float PathReturnMaxLength { get; set; }`

Gets or changes the finite maximum output length clamped to zero; zero disables clipping. Default 0f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathreturnmaxradius"></a>
### `public float PathReturnMaxRadius { get; set; }`

Gets or changes the finite maximum output circle radius clamped to zero; zero disables clipping. Default 0f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathsearchmaxdistance"></a>
### `public float PathSearchMaxDistance { get; set; }`

Gets or changes the finite search distance clamped to zero; zero disables the limit. Default 0f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathsearchmaxpolygons"></a>
### `public int PathSearchMaxPolygons { get; set; }`

Gets or changes the processed polygon limit; nonpositive values are unlimited. Default 4096; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathfindingalgorithm"></a>
### `public NavigationPathQueryParameters.PathfindingAlgorithm PathfindingAlgorithm { get; set; }`

Gets or changes the search algorithm applied on the next path query. Default NavigationPathQueryParameters.PathfindingAlgorithm.AStar; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="simplifyepsilon"></a>
### `public float SimplifyEpsilon { get; set; }`

Gets or changes the finite simplification epsilon clamped to zero, applied on the next path query. Default 0f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="simplifypath"></a>
### `public bool SimplifyPath { get; set; }`

Gets or changes the point simplification applied on the next path query. Default false; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="targetdesireddistance"></a>
### `public float TargetDesiredDistance { get; set; }`

Gets or changes the finite strict target threshold, inclusive for reachability; nonpositive values never reach. Default 10f; retained source setting.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ArgumentOutOfRangeException`: Value is nonfinite or an unsupported enum/flag.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="targetposition"></a>
### `public Vector2 TargetPosition { get; set; }`

Gets or submits a finite world-space target; even equal assignments request a new path. Zero initially; assignment resets completion, result and index.

`ArgumentOutOfRangeException`: The point is nonfinite.

`InvalidOperationException`: Mutation occurs during navigation event delivery or off the tree owner thread.

`ObjectDisposedException`: The agent is disposed.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="linkreached"></a>
### `public event Action<NavigationWaypoint> LinkReached`

Occurs after WaypointReached for a waypoint whose selected type metadata identifies a link.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="navigationfinished"></a>
### `public event Action NavigationFinished`

Occurs after reaching the target or the final waypoint of an unreachable target.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="pathchanged"></a>
### `public event Action PathChanged`

Occurs after a new path is published, including an empty path.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="targetreached"></a>
### `public event Action TargetReached`

Occurs when the parent is strictly within TargetDesiredDistance of the requested target.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="waypointreached"></a>
### `public event Action<NavigationWaypoint> WaypointReached`

Occurs once for each reached waypoint, before LinkReached for a link.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="createsceneinstancefactory"></a>
### `protected override Func<Node> CreateSceneInstanceFactory()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="dispose"></a>
### `protected override void Dispose(bool disposing)`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="distancetotarget"></a>
### `public float DistanceToTarget()`

Returns the current parent-to-requested-target distance without advancing the path. World-space distance.

`InvalidOperationException`: The direct parent is not an attached Entity.

`ObjectDisposedException`: The agent is disposed.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getconfigurationwarnings"></a>
### `public override string[] GetConfigurationWarnings()`

Typed inherited lifecycle/discovery hook.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getcurrentnavigationpath"></a>
### `public Vector2[] GetCurrentNavigationPath()`

Returns a copied current world-space path without advancing navigation. Independent waypoint array, empty before querying.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getcurrentnavigationpathindex"></a>
### `public int GetCurrentNavigationPathIndex()`

Returns the current waypoint index without advancing navigation. Zero initially; the final reached waypoint remains selected.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getcurrentnavigationresult"></a>
### `public NavigationPathQueryResult GetCurrentNavigationResult()`

Returns the retained result used by this agent. Borrowed result; caller edits are observed safely on the next update. Consumer disposal rejects while this agent is alive.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getfinalposition"></a>
### `public Vector2 GetFinalPosition()`

Updates navigation and returns its final reachable path position. World-space final position or zero for an empty path.

`InvalidOperationException`: The direct parent is not an attached Entity or navigation is reentered.

`ObjectDisposedException`: The agent is disposed.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getnavigationlayervalue"></a>
### `public bool GetNavigationLayerValue(int layerNumber)`

Returns a navigation layer bit using one-based indices. Whether the bit is set.

`layerNumber`: Index one through thirty-two.

`ArgumentOutOfRangeException`: Index is outside one through thirty-two.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getnavigationmap"></a>
### `public RID GetNavigationMap()`

Returns the explicit override, attached direct Entity parent's World map, or empty. Source map selection; runtime RIDs are not serialized.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getnextpathposition"></a>
### `public Vector2 GetNextPathPosition()`

Updates navigation and returns the next waypoint or current parent position for an empty path. World-space steering target; the caller moves the parent.

`InvalidOperationException`: The direct parent is not an attached Entity, the caller is off-owner, or update reentry is attempted.

`AggregateException`: Observers fail after completed transitions.

`ObjectDisposedException`: The agent is disposed.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getpathlength"></a>
### `public float GetPathLength()`

Returns the current published path length without advancing navigation. World-space length, zero initially.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getpropertydescriptors"></a>
### `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getrid"></a>
### `public RID GetRID()`

Returns the stable node-owned agent identity. Borrowed RID valid until disposal; FreeRID rejects it.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="isnavigationfinished"></a>
### `public bool IsNavigationFinished()`

Updates navigation and returns completion of the current request. False initially or for an empty path; true after NavigationFinished.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="istargetreachable"></a>
### `public bool IsTargetReachable()`

Updates navigation and tests the final point against the inclusive target reachability threshold. False for an empty path; otherwise whether final-point distance is at most TargetDesiredDistance.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="istargetreached"></a>
### `public bool IsTargetReached()`

Returns cached target completion without advancing the path. Whether TargetReached has occurred since the latest target assignment.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="onentertree"></a>
### `protected override void OnEnterTree()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="onexittree"></a>
### `protected override void OnExitTree()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="onnotification"></a>
### `protected override void OnNotification(int what)`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="setnavigationlayervalue"></a>
### `public void SetNavigationLayerValue(int layerNumber, bool value)`

Changes a navigation layer bit and repaths an active request.

`layerNumber`: Index one through thirty-two.

`value`: New bit state.

`ArgumentOutOfRangeException`: Index is outside one through thirty-two.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: An attached call is off the owner thread; updating getters also reject invalid parent context and reentry.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="setnavigationmap"></a>
### `public void SetNavigationMap(RID map)`

Changes the live map override; empty restores the parent's selected World map.

`map`: Live map RID or empty.

`ArgumentException`: The map RID is stale or of another kind.

`InvalidOperationException`: Mutation occurs during navigation delivery or off the tree owner thread.

`ObjectDisposedException`: The agent is disposed.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="validatedisposal"></a>
### `protected override void ValidateDisposal()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="validatemutation"></a>
### `protected override void ValidateMutation()`

Typed inherited lifecycle/discovery hook.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

## Verification

[NavigationAgentTests](../../tests/Electron2D.Tests/NavigationAgentTests.cs) verifies managed progression, source persistence in a fresh process, metadata-driven native link actions and target pixels. See the component contract for tested backend and allocation limits.

## Reciprocal avoidance

AvoidanceEnabled registers weak scene velocity delivery. Desired Velocity and SetVelocityForced are submitted at the next positive physics-boundary step for an active target; source Velocity stays independent of safe output. The direct Entity parent supplies position and pause state. Path completion resets both preferred and simulation velocity. Callbacks can move the parent or stage next-step settings; recursive synchronization/stepping and node disposal during velocity delivery reject. All participants publish before any callback, and failures do not prevent subsequent deliveries.

## Properties

| Member | Contract |
| --- | --- |
| [`public bool AvoidanceEnabled { get; set; }`](#avoidanceenabled) | Gets or changes whether reciprocal avoidance participates. |
| [`public uint AvoidanceLayers { get; set; }`](#avoidancelayers) | Gets or changes 32-bit layers visible to other masks. |
| [`public uint AvoidanceMask { get; set; }`](#avoidancemask) | Gets or changes 32-bit mask selecting other participants. |
| [`public float AvoidancePriority { get; set; }`](#avoidancepriority) | Gets or changes priority in the interval zero through one. |
| [`public int MaxNeighbors { get; set; }`](#maxneighbors) | Gets or changes maximum selected neighbors; nonpositive disables neighbor selection. |
| [`public float MaxSpeed { get; set; }`](#maxspeed) | Gets or changes finite nonnegative output speed cap. |
| [`public float NeighborDistance { get; set; }`](#neighbordistance) | Gets or changes finite nonnegative neighbor search radius. |
| [`public float Radius { get; set; }`](#radius) | Gets or changes finite nonnegative avoidance disc radius. |
| [`public float TimeHorizonAgents { get; set; }`](#timehorizonagents) | Gets or changes finite nonnegative agent prediction horizon. |
| [`public float TimeHorizonObstacles { get; set; }`](#timehorizonobstacles) | Gets or changes finite nonnegative contour prediction horizon. |
| [`public Vector2 Velocity { get; set; }`](#velocity) | Gets or submits finite desired velocity for the next avoidance boundary of an active target request. |

## Events

| Member | Contract |
| --- | --- |
| [`public event Action<Vector2> VelocityComputed`](#velocitycomputed) | Occurs after the current step publishes all avoidance outputs, before physics simulation. |

## Methods and extension points

| Member | Contract |
| --- | --- |
| [`public bool GetAvoidanceLayerValue(int layerNumber)`](#getavoidancelayervalue) | Returns an avoidance layer bit using one-based indices. |
| [`public bool GetAvoidanceMaskValue(int layerNumber)`](#getavoidancemaskvalue) | Returns an avoidance mask bit using one-based indices. |
| [`public void SetAvoidanceLayerValue(int layerNumber, bool value)`](#setavoidancelayervalue) | Changes an avoidance layer bit using one-based indices. |
| [`public void SetAvoidanceMaskValue(int layerNumber, bool value)`](#setavoidancemaskvalue) | Changes an avoidance mask bit using one-based indices. |
| [`public void SetVelocityForced(Vector2 velocity)`](#setvelocityforced) | Submits a finite simulation velocity replacement for the next active-request boundary after teleporting. |

## Member descriptions

<a id="avoidanceenabled"></a>
### `public bool AvoidanceEnabled { get; set; }`

Gets or changes whether reciprocal avoidance participates. False initially; enabling registers typed velocity delivery.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is off-owner or during path navigation delivery.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="avoidancelayers"></a>
### `public uint AvoidanceLayers { get; set; }`

Gets or changes 32-bit layers visible to other masks. Default 1u; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="avoidancemask"></a>
### `public uint AvoidanceMask { get; set; }`

Gets or changes 32-bit mask selecting other participants. Default 1u; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="avoidancepriority"></a>
### `public float AvoidancePriority { get; set; }`

Gets or changes priority in the interval zero through one. Default 1f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="maxneighbors"></a>
### `public int MaxNeighbors { get; set; }`

Gets or changes maximum selected neighbors; nonpositive disables neighbor selection. Default 10; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="maxspeed"></a>
### `public float MaxSpeed { get; set; }`

Gets or changes finite nonnegative output speed cap. Default 100f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="neighbordistance"></a>
### `public float NeighborDistance { get; set; }`

Gets or changes finite nonnegative neighbor search radius. Default 500f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="radius"></a>
### `public float Radius { get; set; }`

Gets or changes finite nonnegative avoidance disc radius. Default 10f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="timehorizonagents"></a>
### `public float TimeHorizonAgents { get; set; }`

Gets or changes finite nonnegative agent prediction horizon. Default 1f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="timehorizonobstacles"></a>
### `public float TimeHorizonObstacles { get; set; }`

Gets or changes finite nonnegative contour prediction horizon. Default 0f; retained node source independent of direct server edits.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

`ArgumentOutOfRangeException`: Value is outside its finite range.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="velocity"></a>
### `public Vector2 Velocity { get; set; }`

Gets or submits finite desired velocity for the next avoidance boundary of an active target request. Zero initially; retained wanted velocity, independent of computed safe velocity.

`ArgumentException`: Velocity is nonfinite.

`ObjectDisposedException`: The agent is disposed.

`InvalidOperationException`: Source mutation is unavailable.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="velocitycomputed"></a>
### `public event Action<Vector2> VelocityComputed`

Occurs after the current step publishes all avoidance outputs, before physics simulation. Handlers may submit next-step velocity and move the parent. Every handler is attempted; exceptions aggregate after delivery. Disposal during this event rejects.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getavoidancelayervalue"></a>
### `public bool GetAvoidanceLayerValue(int layerNumber)`

Returns an avoidance layer bit using one-based indices. Whether the bit is set.

`layerNumber`: Index one through thirty-two.

`ArgumentOutOfRangeException`: The index is outside one through thirty-two.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="getavoidancemaskvalue"></a>
### `public bool GetAvoidanceMaskValue(int layerNumber)`

Returns an avoidance mask bit using one-based indices. Whether the bit is set.

`layerNumber`: Index one through thirty-two.

`ArgumentOutOfRangeException`: The index is outside one through thirty-two.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="setavoidancelayervalue"></a>
### `public void SetAvoidanceLayerValue(int layerNumber, bool value)`

Changes an avoidance layer bit using one-based indices.

`layerNumber`: Index one through thirty-two.

`value`: New bit state.

`ArgumentOutOfRangeException`: The index is outside one through thirty-two.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="setavoidancemaskvalue"></a>
### `public void SetAvoidanceMaskValue(int layerNumber, bool value)`

Changes an avoidance mask bit using one-based indices.

`layerNumber`: Index one through thirty-two.

`value`: New bit state.

`ArgumentOutOfRangeException`: The index is outside one through thirty-two.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

<a id="setvelocityforced"></a>
### `public void SetVelocityForced(Vector2 velocity)`

Submits a finite simulation velocity replacement for the next active-request boundary after teleporting.

`velocity`: Forced world velocity; desired velocity is retained separately.

`ArgumentException`: Velocity is nonfinite.

`InvalidOperationException`: Source mutation is unavailable.

Attached access follows the tree owner thread and disposed access rejects. Advancing getters require an attached direct Entity parent and reject reentry. Observer failures aggregate after delivered transitions. Agent source mutation and disposal reject during delivery; parent movement remains available.

## Path diagnostics

All five debug properties are stored and restored in scenes. DebugEnabled defaults false and lazily creates one internal canvas; ordinary child enumeration and scene packing omit that transient helper. Drawing reads the current result without querying or advancing navigation, including a caller-replaced path. Empty and one-point paths draw nothing. Parent visibility, exit/reentry, canvas transforms and canvas layer membership apply. World path positions do not move with the actor. Parent modulation/material do not apply; overlay Z is MaximumZIndex minus one, independent of the actor Z.

<a id="debugenabled"></a>
### `public bool DebugEnabled { get; set; }`

False initially. Requires both this flag and the retained project default enablement; it is independent of SceneTree.DebugNavigationHint and global map diagnostics. Disabling clears retained drawing, while re-enabling uses the current path. The helper is disposed with the agent.

<a id="debugusecustom"></a>
### `public bool DebugUseCustom { get; set; }`

False initially. Selects custom color and point size; the line width always uses DebugPathCustomLineWidth. Default enablement, red color and four-unit point size are sampled once, with feature overrides, on first diagnostics use. See [project settings](ProjectSettings.md#navigation-agent-diagnostic-keys).

<a id="debugpathcustomcolor"></a>
### `public Color DebugPathCustomColor { get; set; }`

Opaque white initially. Rejects nonfinite components with ArgumentException.

<a id="debugpathcustomlinewidth"></a>
### `public float DebugPathCustomLineWidth { get; set; }`

Minus one initially. Negative widths use one framebuffer pixel; positive widths use canvas units; zero draws no line. Nonfinite values raise ArgumentOutOfRangeException.

<a id="debugpathcustompointsize"></a>
### `public float DebugPathCustomPointSize { get; set; }`

Four initially. Finite negative values clamp to zero; nonfinite values raise ArgumentOutOfRangeException. Zero suppresses waypoints even when DebugUseCustom is false. Otherwise the selected custom/default size draws centered filled squares.

All debug access follows agent lifetime and scene owner-thread guards. Mutation rejects during scene capture or navigation delivery. Failed helper creation preserves attachment/cleanup errors, removes the helper and permits a later retry. Agent disposal exits the tree before retiring its server identity and owned result/helper, attempts every cleanup stage and aggregates observer failures. RID inspection on the disposing thread remains valid through exit; foreign disposal access and access after retirement reject.

NavigationDebugTests covers properties/defaults, finite inputs, world geometry, result replacement, visibility, capture/thread guards, fresh-process scene/project restoration and all four scene navigation identity teardown paths. Native GPU/Compatibility profiles verify actual line/waypoint/visibility/canvas pixels and 64 active plus 64 idle full owner frames with zero managed bytes. Local Linux software-driver evidence does not establish target hardware, foreign platforms, native allocations or game-owner acceptance. Global region/link/avoidance diagnostics remain a separate producer.

Owned path, waypoint, link, target, completion and velocity event stores clear after scene exit during disposal, including when an exit observer fails. Retained [EventConnection](EventConnection.md) tokens then report actual detachment. Accepted immutable emissions retain their ordinary delivery rules.

The [global scene diagnostics](../components/navigation-diagnostics.md) prepare one separate constructor-owned internal canvas for this source. Its lifetime, storage exclusion, global/tree/editor enablement and world-space drawing are owned by ADR 0097.
