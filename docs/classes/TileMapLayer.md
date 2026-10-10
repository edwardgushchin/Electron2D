# TileMapLayer

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileMapLayer.cs](../../src/Scene/2D/TileMapLayer.cs).

## Overview

Draws a square atlas tile layer and generates tile-owned collision bodies in its selected world.

## Syntax

```csharp
public class Electron2D.TileMapLayer
```

### Remarks

Cell and resource edits are batched until the tree flushes deferred work or UpdateInternals is called. Generated bodies do not appear as child nodes. Queries and contacts report this layer as their collider object.

**Inherits:** [Entity](Entity.md)

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Example

Assumes the live resources and identities prepared by the [complete authoring example](../components/tiles.md#authoring-example).

```csharp
var layer = new TileMapLayer { TileSet = set };
layer.SetCell(Vector2i.Zero, sourceID, Vector2i.Zero);
// After entering a SceneTree:
layer.UpdateInternals();
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public TileMapLayer()`](#member-535034972235) | constructor | Creates an empty enabled tile layer. |
| [`public event System.Action Changed`](#member-0b71f16522de) | event | Occurs after cell, resource or layer settings change; multiple changes may precede one rebuild. |
| [`public System.Void Clear()`](#member-52d2606bd80b) | method | Erases every authored cell. |
| [`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`](#member-3adcc2a09d9a) | method |  |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-5cda6af091de) | method |  |
| [`public System.Void EraseCell(Electron2D.Vector2i coords)`](#member-31de12e0b671) | method | Erases a cell if present. |
| [`public System.Void FixInvalidTiles()`](#member-ca507cdfe090) | method | Removes cells whose source, atlas coordinates or alternative no longer exists. |
| [`public System.Int32 GetCellAlternativeTile(Electron2D.Vector2i coords)`](#member-205665bde5d7) | method | Returns a cell's alternative identity including transform flags, or minus one when empty. |
| [`public Electron2D.Vector2i GetCellAtlasCoords(Electron2D.Vector2i coords)`](#member-eb4fd1717e0a) | method | Returns a cell's atlas coordinates, or (-1,-1) for an empty cell. |
| [`public System.Int32 GetCellSourceID(Electron2D.Vector2i coords)`](#member-54c32f8b4684) | method | Returns a cell's source identity, or minus one for an empty cell. |
| [`public Electron2D.TileData GetCellTileData(Electron2D.Vector2i coords)`](#member-f14ecadb8e3d) | method | Returns borrowed authored tile data, or null if the cell cannot be resolved. |
| [`public Electron2D.Vector2i GetCoordsForBodyRID(Electron2D.RID body)`](#member-24b32838d457) | method | Returns physical quadrant coordinates for a generated body; quadrant size one identifies a cell. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-0dcd2f433657) | method |  |
| [`public Electron2D.Vector2i[] GetUsedCells()`](#member-0d16d775f5d6) | method | Returns a caller-owned array of occupied cell coordinates. |
| [`public Electron2D.Vector2i[] GetUsedCellsByID(System.Int32 sourceID = -1, System.Nullable<Electron2D.Vector2i> atlasCoords = null, System.Int32 alternativeTile = -1)`](#member-d6f234f96a3e) | method | Returns occupied coordinates matching each specified identity; minus one components are wildcards. |
| [`public Electron2D.Rect2i GetUsedRect()`](#member-1952c45cd20b) | method | Returns the cell-coordinate bounding rectangle, or an empty rectangle for an empty layer. |
| [`public System.Boolean HasBodyRID(Electron2D.RID body)`](#member-b9376f2a561e) | method | Tests whether a body RID currently belongs to this layer. |
| [`public System.Boolean IsCellFlippedH(Electron2D.Vector2i coords)`](#member-603870067167) | method | Returns whether the cell's transform flag reflects its horizontal axis. |
| [`public System.Boolean IsCellFlippedV(Electron2D.Vector2i coords)`](#member-5865fab12059) | method | Returns whether the cell's transform flag reflects its vertical axis. |
| [`public System.Boolean IsCellTransposed(Electron2D.Vector2i coords)`](#member-0efa037fc9e8) | method | Returns whether the cell's transform flag exchanges its axes. |
| [`public Electron2D.Vector2i LocalToMap(Electron2D.Vector2 localPosition)`](#member-841d32433cfc) | method | Returns the square cell containing a finite layer-local position, using floor for negative coordinates. |
| [`public Electron2D.Vector2 MapToLocal(Electron2D.Vector2i mapPosition)`](#member-babe4678b07c) | method | Returns the center of a square cell in layer-local scene units. |
| [`public System.Void NotifyRuntimeTileDataUpdate()`](#member-e57b359252fa) | method | Schedules recreation of per-cell runtime tile data. |
| [`protected override System.Void OnDraw()`](#member-ffcf0b3e7b7e) | method |  |
| [`protected override System.Void OnEnterTree()`](#member-bf3b8554eb12) | method |  |
| [`protected override System.Void OnExitTree()`](#member-272e33183bcc) | method |  |
| [`protected override System.Void OnNotification(System.Int32 what)`](#member-88b58145c236) | method |  |
| [`public System.Void SetCell(Electron2D.Vector2i coords, System.Int32 sourceID = -1, System.Nullable<Electron2D.Vector2i> atlasCoords = null, System.Int32 alternativeTile = 0)`](#member-baf376f9dcab) | method | Assigns a tile identity; any invalid identity component erases the cell. |
| [`protected virtual System.Void TileDataRuntimeUpdate(Electron2D.Vector2i coords, Electron2D.TileData tileData)`](#member-a79f5c6d2697) | method | Modifies a temporary cell-specific tile-data copy before rendering and physics publication. |
| [`protected virtual System.Void UpdateCells(Electron2D.Vector2i[] coords, System.Boolean forcedCleanup)`](#member-cca25090313e) | method | Observes batched modified coordinates; cleanup is true when disabled, hidden, detached or without a tile set. |
| [`public System.Void UpdateInternals()`](#member-398e644c126d) | method | Publishes pending tile edits now; cannot run during a solver or overlap callback. |
| [`protected virtual System.Boolean UseTileDataRuntimeUpdate(Electron2D.Vector2i coords)`](#member-b75cd7245241) | method | Chooses which visible enabled cells need an owned runtime copy of tile data. |
| [`protected override System.Void ValidateDisposal()`](#member-a9729e82967e) | method |  |
| [`public System.Boolean CollisionEnabled { get; set; }`](#member-d41197f7a75a) | property | Gets or sets whether this layer generates physics bodies. |
| [`public Electron2D.TileMapLayer.DebugVisibilityMode CollisionVisibilityMode { get; set; }`](#member-6b7481e5611c) | property | Gets or sets the policy for drawing tile collision geometry. |
| [`public System.Boolean Enabled { get; set; }`](#member-94ecbe15f2ef) | property | Gets or sets whether tiles render and generate collision bodies. |
| [`public System.Int32 PhysicsQuadrantSize { get; set; }`](#member-6060eeabe9b1) | property | Gets or sets the positive number of cells along each physical quadrant side, 16 by default. |
| [`public System.Byte[] TileMapData { get; set; }`](#member-4d64bdedd231) | property | Gets or sets a copied little-endian tile array: version zero followed by 12-byte cell records. |
| [`public Electron2D.TileSet TileSet { get; set; }`](#member-1dfe48ee2cd8) | property | Gets or sets the borrowed tile set used for rendering and collision. |
| [`public System.Boolean UseKinematicBodies { get; set; }`](#member-cdfcc3b994a4) | property | Gets or sets whether generated bodies use kinematic instead of static motion. |

## Member Details

<a id="member-535034972235"></a>
### `TileMapLayer()`

Kind: `constructor`

```csharp
public TileMapLayer()
```

#### Summary

Creates an empty enabled tile layer.

<a id="member-0b71f16522de"></a>
### `Changed`

Kind: `event`

```csharp
public event System.Action Changed
```

#### Summary

Occurs after cell, resource or layer settings change; multiple changes may precede one rebuild.

<a id="member-52d2606bd80b"></a>
### `Clear()`

Kind: `method`

```csharp
public System.Void Clear()
```

#### Summary

Erases every authored cell.

<a id="member-3adcc2a09d9a"></a>
### `CreateSceneInstanceFactory()`

Kind: `method`

```csharp
protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()
```

<a id="member-5cda6af091de"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-31de12e0b671"></a>
### `EraseCell(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Void EraseCell(Electron2D.Vector2i coords)
```

#### Summary

Erases a cell if present.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-ca507cdfe090"></a>
### `FixInvalidTiles()`

Kind: `method`

```csharp
public System.Void FixInvalidTiles()
```

#### Summary

Removes cells whose source, atlas coordinates or alternative no longer exists.

<a id="member-205665bde5d7"></a>
### `GetCellAlternativeTile(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Int32 GetCellAlternativeTile(Electron2D.Vector2i coords)
```

#### Summary

Returns a cell's alternative identity including transform flags, or minus one when empty.

#### Returns

Returns a cell's alternative identity including transform flags, or minus one when empty.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-eb4fd1717e0a"></a>
### `GetCellAtlasCoords(Electron2D.Vector2i)`

Kind: `method`

```csharp
public Electron2D.Vector2i GetCellAtlasCoords(Electron2D.Vector2i coords)
```

#### Summary

Returns a cell's atlas coordinates, or (-1,-1) for an empty cell.

#### Returns

Returns a cell's atlas coordinates, or (-1,-1) for an empty cell.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-54c32f8b4684"></a>
### `GetCellSourceID(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Int32 GetCellSourceID(Electron2D.Vector2i coords)
```

#### Summary

Returns a cell's source identity, or minus one for an empty cell.

#### Returns

Returns a cell's source identity, or minus one for an empty cell.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-f14ecadb8e3d"></a>
### `GetCellTileData(Electron2D.Vector2i)`

Kind: `method`

```csharp
public Electron2D.TileData GetCellTileData(Electron2D.Vector2i coords)
```

#### Summary

Returns borrowed authored tile data, or null if the cell cannot be resolved.

#### Returns

Returns borrowed authored tile data, or null if the cell cannot be resolved.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-24b32838d457"></a>
### `GetCoordsForBodyRID(Electron2D.RID)`

Kind: `method`

```csharp
public Electron2D.Vector2i GetCoordsForBodyRID(Electron2D.RID body)
```

#### Summary

Returns physical quadrant coordinates for a generated body; quadrant size one identifies a cell.

#### Returns

Returns physical quadrant coordinates for a generated body; quadrant size one identifies a cell.

#### Parameters

- `body`: Borrowed generated body identity; do not release it separately.

#### Exceptions

- `T:System.ArgumentException`: The RID is not a current body of this layer.

<a id="member-0dcd2f433657"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-0d16d775f5d6"></a>
### `GetUsedCells()`

Kind: `method`

```csharp
public Electron2D.Vector2i[] GetUsedCells()
```

#### Summary

Returns a caller-owned array of occupied cell coordinates.

#### Returns

Returns a caller-owned array of occupied cell coordinates.

<a id="member-d6f234f96a3e"></a>
### `GetUsedCellsByID(System.Int32, System.Nullable<Electron2D.Vector2i>, System.Int32)`

Kind: `method`

```csharp
public Electron2D.Vector2i[] GetUsedCellsByID(System.Int32 sourceID = -1, System.Nullable<Electron2D.Vector2i> atlasCoords = null, System.Int32 alternativeTile = -1)
```

#### Summary

Returns occupied coordinates matching each specified identity; minus one components are wildcards.

#### Returns

Returns occupied coordinates matching each specified identity; minus one components are wildcards.

#### Parameters

- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.
- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, with optional cell transform flags where supported.

<a id="member-1952c45cd20b"></a>
### `GetUsedRect()`

Kind: `method`

```csharp
public Electron2D.Rect2i GetUsedRect()
```

#### Summary

Returns the cell-coordinate bounding rectangle, or an empty rectangle for an empty layer.

#### Returns

Returns the cell-coordinate bounding rectangle, or an empty rectangle for an empty layer.

<a id="member-b9376f2a561e"></a>
### `HasBodyRID(Electron2D.RID)`

Kind: `method`

```csharp
public System.Boolean HasBodyRID(Electron2D.RID body)
```

#### Summary

Tests whether a body RID currently belongs to this layer.

#### Returns

Tests whether a body RID currently belongs to this layer.

#### Parameters

- `body`: Borrowed generated body identity; do not release it separately.

<a id="member-603870067167"></a>
### `IsCellFlippedH(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Boolean IsCellFlippedH(Electron2D.Vector2i coords)
```

#### Summary

Returns whether the cell's transform flag reflects its horizontal axis.

#### Returns

Returns whether the cell's transform flag reflects its horizontal axis.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-5865fab12059"></a>
### `IsCellFlippedV(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Boolean IsCellFlippedV(Electron2D.Vector2i coords)
```

#### Summary

Returns whether the cell's transform flag reflects its vertical axis.

#### Returns

Returns whether the cell's transform flag reflects its vertical axis.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-0efa037fc9e8"></a>
### `IsCellTransposed(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Boolean IsCellTransposed(Electron2D.Vector2i coords)
```

#### Summary

Returns whether the cell's transform flag exchanges its axes.

#### Returns

Returns whether the cell's transform flag exchanges its axes.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-841d32433cfc"></a>
### `LocalToMap(Electron2D.Vector2)`

Kind: `method`

```csharp
public Electron2D.Vector2i LocalToMap(Electron2D.Vector2 localPosition)
```

#### Summary

Returns the square cell containing a finite layer-local position, using floor for negative coordinates.

#### Returns

Returns the square cell containing a finite layer-local position, using floor for negative coordinates.

#### Parameters

- `localPosition`: Finite layer-local position in scene units.

<a id="member-babe4678b07c"></a>
### `MapToLocal(Electron2D.Vector2i)`

Kind: `method`

```csharp
public Electron2D.Vector2 MapToLocal(Electron2D.Vector2i mapPosition)
```

#### Summary

Returns the center of a square cell in layer-local scene units.

#### Returns

Returns the center of a square cell in layer-local scene units.

#### Parameters

- `mapPosition`: Square-grid cell coordinates.

<a id="member-e57b359252fa"></a>
### `NotifyRuntimeTileDataUpdate()`

Kind: `method`

```csharp
public System.Void NotifyRuntimeTileDataUpdate()
```

#### Summary

Schedules recreation of per-cell runtime tile data.

<a id="member-ffcf0b3e7b7e"></a>
### `OnDraw()`

Kind: `method`

```csharp
protected override System.Void OnDraw()
```

<a id="member-bf3b8554eb12"></a>
### `OnEnterTree()`

Kind: `method`

```csharp
protected override System.Void OnEnterTree()
```

<a id="member-272e33183bcc"></a>
### `OnExitTree()`

Kind: `method`

```csharp
protected override System.Void OnExitTree()
```

<a id="member-88b58145c236"></a>
### `OnNotification(System.Int32)`

Kind: `method`

```csharp
protected override System.Void OnNotification(System.Int32 what)
```

<a id="member-baf376f9dcab"></a>
### `SetCell(Electron2D.Vector2i, System.Int32, System.Nullable<Electron2D.Vector2i>, System.Int32)`

Kind: `method`

```csharp
public System.Void SetCell(Electron2D.Vector2i coords, System.Int32 sourceID = -1, System.Nullable<Electron2D.Vector2i> atlasCoords = null, System.Int32 alternativeTile = 0)
```

#### Summary

Assigns a tile identity; any invalid identity component erases the cell.

#### Parameters

- `coords`: Layer cell coordinates.
- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.
- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, with optional cell transform flags where supported.

<a id="member-a79f5c6d2697"></a>
### `TileDataRuntimeUpdate(Electron2D.Vector2i, Electron2D.TileData)`

Kind: `method`

```csharp
protected virtual System.Void TileDataRuntimeUpdate(Electron2D.Vector2i coords, Electron2D.TileData tileData)
```

#### Summary

Modifies a temporary cell-specific tile-data copy before rendering and physics publication.

#### Parameters

- `coords`: Layer cell coordinates.
- `tileData`: Borrowed runtime copy, valid until rebuild or cleanup.

<a id="member-cca25090313e"></a>
### `UpdateCells(Electron2D.Vector2i[], System.Boolean)`

Kind: `method`

```csharp
protected virtual System.Void UpdateCells(Electron2D.Vector2i[] coords, System.Boolean forcedCleanup)
```

#### Summary

Observes batched modified coordinates; cleanup is true when disabled, hidden, detached or without a tile set.

#### Parameters

- `coords`: Layer cell coordinates.
- `forcedCleanup`: Whether drawing and runtime tile data are being cleaned up.

<a id="member-398e644c126d"></a>
### `UpdateInternals()`

Kind: `method`

```csharp
public System.Void UpdateInternals()
```

#### Summary

Publishes pending tile edits now; cannot run during a solver or overlap callback.

#### Remarks

Geometry compilation is allocating authoring work. Unchanged layers do no work. Updates outside the tree retain authored cells but do not create physics bodies.

<a id="member-b75cd7245241"></a>
### `UseTileDataRuntimeUpdate(Electron2D.Vector2i)`

Kind: `method`

```csharp
protected virtual System.Boolean UseTileDataRuntimeUpdate(Electron2D.Vector2i coords)
```

#### Summary

Chooses which visible enabled cells need an owned runtime copy of tile data.

#### Returns

Chooses which visible enabled cells need an owned runtime copy of tile data.

#### Parameters

- `coords`: Layer cell coordinates.

<a id="member-a9729e82967e"></a>
### `ValidateDisposal()`

Kind: `method`

```csharp
protected override System.Void ValidateDisposal()
```

<a id="member-d41197f7a75a"></a>
### `CollisionEnabled`

Kind: `property`

```csharp
public System.Boolean CollisionEnabled { get; set; }
```

#### Summary

Gets or sets whether this layer generates physics bodies.

<a id="member-6b7481e5611c"></a>
### `CollisionVisibilityMode`

Kind: `property`

```csharp
public Electron2D.TileMapLayer.DebugVisibilityMode CollisionVisibilityMode { get; set; }
```

#### Summary

Gets or sets the policy for drawing tile collision geometry.

<a id="member-94ecbe15f2ef"></a>
### `Enabled`

Kind: `property`

```csharp
public System.Boolean Enabled { get; set; }
```

#### Summary

Gets or sets whether tiles render and generate collision bodies.

<a id="member-6060eeabe9b1"></a>
### `PhysicsQuadrantSize`

Kind: `property`

```csharp
public System.Int32 PhysicsQuadrantSize { get; set; }
```

#### Summary

Gets or sets the positive number of cells along each physical quadrant side, 16 by default.

<a id="member-4d64bdedd231"></a>
### `TileMapData`

Kind: `property`

```csharp
public System.Byte[] TileMapData { get; set; }
```

#### Summary

Gets or sets a copied little-endian tile array: version zero followed by 12-byte cell records.

#### Remarks

Coordinates wrap to signed 16-bit values on serialization. Each record contains X, Y, source identity, atlas X, atlas Y and alternative identity as six 16-bit fields. Malformed input is rejected before authored cells change; an empty array clears the layer.

<a id="member-1dfe48ee2cd8"></a>
### `TileSet`

Kind: `property`

```csharp
public Electron2D.TileSet TileSet { get; set; }
```

#### Summary

Gets or sets the borrowed tile set used for rendering and collision.

<a id="member-cdfcc3b994a4"></a>
### `UseKinematicBodies`

Kind: `property`

```csharp
public System.Boolean UseKinematicBodies { get; set; }
```

#### Summary

Gets or sets whether generated bodies use kinematic instead of static motion.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).
