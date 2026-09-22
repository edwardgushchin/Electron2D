# Image.CompressSource

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.CompressSource`

> Supplies source semantics to channel detection and the future texture-compression pipeline.

## Description

The current CPU image component uses this enum only in [`Image.DetectUsedChannels`](Image.md#detectusedchannels). Compression itself is deliberately absent until a renderer/editor toolchain is approved.

## Examples

Partial snippet: `image` is an existing nonempty, uncompressed `Image` supplied by the caller.

```csharp
Image.UsedChannels channels = image.DetectUsedChannels(Image.CompressSource.Normal);
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`Generic`](#generic) | 0 | Ordinary color or data. |
| [`Srgb`](#srgb) | 1 | Nonlinear sRGB color. |
| [`Normal`](#normal) | 2 | Tangent-space normal data. |

## Enumeration Descriptions

<a id="generic"></a>
### `Generic = 0`

Requests ordinary value-based channel detection without additional semantic assumptions.

<a id="srgb"></a>
### `Srgb = 1`

Marks nonlinear sRGB color. Current channel detection uses the same component-presence rules as `Generic`; future compressors may choose color-specific encodings.

<a id="normal"></a>
### `Normal = 2`

Marks tangent-space normal data and selects `UsedChannels.RedGreen`, because X and Y are sufficient to reconstruct Z in the intended future compression path.

## Invariants and errors

The enum is immutable and thread-safe. Undefined values cause `ArgumentOutOfRangeException` before analysis begins.

## Verification and limitations

Tests cover all three valid modes and invalid sentinels. No compression API exists yet; its exact implementation trigger is recorded in [ADR 0039](../decisions/resources.md#adr-0039).
