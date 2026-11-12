# RenderingDevice.DriverResource

Last updated: 2026-11-12

**Declaration** `public enum RenderingDevice.DriverResource`

Selects the native object or value returned by `RenderingDevice.GetDriverResource`. The active GPU driver determines the concrete type. See the [backend types, ownership and verification](../components/driver-resources.md).

| Value | Role |
| --- | --- |
| `LogicalDevice = 0` | The device executing the GPU commands. |
| `PhysicalDevice = 1` | The physical device or adapter selected for that device. |
| `TopmostObject = 2` | The driver instance or DXGI factory. |
| `CommandQueue = 3` | The device's native command queue. |
| `QueueFamily = 4` | The native Vulkan queue-family index. Zero is valid. |
| `Texture = 5` | The active allocation of a live texture. |
| `TextureView = 6` | The native sampled image view or CPU shader-resource descriptor handle. |
| `TextureDataFormat = 7` | The native numeric format of the view or attachment. |
| `Sampler = 8` | The native sampler object or CPU sampler descriptor handle. |
| `UniformSet = 9` | Reserved for a native set with its own retained lifetime. |
| `Buffer = 10` | The active allocation of a live buffer. |
| `ComputePipeline = 11` | The published native compute pipeline. |
| `RenderPipeline = 12` | The published native graphics pipeline. |

Query on the device's owner thread. Device and queue selectors ignore the RID. Resource selectors require a live RID of the matching kind. Borrowed objects and descriptors may expire when the owning resource is replaced, cycled, or released. Unsupported selectors throw `NotSupportedException`.
