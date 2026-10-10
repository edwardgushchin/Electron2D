# TileSetAtlasSource

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileSetAtlasSource.cs](../../src/Scene/Resources/TileSetAtlasSource.cs).

## Overview

Defines static texture-atlas tiles and their owned alternatives.

## Syntax

```csharp
public sealed class Electron2D.TileSetAtlasSource
```

### Remarks

Texture and material resources are borrowed. Tile data is invalidated by removal or source disposal. Texture changes preserve authored tiles even when their regions no longer fit.

**Inherits:** [TileSetSource](TileSetSource.md)

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Example

Assumes the live resources and identities prepared by the [complete authoring example](../components/tiles.md#authoring-example).

```csharp
var atlas = new TileSetAtlasSource { Texture = texture };
set.AddSource(atlas);
atlas.CreateTile(Vector2i.Zero);
int alternate = atlas.CreateAlternativeTile(Vector2i.Zero);
atlas.GetTileData(Vector2i.Zero, alternate).FlipH = true;
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public const System.Int32 TransformFlipH = 4096`](#member-1495016d8329) | constant | Cell alternative flag for horizontal reflection. |
| [`public const System.Int32 TransformFlipV = 8192`](#member-91adce39d420) | constant | Cell alternative flag for vertical reflection. |
| [`public const System.Int32 TransformTranspose = 16384`](#member-ba9397eeb42e) | constant | Cell alternative flag for exchanging axes before reflection. |
| [`public TileSetAtlasSource()`](#member-06bb77e602cd) | constructor | Creates an empty source with 16 by 16 pixel texture regions. |
| [`public System.Void ClearTilesOutsideTexture()`](#member-990c2f1876a3) | method | Removes tiles extending outside the current texture. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`](#member-861c2b7e3953) | method |  |
| [`public System.Int32 CreateAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeIDOverride = -1)`](#member-2a197ded5e37) | method | Adds an alternative with empty physics data and returns its nonzero identity. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-df57eaffcc5e) | method |  |
| [`public System.Void CreateTile(Electron2D.Vector2i atlasCoords, System.Nullable<Electron2D.Vector2i> size = null)`](#member-7ee05ee7870c) | method | Creates a base tile occupying a positive rectangular region of currently unused atlas cells. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-b0528e24bb65) | method |  |
| [`public override System.Int32 GetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 index)`](#member-c965f71c07b0) | method |  |
| [`public override System.Int32 GetAlternativeTilesCount(Electron2D.Vector2i atlasCoords)`](#member-61a9ee74c141) | method |  |
| [`public Electron2D.Vector2i GetAtlasGridSize()`](#member-33719c0243c0) | method | Returns the number of complete atlas cells inside the current texture. |
| [`public System.Int32 GetNextAlternativeTileID(Electron2D.Vector2i atlasCoords)`](#member-7f66a8e6f264) | method | Returns the first free alternative identity without transform bits. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-34498d9adbd3) | method |  |
| [`public Electron2D.Vector2i GetTileAtCoords(Electron2D.Vector2i atlasCoords)`](#member-d95c0652e292) | method | Returns the tile occupying an atlas cell, or (-1,-1) when unused. |
| [`public Electron2D.TileData GetTileData(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)`](#member-953a07048bb3) | method | Returns borrowed data for an alternative, ignoring cell transform flags. |
| [`public override Electron2D.Vector2i GetTileID(System.Int32 index)`](#member-1c206810b2b2) | method |  |
| [`public Electron2D.Vector2i GetTileSizeInAtlas(Electron2D.Vector2i atlasCoords)`](#member-96058d4ac30e) | method | Returns the tile's dimensions in atlas cells. |
| [`public Electron2D.Rect2i GetTileTextureRegion(Electron2D.Vector2i atlasCoords, System.Int32 frame = 0)`](#member-122a17e0b055) | method | Returns a static tile's pixel region; only frame zero exists. |
| [`public override System.Int32 GetTilesCount()`](#member-bdb58bdf0b7c) | method |  |
| [`public override System.Boolean HasAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)`](#member-9ae554414339) | method |  |
| [`public override System.Boolean HasTile(Electron2D.Vector2i atlasCoords)`](#member-1c80ed7e2a24) | method |  |
| [`public System.Boolean HasTilesOutsideTexture()`](#member-bb27008a04cf) | method | Tests whether any authored tile extends outside the current texture. |
| [`public System.Void MoveTileInAtlas(Electron2D.Vector2i atlasCoords, System.Nullable<Electron2D.Vector2i> newAtlasCoords = null, System.Nullable<Electron2D.Vector2i> newSize = null)`](#member-73261d69e2d5) | method | Moves or resizes a tile while retaining its alternatives; null leaves that component unchanged. |
| [`public System.Void RemoveAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)`](#member-18d08278e71f) | method | Removes a nonbase alternative and invalidates its borrowed data. |
| [`public System.Void RemoveTile(Electron2D.Vector2i atlasCoords)`](#member-dccb4c4ffe07) | method | Removes a tile and disposes all its owned data alternatives. |
| [`public System.Void SetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile, System.Int32 newID)`](#member-5ff15fbf6fb6) | method | Renames a nonbase alternative to an unoccupied nonzero identity. |
| [`public Electron2D.Vector2i Margins { get; set; }`](#member-d0370c703e8b) | property | Gets or sets the nonnegative top and left texture margins in pixels. |
| [`public Electron2D.Vector2i Separation { get; set; }`](#member-1d468f9b207f) | property | Gets or sets nonnegative spacing between atlas cells in pixels. |
| [`public Electron2D.Texture Texture { get; set; }`](#member-8a30d9714ea5) | property | Gets or sets the borrowed atlas texture; null has an empty atlas grid. |
| [`public Electron2D.Vector2i TextureRegionSize { get; set; }`](#member-b9f2ec976001) | property | Gets or sets positive atlas cell dimensions in pixels. |

## Member Details

<a id="member-1495016d8329"></a>
### `TransformFlipH`

Kind: `constant`

```csharp
public const System.Int32 TransformFlipH = 4096
```

#### Summary

Cell alternative flag for horizontal reflection.

<a id="member-91adce39d420"></a>
### `TransformFlipV`

Kind: `constant`

```csharp
public const System.Int32 TransformFlipV = 8192
```

#### Summary

Cell alternative flag for vertical reflection.

<a id="member-ba9397eeb42e"></a>
### `TransformTranspose`

Kind: `constant`

```csharp
public const System.Int32 TransformTranspose = 16384
```

#### Summary

Cell alternative flag for exchanging axes before reflection.

<a id="member-06bb77e602cd"></a>
### `TileSetAtlasSource()`

Kind: `constructor`

```csharp
public TileSetAtlasSource()
```

#### Summary

Creates an empty source with 16 by 16 pixel texture regions.

<a id="member-990c2f1876a3"></a>
### `ClearTilesOutsideTexture()`

Kind: `method`

```csharp
public System.Void ClearTilesOutsideTexture()
```

#### Summary

Removes tiles extending outside the current texture.

<a id="member-861c2b7e3953"></a>
### `CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)`

Kind: `method`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)
```

<a id="member-2a197ded5e37"></a>
### `CreateAlternativeTile(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public System.Int32 CreateAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeIDOverride = -1)
```

#### Summary

Adds an alternative with empty physics data and returns its nonzero identity.

#### Returns

Adds an alternative with empty physics data and returns its nonzero identity.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `alternativeIDOverride`: Nonzero alternative identity without transform bits, or minus one for automatic selection.

<a id="member-df57eaffcc5e"></a>
### `CreateDuplicateInstance()`

Kind: `method`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-7ee05ee7870c"></a>
### `CreateTile(Electron2D.Vector2i, System.Nullable<Electron2D.Vector2i>)`

Kind: `method`

```csharp
public System.Void CreateTile(Electron2D.Vector2i atlasCoords, System.Nullable<Electron2D.Vector2i> size = null)
```

#### Summary

Creates a base tile occupying a positive rectangular region of currently unused atlas cells.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `size`: Positive atlas-cell size, or null for one cell.

<a id="member-b0528e24bb65"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-c965f71c07b0"></a>
### `GetAlternativeTileID(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public override System.Int32 GetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 index)
```

<a id="member-61a9ee74c141"></a>
### `GetAlternativeTilesCount(Electron2D.Vector2i)`

Kind: `method`

```csharp
public override System.Int32 GetAlternativeTilesCount(Electron2D.Vector2i atlasCoords)
```

<a id="member-33719c0243c0"></a>
### `GetAtlasGridSize()`

Kind: `method`

```csharp
public Electron2D.Vector2i GetAtlasGridSize()
```

#### Summary

Returns the number of complete atlas cells inside the current texture.

#### Returns

Returns the number of complete atlas cells inside the current texture.

<a id="member-7f66a8e6f264"></a>
### `GetNextAlternativeTileID(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Int32 GetNextAlternativeTileID(Electron2D.Vector2i atlasCoords)
```

#### Summary

Returns the first free alternative identity without transform bits.

#### Returns

Returns the first free alternative identity without transform bits.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

<a id="member-34498d9adbd3"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-d95c0652e292"></a>
### `GetTileAtCoords(Electron2D.Vector2i)`

Kind: `method`

```csharp
public Electron2D.Vector2i GetTileAtCoords(Electron2D.Vector2i atlasCoords)
```

#### Summary

Returns the tile occupying an atlas cell, or (-1,-1) when unused.

#### Returns

Returns the tile occupying an atlas cell, or (-1,-1) when unused.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

<a id="member-953a07048bb3"></a>
### `GetTileData(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public Electron2D.TileData GetTileData(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)
```

#### Summary

Returns borrowed data for an alternative, ignoring cell transform flags.

#### Returns

Returns borrowed data for an alternative, ignoring cell transform flags.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, with optional cell transform flags where supported.

<a id="member-1c206810b2b2"></a>
### `GetTileID(System.Int32)`

Kind: `method`

```csharp
public override Electron2D.Vector2i GetTileID(System.Int32 index)
```

<a id="member-96058d4ac30e"></a>
### `GetTileSizeInAtlas(Electron2D.Vector2i)`

Kind: `method`

```csharp
public Electron2D.Vector2i GetTileSizeInAtlas(Electron2D.Vector2i atlasCoords)
```

#### Summary

Returns the tile's dimensions in atlas cells.

#### Returns

Returns the tile's dimensions in atlas cells.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

<a id="member-122a17e0b055"></a>
### `GetTileTextureRegion(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public Electron2D.Rect2i GetTileTextureRegion(Electron2D.Vector2i atlasCoords, System.Int32 frame = 0)
```

#### Summary

Returns a static tile's pixel region; only frame zero exists.

#### Returns

Returns a static tile's pixel region; only frame zero exists.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `frame`: Static frame index; only zero is currently available.

<a id="member-bdb58bdf0b7c"></a>
### `GetTilesCount()`

Kind: `method`

```csharp
public override System.Int32 GetTilesCount()
```

<a id="member-9ae554414339"></a>
### `HasAlternativeTile(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public override System.Boolean HasAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)
```

<a id="member-1c80ed7e2a24"></a>
### `HasTile(Electron2D.Vector2i)`

Kind: `method`

```csharp
public override System.Boolean HasTile(Electron2D.Vector2i atlasCoords)
```

<a id="member-bb27008a04cf"></a>
### `HasTilesOutsideTexture()`

Kind: `method`

```csharp
public System.Boolean HasTilesOutsideTexture()
```

#### Summary

Tests whether any authored tile extends outside the current texture.

#### Returns

Tests whether any authored tile extends outside the current texture.

<a id="member-73261d69e2d5"></a>
### `MoveTileInAtlas(Electron2D.Vector2i, System.Nullable<Electron2D.Vector2i>, System.Nullable<Electron2D.Vector2i>)`

Kind: `method`

```csharp
public System.Void MoveTileInAtlas(Electron2D.Vector2i atlasCoords, System.Nullable<Electron2D.Vector2i> newAtlasCoords = null, System.Nullable<Electron2D.Vector2i> newSize = null)
```

#### Summary

Moves or resizes a tile while retaining its alternatives; null leaves that component unchanged.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `newAtlasCoords`: Replacement coordinates, or null or (-1,-1) to retain them.
- `newSize`: Replacement positive size, or null or (-1,-1) to retain it.

<a id="member-18d08278e71f"></a>
### `RemoveAlternativeTile(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public System.Void RemoveAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)
```

#### Summary

Removes a nonbase alternative and invalidates its borrowed data.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, with optional cell transform flags where supported.

<a id="member-dccb4c4ffe07"></a>
### `RemoveTile(Electron2D.Vector2i)`

Kind: `method`

```csharp
public System.Void RemoveTile(Electron2D.Vector2i atlasCoords)
```

#### Summary

Removes a tile and disposes all its owned data alternatives.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

<a id="member-5ff15fbf6fb6"></a>
### `SetAlternativeTileID(Electron2D.Vector2i, System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Void SetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile, System.Int32 newID)
```

#### Summary

Renames a nonbase alternative to an unoccupied nonzero identity.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, with optional cell transform flags where supported.
- `newID`: Unoccupied nonzero alternative identity without transform bits.

<a id="member-d0370c703e8b"></a>
### `Margins`

Kind: `property`

```csharp
public Electron2D.Vector2i Margins { get; set; }
```

#### Summary

Gets or sets the nonnegative top and left texture margins in pixels.

<a id="member-1d468f9b207f"></a>
### `Separation`

Kind: `property`

```csharp
public Electron2D.Vector2i Separation { get; set; }
```

#### Summary

Gets or sets nonnegative spacing between atlas cells in pixels.

<a id="member-8a30d9714ea5"></a>
### `Texture`

Kind: `property`

```csharp
public Electron2D.Texture Texture { get; set; }
```

#### Summary

Gets or sets the borrowed atlas texture; null has an empty atlas grid.

<a id="member-b9f2ec976001"></a>
### `TextureRegionSize`

Kind: `property`

```csharp
public Electron2D.Vector2i TextureRegionSize { get; set; }
```

#### Summary

Gets or sets positive atlas cell dimensions in pixels.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).
