# Native driver resources

Last updated: 2026-11-12

`RenderingDevice.GetDriverResource(resource, rid, index)` returns the unsigned handle bits or native numeric value for an existing resource. Query the device that owns the RID from its owner thread. Device, instance and queue selectors ignore the RID. All selectors ignore the reserved `index` argument.

| Selector | Vulkan result | Windows GPU Direct3D 12 result |
| --- | --- | --- |
| `LogicalDevice`, `PhysicalDevice`, `TopmostObject` | `VkDevice`, `VkPhysicalDevice`, `VkInstance` | `ID3D12Device*`, `IDXGIAdapter1*`, `IDXGIFactory4*` |
| `CommandQueue` | Unified `VkQueue` | Direct `ID3D12CommandQueue*` |
| `QueueFamily` | Native family index, which can be zero | Unsupported |
| `Texture` | Active `VkImage` | Active `ID3D12Resource*` |
| `TextureView` | `VkImageView` | CPU shader-resource descriptor handle for a sampled view. Attachment-only images reject. |
| `TextureDataFormat` | Effective view `VkFormat` | Typed `DXGI_FORMAT` of the SRV or attachment. A sampled depth image reports its SRV format. |
| `Sampler` | `VkSampler` | CPU sampler descriptor handle |
| `Buffer` | Active `VkBuffer` | Active `ID3D12Resource*` |
| `ComputePipeline`, `RenderPipeline` | Native pipeline for the resource's program and primary view | `ID3D12PipelineState*` |
| `UniformSet` | Unsupported | Unsupported |

Direct3D 12 returns borrowed COM interface pointers without acquiring a reference. Its descriptor values are the `ptr` fields of `D3D12_CPU_DESCRIPTOR_HANDLE` values from live CPU staging heaps. Copy them while the owning texture or sampler is live, using the corresponding heap type. Leave source descriptors untouched. Logical uniform sets have command-owned descriptors. [Microsoft descriptor copy rules](https://learn.microsoft.com/en-us/windows/win32/direct3d12/copying-descriptors)

Shared views can report one image with distinct image-view handles and formats. A renderer-borrowed viewport RID follows the last completed ping-pong image and expires when the target is replaced. Buffer views report the underlying native allocation. A pipeline that still needs a concrete binding configuration has no published native handle.

The query observes current storage without allocating a wrapper, uploading authored changes, recording GPU work or waiting for completion. On Vulkan, the native read lock protects the snapshot from defragmentation during the query. The Direct3D 12 adapter reads the current resource and descriptor from the owner-affine storage. Cycling, resize, release or defragmentation can invalidate the result afterward. Native handles carry no ownership, storage offset or layout guarantee. External use requires independent synchronization and native lifetime coordination.

A stale, foreign or wrong-kind resource RID is rejected. An unavailable native adapter or native descriptor-set query raises `NotSupportedException`. The adapters require the matched SDL 3.4.18 core. The Direct3D 12 layout is generated from the pinned source and compiled against the desktop Windows SDK layout. Metal, browser GPU and other driver adapters and package qualification remain separate.

[Native texture export](native-texture-handles.md) uses the GPU image selector for canvas texture RIDs on Vulkan and Direct3D 12 and exposes SDL texture properties on supported Compatibility drivers.

## Verification

Run `ELECTRON2D_TEST_DRIVER_RESOURCE=local` to check the instance, physical device, queue, queue family, and image and buffer memory requirements through the independent Vulkan loader. This profile also exercises shared views, graphics and compute output, RID lifetime and 64 warmed zero-owner-byte queries. Use `canvas` to check the completed viewport allocation and resize lifetime. Use `unavailable` with the preceding native module on Direct3D 12 to check refusal while ordinary GPU work remains available.

Build the [Direct3D 12 probe](../../tests/Electron2D.Tests/Fixtures/DriverResources/d3d12.cpp) with `cl /std:c++20 /EHsc /LD /MT d3d12.cpp /link /OUT:D3D12DriverProbe.dll`. Set `ELECTRON2D_D3D12_DRIVER_FIXTURE` to its absolute path. Run the Release tests with `SDL_GPU_DRIVER=direct3d12` and `ELECTRON2D_TEST_DRIVER_RESOURCE=direct3d12`. The probe checks DXGI adapter selection against the device LUID, actual COM resource descriptions and owner identity, descriptor copying and queue completion with its own fence. The managed test exercises graphics/compute output, sampled and attachment-only roles, stale and foreign RIDs and 64 warm owner queries. Use `ELECTRON2D_TEST_NATIVE_TEXTURE_HANDLE=gpu-direct3d12` for public canvas texture export and completed viewport images. Windows x64 is the verified Direct3D 12 profile.
