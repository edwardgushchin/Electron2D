# Native driver resources

`RenderingDevice.GetDriverResource(resource, rid, index)` returns the unsigned handle bits or native numeric value for an existing resource. Query the device that owns the RID from its owner thread. Device, instance and queue selectors ignore the RID. All selectors ignore the reserved `index` argument. Queue family zero is valid.

| Selector | Vulkan result |
| --- | --- |
| `LogicalDevice`, `PhysicalDevice`, `TopmostObject` | `VkDevice`, `VkPhysicalDevice` and `VkInstance` |
| `CommandQueue`, `QueueFamily` | Unified `VkQueue` and its family index |
| `Texture`, `TextureView`, `TextureDataFormat` | Active `VkImage`, `VkImageView` and effective view `VkFormat` |
| `Sampler`, `Buffer` | `VkSampler` and the active `VkBuffer` allocation |
| `ComputePipeline`, `RenderPipeline` | Native pipeline for the resource's program and primary view |
| `UniformSet` | Unsupported. Logical bindings produce native descriptor sets owned by recorded commands. |

Shared views can report one image with distinct image-view handles and formats. A renderer-borrowed viewport RID follows the last completed ping-pong image and expires when the target is replaced. Buffer views report the underlying native allocation. A pipeline that still needs a concrete binding configuration has no published native handle.

The query observes current storage without allocating a wrapper, uploading authored changes, recording GPU work or waiting for completion. The native read lock protects the snapshot from defragmentation during the query. Cycling, resize, release or defragmentation can invalidate the result afterward. Native handles carry no ownership, storage offset or layout guarantee. External use requires independent synchronization and native lifetime coordination.

A stale, foreign or wrong-kind resource RID is rejected. An unavailable native adapter or native descriptor-set query raises `NotSupportedException`. The adapter requires the matched SDL 3.4.18 Vulkan core. Other drivers and native package qualification remain separate.

## Verification

Run `ELECTRON2D_TEST_DRIVER_RESOURCE=local` to check the instance, physical device, queue, queue family, and image and buffer memory requirements through the independent Vulkan loader. This profile also exercises shared views, graphics and compute output, RID lifetime and 64 warmed zero-owner-byte queries. Use `canvas` to check the completed viewport allocation and resize lifetime. Use `unavailable` to check refusal from a foreign driver or preceding native module while ordinary GPU work remains available.
