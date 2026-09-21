# Image.UsedChannels

Last updated: 2026-09-21

**Inherits:** `System.Enum`

**Inherited By:** none

- **Source:** [`src/Core/IO/Image.cs`](../../src/Core/IO/Image.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Image.UsedChannels`

> Describes the smallest meaningful decoded channel set selected by [`Image.DetectUsedChannels`](Image.md#detectusedchannels).

## Description

The result is an analysis hint for storage and future compression choices. It does not change the image and does not promise that a renderer supports a corresponding texture format.

## Examples

Partial snippet: `image` is an existing nonempty, uncompressed `Image` supplied by the caller.

```csharp
Image.UsedChannels channels = image.DetectUsedChannels();
```

## Enumeration values

| Member | Value | Description |
| --- | ---: | --- |
| [`Luminance`](#luminance) | 0 | Equal RGB values, opaque alpha. |
| [`LuminanceAlpha`](#luminancealpha) | 1 | Equal RGB values plus transparency. |
| [`Red`](#red) | 2 | One red/data channel. |
| [`RedGreen`](#redgreen) | 3 | Two data channels. |
| [`Rgb`](#rgb) | 4 | Color without transparency. |
| [`Rgba`](#rgba) | 5 | Color with transparency. |

## Enumeration Descriptions

<a id="luminance"></a>
### `Luminance = 0`

One luminance value is sufficient because decoded RGB values are equal and alpha is opaque.

<a id="luminancealpha"></a>
### `LuminanceAlpha = 1`

Equal decoded RGB values can share luminance, but varying alpha must also be retained.

<a id="red"></a>
### `Red = 2`

One red/data channel is sufficient, including explicitly single-channel red formats.

<a id="redgreen"></a>
### `RedGreen = 3`

Two data channels are required. Normal-map source semantics select this result directly.

<a id="rgb"></a>
### `Rgb = 4`

Three color channels are required and decoded alpha remains opaque.

<a id="rgba"></a>
### `Rgba = 5`

Three color channels and varying alpha are required.

## Invariants and errors

The enum is immutable and thread-safe. Detection scans only a readable base level and rejects empty or compressed storage.

## Verification and limitations

Tests cover luminance, luminance-alpha, red, red-green, RGB, RGBA, and normal-map classification. Thresholds are deterministic but are not a color-management system. See [Image](Image.md).
