# RenderingDevice.Limit

Last updated: 2026-10-27

**Declaration:** `public enum RenderingDevice.Limit` · **Source:** [device limits](../../src/Servers/Rendering/Device/RenderingDevice.Limits.cs).

| Value | Identity | Meaning |
| --- | ---: | --- |
| MaxBoundUniformSets | 0 | Maximum simultaneously populated logical descriptor sets; set numbers remain sparse. |
| MaxFramebufferColorAttachments | 1 | Maximum color targets in one native framebuffer pass. |
| MaxTexturesPerUniformSet | 2 | Maximum sampled image descriptors in one logical set across graphics stages. |
| MaxSamplersPerUniformSet | 3 | Maximum sampler descriptors in one logical set across graphics stages. |
| MaxStorageBuffersPerUniformSet | 4 | Maximum storage buffer descriptors in one logical set across graphics stages. |
| MaxStorageImagesPerUniformSet | 5 | Maximum storage image descriptors in one logical set across graphics stages. |
| MaxUniformBuffersPerUniformSet | 6 | Maximum uniform buffer descriptors in one logical set across graphics stages. |
| MaxDrawIndexedIndex | 7 | Maximum ordinary unsigned index value; enabled restart markers retain their own role. |
| MaxFramebufferHeight | 8 | Maximum framebuffer height in pixels. |
| MaxFramebufferWidth | 9 | Maximum framebuffer width in pixels. |
| MaxTextureArrayLayers | 10 | Maximum color image array layers; depth initialization has its separate 256-layer bound. |
| MaxTextureSize2D | 12 | Maximum ordinary image width or height in texels, subject to format and byte capacity. |
| MaxTexturesPerShaderStage | 15 | Maximum sampled image slots in one shader stage. |
| MaxSamplersPerShaderStage | 16 | Maximum sampler slots in one shader stage. |
| MaxStorageBuffersPerShaderStage | 17 | Maximum storage buffer slots in one shader stage. |
| MaxStorageImagesPerShaderStage | 18 | Maximum storage image slots in one shader stage. |
| MaxUniformBuffersPerShaderStage | 19 | Maximum uniform slots in one stage, including a slot used by lowered push data. |
| MaxPushConstantSize | 20 | Maximum copied push data in bytes. |
| MaxUniformBufferSize | 21 | Maximum ordinary uniform buffer capacity in bytes. |
| MaxVertexInputAttributeOffset | 22 | Maximum byte offset of an attribute, subject to its format and stride. |
| MaxVertexInputAttributes | 23 | Maximum vertex attribute count and location capacity. |
| MaxVertexInputBindings | 24 | Maximum contiguous vertex input bindings. |
| MaxVertexInputBindingStride | 25 | Maximum vertex input byte stride. |
| MinUniformBufferOffsetAlignment | 26 | Native uniform suballocation alignment in bytes; public uniform RIDs begin at zero. |
| MaxComputeSharedMemorySize | 27 | Maximum native compute workgroup shared storage in bytes. |
| MaxComputeWorkgroupCountX | 28 | Maximum direct or GPU-authored indirect X workgroup count. |
| MaxComputeWorkgroupCountY | 29 | Maximum direct or GPU-authored indirect Y workgroup count. |
| MaxComputeWorkgroupCountZ | 30 | Maximum direct or GPU-authored indirect Z workgroup count. |
| MaxComputeWorkgroupInvocations | 31 | Maximum product of local workgroup dimensions. |
| MaxComputeWorkgroupSizeX | 32 | Maximum local X workgroup dimension. |
| MaxComputeWorkgroupSizeY | 33 | Maximum local Y workgroup dimension. |
| MaxComputeWorkgroupSizeZ | 34 | Maximum local Z workgroup dimension. |
| MaxViewportDimensionsX | 35 | Maximum viewport width in pixels. |
| MaxViewportDimensionsY | 36 | Maximum viewport height in pixels. |

See [effective device limits](../components/device-limits.md) for profile/native constraints, remapped logical sets, query ownership and verification.
