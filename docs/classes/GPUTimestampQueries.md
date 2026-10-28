# GPUTimestampQueries

Last updated: 2026-10-29

- Declaration: `internal sealed unsafe partial class GPUTimestampQueries : IDisposable`
- Source: [GPUTimestampQueries.cs](../../src/Servers/Rendering/GPUTimestampQueries.cs)
- Component: [viewport render time](../components/viewport-render-time.md)
- Visibility: internal; unavailable to engine consumers.

Owns a private native Vulkan query pool through RenderHandle and its device lease. Shared by RenderingDevice markers and GPU canvas elapsed measurements. Validates native reset/write/read/elapsed results and preserves the pool-before-device retirement order. No native handle is public.
