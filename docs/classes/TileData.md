# TileData

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileData.cs](../../src/Scene/Resources/TileData.cs).

## Overview

Stores the visual transform and collision polygons of one atlas alternative.

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
