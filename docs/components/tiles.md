# Tile grids, animated atlases and physical layers

Last updated: 2026-10-11

## Scope and ownership

[ADR 0101](../decisions/tiles.md#adr-0101) connects `TileSet`, `TileSetSource`, `TileSetAtlasSource`, source-owned `TileData` and scene `TileMapLayer`. The implementation supplies square, isometric, half-offset square and hexagonal cells, all coordinate layouts, animated atlas regions/alternatives and all tile physics-layer policies. It uses the world's selected CPU or independent GPU implementation; renderer choice is independent.

A source belongs to one set. Transfer removes it from the former set before publishing notifications. Texture/material resources are borrowed. Shallow TileSet duplication still duplicates each source and its owned data, retaining texture/material references; deep resource duplication follows the ordinary resource graph policy. CopyFromResource also preserves the original source owner. Removing/disposal of an alternative invalidates borrowed TileData. Callers cannot dispose borrowed data separately. Source disposal removes it from its set; set/texture/material disposal updates consumers without taking ownership of those borrowed resources.

## Authoring example

The following assumes a live caller-owned `ImageTexture texture` containing a 16-pixel tile. Tests compile the same public workflow, save the graph and load an executable scene in another process.

```csharp
var set = new TileSet();
set.AddPhysicsLayer();
var atlas = new TileSetAtlasSource { Texture = texture };
int sourceID = set.AddSource(atlas);
atlas.CreateTile(Vector2i.Zero);
TileData tile = atlas.GetTileData(Vector2i.Zero, 0);
tile.AddCollisionPolygon(0);
tile.SetCollisionPolygonPoints(0, 0,
    [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
var layer = new TileMapLayer { TileSet = set };
layer.SetCell(Vector2i.Zero, sourceID, Vector2i.Zero);
// Add layer to a viewport's SceneTree, then flush deferred work or call UpdateInternals.
```

## Typed custom data

TileSet defines ordered named custom layers; TileData stores actual per-layer values with generic access. Names are exact/case-sensitive and unique when nonempty. Renaming/moving removes stale lookup aliases. A null configured type is nil; nil reads return default(T), while HasCustomData still reports the definition. Configured types provide defaults/conversions and do not restrict ordinary writes. Wrong non-nil reads or missing names throw KeyNotFoundException; named access without a set throws InvalidOperationException. Indices are checked.

```csharp
set.AddCustomDataLayer();
set.SetCustomDataLayerName(0, "Damage");
set.SetCustomDataLayerType<long>(0);
TileData data = atlas.GetTileData(Vector2i.Zero, 0);
data.SetCustomData("Damage", 12L);
long damage = data.GetCustomData<long>("Damage");
```

Schema edits prepare all conversions before commitment; overflow rejects the entire edit. All child lists/current names are committed before notifications, reentrant schema edits reject, and a listener failure cannot leave alternatives on different indices. Source transfer preflights the destination schema. Copies retain actual values without rebinding conversion, including values differing from configured types. Arrays/resources are borrowed during ordinary assignment and runtime copies; deep duplication follows the existing typed Resource graph policy. Replacing a callback copy's value does not replace the authored value; editing a shared borrowed resource/array still affects its owner.

Bool/numeric/text, Vector2/Vector2i, Vector4/Vector4i, Rect2/Rect2i and text/integer Color conversions execute. Incompatible types reset to default(T), with empty string/array, opaque black Color and identity Transform. Numeric text uses the documented prefix/saturation rules in [ADR 0101](../decisions/tiles.md#typed-custom-tile-data). Successful string conversion uses invariant scalar text, existing math text and bracketed arrays. Type edits are allocating authoring work. Warmed stable exact reads/writes reuse typed holders.

SetCustomDataLayerType(int, Type?) requires a factory already prepared by typed setters or portable codec/resource registration. Its generic overload prepares a direct factory for T. Files store configured and actual stable IDs separately; a fresh host must register its application codecs/resource factories before loading. Portable writes/deep copies reject unsupported transient/application types through their existing storage contracts. Private stored count/type arrays reject more than 65536 entries. No active callback state is serialized.

## Per-tile materials and draw order

TileData.Material borrows a material; null inherits the layer's effective material. ZIndex is a relative drawing offset within the canvas range, and YSortOrigin offsets the sorting anchor without moving image/collision/navigation geometry. Source data, runtime callback copies, deep/shallow duplicates and portable archives retain these fields. Every TileData assignment emits Changed, including equal values; disposed material assignment and invalid Z reject before mutation. Borrowed material disposal does not transfer ownership and rendering rejects its later use until replacement, as for CanvasItem.

TileMapLayer.RenderingQuadrantSize defaults 16 and independently groups floored grid coordinates. YSortEnabled replaces ordinary quadrants with groups of Y anchors, including tile/layer offsets at hundredth-unit resolution. Cells sort by X then Y; XDrawOrderReversed only changes Y-sorted groups. Consecutive material/Z runs get separate internal canvas items; normal quadrants draw by mapped quadrant Y ascending/X descending, below authored children. Effective canvas Z takes precedence. Generated items are transient, layer-owned and absent from the authored hierarchy. Caller writes/free/disposal reject; removal, exit, disposal and renderer shutdown release them and their weak parent links.

```csharp
set.UVClipping = true;
TileData data = atlas.GetTileData(Vector2i.Zero, 0);
data.Material = material; // Caller-owned CanvasItemMaterial or ShaderMaterial.
data.ZIndex = 2;
data.YSortOrigin = 6;
layer.RenderingQuadrantSize = 8;
layer.YSortEnabled = true;
layer.YSortOrigin = -4;
layer.XDrawOrderReversed = true;
```

Required context: the live set/atlas/layer from the authoring example, plus caller-owned material. Tests compile the equivalent public workflow. Null material uses live inherited material policy; child groups retain layer modulation/self modulation, visibility masks and filter/repeat/transform/canvas routing. Shader parameters and fixed blend changes execute without regrouping. Both source animation modes retain their existing timed commands. UVClipping defaults false and controls source-region texel-center sampling, independently of padding.

Preparation keeps previous groups until all candidates are ready; failures release candidates. Negative-edge quadrant multiplication is checked; nonrepresentable anchors reject rather than wrap. Y keys retain finite floating values rather than narrowing large hundredth-unit offsets to int32. Ordinary float scene precision remains the grid contract. Group preparation is allocating authoring work; unchanged and forced redraw frames reuse commands and identities.

## Runtime flow

Cell/resource edits mark dirty cells or the full layer and queue one reusable tree action. UpdateInternals compiles dirty quadrants. A quadrant groups cells by physics layer, constant surface velocity, one-way policy/margin and one-way row. Polygons are unioned and convex-decomposed; a union containing holes is triangulated with the existing geometry dependency. Invalid topology contributes no fixtures. The compilation validates finite coordinates and clipping range before allocating server bodies.

Generated static/kinematic bodies use shared PhysicsServer operations, with weak object association to the TileMapLayer and the current canvas-layer identity. There are no generated scene children. GetCoordsForBodyRID returns quadrant coordinates, including floor division for negative cells. PhysicsQuadrantSize defaults to 16; size one identifies a cell. Rebuilding a quadrant retires its old body RIDs while untouched quadrants retain theirs. Returned identities are borrowed: callers must not release them independently.

Translation/rotation preserve generated identities. Layer scale/skew is baked into shape geometry and triggers rebuilding. Cell transform flags affect both drawing and collision. TileData FlipH/FlipV/Transpose affect the image only; the author edits an alternative's collision contours independently. TextureOrigin also affects drawing only. Constant surface velocities remain world-space values. Signed material coefficients preserve Rough and Absorbent behavior; collision priority is passed to the common recovery policy.

Hidden layers retain authored collision. Runtime tile-data copies are prepared only for visible enabled cells selected by the protected callbacks; hiding cleans those copies and restores authored collision. Disabling the layer or collision removes bodies. World reassignment moves bodies to the newly selected space. Exit and disposal release generated bodies/shapes and invalidate runtime copies. Collision overlays follow Default/ForceShow/ForceHide and ordinary canvas visibility.

Queries use the existing typed owner split: Collider is the CollisionObject convenience view and is null for a generated tile body; ColliderObject identifies the TileMapLayer. Motion/cast results, typed direct contacts and Area/RigidBody object and shape events retain the same layer identity. Object events deduplicate multiple tile bodies; per-shape events preserve each body RID. Excluding the layer's instance ID excludes all its generated bodies.

## Grid placement and neighbors

TileSet stores TileShape (Square, Isometric, HalfOffsetSquare, Hexagon), TileLayout (Stacked, StackedOffset, StairsRight, StairsDown, DiamondRight, DiamondDown), TileOffsetAxis (Horizontal, Vertical) and positive TileSize. Their defaults preserve the square 16×16 grid. Square cells ignore layout and axis; the other shapes use alternating offsets and overlap ratios of one, one-half or three-quarters. Negative rows/columns use positive parity. All six coordinate systems preserve their authored coordinates through cell storage, quadrant identity, duplication and resource/scene files.

MapToLocal returns the cell center in local scene units, independently of TextureOrigin. LocalToMap selects the containing cell. Square boundaries use a normalized 0.00005 positive bias; sloping edges retain directional ties, including strict versus inclusive corner tests. Nonfinite inputs throw ArgumentOutOfRangeException and integer results outside Vector2i throw OverflowException. Floating scene coordinates lose adjacent-cell precision at extreme magnitudes; integer neighbor queries retain their precision with widened intermediates. Every geometry query requires a live TileSet.

GetNeighborCell validates the named CellNeighbor against the current shape and axis. Square cells have side and diagonal-corner directions. Isometric cells have diagonal sides and cardinal corners. HalfOffsetSquare and Hexagon have six sides selected by the offset axis. Invalid/absent directions throw ArgumentOutOfRangeException, and final integer overflow throws. GetSurroundingCells returns an owned array of four or six edge-sharing cells, clockwise and without corners.

Drawing, collision generation, navigation regions and root-local source parsing use this same center transform. The tile image and authored collision/navigation polygons keep their own shape and dimensions. Changing grid settings schedules existing consumers to rebuild through the normal batch; it does not warp a tile texture or manufacture a polygon.

```csharp
set.TileShape = TileShape.Hexagon;
set.TileLayout = TileLayout.Stacked;
set.TileOffsetAxis = TileOffsetAxis.Horizontal;
Vector2 center = layer.MapToLocal(new(-2, 3));
Vector2i next = layer.GetNeighborCell(new(-2, 3), CellNeighbor.BottomRightSide);
Vector2i[] edges = layer.GetSurroundingCells(new(-2, 3));
```

## Storage and failure behavior

Built-in compiled factories reconstruct TileSet, TileSetAtlasSource and TileMapLayer. Resource descriptors store ordered source identities, layer policies/material references, atlas regions/alternatives and owned tile-data fields; Vector2i arrays have a built-in portable codec. TileMapData is a copied version-zero little-endian array with 12-byte records and the documented signed-16-bit coordinate wrapping. Malformed array input fails before changing cells. Generated bodies and runtime copies are transient.

Attached node mutation requires the scene owner thread. Authored edits may queue work; publishing physics changes is prohibited while a solver/overlap callback holds the world. Runtime callback reentry is rejected. A failing runtime-data callback releases its new copy, leaves committed physical bodies intact and keeps a full retry pending. Resource/file operations and geometry changes allocate; unchanged physics frames and warmed redraw reuse existing storage.

## Verification

`TileLayoutTests` compares 48 pinned executable configurations: 3,888 centers and surrounding arrays, 27,216 named neighbors and 32,352 inverse/boundary results. It also checks reciprocal/invalid directions, quiet validated setters, finite/integer limits and widened large integer intermediates. All 48 geometry configurations are duplicated, packed, saved and reconstructed in a fresh process. Explicit CPU/GPU world fixtures check transformed collision/owner lookup, navigation regions, geometry edits, root-local traversable baking and cleanup. Root/global inverse roundtrips allow 0.0001 scene units of float error; grid integer/center oracle comparisons are exact. Warmed scalar geometry queries allocate zero owner-thread managed bytes. [Fixture provenance and reproduction](../../tools/tiles/REFERENCE.md).

`TileLayoutTests.Native` renders 48 configurations and independently checks 96 cell centers against collision owners on all four renderer/physics combinations. Each real 768×384 window measures 24 warmed full owner frames with forced redraw, all at zero managed bytes, then verifies redraw after shape edits. GPU and compatibility readback images agree. Captures live under ignored `bin/tile-layout-validation/`. All-thread/native allocator totals, foreign targets, dense-map throughput and human visual acceptance remain unverified for this slice.

`TileMapLayerTests` runs against explicit CPU and GPU worlds. It covers source transfer and copy ownership, borrowed lifetime, typed resource and PackedScene storage, fresh-process reconstruction, negative quadrants, dirty identity replacement, merged neighboring tiles, holes, surface velocity and material policy, one-way motion, foreign-thread rejection, owner projection through queries/casts/motion/contacts/monitoring, world/transform/enable/exit/reentry/disposal and runtime callback isolation/recovery. Unchanged layers measured 0 owner-thread bytes over 120 warmed physics frames on each backend.

`TileMapLayerTests.Native` runs a real 256×160 window with an active nonsleeping rigid body, atlas transform pixel assertions and forced redraw. Each combination measures 64 full frame intervals after preparation and warmup; MaxFPS is 60. This is an integration/allocation check, not a stress-throughput benchmark.

| Renderer | Physics | Measured frames / physics ticks | Elapsed seconds | FPS | Managed owner/all-thread bytes |
| --- | --- | --- | --- | --- | --- |
| GPU | CPU | 64 / 67 | 1.112 | 57.54 | 0 / 0 |
| GPU | GPU | 64 / 66 | 1.100 | 58.16 | 0 / 0 |
| Compatibility | CPU | 64 / 67 | 1.112 | 57.53 | 0 / 0 |
| Compatibility | GPU | 64 / 66 | 1.095 | 58.44 | 0 / 0 |

The initial native bracket put a collection immediately before measurement and reported 0/2616 owner/all-thread bytes. The final fixture completes blocking collection/finalization before warmup, as required by the existing allocation-check guidance; it keeps the same 64-frame work and zero-byte assertions without subtracting an allowance. This distinguishes fixture preparation from the measured interval, not an engine optimization. Logs and four inspected/readback-asserted images are retained under ignored `bin/physics-tiles-validation/2026-10-10/`.

## Remaining boundaries

Scene-collection runtime sources execute; their editor placeholder consumer and tile occlusion retain their own prerequisites. Per-tile materials/order now execute through retained items. Their exact members remain open in coverage. Large-map rebuild throughput, native allocator tracing, cross-platform execution, editor authoring, tile-specific network spawn topology and full GPU physics acceptance are not established by these checks. General backend extensions remain separate work.

## Navigation baking integration

Navigation source parsing consumes effective/runtime collision polygons with the same cell transpose/reflection and map-local positions as physics. Tile navigation outlines and stored polygon faces provide traversable source contours, independently of collider parsing mode. See [navigation baking](navigation-baking.md).

## Navigation layers

TileSet owns masks for each navigation layer. Layer insertion, movement, removal and source transfer remap every alternative. TileData borrows live NavigationPolygon resources and observes edits/disposal. The identity getter returns the original; seven flip/transpose variants are cached resources owned by the data. Reflections preserve outline/polygon winding and polygon indices. Cache resources become disposed on source mutation/removal and are never serialized. Deep copies duplicate shared originals once, shallow copies borrow them; file graphs preserve sharing.

TileMapLayer batches one region per nonempty cell/layer, using effective runtime tile data, cell transform flags, MapToLocal placement and its complete GlobalTransform. Every region reports the layer's InstanceID and is node-owned; external FreeRID rejects. The layer uses its World.NavigationMap or a validated borrowed override. The override is transient. NavigationEnabled defaults true. Disabling navigation/the layer, erasing a cell, clearing the source, leaving the tree and disposing release generated regions. Ordinary hiding retains navigation. Transform-only changes preserve region identities; geometry/policy edits replace affected regions. World changes rebind surviving regions. Updates stage data; server Synchronize or the physics lane publishes query topology.

NavigationVisibilityMode consumes actual stored polygon geometry in the canvas: Default follows SceneTree.DebugNavigationHint, ForceShow/ForceHide override it. Canvas visibility, routing, modulation and clipping still apply. The tree hint currently covers this tile consumer; server-wide region/link/avoidance diagnostics remain open. Drawing records allocating commands when authoring/diagnostics change, then reuses retained commands on unchanged frames.

The source parser collects runtime navigation outlines in root-local coordinates and uses convex faces when no outline was authored. This traversable source is independent of MeshInstances/StaticColliders filtering; collision polygons remain filtered obstructions. Agent radius, holes and map CellSize checks belong to the existing baking/map pipeline. No per-tile baking or uninitialized region stubs are introduced.

[TileRoutes](../../examples/TileRoutes/Program.cs) writes and reloads a portable tile scene, generates a ring-shaped map, queries a route and renders the diagnostics. File-owned TileSet subscriptions are detached before node resource leases are released, preventing teardown from invalidating an already disposed tile canvas.

### Navigation verification

TileNavigationTests covers membership bits one/32 and invalid bit numbers, insertion/movement/removal/source transfer, all seven transforms/winding/cache identity and mutation disposal, shallow/deep graphs with shared originals, portable file reconstruction, runtime-data isolation, owner RID lifetime, map overrides and rejected RIDs, hidden navigation, transforms/world replacement, masks/disable/erase/exit, tile-derived baking including face-only polygons, and 1024 unchanged UpdateInternals calls with zero managed allocation. Native TileNavigationRenderingTests separately loads a scene authored in another process and checks route pixels, the empty center, diagnostic visibility modes and cleanup on GPU/compatibility. Linux Wayland GPU and compatibility each passed 40 actual rendered frames with fresh-process file loading, diagnostic and route pixel assertions, and clean native/node teardown. Each measured 16 unchanged rendered frame intervals with zero managed owner-thread bytes. Screenshots were inspected at /tmp/e2d-tile-routes-gpu.png and /tmp/e2d-tile-routes-compatibility.png. These checks do not establish foreign platforms, NativeAOT, large-map throughput, native allocation tracing, editor workflows or human acceptance.

## Atlas frame animation and padding

TileSetAtlasSource stores an animation layout per base tile: nonnegative columns (zero means one horizontal row), nonnegative frame gaps in grid units, a positive frame count, finite positive durations/speed, and Default or RandomStartTimes mode. Defaults are one frame of duration one, zero columns/gaps, speed one and synchronized Default. Max is the enum sentinel and is rejected as a playable mode. TotalDuration is the sum before speed scaling; duration totals must remain finite float values.

All frame footprints participate in GetTileAtCoords, CreateTile, HasRoomForTile, movement/resizing, and current/proposed texture-removal checks. Overlap or invalid fit rejects before changing authored tiles. GetTileTextureRegion accepts every valid frame index. Texture changes retain authored tiles even when frames no longer fit; HasTilesOutsideTexture and ClearTilesOutsideTexture provide repair. Typed file graphs preserve this authored invalid/outside state, all duration arrays and layout values. Duplication copies duration arrays and alternatives; runtime texture caches are separate and transient.

TileMapLayer records every frame through retained DrawAnimationSlice commands once. The renderer chooses the half-open interval from its clock, accumulating unscaled durations before dividing by speed. Cells in Default mode share the clock phase. RandomStartTimes uses a stable layer/cell hash, so re-recording keeps a cell phase; identical RNG bits across other implementations are not promised. DrawEndAnimation resets the interval before later tiles and diagnostic geometry. Animation does not run scene processing, rebuild tile physics/navigation per frame, or mutate source texture pixels.

UseTexturePadding defaults true. GetRuntimeTexture lazily prepares a borrowed source-owned ImageTexture; GetRuntimeTileTextureRegion returns each frame's corresponding coordinates. Each frame has a one-pixel duplicate edge/corner border, and base format/pixel bytes survive without color conversion. The runtime image contains base pixels without generated mip levels. Disabled padding returns the original borrowed texture/region. Null/unreadable source images return null. Block-compressed/integer texture integration remains the owning rendering/image prerequisite; these formats are already rejected by the ordinary ImageTexture backend. Runtime caches are not serialized and are released on source disposal or successful replacement. Failed preparation retains the preceding cache lifetime and can retry after the source is repaired.

Padded grid stride is region size plus max(two pixels, atlas pixel separation) on each axis. Ordinary zero-to-two spacing retains the established coordinates. Larger spacing gives multi-cell images enough room, correcting overlap in the pinned tight-grid algorithm. Authored frames outside a shrunken image retain their data but emit no padded pixels until repaired. Overflow and the renderer's 16384-pixel axis bound reject before replacing a prepared cache. CanvasTexture normal/specular material generation remains a separate absent consumer.

[AnimatedTiles](../../examples/AnimatedTiles/Program.cs) authors a portable scene in one process, then reloads it in another. The upper row has synchronized unequal-duration animation, the lower row has stable random phases, and the blue final cells verify animation scope reset. Tests separately verify resource authoring, retained timed geometry and actual renderer output.

### Animation verification

TileAnimationTests covers frame reservations and overlaps, columns/gaps, original/runtime regions, proposed texture removal, shrinkage and portable reconstruction, duration resize/defaults, invalid values/sentinel/frame indices, precision-preserving extrusion, cache identity/invalidation/failed preparation/disposal, duration/speed boundaries including negative time, stable random phases and zero-allocation replay. Linux Wayland GPU and compatibility each passed 100 rendered frames from a scene authored in another process. The first 40 probes observed 11 red/29 green (GPU) and 10 red/30 green (compatibility), synchronized Default cells, random phase differences, padded linear edges and static blue cells after animation-scope reset. Each measured 40 continuously animating intervals with zero managed owner-thread allocation and clean native/resource teardown. Both screenshots were inspected: /tmp/e2d-animated-tiles-gpu.png and /tmp/e2d-animated-tiles-compatibility.png. Unchanged runtime-cache queries and retained timed replay each measured 1024 calls with zero managed allocation. Foreign platforms, editor workflows, NativeAOT, large-map throughput/native allocation and human acceptance are separate gates.

## Terrain authoring and painting

TileSet stores ordered terrain sets, their TerrainMode and terrain names/opaque colors. New terrains receive a generated name and palette color. Insertion, movement, removal and clearing remap all source alternatives' center and peering identities coherently. Structural edits commit all remapping before data notifications, reject recursive structural edits and attempt parent publication even when a listener throws. Changing mode or grid shape filters available peering directions while retaining inactive authored values.

TileData TerrainSet selects a set or -1; changing it while attached clears center/peering assignments. Terrain selects a center or -1. Get/SetTerrainPeeringBit operate on the active shape/mode directions; IsValidTerrainPeeringBit returns false for undefined/unassigned/unavailable directions. Probability is finite, nonnegative and defaults to one. Zero weights do not displace positive alternatives; all-zero candidates use the first sorted candidate. Exact random sequences are not a contract.

```csharp
// Assumes caller-owned texture and a TileMapLayer named layer.
var set = new TileSet();
set.AddTerrainSet();
set.SetTerrainSetMode(0, TerrainMode.MatchSides);
set.AddTerrain(0);
set.SetTerrainName(0, 0, "Grass");
var atlas = new TileSetAtlasSource { Texture = texture };
set.AddSource(atlas, 0);
atlas.CreateTile(Vector2i.Zero);
TileData data = atlas.GetTileData(Vector2i.Zero, 0);
data.TerrainSet = 0;
data.Terrain = 0;
data.Probability = 1;
layer.TileSet = set;
// Populate all required peering combinations before painting a complete map.
layer.SetCellsTerrainConnect([new(0, 0), new(1, 0)], 0, 0);
```

This deliberately partial authoring example compiles the same public workflow as tests. One atlas tile does not provide every required transition. Paint uses an authored catalog, preserves unconstrained values, scores requested center/connection constraints, propagates chosen constraints in input order and can update surrounding transitions. Incomplete catalogs produce the closest available patterns, including possible erased cells. Connect joins matching centers; Path constrains successive named neighbors, according to the selected mode's applicable bits. Runtime tile-data copies do not override the authored catalog. A painted atlas alternative supplies its usual image, collision and navigation data.

Painting is an explicit authoring operation and allocates for catalog/constraint preparation. Cell publication uses the existing batch. Missing set, invalid terrain identities, non-neighbor paths and required integer-coordinate overflow fail before cell mutation. Resource setters preserve state after rejected input; listener exceptions propagate after coherent definition commitment. Caller-owned resources still require coordination with consuming scene trees.

Terrain definitions, all authored peering values and weights persist through typed archives, shallow/deep copies and scene-local graphs. Standalone atlas indices are retained until binding; attaching/transferring clips assignments invalid for the new definitions. Typed tooling writes use ordinary validation; internal restoration stages raw fields before associations and preserves inactive peering values.

`TileTerrainTests` compares 144 pinned shape/layout/axis/mode configurations and 1,152 connect/path/clear stages, with ordered unique-pattern selections. It also checks metadata/remapping, quiet/invalid setters, callback/reentry failure, finite weights, weighted distribution, file graphs, CPU/GPU collision/navigation placement and fresh-process re-painting. The reference terrain insertion/move omission and forward insertion-index shifts are corrected; empty/removed assignments reset coherently. Offset-corner paths enumerate actual shared-corner cells instead of asking for unsupported corner neighbor cells. [Pinned fixture production](../../tools/tiles/terrain_reference.gd).

Prepared unchanged updates allocate zero owner-thread managed bytes. Real GPU/compatibility windows on CPU/GPU worlds pass 12 shape/mode panels, connect/path editing, authored-pattern pixel colors and placed collision/navigation. Each combination measures 24 warmed complete owner frame intervals with forced redraw at zero managed bytes. Captures are under ignored bin/terrain-validation/. Other platforms, native/all-thread allocator totals, dense-map authoring throughput and human visual acceptance remain unverified.

## Reusable patterns

TileMapPattern stores independent nonnegative coordinates, source/atlas/alternative identities and an extent. Empty assignments remain stored and erase their destination, while omitted coordinates and size-only margins preserve existing layer cells. Assignment always emits Resource.Changed. RemoveCell(updateSize: true) recomputes extent, false preserves it; SetSize can shrink unused margins but cannot exclude a stored cell. Array exports are independent and unordered. Negative coordinates, invalid identifiers and extent overflow reject before mutation.

```csharp
using var stamp = layer.GetPattern([new(-3, -3), new(-3, -2), new(-2, -2)]);
int index = set.AddPattern(stamp);
Vector2i placed = layer.MapPattern(new(5, 5), stamp.GetUsedCells()[0], stamp);
layer.SetPattern(new(5, 5), set.GetPattern(index));
layer.UpdateInternals();
set.RemovePattern(index);
```

This snippet requires a live TileMapLayer with the same assigned TileSet. The compiled tests exercise it with rendering, collision and navigation. Capture uses the selected componentwise minimum and may add one-cell nonnegative padding in stacked layouts. Placement follows current set geometry and corrects row/column parity, including negative anchors. It snapshots and validates every destination and identity before committing all changed cells, queues the batch and emits one layer Changed event. Empty/no-op placement is quiet. Listener failure leaves the complete stamp committed and queued. Resource authors coordinate edits with attached scene owners.

TileSet's list borrows distinct nonempty patterns; a negative insertion index appends, but GetPattern's default -1 remains invalid. Insertions/removals update the ordered list. Existing patterns may later become empty. Pattern disposal removes list references; set removal/disposal does not dispose the pattern. Shallow set copies share patterns, deep copies follow ordinary Resource modes; standalone pattern copies always own independent cells. Typed stored arrays validate atomically and preserve full coordinates, explicit emptiness and size margins through local/fresh-process resource and scene files.

TilePatternTests compares all 48 shape/layout/axis profiles in 144 captures and 576 placements against the pinned producer [pattern_reference.gd](../../tools/tiles/pattern_reference.gd). It checks duplicates, negative-anchor parity, explicit emptiness, missing entries, margins, overflow, malformed stored arrays, listener failure, copies/borrowed disposal, real CPU/GPU collision and navigation, root-local baking, disable/clear cleanup and fresh-process loaded scene placement. Scalar and mapping checks measure 1024 warmed owner operations; prepared updates measure 256 unchanged intervals. Native GPU/compatibility windows with CPU/GPU worlds exercise four shape panels, capture/store/re-place and actual pixels/owners/regions; 24 warmed full forced-redraw owner frames are measured separately. All four native combinations pass; scalar/mapping checks, unchanged updates and all four warmed frame brackets measure zero owner-thread managed bytes. Four captures have SHA-256 f8efb91c2ed690b8d5fb36ba0b41dd9b54a93045f410afedf55a172d064a3ae0; one capture was visually inspected. Images/logs remain under ignored bin/pattern-validation/. Other platforms, native/all-thread allocation totals, dense authoring throughput and human acceptance remain unverified.

## Explicit tile proxies

TileSet stores three independent replacement tables. MapTileProxy preserves an existing original tile; otherwise it checks complete alternative, source-and-atlas, then source-only keys and returns one copied typed identity. It does not recursively follow mappings or check the destination. Coordinate/source mappings preserve the alternative and its flags; complete mappings replace them. This supports repairing removed atlas identifiers while retaining already valid tiles.

```csharp
set.SetCoordsLevelTileProxy(101, new(4, 4), 0, new(0, 0));
var mapped = set.MapTileProxy(101, new(4, 4), TileSetAtlasSource.TransformFlipH);
layer.SetCell(new(-1, -2), mapped.SourceID, mapped.AtlasCoords, mapped.AlternativeTile);
layer.UpdateInternals();
```

The compiled test workflow assumes a live set with source zero and an atlas tile at zero, assigned to the layer. Mapping is explicit; raw layer lookup and existing placed cells retain their authored identity. Mapped identifiers can be captured/stored in a TileMapPattern and consumed by its normal placement path. The resulting selected atlas data supplies drawing, collision, navigation and baked outlines.

Assignments accept nonnegative source/atlas values and alternatives at least -1; target existence is not required. Queries without a stored mapping throw KeyNotFoundException, has queries return false. Set and clear always emit Resource.Changed; remove emits after successful deletion. Cleanup removes obsolete original keys (source existence at source level, tile existence at coordinate level, full alternative existence at alternative level), retaining missing targets. It prepares and commits all removals before one callback, so observer failure leaves coherent tables. Source renaming/removal does not rewrite keys. Copies own independent tables; portable typed arrays reject malformed shape, invalid bounds and duplicate keys before replacement, and restore before source associations without eager destination validation.

TileProxyTests compares 19 pinned stages and 190 mapped identities, plus all getters/membership/removals, priority, valid-original bypass, negative/sentinel/transform values, cycles, source rename, cleanup, independent copies and malformed stored-array/callback failures. CPU/GPU worlds execute mapped and pattern-captured cells, actual owned collisions/navigation/baking, zero warmed scalar/unchanged-update brackets and fresh-process saved-scene remapping. Native GPU/compatibility workflows cover all three levels, editing to erase a destination, actual pixels/owners/regions and 24 warmed complete forced-redraw owner frames. All four native combinations pass with zero owner-thread managed bytes in each frame bracket; 1024 warmed query brackets cover all mapping branches and 256 unchanged updates retain zero owner bytes. Four captures have SHA-256 7b20659e129e1054f053c78b9738658b7a42af4db148ad220bb0c2ffdeb9ca11; one was visually inspected. Other platforms, native/all-thread allocation totals, dense migration throughput, editor workflow and human acceptance remain unverified.

## Custom-data verification

TileCustomDataTests compares the 30-stage pinned fixture, checks forward/backward layer moves, insertion/removal/name repair, conversions/defaults, overflow preflight, callback failure/reentry and independent alternative notification. It exercises arrays/resource graph duplication, mismatched actual/configured types, cache replacement, standalone indexed source files, malformed IDs/counts and fresh-process executable scenes, including an explicitly registered application value codec. In-memory typed rules control actual generated collision/navigation on both physics backends without changing authored scalar values.

The native workflow verifies Tint pixels and Solid collision/navigation before and after edits for gpu/compatibility renderers with CPU/GPU physics. Each measured 24-frame warmed forced-redraw interval allocated 0 owner-thread managed bytes; exact typed reads/writes and name queries use 32 warmup/1024 measured operations, and unchanged layer updates use 32 warmup/256 measured calls. Four captured 320x160 PNGs have identical SHA-256 `27d2e2e6e9064b486850f9fee08c7b03bb0d04568f91d0b4b2d5e40a0095b450`; the representative capture was inspected. These checks establish current Linux native pixels/behavior, with no native allocator trace, foreign-platform or owner acceptance claim.

Development producer `tools/tiles/custom_data_reference.gd` generated `tests/Electron2D.Tests/Fixtures/Tiles/custom-data-reference.json` with pin `ed1daf0bf001b61586d9930840f2f1394092c079`: 4204 bytes, SHA-256 `a6112bbd6545503917603c739deb3c60a86b9a363f1c481b6f3562ce5f5d5447`. The documented name-index/string-constructor corrections are checked separately from reference equality. Release runtime/test builds, complete executable harness with the stock ENet oracle, focused/native custom-data checks, formatting, compiled coverage and wiki generation/check are independent gates.

## Drawing-policy verification

TileDrawingTests verifies defaults, repeated TileData notifications, invalid Z/size before mutation, shallow/deep material graphs, runtime copies and a fresh-process scene with every drawing policy. Native tests execute gpu/compatibility with CPU/GPU physics, check all 48 shape/layout/axis placements at negative coordinates, actual collision/navigation ownership, Z precedence, floored rendering quadrant anchors, scene/server Y-sort switches, conditional X reversal, tile/layer Y offsets, inherited self modulation, direct/inherited/live blend policy, UV clipping and runtime material/Z copies without authored mutation.

The native failure path retains all previous drawing identities after an unrepresentable negative-edge anchor. Generated item mutation/free/disposal rejects; clearing releases commands and weak links. Each 24-frame warmed forced-redraw owner interval allocated 0 managed bytes and retained group RIDs; the cold capturing comparator had first allocated 40 B/frame and was moved behind the stable early-return path. Four 400x360 captures have SHA-256 `b56944ccc3e39d65cf8e0a56ea71c07ba990ab6590aefea7f619e5f834d90579`; one was inspected. GLSL/HLSL SPIR-V fixtures verify typed live GPU tile shader pixels and explicit compatibility rejection. Native allocator tracing, foreign platforms, editor authoring and owner acceptance remain separate gates.

The internal headless command compiler retains atlas geometry/animation replay when no rendering session exists. It establishes prepared geometry/time only; material/order/canvas publication and real pixels use the actual retained renderer workflow above. This is separate from a native output acceptance claim under ADR 0090.

Navigation/collision diagnostic geometry uses a separate relative Z=max-minus-one item, matching its overlay role. It remains visible with tile Z/Y grouping and can tint authored child route lines; the route pixel check now verifies the composited color rather than requiring its previous uncovered opaque blue.

## Scene collections

TileSetScenesCollectionSource borrows nullable canvas-rooted PackedScene definitions at the single tile coordinate (0,0); scene IDs are alternatives. Definition IDs are nonnegative, sorted and independent of atlas pixel reservations. Automatic selection retains a nonzero cursor and wraps at 1073741823. Exact authored lookups precede transformed-ID fallback; actual layer cell resolution explicitly masks transform flags. This corrects ambiguous high authored IDs while retaining the existing cell identity encoding. Scene data is absent: GetCellTileData returns null, and atlas runtime-data callbacks exclude scene alternatives.

```csharp
using var collection = new TileSetScenesCollectionSource();
int sceneID = collection.CreateSceneTile(prefab); // Borrowed canvas-rooted PackedScene.
int sourceID = set.AddSource(collection);
layer.TileSet = set;
layer.SetCell(new(2, -1), sourceID, Vector2i.Zero, sceneID);
```

Caller context: live set/layer and a prefab whose stored root is CanvasItem. Detached authoring stores cells; tree entry queues instantiation. UpdateInternals publishes explicitly. Root Entity transforms include the cell basis and authored local transform, while Control roots add cell position and preserve their own transform fields. Runtime roots are ordinary children with null Owner and are not packed as authored children. Their descendants retain normal processing, world, body and navigation behavior; scene colliders report the actual CollisionObject rather than the TileMapLayer. Atlas-only switches do not disable these independent bodies/regions.

Definition edits validate before source mutation. Shared template events publish once per source, and disposal clears borrowed definitions without disposing caller resources on source removal/disposal. Shallow copies borrow templates, deep copies duplicate them; files preserve shared references and the next-ID cursor. Root metadata is available before validated resource assignments during archive restoration.

Layer hiding and atlas-only settings preserve instance identity. Disabling, clear/erasure, invalid/null definitions and tree exit retire children at the tree deletion safe point. Reparented roots leave cell ownership. Failed prepare/ready callbacks clean candidates and retain prior instances; ancestor-template cycles reject before recursive allocation. Runtime roots use unique names independent of their template names. Template/root changes may require allocating authoring work; unchanged frames do not recreate scenes.

The [SceneTiles](../../examples/SceneTiles/Program.cs) public consumer writes a portable scene and loads it in another process. Its upper row shows all eight image transforms; its lower row places ordinary static bodies and navigation regions. Existing PhysicsBody unit-global-scale/zero-skew constraints reject reflected/scaled body scenes. The missing physics shape-transform consumer is explicit; rendering-only flags are independently executable. Editor display-placeholder API remains unexposed/Blocked until the editor tile workspace, host/hint and actual placeholder drawing exist.

Scene validation, typed file reconstruction and actual native pixels have separate checks. Foreign platforms, native allocator tracing, editor/user acceptance and large scene-map throughput remain unverified.

### Scene collection verification

TileSceneTests passed exact/flagged and sorted IDs, full nonnegative authored ID range, next-cursor wrap, shared template change/disposal, rejected roots before mutation, deep/shallow copies, guarded scene/cell ownership, Entity/Control placement, null TileData, FixInvalidTiles, deferred creation/deletion, disable/exit/reentry, unchanged update allocations, fresh-process shared/high-ID graphs, failed ready callback cleanup and ancestor-template recursion rejection. Resource archives first exposed the unprepared-template-root failure; metadata restoration order fixed it without bypassing source validation.

The public SceneTiles authoring command wrote a portable scene in a separate process. gpu/compatibility × CPU/GPU native sessions each passed sixteen ordinary scene children, eight visual transform flag pixels, eight actual child-owned static bodies/navigation regions, atlas-only toggle identity preservation, hidden rendering with retained simulation, disable/reenable/clear lifetime, and reflected-body rejection with prior collider/navigation retention and candidate cleanup. Each measured 24 complete warmed forced-redraw owner intervals at 0 managed bytes. Four 288x128 captures have SHA-256 `2397240e054e3d87fa7592a0c8895448497c090c4c200beca7d67ae05e8617cf`; one was inspected.

The full executable check harness passed before main synchronization; focused fresh-graph and all four native sessions also passed after the immutable physics input commit was incorporated. These establish current Linux/native and managed behavior, not editor acceptance, foreign platforms, native allocator totals, large-map throughput or user visual acceptance.
