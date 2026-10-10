# Square atlas tiles and physical layers

Last updated: 2026-10-10

## Scope and ownership

[ADR 0101](../decisions/tiles.md#adr-0101) connects `TileSet`, `TileSetSource`, `TileSetAtlasSource`, source-owned `TileData` and scene `TileMapLayer`. The current slice supplies square cells, static atlas regions/alternatives and all tile physics-layer policies. It uses the world's selected CPU or independent GPU implementation; renderer choice is independent.

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

## Runtime flow

Cell/resource edits mark dirty cells or the full layer and queue one reusable tree action. UpdateInternals compiles dirty quadrants. A quadrant groups cells by physics layer, constant surface velocity, one-way policy/margin and one-way row. Polygons are unioned and convex-decomposed; a union containing holes is triangulated with the existing geometry dependency. Invalid topology contributes no fixtures. The compilation validates finite coordinates and clipping range before allocating server bodies.

Generated static/kinematic bodies use shared PhysicsServer operations, with weak object association to the TileMapLayer and the current canvas-layer identity. There are no generated scene children. GetCoordsForBodyRID returns quadrant coordinates, including floor division for negative cells. PhysicsQuadrantSize defaults to 16; size one identifies a cell. Rebuilding a quadrant retires its old body RIDs while untouched quadrants retain theirs. Returned identities are borrowed: callers must not release them independently.

Translation/rotation preserve generated identities. Layer scale/skew is baked into shape geometry and triggers rebuilding. Cell transform flags affect both drawing and collision. TileData FlipH/FlipV/Transpose affect the image only; the author edits an alternative's collision contours independently. TextureOrigin also affects drawing only. Constant surface velocities remain world-space values. Signed material coefficients preserve Rough and Absorbent behavior; collision priority is passed to the common recovery policy.

Hidden layers retain authored collision. Runtime tile-data copies are prepared only for visible enabled cells selected by the protected callbacks; hiding cleans those copies and restores authored collision. Disabling the layer or collision removes bodies. World reassignment moves bodies to the newly selected space. Exit and disposal release generated bodies/shapes and invalidate runtime copies. Collision overlays follow Default/ForceShow/ForceHide and ordinary canvas visibility.

Queries use the existing typed owner split: Collider is the CollisionObject convenience view and is null for a generated tile body; ColliderObject identifies the TileMapLayer. Motion/cast results, typed direct contacts and Area/RigidBody object and shape events retain the same layer identity. Object events deduplicate multiple tile bodies; per-shape events preserve each body RID. Excluding the layer's instance ID excludes all its generated bodies.

## Storage and failure behavior

Built-in compiled factories reconstruct TileSet, TileSetAtlasSource and TileMapLayer. Resource descriptors store ordered source identities, layer policies/material references, atlas regions/alternatives and owned tile-data fields; Vector2i arrays have a built-in portable codec. TileMapData is a copied version-zero little-endian array with 12-byte records and the documented signed-16-bit coordinate wrapping. Malformed array input fails before changing cells. Generated bodies and runtime copies are transient.

Attached node mutation requires the scene owner thread. Authored edits may queue work; publishing physics changes is prohibited while a solver/overlap callback holds the world. Runtime callback reentry is rejected. A failing runtime-data callback releases its new copy, leaves committed physical bodies intact and keeps a full retry pending. Resource/file operations and geometry changes allocate; unchanged physics frames and warmed redraw reuse existing storage.

## Verification

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

Non-square layouts, terrain/pattern/proxy painting, scene-collection sources, per-tile materials/Z sorting, custom data, tile occlusion are not implemented by this slice. Their exact members remain open in coverage. Large-map rebuild throughput, native allocator tracing, cross-platform execution, editor authoring, tile-specific network spawn topology and full GPU physics acceptance are not established by these checks. General backend extensions remain separate work.

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
