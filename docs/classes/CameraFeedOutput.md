# CameraFeedOutput

Last updated: 2026-10-27

`public enum Electron2D.CameraFeedOutput`

Inherits: `System.Enum`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

Typed conversion policy for CameraFeed.SetFormat and its protected hook. Native RGB uses colorspace-aware RGBA conversion. YUYV additionally supports Separate, Grayscale and Copy. Unsupported native combinations return false; custom sources implement their declared policy.

## Example

The source/format comments below identify any required caller context.

```csharp
// source is an inactive CameraFeed with at least one declared format.
var accepted = source.SetFormat(0, CameraFeedOutput.RGB);
```

## API reference

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.CameraFeedOutput Copy = 3`](#member-5564bb2274d2) | Copy packed YUYV bytes into a two-channel image. |
| [`public const Electron2D.CameraFeedOutput Grayscale = 2`](#member-988fd327846f) | Publish the YUYV luminance as an opaque grayscale image. |
| [`public const Electron2D.CameraFeedOutput RGB = 0`](#member-4071bebe88f5) | Convert to full-color RGBA pixels. |
| [`public const Electron2D.CameraFeedOutput Separate = 1`](#member-3fedfb73c92f) | Publish Y and interleaved CbCr planes from a YUYV source. |

## Member descriptions

<a id="member-5564bb2274d2"></a>
### `public const Electron2D.CameraFeedOutput Copy = 3`

Copy packed YUYV bytes into a two-channel image.

<a id="member-988fd327846f"></a>
### `public const Electron2D.CameraFeedOutput Grayscale = 2`

Publish the YUYV luminance as an opaque grayscale image.

<a id="member-4071bebe88f5"></a>
### `public const Electron2D.CameraFeedOutput RGB = 0`

Convert to full-color RGBA pixels.

<a id="member-3fedfb73c92f"></a>
### `public const Electron2D.CameraFeedOutput Separate = 1`

Publish Y and interleaved CbCr planes from a YUYV source.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
