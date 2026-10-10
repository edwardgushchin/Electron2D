# Tile resources and scene layers

<a id="adr-0101"></a>
## ADR 0101: Tile layers own generated physics bodies

Last updated: 2026-10-11

### Status

Accepted.

### Context

A collider object association alone cannot provide tile authoring, physical quadrants, or tile-owner identity. Tile layers must use the same selected world and query/event contract as other scene physics consumers, without exposing backend objects or adding hidden scene children.

### Decision

Under [ADR 0004](product.md#adr-0004) and [ADR 0008](scene.md#adr-0008), `TileSet` and `TileSetSource` are resources, `TileSetAtlasSource` derives from the latter, source-owned `TileData` derives from `ElectronObject`, and `TileMapLayer` derives from `Entity`. Atlas alternatives own their tile data; returned data is borrowed. A source belongs to one set; assigning it to another set transfers it. Materials and textures remain borrowed resources.

A layer batches authored cell/resource changes. `UpdateInternals` explicitly publishes them; the scene tree also flushes queued updates. Physics is generated through the shared server API in the layer's selected `World`, using static or kinematic bodies with owner association to the layer. Bodies are grouped by physical quadrant, physics layer, surface velocity and one-way policy; changed geometry replaces affected generated bodies. Shape geometry includes cell transforms and layer scale/skew. Layer transform updates preserve body identities when the shape basis does not change. No generated physics child nodes are added to the authored hierarchy.

`GetCoordsForBodyRID` returns quadrant coordinates; a quadrant size of one makes these cell coordinates. Owner projections preserve the layer and the individual body RID through queries, motion results, direct-state contacts and overlap events. Hiding drawing does not disable collision. Disabling the layer or collision removes generated bodies. Exit, disposal and world replacement release or move their server resources, including canvas association.

The executable implementation supports square, isometric, half-offset square and hexagonal cells, all six coordinate layouts and both offset axes. `TileShape`, `TileLayout`, `TileOffsetAxis` and `CellNeighbor` are namespace enums under ADR 0051; the first three are stored TileSet properties. Square cells ignore layout and offset axis. Map/local conversion and named neighbors share one geometry kernel used by drawing, generated physics and navigation, including source parsing. Cell images and authored polygons keep their own geometry; grid shape changes placement, not the contents of a tile. Occlusion remains an explicit coverage gap. Resource/scene storage uses typed property descriptors and compiled factories; generated bodies and runtime tile-data copies are not serialized.

Named neighbors depend on cell shape and offset axis; requesting an undefined enum or a direction absent from that shape throws `ArgumentOutOfRangeException`. Surrounding cells return the four or six edge-sharing cells in clockwise order. Negative rows/columns use positive parity. Neighbor calculations widen intermediate coordinates and throw `OverflowException` when the final coordinate cannot fit Vector2i. LocalToMap rejects nonfinite inputs and out-of-range results, preserves the square grid's normalized 0.00005 boundary bias and the asymmetric sloping-edge ties of the pinned geometry. MapToLocal returns float scene positions, with their ordinary loss of adjacent-cell precision at very large coordinates. Missing TileSet continues to throw `InvalidOperationException` for geometry queries. Geometry setters validate before changing state, publish Changed only on a real edit, and rebuild consuming layers through the existing batch mechanism.

### Reusable patterns

TileMapPattern is a Resource with independent nonnegative cell coordinates, tile identifiers and a cell extent. Stored empty cells are present entries and erase placement destinations; unstored margins do not erase them. SetCell grows the extent and emits Changed on every assignment; RemoveCell optionally recomputes it. SetSize may shrink unused margins while containing all entries. Pattern coordinates retain the full signed 32-bit nonnegative domain with a representable extent; identifiers follow the existing tile storage domain including -1 sentinels. Invalid inputs reject before state changes, and absent identity queries return the empty sentinels.

TileSet owns an ordered borrowed pattern list: nonempty insertion rejects duplicate instance identities; any negative insertion index appends. GetPattern preserves its reference default of -1, which is invalid rather than an alias for the last entry. Pattern mutation after insertion is permitted, including becoming empty. Removal/set disposal does not dispose borrowed patterns; disposing a pattern removes references from observing sets. Shallow copies share patterns, deep modes follow the Resource duplicate policy. Stored references restore before/after child data without requiring a nonempty intermediate object.

TileMapLayer captures every requested coordinate, including empty cells and duplicate coordinates, relative to their componentwise minimum. Stacked offset parity is corrected and a one-cell margin can ensure nonnegative pattern coordinates. MapPattern and placement use the same parity rule for negative anchors; other layouts use direct coordinate addition. Placement uses the consuming set's geometry and authored identifiers, never live links to captured cells. Capture/mapping require an assigned set. Placement snapshots and validates every coordinate/identity before changing cells, then commits the complete edit and queues the ordinary rendering/physics/navigation update before one Changed callback. A callback failure does not leave half a stamp or prevent its queued publication.

Under ADR 0034, portable pattern storage preserves explicit extent/margins and full coordinates instead of reproducing the pinned private 16-bit coordinate truncation and missing extent field. Typed storage includes explicit empty entries and validates the complete array before replacement. Authoring and export may allocate; scalar reads/mapping and unchanged prepared frames follow ADR 0014.

### Terrain authoring and painting

TileSet owns ordered terrain sets, their TerrainMode (MatchCornersAndSides, MatchCorners, MatchSides), and each terrain's name/opaque diagnostic color. Source-owned TileData stores a terrain-set identity, center terrain, sixteen peering values and finite nonnegative Probability. Indices use -1 for empty terrain. Namespace TerrainMode follows ADR 0051. Standalone atlas data can retain authored indices before association; attaching/transferring it validates against its new set. Portable restoration stores raw authored values before graph associations and validates at binding.

Set insertion/movement/removal remaps every alternative; terrain edits remap both center and peering identities. Correct the pinned insertion/movement omission for center terrain and reset center as well as peering bits when its set is removed. Correct forward move remapping to use the final insertion index consistently for the moved and intervening identities. These operations preserve authored meaning instead of silently changing which terrain a tile represents. Geometry/mode changes adjust available peering directions while retaining inactive authored values for later mode changes. Structural definition edits remap all children before callbacks, reject recursive structural edits, attempt notifications after a listener failure and leave committed identities coherent. Typed tooling writes retain normal setter validation; the internal stored-restore delegate stages raw fields before graph association. Invalid indices/enum values and nonfinite colors/weights reject before ordinary mutation; supplied color alpha is coerced to one.

TileMapLayer SetCellsTerrainConnect and SetCellsTerrainPath collect existing terrain constraints, preserve unconstrained bits, score candidates with the pinned center/connection priorities, propagate chosen constraints in authored order and select an atlas alternative by Probability. Surrounding cells can change to provide transitions; unchanged neighboring patterns retain their existing identity. Empty terrain participates according to ignoreEmptyTerrains. Runtime tile-data copies do not override the authored painting catalog. Painted cells publish through the normal batched draw/physics/navigation pipeline. Invalid/non-neighbor paths and coordinate overflow reject before altering cells. Corner-only path painting on offset shapes gathers the actual shared-corner cells; correct the pinned attempt to treat those corners as named neighbor cells, which returns the input coordinate and omits surrounding transitions. Random selection preserves weights and the deterministic all-zero fallback, without promising the reference random sequence. Painting/catalog preparation is authoring work that can allocate; unchanged frames and scalar getters retain the realtime allocation contract.

### Navigation layers

TileSet navigation layers own 32-bit membership masks; insertion, movement and removal remap every alternative's borrowed NavigationPolygon. TileData observes polygon changes/disposal. Transformed getters cache tile-data-owned polygon resources for the seven nonidentity flip/transpose combinations, preserve outline winding and polygon indices, and invalidate caches on source changes. Resource duplication and file graphs preserve sharing according to the selected duplicate mode; only original polygons are stored.

TileMapLayer generates one node-owned server region per nonempty cell/navigation-layer pair, with its instance identity, cell transform and navigation mask. Regions use the selected World map or a borrowed explicit map and are batched through UpdateInternals. Disabling navigation, disabling the layer, erasing cells, removing source data and leaving the tree release regions; hiding ordinary drawing retains navigation. World replacement and transform changes rebind surviving regions. Generated RIDs cannot be freed by callers. Map publication follows [ADR 0097](navigation.md#adr-0097); invalid preparation retains the preceding cell regions. Navigation diagnostics consume actual polygons through NavigationVisibilityMode and SceneTree.DebugNavigationHint, with ordinary canvas routing/visibility.

Source parsing consumes tile navigation outlines as traversable contours independently of collider parsing mode, with the same runtime tile data and cell transforms. It also consumes stored polygon faces when no outlines exist, making manually authored navigation usable for baking. Collision contours retain their existing obstruction/mask policy. Generated regions and transformed cache resources are transient.

### Animated atlas storage and drawing

Atlas tiles reserve every animation-frame rectangle, including multi-cell dimensions, columns (zero means a horizontal row) and grid-unit frame separation. Frame reservations participate in overlap/fit checks, lookup, movement/resizing and outside-texture/removal queries. Duration and speed values are finite positive numbers; duration totals stay float-representable. Copies and portable schemas store the complete authored configuration and preserve outside-texture authored data after texture shrinkage.

Tile drawing uses the existing retained canvas animation slices, with duration divided by speed and a stable hashed phase for RandomStartTimes based on layer identity and cell position. The hash distribution is accepted, not an exact reference RNG sequence. Commands record all frames once; ongoing rendering consumes the server clock without per-frame tile-data copies, callbacks or scene processing. Physics/navigation remain authored cell geometry, independent of visual frames.

UseTexturePadding defaults true. GetRuntimeTexture returns a borrowed source-owned cached texture with one pixel of edge/corner extrusion per frame; false returns the borrowed original. Padding is lazily rebuilt on the consuming owner thread after source/layout edits. Cache preparation succeeds before replacing prior data; runtime caches are transient. Current renderable texture formats provide readable uncompressed images; block-compressed texture decode retains the owning image/rendering prerequisite. A source with no readable image returns no runtime texture instead of a fabricated image. Original pixels and format are preserved, including multi-cell internal spacing.

Correct the pinned padding overlap when atlas Separation exceeds two pixels: padded grid stride uses region size plus max(two pixels, separation) on each axis. Ordinary zero-to-two spacing retains the reference positions; larger spacing receives enough room for the complete multi-cell image and its extruded edge. Bounds and integer overflow reject before altering authored layout or replacing a prepared cache. Padding does not claim unsupported CanvasTexture normal/specular consumers.

### Consequences

The CPU and GPU backends receive the same authored shapes and owner identities. Geometry compilation allocates when authoring changes; unchanged steps do not rebuild tiles. Borrowed tile data becomes invalid when its alternative or owning source is removed/disposed. Resource changes propagate to every consuming layer.

### Rejected alternatives

- Hidden `StaticBody` child nodes: changes the authored hierarchy and reports the wrong collider owner.
- A body per cell regardless of quadrant policy: changes RID lookup and creates unnecessary solver objects.
- CPU-only tile collision: violates explicit world backend selection in [ADR 0054](physics-backends.md#adr-0054).

### Verification boundary

Implementation and test evidence are recorded in the tile component and class pages. A subset of tile authoring does not close the entire tile family or the full GPU physics goal. Native rendering, warmed allocation checks and CPU/GPU behavioral tests are distinct acceptance gates under [ADR 0021](product.md#adr-0021).
