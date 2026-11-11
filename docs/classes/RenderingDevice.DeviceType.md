# RenderingDevice.DeviceType

Classifies the physical adapter reported by [RenderingServer.GetVideoAdapterType](RenderingServer.md). Vulkan supplies the native classification. OpenGL, software and renderers without a classification report `Other`.

| Member | Value | Meaning |
| --- | ---: | --- |
| `Other` | 0 | Other or unclassified device. |
| `IntegratedGPU` | 1 | Integrated GPU. |
| `DiscreteGPU` | 2 | Discrete GPU. |
| `VirtualGPU` | 3 | Virtual GPU. |
| `CPU` | 4 | CPU graphics device. |
| `Max` | 5 | Upper bound of the device type values. |

See [graphics adapter identity](../components/graphics-adapter-identity.md).
