# RenderingServer.TextureType

Last updated: 2026-11-12

**Declaration** `public enum RenderingServer.TextureType`

Selects the native image shape for [`RenderingServer.TextureCreateFromNativeHandle`](RenderingServer.md#texturecreatefromnativehandle).

| Value | Description |
| --- | --- |
| `Texture = 0` | One ordinary sampled image with one layer and unit depth. |
| `Layered = 1` | One or more independent sampled layers with unit depth. |

GPU Vulkan accepts both roles. Direct3D 11 and Direct3D 12 Compatibility accept the ordinary role. The [import adapters](../components/native-texture-handles.md) require a renderer-local sampled color image.
