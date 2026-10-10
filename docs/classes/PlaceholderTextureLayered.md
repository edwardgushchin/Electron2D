# PlaceholderTextureLayered

Last updated: 2026-10-10

- Source: [PlaceholderTextureLayered.cs](../../src/Scene/Resources/PlaceholderTextureLayered.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

Retains image-array dimensions and layer count without image data.

## Syntax

```csharp
public abstract class Electron2D.PlaceholderTextureLayered
```

### Remarks

Metadata may be used by a headless consumer. It does not guarantee shader-coordinate behavior; native sampling uses a single diagnostic layer. Size writes emit Changed, including equal writes; layer writes do not.

**Inherits:** [TextureLayered](TextureLayered.md)

## Example

Public resource snippet; shader assignment separately requires a compiled array sampler.

```csharp
using var metadata = new PlaceholderTextureArray { Size = new(128, 64), Layers = 4 };
// GetLayerData returns null: no images or renderer are required.
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`protected PlaceholderTextureLayered()`](#member-38b38d0fa0fb) | constructor | Initializes one metadata layer of size one by one. |
| [`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`](#member-0ea6a62efea1) | method |  |
| [`public override Electron2D.Image.Format GetFormat()`](#member-b2ed6985256a) | method |  |
| [`public override System.Int32 GetHeight()`](#member-ba280c713f91) | method |  |
| [`public override Electron2D.Image GetLayerData(System.Int32 layerIndex)`](#member-3964a4dfacdd) | method |  |
| [`public override System.Int32 GetLayers()`](#member-6672e242a3b9) | method |  |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-08596db36340) | method |  |
| [`public override System.Int32 GetWidth()`](#member-71790f98070c) | method |  |
| [`public override System.Boolean HasMipmaps()`](#member-757e3252211c) | method |  |
| [`public System.Int32 Layers { get; set; }`](#member-cab5ad3d95b2) | property | Gets or replaces the authored layer count, without allocating images or notifying Changed. |
| [`public Electron2D.Vector2i Size { get; set; }`](#member-5a5a4fc5f349) | property | Gets or replaces authored dimensions in pixels, without allocating images. |

## Member Details

<a id="member-38b38d0fa0fb"></a>
### `PlaceholderTextureLayered()`

Kind: `constructor`

```csharp
protected PlaceholderTextureLayered()
```

#### Summary

Initializes one metadata layer of size one by one.

<a id="member-0ea6a62efea1"></a>
### `CopyCustomStateTo(Electron2D.Resource, System.Boolean, Electron2D.DeepDuplicateMode, System.Func<Electron2D.Resource, Electron2D.Resource>, System.Func<Electron2D.Resource, Electron2D.Resource>)`

Kind: `method`

```csharp
protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)
```

<a id="member-b2ed6985256a"></a>
### `GetFormat()`

Kind: `method`

```csharp
public override Electron2D.Image.Format GetFormat()
```

<a id="member-ba280c713f91"></a>
### `GetHeight()`

Kind: `method`

```csharp
public override System.Int32 GetHeight()
```

<a id="member-3964a4dfacdd"></a>
### `GetLayerData(System.Int32)`

Kind: `method`

```csharp
public override Electron2D.Image GetLayerData(System.Int32 layerIndex)
```

<a id="member-6672e242a3b9"></a>
### `GetLayers()`

Kind: `method`

```csharp
public override System.Int32 GetLayers()
```

<a id="member-08596db36340"></a>
### `GetPropertyDescriptors()`

Kind: `method`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-71790f98070c"></a>
### `GetWidth()`

Kind: `method`

```csharp
public override System.Int32 GetWidth()
```

<a id="member-757e3252211c"></a>
### `HasMipmaps()`

Kind: `method`

```csharp
public override System.Boolean HasMipmaps()
```

<a id="member-cab5ad3d95b2"></a>
### `Layers`

Kind: `property`

```csharp
public System.Int32 Layers { get; set; }
```

#### Summary

Gets or replaces the authored layer count, without allocating images or notifying Changed.

#### Value

The requested integer count; initially one.

<a id="member-5a5a4fc5f349"></a>
### `Size`

Kind: `property`

```csharp
public Electron2D.Vector2i Size { get; set; }
```

#### Summary

Gets or replaces authored dimensions in pixels, without allocating images.

#### Value

The requested integer dimensions, including zero or negative metadata.

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
