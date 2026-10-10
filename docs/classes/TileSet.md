# TileSet

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TileSet.cs](../../src/Scene/Resources/TileSet.cs).

## Overview

Defines tile grid geometry, terrain definitions, reusable patterns, atlas sources and shared physics and navigation layer policies.

## Syntax

```csharp
public sealed class Electron2D.TileSet
```

### Remarks

Sources are transferred between sets, while materials are borrowed. Copies duplicate sources even in shallow mode so copying cannot remove sources from the original set.

**Inherits:** [Resource](Resource.md)

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Example

Assumes the live resources and identities prepared by the [complete authoring example](../components/tiles.md#authoring-example).

```csharp
var set = new TileSet();
set.AddPhysicsLayer();
set.SetPhysicsLayerCollisionLayer(0, 2);
set.SetPhysicsLayerPhysicsMaterial(0, material);
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public int AddPattern(TileMapPattern pattern, int index = -1)`](#pattern-addpattern) | method | Inserts a borrowed live, nonempty, distinct instance at index; every negative index appends. Returns its index. Null/disposed input, empty/duplicate patterns and index beyond count throw typed exceptions before list mutation. Other sets may reference the same instance. Changed and property-list change publish after commitment. |
| [`public TileMapPattern GetPattern(int index = -1)`](#pattern-getpattern) | method | Returns a borrowed pattern at an explicit zero-based index. The retained default -1 is invalid and throws ArgumentOutOfRangeException; it does not select the last entry. |
| [`public int GetPatternsCount()`](#pattern-getpatternscount) | method | Returns list count. A pattern may become empty after insertion and still belongs to the list until removal or disposal. |
| [`public void RemovePattern(int index)`](#pattern-removepattern) | method | Removes the indexed reference, unsubscribes disposal observation and publishes the list schema change. It does not dispose the resource. Missing index throws ArgumentOutOfRangeException. |
| [`public System.Void AddTerrain(System.Int32 terrainSet, System.Int32 toPosition = -1)`](#terrain-7d0f0f8629f1) | method | Inserts a terrain with a generated name and opaque color, remapping center and peering identities. |
| [`public System.Void AddTerrainSet(System.Int32 toPosition = -1)`](#terrain-e61ddae54404) | method | Inserts an empty terrain set; a negative insertion position appends. |
| [`public System.Void ClearTerrains(System.Int32 terrainSet)`](#terrain-400244398cdd) | method | Removes every terrain in a set and clears its atlas assignments. |
| [`public Electron2D.Color GetTerrainColor(System.Int32 terrainSet, System.Int32 terrainIndex)`](#terrain-2ef41b760aab) | method | Returns the terrain's opaque diagnostic color. |
| [`public System.String GetTerrainName(System.Int32 terrainSet, System.Int32 terrainIndex)`](#terrain-9804c45c7585) | method | Returns a terrain's name. |
| [`public Electron2D.TerrainMode GetTerrainSetMode(System.Int32 terrainSet)`](#terrain-da9c2dab0881) | method | Returns the matching mode of a terrain set. |
| [`public System.Int32 GetTerrainSetsCount()`](#terrain-0acf6ba4eafe) | method | Returns the number of terrain sets. |
| [`public System.Int32 GetTerrainsCount(System.Int32 terrainSet)`](#terrain-bc861e728e99) | method | Returns the number of terrains in a set. |
| [`public System.Void MoveTerrain(System.Int32 terrainSet, System.Int32 terrainIndex, System.Int32 toPosition)`](#terrain-61ab2e2533c8) | method | Moves a terrain before an insertion position, preserving the meaning of atlas assignments. |
| [`public System.Void MoveTerrainSet(System.Int32 terrainSet, System.Int32 toPosition)`](#terrain-e07a4f33b753) | method | Moves a terrain set before an insertion position, remapping atlas alternatives. |
| [`public System.Void RemoveTerrain(System.Int32 terrainSet, System.Int32 terrainIndex)`](#terrain-e95eb4bf024c) | method | Removes a terrain and clears its assignments, remapping later identities. |
| [`public System.Void RemoveTerrainSet(System.Int32 terrainSet)`](#terrain-78f57ec5980b) | method | Removes a terrain set and clears its alternatives' center and peering assignments. |
| [`public System.Void SetTerrainColor(System.Int32 terrainSet, System.Int32 terrainIndex, Electron2D.Color color)`](#terrain-fdc307712b65) | method | Sets a finite diagnostic color, coercing its alpha to one. |
| [`public System.Void SetTerrainName(System.Int32 terrainSet, System.Int32 terrainIndex, System.String name)`](#terrain-7bc631cb3ae3) | method | Sets a terrain's name. |
| [`public System.Void SetTerrainSetMode(System.Int32 terrainSet, Electron2D.TerrainMode mode)`](#terrain-0f81b2539230) | method | Sets the matching mode and updates available peering directions. |
| [`TileShape TileShape { get; set; }`](#grid-tileshape) | property | The cell outline, default Square. Undefined values throw before state changes; a real edit changes placement in all consuming layers. |
| [`TileLayout TileLayout { get; set; }`](#grid-tilelayout) | property | The coordinate layout, default Stacked. All six layouts are stored; square cells ignore this setting. |
| [`TileOffsetAxis TileOffsetAxis { get; set; }`](#grid-tileoffsetaxis) | property | The offset direction, default Horizontal. Non-square cells offset rows horizontally or columns vertically; square cells ignore this setting. |
| [`public TileSet()`](#member-8dfd74136481) | constructor | Creates an empty square tile set with 16 by 16 scene-unit cells. |
| [`public System.Void AddPhysicsLayer(System.Int32 toPosition = -1)`](#member-1017cbed061e) | method | Inserts an empty physics layer; a negative position appends. |
| [`public System.Int32 AddSource(Electron2D.TileSetSource source, System.Int32 atlasSourceIDOverride = -1)`](#member-e7dacd2144b0) | method | Adds a source under an explicit or automatically selected nonnegative identity. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)`](#member-2bc0c73d2184) | method |  |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-184c820a9f35) | method |  |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-8c8b48cefdc3) | method |  |
| [`public System.Int32 GetNextSourceID()`](#member-4ad9ef092913) | method | Returns the first free nonnegative source identity. |
| [`public System.UInt32 GetPhysicsLayerCollisionLayer(System.Int32 layerIndex)`](#member-2b6434e64c8f) | method | Returns the layer's 32-bit collision membership mask. |
| [`public System.UInt32 GetPhysicsLayerCollisionMask(System.Int32 layerIndex)`](#member-ce326ec88bf1) | method | Returns the layer's 32-bit collision query mask. |
| [`public System.Single GetPhysicsLayerCollisionPriority(System.Int32 layerIndex)`](#member-c89bf64878ad) | method | Returns the layer's collision recovery priority. |
| [`public Electron2D.PhysicsMaterial GetPhysicsLayerPhysicsMaterial(System.Int32 layerIndex)`](#member-994810e7142b) | method | Returns the borrowed physics material, or null for default friction and bounce. |
| [`public System.Int32 GetPhysicsLayersCount()`](#member-9ac5fdecf5ea) | method | Returns the number of physics layers. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f91eb94d0c40) | method |  |
| [`public Electron2D.TileSetSource GetSource(System.Int32 sourceID)`](#member-c0e1b3ef74e1) | method | Returns a borrowed source by identity. |
| [`public System.Int32 GetSourceCount()`](#member-0018ba2cfc24) | method | Returns the number of sources. |
| [`public System.Int32 GetSourceID(System.Int32 index)`](#member-01488d8fcab6) | method | Returns the source identity at a sorted index. |
| [`public System.Boolean HasSource(System.Int32 sourceID)`](#member-3223e524fa82) | method | Tests whether a live source exists under the identity. |
| [`public System.Void MovePhysicsLayer(System.Int32 layerIndex, System.Int32 toPosition)`](#member-e97226fd4096) | method | Moves a physics layer before the given insertion position, preserving tile polygons. |
| [`public System.Void RemovePhysicsLayer(System.Int32 layerIndex)`](#member-c291991cbba1) | method | Removes a physics layer and its polygons from every source alternative. |
| [`public System.Void RemoveSource(System.Int32 sourceID)`](#member-f9b22c9cdd83) | method | Removes a source without disposing its resource. |
| [`public System.Void SetPhysicsLayerCollisionLayer(System.Int32 layerIndex, System.UInt32 layer)`](#member-5b34c96a4f18) | method | Sets the layer's collision membership mask. |
| [`public System.Void SetPhysicsLayerCollisionMask(System.Int32 layerIndex, System.UInt32 mask)`](#member-c95254e0e9b8) | method | Sets the layer's collision query mask. |
| [`public System.Void SetPhysicsLayerCollisionPriority(System.Int32 layerIndex, System.Single priority)`](#member-8b9a169c3587) | method | Sets a finite positive collision recovery priority. |
| [`public System.Void SetPhysicsLayerPhysicsMaterial(System.Int32 layerIndex, Electron2D.PhysicsMaterial physicsMaterial)`](#member-ea39bcb6c236) | method | Assigns a borrowed material and observes its changes and disposal. |
| [`public System.Void SetSourceID(System.Int32 sourceID, System.Int32 newSourceID)`](#member-47730f062bb3) | method | Renames a source identity to an unoccupied nonnegative value. |
| [`public Electron2D.Vector2i TileSize { get; set; }`](#member-ac39ee317d80) | property | Gets or sets the positive cell bounding dimensions in scene units. |

## Member Details

<a id="member-8dfd74136481"></a>
### `TileSet()`

Kind: `constructor`

```csharp
public TileSet()
```

#### Summary

Creates an empty square tile set with 16 by 16 scene-unit cells.

<a id="member-1017cbed061e"></a>
### `AddPhysicsLayer(System.Int32)`

Kind: `method`

```csharp
public System.Void AddPhysicsLayer(System.Int32 toPosition = -1)
```

#### Summary

Inserts an empty physics layer; a negative position appends.

#### Parameters

- `toPosition`: Insertion position; a negative position appends when adding.

<a id="member-e7dacd2144b0"></a>
### `AddSource(Electron2D.TileSetSource, System.Int32)`

Kind: `method`

```csharp
public System.Int32 AddSource(Electron2D.TileSetSource source, System.Int32 atlasSourceIDOverride = -1)
```

#### Summary

Adds a source under an explicit or automatically selected nonnegative identity.

#### Returns

The assigned source identity.

#### Parameters

- `source`: Live source to transfer into this set.
- `atlasSourceIDOverride`: Nonnegative source identity, or minus one to choose a free identity.

<a id="member-2bc0c73d2184"></a>
### `CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)`

Kind: `method`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode mode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicate, System.Func<Electron2D.Resource, Electron2D.Resource> force)
```

<a id="member-184c820a9f35"></a>
### `CreateDuplicateInstance()`

Kind: `method`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-8c8b48cefdc3"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-4ad9ef092913"></a>
### `GetNextSourceID()`

Kind: `method`

```csharp
public System.Int32 GetNextSourceID()
```

#### Summary

Returns the first free nonnegative source identity.

#### Returns

Returns the first free nonnegative source identity.

<a id="member-2b6434e64c8f"></a>
### `GetPhysicsLayerCollisionLayer(System.Int32)`

Kind: `method`

```csharp
public System.UInt32 GetPhysicsLayerCollisionLayer(System.Int32 layerIndex)
```

#### Summary

Returns the layer's 32-bit collision membership mask.

#### Returns

Returns the layer's 32-bit collision membership mask.

#### Parameters

- `layerIndex`: Zero-based physics layer index.

<a id="member-ce326ec88bf1"></a>
### `GetPhysicsLayerCollisionMask(System.Int32)`

Kind: `method`

```csharp
public System.UInt32 GetPhysicsLayerCollisionMask(System.Int32 layerIndex)
```

#### Summary

Returns the layer's 32-bit collision query mask.

#### Returns

Returns the layer's 32-bit collision query mask.

#### Parameters

- `layerIndex`: Zero-based physics layer index.

<a id="member-c89bf64878ad"></a>
### `GetPhysicsLayerCollisionPriority(System.Int32)`

Kind: `method`

```csharp
public System.Single GetPhysicsLayerCollisionPriority(System.Int32 layerIndex)
```

#### Summary

Returns the layer's collision recovery priority.

#### Returns

Returns the layer's collision recovery priority.

#### Parameters

- `layerIndex`: Zero-based physics layer index.

<a id="member-994810e7142b"></a>
### `GetPhysicsLayerPhysicsMaterial(System.Int32)`

Kind: `method`

```csharp
public Electron2D.PhysicsMaterial GetPhysicsLayerPhysicsMaterial(System.Int32 layerIndex)
```

#### Summary

Returns the borrowed physics material, or null for default friction and bounce.

#### Returns

Returns the borrowed physics material, or null for default friction and bounce.

#### Parameters

- `layerIndex`: Zero-based physics layer index.

<a id="member-9ac5fdecf5ea"></a>
### `GetPhysicsLayersCount()`

Kind: `method`

```csharp
public System.Int32 GetPhysicsLayersCount()
```

#### Summary

Returns the number of physics layers.

#### Returns

Returns the number of physics layers.

<a id="member-f91eb94d0c40"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-c0e1b3ef74e1"></a>
### `GetSource(System.Int32)`

Kind: `method`

```csharp
public Electron2D.TileSetSource GetSource(System.Int32 sourceID)
```

#### Summary

Returns a borrowed source by identity.

#### Returns

Returns a borrowed source by identity.

#### Parameters

- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.

<a id="member-0018ba2cfc24"></a>
### `GetSourceCount()`

Kind: `method`

```csharp
public System.Int32 GetSourceCount()
```

#### Summary

Returns the number of sources.

#### Returns

Returns the number of sources.

<a id="member-01488d8fcab6"></a>
### `GetSourceID(System.Int32)`

Kind: `method`

```csharp
public System.Int32 GetSourceID(System.Int32 index)
```

#### Summary

Returns the source identity at a sorted index.

#### Returns

Returns the source identity at a sorted index.

#### Parameters

- `index`: Zero-based sorted entry index.

<a id="member-3223e524fa82"></a>
### `HasSource(System.Int32)`

Kind: `method`

```csharp
public System.Boolean HasSource(System.Int32 sourceID)
```

#### Summary

Tests whether a live source exists under the identity.

#### Returns

Tests whether a live source exists under the identity.

#### Parameters

- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.

<a id="member-e97226fd4096"></a>
### `MovePhysicsLayer(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Void MovePhysicsLayer(System.Int32 layerIndex, System.Int32 toPosition)
```

#### Summary

Moves a physics layer before the given insertion position, preserving tile polygons.

#### Parameters

- `layerIndex`: Zero-based physics layer index.
- `toPosition`: Insertion position; a negative position appends when adding.

<a id="member-c291991cbba1"></a>
### `RemovePhysicsLayer(System.Int32)`

Kind: `method`

```csharp
public System.Void RemovePhysicsLayer(System.Int32 layerIndex)
```

#### Summary

Removes a physics layer and its polygons from every source alternative.

#### Parameters

- `layerIndex`: Zero-based physics layer index.

<a id="member-f9b22c9cdd83"></a>
### `RemoveSource(System.Int32)`

Kind: `method`

```csharp
public System.Void RemoveSource(System.Int32 sourceID)
```

#### Summary

Removes a source without disposing its resource.

#### Parameters

- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.

<a id="member-5b34c96a4f18"></a>
### `SetPhysicsLayerCollisionLayer(System.Int32, System.UInt32)`

Kind: `method`

```csharp
public System.Void SetPhysicsLayerCollisionLayer(System.Int32 layerIndex, System.UInt32 layer)
```

#### Summary

Sets the layer's collision membership mask.

#### Parameters

- `layerIndex`: Zero-based physics layer index.
- `layer`: Collision membership bits.

<a id="member-c95254e0e9b8"></a>
### `SetPhysicsLayerCollisionMask(System.Int32, System.UInt32)`

Kind: `method`

```csharp
public System.Void SetPhysicsLayerCollisionMask(System.Int32 layerIndex, System.UInt32 mask)
```

#### Summary

Sets the layer's collision query mask.

#### Parameters

- `layerIndex`: Zero-based physics layer index.
- `mask`: Collision filtering bits.

<a id="member-8b9a169c3587"></a>
### `SetPhysicsLayerCollisionPriority(System.Int32, System.Single)`

Kind: `method`

```csharp
public System.Void SetPhysicsLayerCollisionPriority(System.Int32 layerIndex, System.Single priority)
```

#### Summary

Sets a finite positive collision recovery priority.

#### Parameters

- `layerIndex`: Zero-based physics layer index.
- `priority`: Finite positive recovery priority.

<a id="member-ea39bcb6c236"></a>
### `SetPhysicsLayerPhysicsMaterial(System.Int32, Electron2D.PhysicsMaterial)`

Kind: `method`

```csharp
public System.Void SetPhysicsLayerPhysicsMaterial(System.Int32 layerIndex, Electron2D.PhysicsMaterial physicsMaterial)
```

#### Summary

Assigns a borrowed material and observes its changes and disposal.

#### Parameters

- `layerIndex`: Zero-based physics layer index.
- `physicsMaterial`: Borrowed live material, or null for defaults.

<a id="member-47730f062bb3"></a>
### `SetSourceID(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Void SetSourceID(System.Int32 sourceID, System.Int32 newSourceID)
```

#### Summary

Renames a source identity to an unoccupied nonnegative value.

#### Parameters

- `sourceID`: Source identity; minus one erases or acts as a query wildcard where documented.
- `newSourceID`: Unoccupied nonnegative replacement source identity.

<a id="member-ac39ee317d80"></a>
### `TileSize`

Kind: `property`

```csharp
public Electron2D.Vector2i TileSize { get; set; }
```

#### Summary

Gets or sets the positive cell bounding dimensions in scene units.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).

## Tile navigation

See [tile navigation](../components/tiles.md#navigation-layers) for ownership, batching, file graphs and verification.

### GetNavigationLayersCount

```csharp
int GetNavigationLayersCount()
```

Returns the shared navigation layer count.

### AddNavigationLayer

```csharp
void AddNavigationLayer(int toPosition = -1)
```

Inserts a mask-one layer; negative positions append. Every source alternative is remapped.

### MoveNavigationLayer

```csharp
void MoveNavigationLayer(int layerIndex, int toPosition)
```

Moves before an insertion position, preserving masks and each alternative polygon.

### RemoveNavigationLayer

```csharp
void RemoveNavigationLayer(int layerIndex)
```

Removes the policy and its polygon references; borrowed polygons remain owned by their callers.

### GetNavigationLayerLayers

```csharp
uint GetNavigationLayerLayers(int layerIndex)
```

Returns the complete unsigned membership mask.

### SetNavigationLayerLayers

```csharp
void SetNavigationLayerLayers(int layerIndex, uint layers)
```

Sets the mask; zero excludes the regions from all queries.

### GetNavigationLayerLayerValue

```csharp
bool GetNavigationLayerLayerValue(int layerIndex, int layerNumber)
```

Tests bit one through 32; other bit numbers throw.

### SetNavigationLayerLayerValue

```csharp
void SetNavigationLayerLayerValue(int layerIndex, int layerNumber, bool value)
```

Changes one membership bit while preserving the rest.

## Grid geometry

See [grid placement and neighbors](../components/tiles.md#grid-placement-and-neighbors) for exact boundary and missing-set behavior.

<a id="grid-tileshape"></a>
### TileShape

```csharp
TileShape TileShape { get; set; }
```

The cell outline, default Square. Undefined values throw before state changes; a real edit changes placement in all consuming layers.

<a id="grid-tilelayout"></a>
### TileLayout

```csharp
TileLayout TileLayout { get; set; }
```

The coordinate layout, default Stacked. All six layouts are stored; square cells ignore this setting.

<a id="grid-tileoffsetaxis"></a>
### TileOffsetAxis

```csharp
TileOffsetAxis TileOffsetAxis { get; set; }
```

The offset direction, default Horizontal. Non-square cells offset rows horizontally or columns vertically; square cells ignore this setting.

## Terrain API

See [terrain authoring and painting](../components/tiles.md#terrain-authoring-and-painting) for the shared lifecycle, defaults, callback, persistence and allocation contract. Owners reject access after disposal. Indices are validated against the live definitions.

<a id="terrain-7d0f0f8629f1"></a>
### AddTerrain

```csharp
public System.Void AddTerrain(System.Int32 terrainSet, System.Int32 toPosition = -1)
```

Inserts a terrain with a generated name and opaque color, remapping center and peering identities.

- `terrainSet`: The terrain set index.
- `toPosition`: Insertion position, or a negative value to append.

<a id="terrain-e61ddae54404"></a>
### AddTerrainSet

```csharp
public System.Void AddTerrainSet(System.Int32 toPosition = -1)
```

Inserts an empty terrain set; a negative insertion position appends.

- `toPosition`: Insertion position, or a negative value to append.

<a id="terrain-400244398cdd"></a>
### ClearTerrains

```csharp
public System.Void ClearTerrains(System.Int32 terrainSet)
```

Removes every terrain in a set and clears its atlas assignments.

- `terrainSet`: The terrain set index.

<a id="terrain-2ef41b760aab"></a>
### GetTerrainColor

```csharp
public Electron2D.Color GetTerrainColor(System.Int32 terrainSet, System.Int32 terrainIndex)
```

Returns the terrain's opaque diagnostic color.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain index.

<a id="terrain-9804c45c7585"></a>
### GetTerrainName

```csharp
public System.String GetTerrainName(System.Int32 terrainSet, System.Int32 terrainIndex)
```

Returns a terrain's name.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain index.

<a id="terrain-da9c2dab0881"></a>
### GetTerrainSetMode

```csharp
public Electron2D.TerrainMode GetTerrainSetMode(System.Int32 terrainSet)
```

Returns the matching mode of a terrain set.

- `terrainSet`: The terrain set index.

<a id="terrain-0acf6ba4eafe"></a>
### GetTerrainSetsCount

```csharp
public System.Int32 GetTerrainSetsCount()
```

Returns the number of terrain sets.

<a id="terrain-bc861e728e99"></a>
### GetTerrainsCount

```csharp
public System.Int32 GetTerrainsCount(System.Int32 terrainSet)
```

Returns the number of terrains in a set.

- `terrainSet`: The terrain set index.

<a id="terrain-61ab2e2533c8"></a>
### MoveTerrain

```csharp
public System.Void MoveTerrain(System.Int32 terrainSet, System.Int32 terrainIndex, System.Int32 toPosition)
```

Moves a terrain before an insertion position, preserving the meaning of atlas assignments.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain to move.
- `toPosition`: Insertion position between zero and the terrain count, inclusive.

<a id="terrain-e07a4f33b753"></a>
### MoveTerrainSet

```csharp
public System.Void MoveTerrainSet(System.Int32 terrainSet, System.Int32 toPosition)
```

Moves a terrain set before an insertion position, remapping atlas alternatives.

- `terrainSet`: The terrain set to move.
- `toPosition`: Insertion position between zero and the set count, inclusive.

<a id="terrain-e95eb4bf024c"></a>
### RemoveTerrain

```csharp
public System.Void RemoveTerrain(System.Int32 terrainSet, System.Int32 terrainIndex)
```

Removes a terrain and clears its assignments, remapping later identities.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain to remove.

<a id="terrain-78f57ec5980b"></a>
### RemoveTerrainSet

```csharp
public System.Void RemoveTerrainSet(System.Int32 terrainSet)
```

Removes a terrain set and clears its alternatives' center and peering assignments.

- `terrainSet`: The terrain set to remove.

<a id="terrain-fdc307712b65"></a>
### SetTerrainColor

```csharp
public System.Void SetTerrainColor(System.Int32 terrainSet, System.Int32 terrainIndex, Electron2D.Color color)
```

Sets a finite diagnostic color, coercing its alpha to one.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain index.
- `color`: Finite color components; alpha is ignored.

<a id="terrain-7bc631cb3ae3"></a>
### SetTerrainName

```csharp
public System.Void SetTerrainName(System.Int32 terrainSet, System.Int32 terrainIndex, System.String name)
```

Sets a terrain's name.

- `terrainSet`: The terrain set index.
- `terrainIndex`: The terrain index.
- `name`: A nonnull name, including an empty name.

<a id="terrain-0f81b2539230"></a>
### SetTerrainSetMode

```csharp
public System.Void SetTerrainSetMode(System.Int32 terrainSet, Electron2D.TerrainMode mode)
```

Sets the matching mode and updates available peering directions.

- `terrainSet`: The terrain set index.
- `mode`: A defined matching mode.

## Pattern Method Descriptions

<a id="pattern-addpattern"></a>
### AddPattern

`public int AddPattern(TileMapPattern pattern, int index = -1)`

Inserts a borrowed live, nonempty, distinct instance at index; every negative index appends. Returns its index. Null/disposed input, empty/duplicate patterns and index beyond count throw typed exceptions before list mutation. Other sets may reference the same instance. Changed and property-list change publish after commitment.

<a id="pattern-getpattern"></a>
### GetPattern

`public TileMapPattern GetPattern(int index = -1)`

Returns a borrowed pattern at an explicit zero-based index. The retained default -1 is invalid and throws ArgumentOutOfRangeException; it does not select the last entry.

<a id="pattern-getpatternscount"></a>
### GetPatternsCount

`public int GetPatternsCount()`

Returns list count. A pattern may become empty after insertion and still belongs to the list until removal or disposal.

<a id="pattern-removepattern"></a>
### RemovePattern

`public void RemovePattern(int index)`

Removes the indexed reference, unsubscribes disposal observation and publishes the list schema change. It does not dispose the resource. Missing index throws ArgumentOutOfRangeException.

See [reusable patterns](../components/tiles.md#reusable-patterns) for the executable example, ownership, file graphs and verification boundaries.
