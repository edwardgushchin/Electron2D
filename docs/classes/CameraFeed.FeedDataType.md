# CameraFeed.FeedDataType

Last updated: 2026-10-27

`public enum Electron2D.CameraFeed.FeedDataType`

Inherits: `System.Enum`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

Current source pixel arrangement. A plain source initially reports RGB without pixels; native discovered sources initially report NoImage. YCBCR and YCBCRSeparate expose raw data requiring producer/consumer conversion. The external native-image selector retains its separate import prerequisite and is absent.

## API reference

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.CameraFeed.FeedDataType NoImage = 0`](#member-abb00839859b) | No image has been published. |
| [`public const Electron2D.CameraFeed.FeedDataType RGB = 1`](#member-118a1aa1db03) | RGB or RGBA pixels. |
| [`public const Electron2D.CameraFeed.FeedDataType YCBCR = 2`](#member-1744b5406e35) | Packed YCbCr pixels requiring consumer conversion. |
| [`public const Electron2D.CameraFeed.FeedDataType YCBCRSeparate = 3`](#member-89f055158d9c) | Separate Y and interleaved CbCr planes requiring consumer conversion. |

## Member descriptions

<a id="member-abb00839859b"></a>
### `public const Electron2D.CameraFeed.FeedDataType NoImage = 0`

No image has been published.

<a id="member-118a1aa1db03"></a>
### `public const Electron2D.CameraFeed.FeedDataType RGB = 1`

RGB or RGBA pixels.

<a id="member-1744b5406e35"></a>
### `public const Electron2D.CameraFeed.FeedDataType YCBCR = 2`

Packed YCbCr pixels requiring consumer conversion.

<a id="member-89f055158d9c"></a>
### `public const Electron2D.CameraFeed.FeedDataType YCBCRSeparate = 3`

Separate Y and interleaved CbCr planes requiring consumer conversion.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
