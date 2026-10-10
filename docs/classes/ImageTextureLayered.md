# ImageTextureLayered

Last updated: 2026-10-10

- Source: [ImageTextureLayered.cs](../../src/Scene/Resources/ImageTextureLayered.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

Owns immutable copied image layers for an array texture.

## Syntax

```csharp
public abstract class Electron2D.ImageTextureLayered
```

### Remarks

Creation and updates validate before publication. Callbacks run after releasing the state lock; callback exceptions retain committed pixels. Image inputs and outputs remain caller-owned.

**Inherits:** [TextureLayered](TextureLayered.md)

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
| [`protected ImageTextureLayered()`](#member-c9f4c4ca654e) | constructor | Initializes an empty image-layer resource. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-b17b2ee9089a) | method |  |
| [`public System.Void CreateFromImages(System.ReadOnlySpan<Electron2D.Image> images)`](#member-0e75dfcc79fe) | method | Atomically replaces this array with independent copies of homogeneous images. |
| [`protected override System.Void Dispose(System.Boolean disposing)`](#member-9185df458368) | method |  |
| [`public override Electron2D.Image.Format GetFormat()`](#member-45d0311fc988) | method |  |
| [`public override System.Int32 GetHeight()`](#member-439d7d453ff1) | method |  |
| [`public override Electron2D.Image GetLayerData(System.Int32 layerIndex)`](#member-3c90a5a2b4de) | method |  |
| [`public override System.Int32 GetLayers()`](#member-80b476c8e6b1) | method |  |
| [`public override System.Int32 GetWidth()`](#member-eeb3050e34ca) | method |  |
| [`public override System.Boolean HasMipmaps()`](#member-2701c5692b1e) | method |  |
| [`public System.Void UpdateLayer(Electron2D.Image image, System.Int32 layer)`](#member-7cd6a0f67ea5) | method | Replaces one layer while retaining all allocation parameters. |

## Member Details

<a id="member-c9f4c4ca654e"></a>
### `ImageTextureLayered()`

Kind: `constructor`

```csharp
protected ImageTextureLayered()
```

#### Summary

Initializes an empty image-layer resource.

<a id="member-b17b2ee9089a"></a>
### `CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)`

Kind: `method`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-0e75dfcc79fe"></a>
### `CreateFromImages(System.ReadOnlySpan<Electron2D.Image>)`

Kind: `method`

```csharp
public System.Void CreateFromImages(System.ReadOnlySpan<Electron2D.Image> images)
```

#### Summary

Atomically replaces this array with independent copies of homogeneous images.

#### Remarks

Failure preserves the previous layers and RID. Native upload occurs before subsequent drawing.

#### Parameters

- `images`: At least one live, nonempty image, with equal size, format and mipmap state.

#### Exceptions

- `T:System.ArgumentException`: The array is empty, heterogeneous, too large, or contains an empty image.
- `T:System.NotSupportedException`: A pixel format is unsupported by the existing sampling conversion.
- `T:System.ObjectDisposedException`: This resource or an input image is disposed.

<a id="member-9185df458368"></a>
### `Dispose(System.Boolean)`

Kind: `method`

```csharp
protected override System.Void Dispose(System.Boolean disposing)
```

<a id="member-45d0311fc988"></a>
### `GetFormat()`

Kind: `method`

```csharp
public override Electron2D.Image.Format GetFormat()
```

<a id="member-439d7d453ff1"></a>
### `GetHeight()`

Kind: `method`

```csharp
public override System.Int32 GetHeight()
```

<a id="member-3c90a5a2b4de"></a>
### `GetLayerData(System.Int32)`

Kind: `method`

```csharp
public override Electron2D.Image GetLayerData(System.Int32 layerIndex)
```

<a id="member-80b476c8e6b1"></a>
### `GetLayers()`

Kind: `method`

```csharp
public override System.Int32 GetLayers()
```

<a id="member-eeb3050e34ca"></a>
### `GetWidth()`

Kind: `method`

```csharp
public override System.Int32 GetWidth()
```

<a id="member-2701c5692b1e"></a>
### `HasMipmaps()`

Kind: `method`

```csharp
public override System.Boolean HasMipmaps()
```

<a id="member-7cd6a0f67ea5"></a>
### `UpdateLayer(Electron2D.Image, System.Int32)`

Kind: `method`

```csharp
public System.Void UpdateLayer(Electron2D.Image image, System.Int32 layer)
```

#### Summary

Replaces one layer while retaining all allocation parameters.

#### Remarks

Other layers remain unchanged. Validation failure preserves the array; compatible native allocation is reused.

#### Parameters

- `image`: A copied image matching the array dimensions, source format and mipmap state.
- `layer`: The zero-based layer to replace.

#### Exceptions

- `T:System.InvalidOperationException`: The array is uninitialized.
- `T:System.ArgumentOutOfRangeException`: The layer does not exist.
- `T:System.ArgumentException`: Image configuration does not match.
- `T:System.ObjectDisposedException`: This resource or the image is disposed.

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
