# LayeredTexturePixels

Last updated: 2026-10-10

- Source: [TextureLayered.cs](../../src/Scene/Resources/TextureLayered.cs)
- Visibility: internal; unavailable to engine consumers.
- Component: [Array resources](../components/texture-arrays.md)

## Description

Immutable TexturePixels[] with homogeneous dimensions/format/mipmap state, total upload bytes and Allocation identity. Construction validates nonempty payload and a 256 MiB source/upload bound. Compatible updates retain Allocation; duplication shares immutable snapshots while future publication remains independent.

## Internal use

Connects copied resource snapshots and logical identity to the existing GPU renderer. No native handle is exposed through the public API.

## Verification and limits

[TextureArrayTests](../../tests/Electron2D.Tests/TextureArrayTests.cs) checks homogeneous copied layers, atomic update/failure/callback behavior, custom producers, typed descriptors/defaults/overrides, incompatible reload, duplication, archives/placeholders and 1000 prepared reads with zero owner-thread managed bytes. [TextureArrayRenderingTests](../../tests/Electron2D.Tests/TextureArrayRenderingTests.cs) checks HLSL/GLSL and owned-RID output, every mip of two physical GPU layers, compatible allocation reuse, HDR reallocation, proxy nesting/retarget/replacement, guards and cleanup. Linux Wayland GPU passes 24 warmup and 64 active frames with zero owner-thread managed bytes. Compatibility rejects shader use before drawing and releases the host.

Compressed/integer-sampled source formats, descriptor binding arrays, configurable named samplers, native allocation totals, foreign platforms, new-family trimming/AOT and owner acceptance remain separate. Cube sampling is outside the product boundary. See [array resources](../components/texture-arrays.md), [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0004](../decisions/product.md#adr-0004).
