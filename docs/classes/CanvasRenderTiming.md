# CanvasRenderTiming

Last updated: 2026-10-29

- Declaration: `internal abstract class CanvasRenderTiming : IDisposable`
- Source: [CanvasRenderTiming.cs](../../src/Servers/Rendering/CanvasRenderTiming.cs)
- Component: [viewport render time](../components/viewport-render-time.md)
- Visibility: internal; unavailable to engine consumers.

Retains CPU ticks and publishes a CPU/GPU pair only after a native query completes. Backend subclasses own query collection, abort and disposal; BeginFrame retires the previous sample before reuse. No public consumer owns this object.
