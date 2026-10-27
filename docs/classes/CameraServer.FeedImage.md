# CameraServer.FeedImage

Last updated: 2026-10-27

`public enum Electron2D.CameraServer.FeedImage`

Inherits: `System.Enum`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

CameraTexture plane selector. RGBAImage, YCBCRImage and YImage share zero because the source datatype determines first-plane interpretation. CBCRImage selects plane one. Unknown values are rejected; selecting missing chroma returns no image while retaining full frame logical dimensions.

## API reference

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.CameraServer.FeedImage CBCRImage = 1`](#member-625ef1a9fcd7) | The separate interleaved chroma image. |
| [`public const Electron2D.CameraServer.FeedImage RGBAImage = 0`](#member-d20d4742398b) | The ordinary RGB/RGBA image. |
| [`public const Electron2D.CameraServer.FeedImage YCBCRImage = 0`](#member-ae59187d42bc) | The packed YCbCr image. |
| [`public const Electron2D.CameraServer.FeedImage YImage = 0`](#member-dd7c6a6832a5) | The separate luminance image. |

## Member descriptions

<a id="member-625ef1a9fcd7"></a>
### `public const Electron2D.CameraServer.FeedImage CBCRImage = 1`

The separate interleaved chroma image.

<a id="member-d20d4742398b"></a>
### `public const Electron2D.CameraServer.FeedImage RGBAImage = 0`

The ordinary RGB/RGBA image.

<a id="member-ae59187d42bc"></a>
### `public const Electron2D.CameraServer.FeedImage YCBCRImage = 0`

The packed YCbCr image.

<a id="member-dd7c6a6832a5"></a>
### `public const Electron2D.CameraServer.FeedImage YImage = 0`

The separate luminance image.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
