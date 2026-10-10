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

Animated atlas frames, padded atlas generation, non-square layouts, terrain/pattern/proxy painting, scene-collection sources, per-tile materials/Z sorting, custom data, tile occlusion and navigation are not implemented by this slice. Their exact members remain open in coverage. Large-map rebuild throughput, native allocator tracing, cross-platform execution, editor authoring, tile-specific network spawn topology and full GPU physics acceptance are not established by these checks. General backend extensions remain separate work.
