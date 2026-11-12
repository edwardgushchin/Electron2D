# RenderingServer.TextureType

Last updated: 2026-11-12

**Declaration** `public enum RenderingServer.TextureType`

Selects the native image shape for [`RenderingServer.TextureCreateFromNativeHandle`](RenderingServer.md#texturecreatefromnativehandle).

| Value | Description |
| --- | --- |
| `Texture = 0` | One ordinary sampled image with one layer and unit depth. |

Use `RenderingServer.TextureType.Texture` when wrapping a live Direct3D 11 or Direct3D 12 ordinary texture. The [import adapters](../components/native-texture-handles.md) require a renderer-local sampled color image.
