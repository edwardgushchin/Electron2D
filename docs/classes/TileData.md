# TileData

Last updated: 2026-10-11

**Namespace:** `Electron2D`. **Source:** [TileData.cs](../../src/Scene/Resources/TileData.cs).

## Overview

Stores typed custom values, terrain assignments and weights, the visual transform, collision polygons and borrowed navigation polygons of one atlas alternative.

## Syntax

```csharp
public sealed class Electron2D.TileData
```

### Remarks

Instances are owned by an atlas source. Polygon arrays are copied on reads and writes. Removing an alternative invalidates its borrowed data. Runtime update callbacks receive a separate owned copy.

**Inherits:** [ElectronObject](ElectronObject.md)

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Example

Assumes the live resources and identities prepared by the [complete authoring example](../components/tiles.md#authoring-example).

```csharp
TileData data = atlas.GetTileData(Vector2i.Zero, 0);
data.AddCollisionPolygon(0);
data.SetCollisionPolygonPoints(0, 0,
    [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
data.SetConstantLinearVelocity(0, new Vector2(40, 0));
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public System.Int32 GetTerrainPeeringBit(Electron2D.CellNeighbor peeringBit)`](#terrain-df7dd9f01294) | method | Returns an applicable terrain peering value; -1 is an empty terrain. |
| [`public System.Boolean IsValidTerrainPeeringBit(Electron2D.CellNeighbor peeringBit)`](#terrain-cafe7f73d449) | method | Tests whether a defined peering direction is applicable to this data's attached set, terrain set and mode. |
| [`public System.Void SetTerrainPeeringBit(Electron2D.CellNeighbor peeringBit, System.Int32 terrainIndex)`](#terrain-d7a2f9cbeaa0) | method | Sets a terrain peering value; -1 clears it. |
| [`public System.Single Probability { get; set; }`](#terrain-90b9c67335c8) | property | Gets or sets the finite nonnegative weight used to select atlas alternatives with the same terrain pattern. |
| [`public System.Int32 Terrain { get; set; }`](#terrain-e7a66615d398) | property | Gets or sets the center terrain index, or -1 for an empty center. |
| [`public System.Int32 TerrainSet { get; set; }`](#terrain-1a9722452594) | property | Gets or sets the terrain set index, or -1 to disable terrain assignments. |
| [`public event System.Action Changed`](#member-7705766a08ba) | event | Occurs after authored data changes. |
| [`public System.Void AddCollisionPolygon(System.Int32 layerID)`](#member-0b7923d46bc9) | method | Adds an empty collision polygon to a physics layer. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-90ae228c5083) | method |  |
| [`public System.Single GetCollisionPolygonOneWayMargin(System.Int32 layerID, System.Int32 polygonIndex)`](#member-2875412048ad) | method | Returns the one-way recovery margin in scene units. |
| [`public Electron2D.Vector2[] GetCollisionPolygonPoints(System.Int32 layerID, System.Int32 polygonIndex)`](#member-34fb78d1f641) | method | Returns an independent polygon contour in tile-local scene units. |
| [`public System.Int32 GetCollisionPolygonsCount(System.Int32 layerID)`](#member-e676c4597e1e) | method | Returns a physics layer's polygon count. |
| [`public System.Single GetConstantAngularVelocity(System.Int32 layerID)`](#member-e20e84ae9b3f) | method | Returns the constant angular surface velocity in radians per second. |
| [`public Electron2D.Vector2 GetConstantLinearVelocity(System.Int32 layerID)`](#member-ad54aa8366e8) | method | Returns the constant world-space surface velocity for this physics layer. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-4e7e0a855217) | method |  |
| [`public System.Boolean IsCollisionPolygonOneWay(System.Int32 layerID, System.Int32 polygonIndex)`](#member-f7ada14bc4ca) | method | Tests whether a polygon accepts contacts only from above the layer. |
| [`public System.Void RemoveCollisionPolygon(System.Int32 layerID, System.Int32 polygonIndex)`](#member-c9730e595c55) | method | Removes a polygon and shifts later indices down. |
| [`public System.Void SetCollisionPolygonOneWay(System.Int32 layerID, System.Int32 polygonIndex, System.Boolean oneWay)`](#member-f0b0f20c5bf3) | method | Sets a polygon's one-way contact policy. |
| [`public System.Void SetCollisionPolygonOneWayMargin(System.Int32 layerID, System.Int32 polygonIndex, System.Single oneWayMargin)`](#member-2b32548ad500) | method | Sets a finite nonnegative one-way recovery margin. |
| [`public System.Void SetCollisionPolygonPoints(System.Int32 layerID, System.Int32 polygonIndex, Electron2D.Vector2[] polygon)`](#member-502ec201bf48) | method | Copies a finite closed contour; empty or invalid topology contributes no fixtures. |
| [`public System.Void SetCollisionPolygonsCount(System.Int32 layerID, System.Int32 polygonsCount)`](#member-78848d4eb456) | method | Resizes a physics layer's polygon list, preserving surviving entries. |
| [`public System.Void SetConstantAngularVelocity(System.Int32 layerID, System.Single velocity)`](#member-4bb23f6cf8dc) | method | Sets a finite angular surface velocity in radians per second. |
| [`public System.Void SetConstantLinearVelocity(System.Int32 layerID, Electron2D.Vector2 velocity)`](#member-df32a8259fe3) | method | Sets a finite world-space surface velocity without moving static tile geometry. |
| [`protected override System.Void ValidateDisposal()`](#member-4076493ef3bb) | method | Prevents disposal of data borrowed from an atlas or a runtime update. |
| [`public System.Boolean FlipH { get; set; }`](#member-d9d7ad50f562) | property | Gets or sets horizontal reflection; base alternatives cannot be reflected in their data. |
| [`public System.Boolean FlipV { get; set; }`](#member-de72a71533fa) | property | Gets or sets vertical reflection; base alternatives cannot be reflected in their data. |
| [`public Electron2D.Color Modulate { get; set; }`](#member-b436a2f4f594) | property | Gets or sets the finite texture tint, white by default. |
| [`public Electron2D.Vector2i TextureOrigin { get; set; }`](#member-d78fa4986b14) | property | Gets or sets the texture origin offset in pixels; collision geometry is unaffected. |
| [`public System.Boolean Transpose { get; set; }`](#member-3401e6cc9fa0) | property | Gets or sets whether tile axes are exchanged before reflection. |

## Member Details

<a id="member-7705766a08ba"></a>
### `Changed`

Kind: `event`

```csharp
public event System.Action Changed
```

#### Summary

Occurs after authored data changes.

<a id="member-0b7923d46bc9"></a>
### `AddCollisionPolygon(System.Int32)`

Kind: `method`

```csharp
public System.Void AddCollisionPolygon(System.Int32 layerID)
```

#### Summary

Adds an empty collision polygon to a physics layer.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.

<a id="member-90ae228c5083"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-2875412048ad"></a>
### `GetCollisionPolygonOneWayMargin(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Single GetCollisionPolygonOneWayMargin(System.Int32 layerID, System.Int32 polygonIndex)
```

#### Summary

Returns the one-way recovery margin in scene units.

#### Returns

Returns the one-way recovery margin in scene units.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.

<a id="member-34fb78d1f641"></a>
### `GetCollisionPolygonPoints(System.Int32, System.Int32)`

Kind: `method`

```csharp
public Electron2D.Vector2[] GetCollisionPolygonPoints(System.Int32 layerID, System.Int32 polygonIndex)
```

#### Summary

Returns an independent polygon contour in tile-local scene units.

#### Returns

Returns an independent polygon contour in tile-local scene units.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.

<a id="member-e676c4597e1e"></a>
### `GetCollisionPolygonsCount(System.Int32)`

Kind: `method`

```csharp
public System.Int32 GetCollisionPolygonsCount(System.Int32 layerID)
```

#### Summary

Returns a physics layer's polygon count.

#### Returns

Returns a physics layer's polygon count.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.

<a id="member-e20e84ae9b3f"></a>
### `GetConstantAngularVelocity(System.Int32)`

Kind: `method`

```csharp
public System.Single GetConstantAngularVelocity(System.Int32 layerID)
```

#### Summary

Returns the constant angular surface velocity in radians per second.

#### Returns

Returns the constant angular surface velocity in radians per second.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.

<a id="member-ad54aa8366e8"></a>
### `GetConstantLinearVelocity(System.Int32)`

Kind: `method`

```csharp
public Electron2D.Vector2 GetConstantLinearVelocity(System.Int32 layerID)
```

#### Summary

Returns the constant world-space surface velocity for this physics layer.

#### Returns

Returns the constant world-space surface velocity for this physics layer.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.

<a id="member-4e7e0a855217"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-f7ada14bc4ca"></a>
### `IsCollisionPolygonOneWay(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Boolean IsCollisionPolygonOneWay(System.Int32 layerID, System.Int32 polygonIndex)
```

#### Summary

Tests whether a polygon accepts contacts only from above the layer.

#### Returns

Tests whether a polygon accepts contacts only from above the layer.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.

<a id="member-c9730e595c55"></a>
### `RemoveCollisionPolygon(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Void RemoveCollisionPolygon(System.Int32 layerID, System.Int32 polygonIndex)
```

#### Summary

Removes a polygon and shifts later indices down.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.

<a id="member-f0b0f20c5bf3"></a>
### `SetCollisionPolygonOneWay(System.Int32, System.Int32, System.Boolean)`

Kind: `method`

```csharp
public System.Void SetCollisionPolygonOneWay(System.Int32 layerID, System.Int32 polygonIndex, System.Boolean oneWay)
```

#### Summary

Sets a polygon's one-way contact policy.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.
- `oneWay`: Whether to enable the one-way contact policy.

<a id="member-2b32548ad500"></a>
### `SetCollisionPolygonOneWayMargin(System.Int32, System.Int32, System.Single)`

Kind: `method`

```csharp
public System.Void SetCollisionPolygonOneWayMargin(System.Int32 layerID, System.Int32 polygonIndex, System.Single oneWayMargin)
```

#### Summary

Sets a finite nonnegative one-way recovery margin.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.
- `oneWayMargin`: Finite nonnegative recovery margin in scene units.

<a id="member-502ec201bf48"></a>
### `SetCollisionPolygonPoints(System.Int32, System.Int32, Electron2D.Vector2[])`

Kind: `method`

```csharp
public System.Void SetCollisionPolygonPoints(System.Int32 layerID, System.Int32 polygonIndex, Electron2D.Vector2[] polygon)
```

#### Summary

Copies a finite closed contour; empty or invalid topology contributes no fixtures.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonIndex`: Zero-based polygon index in the physics layer.
- `polygon`: Copied finite tile-local contour, empty or containing at least three points.

<a id="member-78848d4eb456"></a>
### `SetCollisionPolygonsCount(System.Int32, System.Int32)`

Kind: `method`

```csharp
public System.Void SetCollisionPolygonsCount(System.Int32 layerID, System.Int32 polygonsCount)
```

#### Summary

Resizes a physics layer's polygon list, preserving surviving entries.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `polygonsCount`: Nonnegative polygon count.

<a id="member-4bb23f6cf8dc"></a>
### `SetConstantAngularVelocity(System.Int32, System.Single)`

Kind: `method`

```csharp
public System.Void SetConstantAngularVelocity(System.Int32 layerID, System.Single velocity)
```

#### Summary

Sets a finite angular surface velocity in radians per second.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `velocity`: Finite surface velocity in scene units per second, or radians per second for angular velocity.

<a id="member-df32a8259fe3"></a>
### `SetConstantLinearVelocity(System.Int32, Electron2D.Vector2)`

Kind: `method`

```csharp
public System.Void SetConstantLinearVelocity(System.Int32 layerID, Electron2D.Vector2 velocity)
```

#### Summary

Sets a finite world-space surface velocity without moving static tile geometry.

#### Parameters

- `layerID`: Zero-based tile-set physics layer index.
- `velocity`: Finite surface velocity in scene units per second, or radians per second for angular velocity.

<a id="member-4076493ef3bb"></a>
### `ValidateDisposal()`

Kind: `method`

```csharp
protected override System.Void ValidateDisposal()
```

#### Summary

Prevents disposal of data borrowed from an atlas or a runtime update.

#### Exceptions

- `T:System.InvalidOperationException`: The caller does not own this data.

<a id="member-d9d7ad50f562"></a>
### `FlipH`

Kind: `property`

```csharp
public System.Boolean FlipH { get; set; }
```

#### Summary

Gets or sets horizontal reflection; base alternatives cannot be reflected in their data.

<a id="member-de72a71533fa"></a>
### `FlipV`

Kind: `property`

```csharp
public System.Boolean FlipV { get; set; }
```

#### Summary

Gets or sets vertical reflection; base alternatives cannot be reflected in their data.

<a id="member-b436a2f4f594"></a>
### `Modulate`

Kind: `property`

```csharp
public Electron2D.Color Modulate { get; set; }
```

#### Summary

Gets or sets the finite texture tint, white by default.

<a id="member-d78fa4986b14"></a>
### `TextureOrigin`

Kind: `property`

```csharp
public Electron2D.Vector2i TextureOrigin { get; set; }
```

#### Summary

Gets or sets the texture origin offset in pixels; collision geometry is unaffected.

<a id="member-3401e6cc9fa0"></a>
### `Transpose`

Kind: `property`

```csharp
public System.Boolean Transpose { get; set; }
```

#### Summary

Gets or sets whether tile axes are exchanged before reflection.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).

## Tile navigation

See [tile navigation](../components/tiles.md#navigation-layers) for ownership, batching, file graphs and verification.

### SetNavigationPolygon

```csharp
void SetNavigationPolygon(int layerID, NavigationPolygon? navigationPolygon)
```

Assigns a borrowed live polygon or null. Changes and disposal invalidate transformed cache resources and notify consuming layers.

### GetNavigationPolygon

```csharp
NavigationPolygon? GetNavigationPolygon(int layerID, bool flipH = false, bool flipV = false, bool transpose = false)
```

Returns the borrowed original without transforms or a cached data-owned transformed resource. Transpose precedes reflections; polygon indices and outline winding are preserved. Copies are invalidated when the original changes or this data is removed.

## Terrain API

See [terrain authoring and painting](../components/tiles.md#terrain-authoring-and-painting) for the shared lifecycle, defaults, callback, persistence and allocation contract. Owners reject access after disposal. Indices are validated against the live definitions.

<a id="terrain-df7dd9f01294"></a>
### GetTerrainPeeringBit

```csharp
public System.Int32 GetTerrainPeeringBit(Electron2D.CellNeighbor peeringBit)
```

Returns an applicable terrain peering value; -1 is an empty terrain.

- `peeringBit`: A side or corner supported by the attached set's shape and terrain mode.

<a id="terrain-cafe7f73d449"></a>
### IsValidTerrainPeeringBit

```csharp
public System.Boolean IsValidTerrainPeeringBit(Electron2D.CellNeighbor peeringBit)
```

Tests whether a defined peering direction is applicable to this data's attached set, terrain set and mode.

False for unassigned, detached, undefined or unavailable directions.

- `peeringBit`: The direction to test.

<a id="terrain-d7a2f9cbeaa0"></a>
### SetTerrainPeeringBit

```csharp
public System.Void SetTerrainPeeringBit(Electron2D.CellNeighbor peeringBit, System.Int32 terrainIndex)
```

Sets a terrain peering value; -1 clears it.

- `peeringBit`: A defined direction, applicable when attached to a set.
- `terrainIndex`: A terrain index or -1 for empty terrain.

<a id="terrain-90b9c67335c8"></a>
### Probability

```csharp
public System.Single Probability { get; set; }
```

Gets or sets the finite nonnegative weight used to select atlas alternatives with the same terrain pattern.

The default is one. When all matching weights are zero the first candidate is selected.

<a id="terrain-e7a66615d398"></a>
### Terrain

```csharp
public System.Int32 Terrain { get; set; }
```

Gets or sets the center terrain index, or -1 for an empty center.

A nonempty center requires an assigned terrain set and must belong to the attached set.

<a id="terrain-1a9722452594"></a>
### TerrainSet

```csharp
public System.Int32 TerrainSet { get; set; }
```

Gets or sets the terrain set index, or -1 to disable terrain assignments.

A change on data attached to a set clears its center and peering assignments. Standalone atlas authoring can retain indices before association.

Actual custom values are independent of configured types. Runtime copies own separate value holders while borrowing their arrays/resources; typed replacements preserve authored values. See [custom data](../components/tiles.md#typed-custom-data) for schema/default, exceptions, storage and ownership.

## Custom data methods

| Signature | Contract |
| --- | --- |
| `public void SetCustomData<T>(string layerName, T value)` | Stores a typed value on the named custom layer, independently of its configured default type. |
| `public T GetCustomData<T>(string layerName)` | Reads the named layer using its actual stored type. |
| `public bool HasCustomData(string layerName)` | Tests whether the attached set defines this named custom layer, including nil entries. |
| `public void SetCustomDataByLayerID<T>(int layerID, T value)` | Stores a typed value at a valid custom-layer index. |
| `public T GetCustomDataByLayerID<T>(int layerID)` | Reads the indexed layer using its actual stored type. |

### `public void SetCustomData<T>(string layerName, T value)`

**summary:** Stores a typed value on the named custom layer, independently of its configured default type.

**T:** The actual stored CLR value type.

**layerName:** Exact nonempty layer name.

**value:** Stored value; arrays and resources are borrowed.

**remarks:** Named access requires an attached tile set. Every write emits Changed.


### `public T GetCustomData<T>(string layerName)`

**summary:** Reads the named layer using its actual stored type.

**T:** The stored value type; resource base types can read derived resources.

**layerName:** Exact layer name.

**returns:** The stored value, or default(T) for a nil entry.

**KeyNotFoundException:** The layer name or stored type does not match.

**InvalidOperationException:** The data has no attached tile set.


### `public bool HasCustomData(string layerName)`

**summary:** Tests whether the attached set defines this named custom layer, including nil entries.

**layerName:** Exact layer name.

**returns:** Whether the named layer exists.


### `public void SetCustomDataByLayerID<T>(int layerID, T value)`

**summary:** Stores a typed value at a valid custom-layer index.

**T:** The actual stored CLR value type.

**layerID:** Zero-based custom layer index.

**value:** Stored value; borrowed references retain their own ownership.

**remarks:** Changing the configured layer type later converts or resets values. Stable typed writes reuse their prepared holder.

**ArgumentOutOfRangeException:** The layer index is absent.


### `public T GetCustomDataByLayerID<T>(int layerID)`

**summary:** Reads the indexed layer using its actual stored type.

**T:** The exact value type, or an assignable resource type.

**layerID:** Zero-based layer index.

**returns:** The stored value, or default(T) for nil.

**ArgumentOutOfRangeException:** The layer index is absent.

**KeyNotFoundException:** The non-nil stored type does not match T.

Typed null resources retain exact and assignable base-resource read projections; scalar nil entries continue to return default(T). TileCustomDataTests checks both null projections alongside graph duplication.

## Tile drawing policies

See [materials and ordering](../components/tiles.md#per-tile-materials-and-draw-order) for grouping, ownership, capability, storage and checked anchor boundaries.

| Signature | Contract |
| --- | --- |
| `public Material? Material { get; set; }` | Gets or sets the borrowed material used to draw this tile; null inherits the layer's material. |
| `public int ZIndex { get; set; }` | Gets or sets this tile's drawing Z offset relative to its layer. |
| `public int YSortOrigin { get; set; }` | Gets or sets the tile's Y sorting anchor offset in scene units; image and physics positions are unchanged. |

### `public Material? Material`

**summary:** Gets or sets the borrowed material used to draw this tile; null inherits the layer's material.

**value:** Null by default.

**remarks:** Assignments emit Changed, including equal assignments. Disposing tile data does not dispose this resource.
A disposed borrowed material remains readable but cannot be rendered until replaced. Shader materials require a capable renderer.

**ObjectDisposedException:** This data or the assigned material is disposed.


### `public int ZIndex`

**summary:** Gets or sets this tile's drawing Z offset relative to its layer.

**value:** Zero by default, bounded by the canvas minimum and maximum Z indices.

**ArgumentOutOfRangeException:** The value exceeds the canvas Z range.


### `public int YSortOrigin`

**summary:** Gets or sets the tile's Y sorting anchor offset in scene units; image and physics positions are unchanged.

**value:** Zero by default; used when the layer enables Y sorting.

## Canvas lighting integration

[Canvas lights and occlusion](../components/canvas-lights.md) records this type's receiving/material, resource, occlusion, sampling or static-service consumer, with current GPU and compatibility behavior.
