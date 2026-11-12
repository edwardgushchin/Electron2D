# Native texture handles

`RenderingServer.TextureGetNativeHandle(texture, SRGB: false)` returns the unsigned native handle bits of a live canvas texture RID. Proxies and atlas and diffuse views follow the sampled source. Use the owner thread outside canvas submission. On a supported exporter, an uninitialized texture, an unresolved source and a viewport without a completed texture-backed frame return zero. Stale or wrong-kind RIDs are rejected.

| Backend | Value |
| --- | --- |
| GPU Vulkan | `VkImage` for an ordinary upload, image array, drawable, device-backed image or completed viewport. |
| Compatibility Direct3D 11 and Direct3D 12 | `ID3D11Texture2D*` or `ID3D12Resource*` from SDL texture properties. |
| Compatibility OpenGL and OpenGL ES | `GLuint` texture name in the owning context. |
| Compatibility Vulkan | `VkImage` from SDL texture properties. |

An unsupported renderer or texture role throws `NotSupportedException`. Software rendering has no native graphics texture, and programmable GPU drivers other than Vulkan need their own exporters. Independent image arrays and device-backed and writable resources require the GPU renderer. Compatibility exports ordinary uploads and completed viewport targets.

The handle identifies an allocation, so Vulkan linear and sRGB image views can return the same value. When an allocation offers a separate sampled interpretation, query the typed view with `TextureGetRDTexture(texture, SRGB: true)`. The native-handle query uses the current allocation when a separate interpretation is unavailable. See [shared texture views](shared-texture-views.md#srgb-canvas-texture-exports).

The first query may prepare an upload or renderer export. A compatible pixel update reuses storage. Explicitly exported ordinary uploads remain cached while the resource and renderer live. Replacement, cycling, resize, retirement or shutdown can invalidate a returned value. A viewport query returns the completed ping-pong image at the moment of the call. The query transfers no ownership, image layout or synchronization. Acquire and use the handle under the selected native backend's lifetime rules.

## Verification

Run `ELECTRON2D_TEST_NATIVE_TEXTURE_HANDLE` with the SDL driver profile. The profile checks ordinary drawing, compatible updates, replacement, diffuse aliases, viewport image publication and resize, and refusal for unsupported roles or drivers. The Vulkan profile also compares device and array handles and the actual sRGB view format. The Direct3D 11 profile calls `ID3D11Texture2D::GetDesc` independently. The OpenGL and OpenGL ES profiles check the native name in the active context. The Windows run also checks Direct3D 12 and Vulkan Compatibility, plus refusal controls for software and GPU Direct3D 12.
