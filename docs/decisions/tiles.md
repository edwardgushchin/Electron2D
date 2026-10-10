# Tile resources and scene layers

<a id="adr-0101"></a>
## ADR 0101: Tile layers own generated physics bodies

Last updated: 2026-10-10

### Status

Accepted.

### Context

A collider object association alone cannot provide tile authoring, physical quadrants, or tile-owner identity. Tile layers must use the same selected world and query/event contract as other scene physics consumers, without exposing backend objects or adding hidden scene children.

### Decision

Under [ADR 0004](product.md#adr-0004) and [ADR 0008](scene.md#adr-0008), `TileSet` and `TileSetSource` are resources, `TileSetAtlasSource` derives from the latter, source-owned `TileData` derives from `ElectronObject`, and `TileMapLayer` derives from `Entity`. Atlas alternatives own their tile data; returned data is borrowed. A source belongs to one set; assigning it to another set transfers it. Materials and textures remain borrowed resources.

A layer batches authored cell/resource changes. `UpdateInternals` explicitly publishes them; the scene tree also flushes queued updates. Physics is generated through the shared server API in the layer's selected `World`, using static or kinematic bodies with owner association to the layer. Bodies are grouped by physical quadrant, physics layer, surface velocity and one-way policy; changed geometry replaces affected generated bodies. Shape geometry includes cell transforms and layer scale/skew. Layer transform updates preserve body identities when the shape basis does not change. No generated physics child nodes are added to the authored hierarchy.

`GetCoordsForBodyRID` returns quadrant coordinates; a quadrant size of one makes these cell coordinates. Owner projections preserve the layer and the individual body RID through queries, motion results, direct-state contacts and overlap events. Hiding drawing does not disable collision. Disabling the layer or collision removes generated bodies. Exit, disposal and world replacement release or move their server resources, including canvas association.

The first executable implementation targets square cells, static atlas tiles and physical layers/polygons, including one-way margins and constant surface velocities. Other layouts, terrain painting, animated atlases, occlusion and tile navigation remain explicit coverage gaps. They are not represented by inert public switches. Resource/scene storage uses typed property descriptors and compiled factories; generated bodies and runtime tile-data copies are not serialized.

### Consequences

The CPU and GPU backends receive the same authored shapes and owner identities. Geometry compilation allocates when authoring changes; unchanged steps do not rebuild tiles. Borrowed tile data becomes invalid when its alternative or owning source is removed/disposed. Resource changes propagate to every consuming layer.

### Rejected alternatives

- Hidden `StaticBody` child nodes: changes the authored hierarchy and reports the wrong collider owner.
- A body per cell regardless of quadrant policy: changes RID lookup and creates unnecessary solver objects.
- CPU-only tile collision: violates explicit world backend selection in [ADR 0054](physics-backends.md#adr-0054).

### Verification boundary

Implementation and test evidence are recorded in the tile component and class pages. A subset of tile authoring does not close the entire tile family or the full GPU physics goal. Native rendering, warmed allocation checks and CPU/GPU behavioral tests are distinct acceptance gates under [ADR 0021](product.md#adr-0021).
