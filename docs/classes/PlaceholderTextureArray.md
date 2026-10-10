# PlaceholderTextureArray

Last updated: 2026-10-10

- Source: [PlaceholderTextureArray.cs](../../src/Scene/Resources/PlaceholderTextureArray.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

A metadata-only placeholder for an independent image array.

## Syntax

```csharp
public sealed class Electron2D.PlaceholderTextureArray
```

**Inherits:** [PlaceholderTextureLayered](PlaceholderTextureLayered.md)

## Example

Public resource snippet; shader assignment separately requires a compiled array sampler.

```csharp
using var metadata = new PlaceholderTextureArray { Size = new(128, 64), Layers = 4 };
// GetLayerData returns null: no images or renderer are required.
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public PlaceholderTextureArray()`](#member-a9fde1d95b52) | constructor | Creates one metadata layer of size one by one. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-28d7e06819ae) | method |  |

## Member Details

<a id="member-a9fde1d95b52"></a>
### `PlaceholderTextureArray()`

Kind: `constructor`

```csharp
public PlaceholderTextureArray()
```

#### Summary

Creates one metadata layer of size one by one.

<a id="member-28d7e06819ae"></a>
### `CreateDuplicateInstance()`

Kind: `method`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
