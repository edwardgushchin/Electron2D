# CanvasPresentation

Last updated: 2026-10-16

- Declaration: `internal readonly record struct CanvasPresentation(CanvasRenderTarget Target, Rect2 Rect)`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md#viewport-screen-attachment)
- Visibility: internal; unavailable to engine consumers.

A borrowed completed native viewport target and its destination rectangle in client pixels. RenderingServer builds a stable frame-local list after target submission; the backend replaces destination pixels without changing source storage. The record owns no target or native resource. Rect already contains the full-size fallback for an explicitly empty attachment. Backend clipping preserves its source UV mapping. ViewportPresentationTests verifies order, crop, lifetime, HDR/MSAA and prepared zero-managed-allocation presentation through the public server API.
