# Native texture handles

Last updated: 2026-11-12

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

## Importing Direct3D 11 textures

`RenderingServer.TextureCreateFromNativeHandle` wraps a live `ID3D11Texture2D*` on the primary SDR Direct3D 11 Compatibility renderer. The shape is `RenderingServer.TextureType.Texture`, with unit depth and one layer. Declare `Image.Format.Rgba8` for either DXGI RGBA8 or BGRA8 UNORM. The native description must match the supplied size and be a default-usage shader resource with one mip, layer and sample and no miscellaneous flags. The handle must point to a live native interface throughout the import call.

SDL retains a COM reference to the existing allocation. The producer can release its own reference after import. Order producer writes on the same device's immediate context before canvas submission. The engine samples that storage directly and returns the same native handle from the imported RID and its aliases. Another independent native window uses its own Compatibility renderer and cannot sample the import. Embedded windows and offscreen viewports on the same renderer share it.

A producer can obtain the renderer's device with `ID3D11Texture2D::GetDevice` on a live texture returned by `TextureGetNativeHandle`. Release the COM reference returned by `GetDevice` when finished.

Native producer writes do not signal the renderer. Use `TextureSetForceRedrawIfVisible` to keep a visible streaming texture drawing in low-processor mode, or submit a frame with `ForceDraw`.

Use `Texture2DUpdate` with matching portable RGBA8 data to modify the same storage. `Texture2DGet` synchronously copies the native image into a caller-owned RGBA8 snapshot, with native row pitch and channel order handled by the adapter. Logical display size and path use the normal texture metadata. `TextureReplace` can transfer the allocation to another owned ordinary texture RID. `FreeRID` and renderer shutdown release the retained wrapper.

The Direct3D 11 import requires a sampled SDR ordinary texture. Layered and other pixel formats and sRGB native formats require additional adapters. This API imports an interface pointer in the current process and device. Cross-device shared handles and keyed mutex resources require another handoff contract.

## Importing Direct3D 12 textures

Pass a live `ID3D12Resource*` from the primary SDR Direct3D 12 Compatibility device in a 64-bit Windows process. Use the same portable format and ordinary shape as Direct3D 11. The resource must be a one-sample, one-layer, one-mip RGBA8 or BGRA8 UNORM texture on a non-shared default heap, with shader reads enabled. A producer can obtain the device with `ID3D12Resource::GetDevice` on a live exported texture and then release that device reference after preparing its own resource.

Finish the producer's GPU work before import and leave the resource in `D3D12_RESOURCE_STATE_COPY_DEST`. The Compatibility renderer owns resource transitions and queue access from then on. It rejects a second live wrapper for the resource and rejects re-import of its own uploads and viewport targets. Release the producer's COM reference after import when it is no longer needed. Use the server RID for updates, aliases, canvas sampling and retirement. A native-handle query returns the original pointer for identification. Further external commands require a separate synchronization and state handoff contract.

`Texture2DGet` samples the import at its physical size into a temporary SDR target and reads an independent RGBA8 image. A point sample with blending disabled preserves stored channel values, including RGB at zero alpha. The pass uses the existing renderer state tracker for all transitions.

## Importing Vulkan device images

`RenderingDevice.TextureCreateFromExtension` wraps an existing sampled `VkImage` on the pinned Vulkan backend. Use the logical device and queue family reported by [`GetDriverResource`](driver-resources.md). Pass the exact format, dimensions, layers, sample count and mip count of an external optimal-tiling color image. Ordinary images and independent arrays use one sample and unit depth. Include `Sampling` in the declared usage. `CanCopyFrom` and `CPURead` require `VK_IMAGE_USAGE_TRANSFER_SRC_BIT`. `CanCopyTo` and `CanUpdate` require `VK_IMAGE_USAGE_TRANSFER_DST_BIT`.

Complete producer work before import and leave all subresources in `VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL` with ownership on the receiving queue family. Exclude concurrent queue submissions during the producer's handoff and give the renderer exclusive use until retirement. The adapter owns views and a device RID. The producer keeps the image and its bound memory alive. Do not import images that already have an engine owner or another live wrapper. The supplied native handle and description must be valid on this device.

