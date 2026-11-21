# GPUTimestampQueries

Last updated: 2026-11-21

- Declaration: `internal sealed unsafe partial class GPUTimestampQueries : IDisposable`
- Source: [GPUTimestampQueries.cs](../../src/Servers/Rendering/GPUTimestampQueries.cs)
- Component: [GPU timestamps](../components/gpu-timestamps.md)
- Visibility: internal; unavailable to engine consumers.

Owns private native timestamp storage through RenderHandle and its device lease. RenderingDevice markers and GPU canvas elapsed measurements select the Vulkan or Direct3D 12 adapter from the active driver. The owner validates native operation results and disposes the query storage before releasing the device lease.
