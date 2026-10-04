# CanvasRenderTarget

Last updated: 2026-10-04

Internal sealed native target pair, defined in [CanvasRenderTarget.cs](../../src/Servers/Rendering/CanvasRenderTarget.cs). Each pair owns two RenderHandles, immutable pixel dimensions and a completed-frame flag. Commit swaps completed/write roles only after successful drawing. Dispose attempts both releases. CanvasBackend owns the per-viewport map, validates 1..16384 dimensions before allocation and commits replacement only after both images initialize. Renderer detachment/disposal/shutdown releases targets without owning scene nodes/textures. GPU keeps source/write attachments distinct; compatibility copies completed data with overwrite blending for Never clear. Explicit readback samples the completed image. Native allocator/driver internals remain outside measured managed warm intervals. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets).

The existing completed/write pair additionally owns an optional shared backbuffer and whether it has mip levels. Disposal attempts all three releases; native resize replaces the target and its screen storage together. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).
