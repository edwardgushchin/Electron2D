# CanvasInstance

Internal frame-owned GPU record; source [RenderingServer.Instances.cs](../../src/Servers/Rendering/RenderingServer.Instances.cs).

The 64-byte record captures a finite screen-space basis, translation, instance tint and raw custom data. Each eligible triangle surface references a contiguous range in its viewport frame. GPUCanvasBackend uploads that range beside shared vertices and binds it with per-instance input stepping. Repeated draws and dependent viewports retain independent ranges. Unsupported geometry policies continue through CPU expansion; no device identity or writable buffer is exposed.

[MultiMeshRenderingTests](../../tests/Electron2D.Tests/MultiMeshRenderingTests.cs) verifies actual instance-stream use, pixels, group fallback, neighboring ordinary draws, interpolation and warmed replay allocation.
