# Electron2D navigation decisions

Last updated: 2026-10-10

This bounded document owns standalone and server-backed 2D pathfinding decisions. The [decision index](index.md) routes other domains.

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

`AStarGrid` has its own complete grid, cell-shape, diagonal and jump-point slice under ADR 0053. Authored navigation maps/regions now execute under ADR 0097; polygon baking retains its geometry kernel prerequisites; agents and avoidance execute under ADR 0097. This decision does not classify those domains as implemented.

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

Games can query full or partial cell paths without constructing point-edge graphs. The grid is a managed standalone algorithm with no renderer, physics, SDL or native platform verification requirement. Returned paths and cell data are snapshots. Large grid performance and owner game acceptance remain separate verification work; authored navigation-server maps/regions execute under ADR 0097, while baking retains its geometry backend triggers.

<a id="adr-0097"></a>
## ADR 0097: Typed planar navigation maps and authored region topology

Last updated: 2026-10-07

- Status: Accepted for the user-authorized full API implementation goal
- Scope: Executable map/region/polygon/link/query/agent pathfinding backend, World registration and scene ownership
- Depends on: [0004](product.md#adr-0004), [0005](core-object-runtime.md#adr-0005), [0052](#adr-0052), [0063](physics.md#adr-0063), [0095](singleton-services.md#adr-0095)

### Decision

- Implement NavigationServer as a retained process-wide ElectronObject with static typed operations. RID maps/regions keep their own ownership; World.NavigationMap lazily registers an active borrowed map with the same runtime lifetime as canvas/physics. NavigationRegion maps the scene Entity role onto the same server region storage.
- The initial backend consumes copied authored planar vertices and convex polygon indices from NavigationPolygon. Use existing planar math types, .NET collections and a polygon/portal search kernel; no second public AStar facade, backend-native IDs, Variant or 3D vector API is exposed. Polygon data snapshots, numeric/type/index validation and resource graph copying remain typed.
- Commands stage configuration under one cold service gate and immutable committed map iterations supply path/closest-point queries. Synchronize commits at the beginning of the physics lane; an explicit typed Synchronize batch call exercises the same kernel without a SceneTree. It is a C# host operation, not a renamed deprecated MapForceUpdate. Queued topology changes do not silently mutate published paths. MapChanged follows committed publication; handlers may stage later commands, but recursive synchronization rejects.
- Authored geometry connects shared edges; enabled/layer/transform/travel/entry-cost changes participate in real topology and routing. Path search projects endpoints, finds an applicable polygon corridor, and returns copied paths with portal-midpoint or funnel optimization. Keep weighted search, unreachable closest-reached behavior and supported edge-merging policy auditable against the pinned implementation; unsupported topology/property branches retain explicit coverage dependencies.
- The authored topology kernel quantizes edge endpoints by floor into CellSize × MergeRasterizerCellScale, first within each region and then across external region edges. Retain the first two owners in published region/polygon/edge order; subsequent owners do not create connections. Directed free-edge margin pathways are separate published region connections. Cell/raster edits stage and rebuild attached regions transactionally. Region versions retain nonzero 32-bit wrap, projected to ulong like link versions.
- Initialize the effective raster dimensions from both scalar defaults at map construction, correcting the pinned stale initial dimensions. Ordinary finite float raster arithmetic matches the source; wider integer keys preserve finite large-coordinate authoring without the source's undefined int32 conversion aliasing. Margin projection widens finite arithmetic and clamps both overlap endpoints consistently, correcting the pinned asymmetric extrapolation. These changes preserve complete publication and the existing finite-geometry guarantees.
- Publish the complete ProcessInfo family from one successful synchronization, including all active-map member counts, authored polygon/region-edge/internal-merge counts, interregion raster/margin connections and eligible free edges. Disabled regions retain authored topology data but query traversal excludes them. Failed builds or counter overflow retain the prior snapshot; callbacks observe the committed version. Cold builds may allocate; warmed unchanged synchronization and counter/pathway reads reuse storage.
- Random-point queries consume committed nonempty enabled surfaces and layer masks. Uniform mode weights regions, polygons and triangles by area; nonuniform mode chooses each level uniformly. Both sample inside a selected triangle. Use double area/interpolation arithmetic for finite extreme coordinates, and index the filtered region list, fixing the pinned unfiltered-index/layer behavior. Empty eligible surfaces return zero; random sequences are not a cross-engine determinism contract.
- Do not expose bake settings, async switches or constant-result compatibility members before their consumers execute. Baking/source parsing, clearance/outlines and debug/editor producers retain exact separate prerequisites. The deprecated make_polygons_from_outlines, map_force_update and get_region_rid are excluded under ADR 0004 with pinned metadata; current authored geometry, synchronization and GetRID supply executable paths.
- NavigationLink : Entity and server-owned link RIDs use the same directed/bidirectional off-surface connection store. Finite endpoints attach to nearest enabled polygon surfaces strictly inside the map's connection radius (default four world units), before layer filtering at query time. A synthetic segment polygon participates in the same weighted corridor search; link geometry does not become a walkable region or closest-point owner. Link enablement, layers, costs, source transforms and map replacement have real consumers. Publication includes link versions and map topology transactionally.
- Scene link source properties are cached independently of direct server edits. Local/global endpoint conversion preserves the detached local-coordinate role; attachment publishes world coordinates. Global notifications stage both endpoints atomically before the next shared physics-boundary synchronization, following the same existing region publication boundary. This uses the accepted scene transform delivery rather than a second delayed internal-physics pass. Link versions retain the pinned 32-bit nonzero wrap, projected to ulong in the typed public identity counter.
- NavigationPathQueryParameters and NavigationPathQueryResult are caller-owned ElectronObject types with copied arrays, typed enums and discovered property descriptors. Algorithm/PostProcessing avoid C# nested enum/member collisions; RID and instance metadata use RID[] and ulong[]. QueryPath captures parameters plus one committed map, publishes a complete result before an optional zero-argument Action callback, and permits callback changes/nested queries after publication. Invalid queries preserve the previous result. Parameter float limits/epsilon clamp negative values to zero; nonpositive search counts are unlimited; unsupported enum bits/nonfinite inputs reject.
- Query controls have executable consumers: region include/exclude filters (exclusion wins, both link endpoint regions must be included), three postprocessing modes, polygon-count/entry-distance search limits, point metadata flags, iterative RDP simplification and length/circle clipping. MapGetPath shares this query kernel with metadata disabled. Crossed portals preserve primitive provenance; link entry/exit markers remain even when collinear so typed consumers can execute transitions. This corrects lost link markers in the pinned funnel output rather than treating a route across a link as an unmarked region segment.
- Fix two pinned defects in the typed slice: result Reset clears length as well as arrays, matching its documented initial-state contract; include-only link filtering checks both included endpoint regions instead of the source's accidental excluded-end check. Keep exact raster/nonmanifold/search-tie equivalence visible as remaining query coverage, rather than adding inert query settings.
- World and scene nodes use the same server identities. Node exit releases borrowed region membership, stable managed region identity remains until node disposal, and user-created regions/maps require FreeRID. World-owned maps reject FreeRID and are released with runtime teardown.

- NavigationAgent : Node follows its direct Entity parent through the same typed query kernel. Stable weak scene agent RIDs and caller-owned server agents provide executable map membership and consumed committed-version observations; they are not avoidance bodies. Tree entry/exit/reparenting and viewport World replacement rebind agent membership. No avoidance/debug setting is exposed before its producer executes.
- Path progression is getter-driven rather than automatic motion: GetNextPathPosition, GetFinalPosition, IsTargetReachable and IsNavigationFinished update an active submitted request. Equal target assignments repath. Layer/map changes repath active requests; other query controls apply on the next query. Waypoint and target thresholds retain finite signed values and strict distance comparisons; reachability is inclusive and off-path reload is inclusive. Empty paths stay unfinished. Unreachable final-waypoint completion emits NavigationFinished without TargetReached. Double intermediates prevent finite large-coordinate distance/segment overflow.
- NavigationWaypoint replaces dynamic waypoint dictionaries with nullable typed metadata, including logical OwnerID, live scene navigation owner and directional link endpoints. Unknown caller-assigned IDs retain their identity and resolve no managed owner. WaypointReached precedes LinkReached; TargetReached precedes NavigationFinished. State advances before observer delivery, every subscriber is attempted and failures aggregate after the update. Reentrant updates, agent source mutation and agent disposal reject during navigation delivery; parent movement remains permitted for executable link actions. Lifecycle/World rebinding during delivery invalidates the request after the outer update and aborts stale progression. The borrowed result has agent-owned disposal, copied public arrays and immutable internal snapshots; external path replacement triggers a fresh query and mismatched metadata cannot index past an array. This corrects unsafe callback recursion and externally modified result indexing in the pinned implementation.

- Agent and obstacle avoidance use an internal managed adaptation of RVO2 ORCA within Electron2D.dll, retaining its Apache-2.0 license and source provenance in third-party notices. Shared enum/math/lifetime types remain the public projection; no native or managed backend assembly is added. Reusable neighbor, contour-constraint and linear-program buffers support warmed allocation-free stepping. Initial neighbor collection is a deterministic quadratic scan with stable RID ties; spatial indexing is a measured scaling follow-up rather than a second public backend.
- Static NavigationServer.Step(double) drives unbound caller maps for typed batch hosts; SceneTree's physics boundary drives its selected World/caller-override maps before physics calculations. Bound maps cannot be advanced by a second tree or batch owner. Desired node velocities are submitted at the next navigation boundary; path completion zeros both preferred and simulation velocity. Pause notifications immediately update agent pause state; scene obstacles detach their runtime map and restore the previous membership when resumed, while source map selection remains independent. Paused parents/obstacles do not participate. All participants solve from the same pre-step state and publish before callback delivery, fixing order-dependent in-place updates; callbacks can stage the next step, but recursive synchronization/step rejects. Freed/disabled/reassigned callback recipients are checked before delivery.
- Circle obstacles use moving-agent predictions; oriented contour obstacles use static one-sided segment/convex-leg constraints, preserving winding and mask semantics. Source transforms publish world translation, transformed contour offsets and maximum absolute scale for radius. Bake-affect/carve settings stay unavailable until their bake consumer executes. Finite numeric inputs reject invalid signs/ranges; nonpositive max-neighbor counts disable agent neighbors. Float-representable duration bounds reject underflow/overflow before simulation. Zero duration synchronizes without simulation/callbacks, zero horizons use one step for reciprocal predictions, and coincident stationary discs use opposing deterministic normals instead of division by zero.

### Verification

The connected slice requires polygon/resource checks, deferred publication and reentry/lifetime errors, weighted routes, projection/unreachable behavior, region scene membership/World replacement, and real rendered path following on each claimed backend. Source/scene storage is verified separately from native pixels; compilation alone establishes neither.
