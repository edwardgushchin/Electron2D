# RendererTextureBorrow

Last updated: 2026-10-12

Internal sealed IDisposable in [RendererTextureBorrow.cs](../../src/Servers/Rendering/RendererTextureBorrow.cs), owned by RenderingDevice texture entries. It borrows one GPUTexture allocation or both buffers of a completed CanvasRenderTarget, retaining SafeHandle leases and the resource identity. Ordinary/array sources additionally retain renderer cache residency.

Alive validates source disposal, authored allocation identity, target retirement and original viewport binding. Native resolves the current completed target at command-record time; RefreshPixels projects the current compatible authored snapshot without changing allocation. Dispose releases residency and all held leases once. Device completion retires expired entries without retaining native storage; explicit FreeRID can remove their metadata tombstones. Construction failure releases acquired leases.

The owning [renderer export checkpoint](../components/local-compute.md#renderer-texture-export-checkpoint) records native bytes, target resize, queued lifetime and warmed allocation evidence. This type is unavailable to public consumers.
