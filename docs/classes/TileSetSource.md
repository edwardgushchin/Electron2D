# TileSetSource

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileSetSource.cs](../../src/Scene/Resources/TileSetSource.cs).

## Overview

Provides coordinate and alternative identities for tiles owned by a tile set.

## Syntax

```csharp
public abstract class Electron2D.TileSetSource
```

### Remarks

A source belongs to at most one set. Adding it to another set transfers it.

**Inherits:** [Resource](Resource.md)

**Inherited By:** [TileSetAtlasSource](TileSetAtlasSource.md)

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Example

Assumes the live resources and identities prepared by the [complete authoring example](../components/tiles.md#authoring-example).

```csharp
TileSetSource source = set.GetSource(sourceID);
for (int i = 0; i < source.GetTilesCount(); i++)
{
    Vector2i coords = source.GetTileID(i);
    int alternatives = source.GetAlternativeTilesCount(coords);
}
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`protected TileSetSource()`](#member-724de90bcfa9) | constructor |  |
| [`public abstract System.Int32 GetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 index)`](#member-9d804de24a31) | method | Returns the alternative identity at a sorted index. |
| [`public abstract System.Int32 GetAlternativeTilesCount(Electron2D.Vector2i atlasCoords)`](#member-da45f754308d) | method | Returns the number of alternatives, including the base tile, or minus one for a missing tile. |
| [`public abstract Electron2D.Vector2i GetTileID(System.Int32 index)`](#member-aca7828e12f0) | method | Returns the coordinates at a sorted tile index. |
| [`public abstract System.Int32 GetTilesCount()`](#member-c7d8c187c87e) | method | Returns the number of base tiles. |
| [`public abstract System.Boolean HasAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)`](#member-264fdce1e321) | method | Tests whether a tile and alternative exist. |
| [`public abstract System.Boolean HasTile(Electron2D.Vector2i atlasCoords)`](#member-32675504b0ee) | method | Tests whether a base tile exists at the coordinates. |

## Member Details

<a id="member-724de90bcfa9"></a>
### `TileSetSource()`

Kind: `constructor`

```csharp
protected TileSetSource()
```

<a id="member-9d804de24a31"></a>
### `GetAlternativeTileID(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public abstract System.Int32 GetAlternativeTileID(Electron2D.Vector2i atlasCoords, System.Int32 index)
```

#### Summary

Returns the alternative identity at a sorted index.

#### Returns

Returns the alternative identity at a sorted index.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `index`: Zero-based sorted entry index.

<a id="member-da45f754308d"></a>
### `GetAlternativeTilesCount(Electron2D.Vector2i)`

Kind: `method`

```csharp
public abstract System.Int32 GetAlternativeTilesCount(Electron2D.Vector2i atlasCoords)
```

#### Summary

Returns the number of alternatives, including the base tile, or minus one for a missing tile.

#### Returns

Returns the number of alternatives, including the base tile, or minus one for a missing tile.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

<a id="member-aca7828e12f0"></a>
### `GetTileID(System.Int32)`

Kind: `method`

```csharp
public abstract Electron2D.Vector2i GetTileID(System.Int32 index)
```

#### Summary

Returns the coordinates at a sorted tile index.

#### Returns

Returns the coordinates at a sorted tile index.

#### Parameters

- `index`: Zero-based sorted entry index.

<a id="member-c7d8c187c87e"></a>
### `GetTilesCount()`

Kind: `method`

```csharp
public abstract System.Int32 GetTilesCount()
```

#### Summary

Returns the number of base tiles.

#### Returns

Returns the number of base tiles.

<a id="member-264fdce1e321"></a>
### `HasAlternativeTile(Electron2D.Vector2i, System.Int32)`

Kind: `method`

```csharp
public abstract System.Boolean HasAlternativeTile(Electron2D.Vector2i atlasCoords, System.Int32 alternativeTile)
```

#### Summary

Tests whether a tile and alternative exist.

#### Returns

Tests whether a tile and alternative exist.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.
- `alternativeTile`: Alternative identity, including optional cell transform flags where supported.

<a id="member-32675504b0ee"></a>
### `HasTile(Electron2D.Vector2i)`

Kind: `method`

```csharp
public abstract System.Boolean HasTile(Electron2D.Vector2i atlasCoords)
```

#### Summary

Tests whether a base tile exists at the coordinates.

#### Returns

Tests whether a base tile exists at the coordinates.

#### Parameters

- `atlasCoords`: Base atlas tile coordinates.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).
