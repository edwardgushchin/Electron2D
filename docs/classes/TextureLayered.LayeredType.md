# TextureLayered.LayeredType

Last updated: 2026-10-10

- Source: [TextureLayered.cs](../../src/Scene/Resources/TextureLayered.cs)
- Component: [Array resources](../components/texture-arrays.md)

## Overview

Identifies the applicable layered sampling role.

## Syntax

```csharp
public enum Electron2D.TextureLayered.LayeredType
```

## Example

Public resource snippet; shader assignment separately requires a compiled array sampler.

```csharp
TextureLayered.LayeredType type = TextureLayered.LayeredType.Array;
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public const Electron2D.TextureLayered.LayeredType Array = 0`](#member-6ce74b43b3f0) | enumValue | An array of independent two-dimensional image layers. |

## Member Details

<a id="member-6ce74b43b3f0"></a>
### `Array`

Kind: `enumValue`

```csharp
public const Electron2D.TextureLayered.LayeredType Array = 0
```

#### Summary

An array of independent two-dimensional image layers.

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
