# TextureArray

Last updated: 2026-10-10

- Source: [TextureArray.cs](../../src/Scene/Resources/TextureArray.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

A resource containing an independently sampled array of image layers.

## Syntax

```csharp
public sealed class Electron2D.TextureArray
```

### Remarks

Assign it to a reflected array sampler through ShaderMaterial.SetShaderLayeredParameter. GPU sampling uses the layer coordinate; ordinary canvas rectangle drawing uses Texture instead.

**Inherits:** [ImageTextureLayered](ImageTextureLayered.md)

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
| [`public TextureArray()`](#member-a4c5b2897464) | constructor | Creates an uninitialized array with zero dimensions and layers. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-b2c9ae85a74a) | method |  |
| [`public Electron2D.PlaceholderTextureArray CreatePlaceholder()`](#member-fe7cf82d30df) | method | Creates a caller-owned metadata placeholder with this array's dimensions and layer count. |

## Member Details

<a id="member-a4c5b2897464"></a>
### `TextureArray()`

Kind: `constructor`

```csharp
public TextureArray()
```

#### Summary

Creates an uninitialized array with zero dimensions and layers.

<a id="member-b2c9ae85a74a"></a>
### `CreateDuplicateInstance()`

Kind: `method`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-fe7cf82d30df"></a>
### `CreatePlaceholder()`

Kind: `method`

```csharp
public Electron2D.PlaceholderTextureArray CreatePlaceholder()
```

#### Summary

Creates a caller-owned metadata placeholder with this array's dimensions and layer count.

#### Returns

A placeholder containing no copied image data.

#### Exceptions

- `T:System.ObjectDisposedException`: This array is disposed.

## Shader usage

The following continuation requires a live compiled Shader with an image-array sampler named layers.

```csharp
using var material = new ShaderMaterial { Shader = shader };
material.SetShaderLayeredParameter("layers", array);
material.SetShaderParameter("selectedLayer", 1f);
```

Both shader and array are borrowed. Ordinary texture accessors reject array slots. Layered accessors preserve existing ordinary-texture null calls.

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
