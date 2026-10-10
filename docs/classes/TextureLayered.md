# TextureLayered

Last updated: 2026-10-10

- Source: [TextureLayered.cs](../../src/Scene/Resources/TextureLayered.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

A sampled array of equally sized image layers, independent of ordinary canvas texture drawing.

## Syntax

```csharp
public abstract class Electron2D.TextureLayered
```

### Remarks

Resources own copied pixels or supply them through typed getters. Custom producers emit Changed after mutation; prepared rendering reuses immutable snapshots. Native allocations belong to the active renderer.

**Inherits:** [Resource](Resource.md)

## Example

Public resource snippet; shader assignment separately requires a compiled array sampler.

```csharp
using var first = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
using var second = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
first.Fill(Colors.Red); second.Fill(Colors.Blue);
using var array = new TextureArray();
array.CreateFromImages([first, second]);
TextureLayered layers = array;
using var copy = layers.GetLayerData(1); // Caller-owned pixels.
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`protected TextureLayered()`](#member-7060aa85c74b) | constructor | Initializes cached pixel invalidation for a custom layered producer. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-f6a0dc68868f) | method |  |
| [`public abstract Electron2D.Image.Format GetFormat()`](#member-8cec5a2980f6) | method | Gets the source pixel format. |
| [`public abstract System.Int32 GetHeight()`](#member-7f49a1a9e88e) | method | Gets the pixel height of one layer. |
| [`public abstract Electron2D.Image GetLayerData(System.Int32 layerIndex)`](#member-af407c0ec73c) | method | Copies one layer into a caller-owned image. |
| [`public virtual Electron2D.TextureLayered.LayeredType GetLayeredType()`](#member-15130ffd5c40) | method | Gets the sampling role. |
| [`public abstract System.Int32 GetLayers()`](#member-a9d0cda48571) | method | Gets the number of image layers. |
| [`public override Electron2D.RID GetRID()`](#member-7b0335766787) | method |  |
| [`public abstract System.Int32 GetWidth()`](#member-28517e137996) | method | Gets the pixel width of one layer. |
| [`public abstract System.Boolean HasMipmaps()`](#member-70954727b0a0) | method | Reports whether each image stores a mipmap chain. |
| [`protected override System.Void ValidateDisposal()`](#member-02fbf2978ace) | method |  |

## Member Details

<a id="member-7060aa85c74b"></a>
### `TextureLayered()`

Kind: `constructor`

```csharp
protected TextureLayered()
```

#### Summary

Initializes cached pixel invalidation for a custom layered producer.

<a id="member-f6a0dc68868f"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-8cec5a2980f6"></a>
### `GetFormat()`

Kind: `method`

```csharp
public abstract Electron2D.Image.Format GetFormat()
```

#### Summary

Gets the source pixel format.

#### Returns

The stored image format, without exposing upload conversion.

<a id="member-7f49a1a9e88e"></a>
### `GetHeight()`

Kind: `method`

```csharp
public abstract System.Int32 GetHeight()
```

#### Summary

Gets the pixel height of one layer.

#### Returns

The authored height; zero for uninitialized image arrays.

<a id="member-af407c0ec73c"></a>
### `GetLayerData(System.Int32)`

Kind: `method`

```csharp
public abstract Electron2D.Image GetLayerData(System.Int32 layerIndex)
```

#### Summary

Copies one layer into a caller-owned image.

#### Returns

An independent image, or null for a metadata-only placeholder.

#### Parameters

- `layerIndex`: The zero-based image layer.

#### Exceptions

- `T:System.ArgumentOutOfRangeException`: An image-array layer is outside its stored range.

<a id="member-15130ffd5c40"></a>
### `GetLayeredType()`

Kind: `method`

```csharp
public virtual Electron2D.TextureLayered.LayeredType GetLayeredType()
```

#### Summary

Gets the sampling role.

#### Returns

The independent image-array role.

<a id="member-a9d0cda48571"></a>
### `GetLayers()`

Kind: `method`

```csharp
public abstract System.Int32 GetLayers()
```

#### Summary

Gets the number of image layers.

#### Returns

The authored count; zero for uninitialized image arrays.

<a id="member-7b0335766787"></a>
### `GetRID()`

Kind: `method`

```csharp
public override Electron2D.RID GetRID()
```

<a id="member-28517e137996"></a>
### `GetWidth()`

Kind: `method`

```csharp
public abstract System.Int32 GetWidth()
```

#### Summary

Gets the pixel width of one layer.

#### Returns

The authored width; zero for uninitialized image arrays.

<a id="member-70954727b0a0"></a>
### `HasMipmaps()`

Kind: `method`

```csharp
public abstract System.Boolean HasMipmaps()
```

#### Summary

Reports whether each image stores a mipmap chain.

#### Returns

True when stored layers contain mipmaps.

<a id="member-02fbf2978ace"></a>
### `ValidateDisposal()`

Kind: `method`

```csharp
protected override System.Void ValidateDisposal()
```

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
