# TileSet

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileSet.cs](../../src/Scene/Resources/TileSet.cs).

## Overview

Defines square tile dimensions, atlas sources and shared physics layer policies.

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
| [`public Electron2D.Vector2i TileSize { get; set; }`](#member-ac39ee317d80) | property | Gets or sets the positive square-grid cell dimensions in scene units. |

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

Gets or sets the positive square-grid cell dimensions in scene units.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).
