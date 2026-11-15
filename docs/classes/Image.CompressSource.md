# Image.CompressSource

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.CompressSource`

> Supplies source semantics to channel detection and Image compression.

## Description

Both [`Image.DetectUsedChannels`](Image.md#detectusedchannels) and `Image.Compress` use this policy through the same base-level channel analysis. Generic is the default.

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

Marks nonlinear sRGB color. Channel detection promotes Red/RedGreen color results to RGB, except for single-red storage; compression retains the requested stored color values without implicit color-space conversion.

<a id="normal"></a>
### `Normal = 2`

Marks tangent-space normal data and selects `UsedChannels.RedGreen`, because X and Y are sufficient to reconstruct Z in the normal-map compression path.

## Invariants and errors

The enum is immutable and thread-safe. Undefined values cause `ArgumentOutOfRangeException` before analysis begins.

## Verification and limitations

Tests cover all three valid modes and invalid sentinels. No compression API exists yet; its exact implementation trigger is recorded in [ADR 0039](../decisions/resources.md#adr-0039).
