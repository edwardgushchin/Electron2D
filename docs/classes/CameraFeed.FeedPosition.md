# CameraFeed.FeedPosition

Last updated: 2026-10-27

`public enum Electron2D.CameraFeed.FeedPosition`

Inherits: `System.Enum`. Part of [camera pixel feeds](../components/camera-feeds.md), under [ADR 0115](../decisions/camera-feeds.md#adr-0115).

Optional source metadata: Unspecified is initially used by plain sources, with Front/Back for known physical positions. Values have no rendering or capture side effect.

## API reference

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.CameraFeed.FeedPosition Back = 2`](#member-550d8d9a3a40) | Back-facing camera. |
| [`public const Electron2D.CameraFeed.FeedPosition Front = 1`](#member-188ef9841034) | Front-facing camera. |
| [`public const Electron2D.CameraFeed.FeedPosition Unspecified = 0`](#member-e8af6e51a105) | Position is unknown. |

## Member descriptions

<a id="member-550d8d9a3a40"></a>
### `public const Electron2D.CameraFeed.FeedPosition Back = 2`

Back-facing camera.

<a id="member-188ef9841034"></a>
### `public const Electron2D.CameraFeed.FeedPosition Front = 1`

Front-facing camera.

<a id="member-e8af6e51a105"></a>
### `public const Electron2D.CameraFeed.FeedPosition Unspecified = 0`

Position is unknown.

## Verification and limits

The public camera consumer and native boundary tests are linked from the [component verification](../components/camera-feeds.md#verification). Compiled API/XML, native surface conversion and rendered pixels are distinct from physical camera capture, foreign platforms, native allocator and owner acceptance.
