# RenderingServer.TextureType

Last updated: 2026-11-12

**Declaration** `public enum RenderingServer.TextureType`

Selects the native image shape for [`RenderingServer.TextureCreateFromNativeHandle`](RenderingServer.md#texturecreatefromnativehandle).

| Value | Description |
| --- | --- |
| `Texture = 0` | One ordinary sampled image with one layer and unit depth. |

Use `RenderingServer.TextureType.Texture` when wrapping a live Direct3D 11 ordinary texture. The current [import adapter](../components/native-texture-handles.md#importing-direct3d-11-textures) requires a renderer-local sampled color image.