The wrapper preserves contents and supports shared layer/mip views and the declared transfer roles. On the retained renderer device, `RenderingServer.TextureRDCreate` makes an independent canvas wrapper for the imported device RID or a selected slice. Free canvas consumers before retiring the device RID. `FreeRID` completes work and retires its dependent device views, leaving the image in the sampled layout on the same queue family. The producer can then reuse it or destroy the image and memory before the logical device closes.

The server's direct native-handle import still needs ownership of the imported device RID. The Vulkan Compatibility renderer uses a different device adapter. On pinned OpenGL and OpenGL ES, creating an SDL texture around an external name redefines its storage with `glTexImage2D`. Direct import needs a wrapper that preserves the producer's storage.

## Verification

Run `ELECTRON2D_TEST_NATIVE_TEXTURE_HANDLE` with the SDL driver profile. The profile checks ordinary drawing, compatible updates, replacement, diffuse aliases, viewport image publication and resize, and refusal for unsupported roles or drivers. The Vulkan profile also compares device and array handles and the actual sRGB view format. The Direct3D 11 profile calls `ID3D11Texture2D::GetDesc` independently. The OpenGL and OpenGL ES profiles check the native name in the active context. The Windows run also checks Direct3D 12 and Vulkan Compatibility, plus refusal controls for software and GPU Direct3D 12.

Run `ELECTRON2D_TEST_NATIVE_TEXTURE_IMPORT=direct3d11` with `SDL_RENDER_DRIVER=direct3d11` on Windows. The producer creates native RGBA8 and BGRA8 resources on the active renderer device. The profile verifies real canvas output, native writes, release of producer references, imported image update/readback, logical size, alias replacement and owner/shape/device errors. It measures zero managed owner bytes across 64 prepared handle queries and 64 active frames without presentation. The `software` and `vulkan` profiles check explicit refusal and ordinary rendering on the selected backends. Native allocation totals and release packages have separate qualification.

Build the [Direct3D 12 producer](../../tests/Electron2D.Tests/Fixtures/NativeTextureImport/d3d12.cpp) with `cl /std:c++20 /EHsc /LD /MT d3d12.cpp /link d3d12.lib dxgi.lib /OUT:NativeTextureImportD3D12.dll`. Run the Release test executable with `ELECTRON2D_TEST_NATIVE_TEXTURE_IMPORT=direct3d12`, `SDL_RENDER_DRIVER=direct3d12` and `ELECTRON2D_D3D12_IMPORT_FIXTURE` set to the DLL's absolute path. The producer submits a native upload on its own queue, waits on its fence and transfers the initialized resources. The test covers canvas pixels, COM lifetime, portable updates/readback, aliases, replacement and rejection of duplicate owners, borrowed renderer resources, unsupported shapes and foreign devices. Set `ELECTRON2D_D3D12_IMPORT_DEBUG=1` to require the Direct3D 12 debug layer and check for native errors and corruption.

Build the [Vulkan producer](../../tests/Electron2D.Tests/Fixtures/NativeTextureImport/vulkan.cpp) with `cl /std:c++20 /EHsc /LD /MT /I<SDL-source>/src/video/khronos vulkan.cpp /link /OUT:VulkanImportProducer.dll`. Set `ELECTRON2D_VULKAN_IMPORT_FIXTURE` to the DLL's absolute path. With `SDL_GPU_DRIVER=vulkan`, run `ELECTRON2D_TEST_VULKAN_TEXTURE_IMPORT=local` and `canvas`. The producer clears distinct layers and mips on the actual Vulkan queue and waits for a fence. The profiles exercise native bytes and viewport pixels, copy, updates, request-time readback, duplicate refusal, retirement and producer reuse. The `preceding` profile uses the preceding native module and an actual producer image to check the missing entry point. The `unavailable` profile checks another GPU driver.
