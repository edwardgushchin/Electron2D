# CanvasBackend

Last updated: 2026-10-04

- Declaration: `internal abstract class CanvasBackend : IDisposable`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The shared internal contract for [GPUCanvasBackend](GPUCanvasBackend.md) and [CompatibilityCanvasBackend](CompatibilityCanvasBackend.md). RenderingServer exclusively owns one backend and serializes access on the scene thread. Games cannot construct it. Draw consumes prepared borrowed data synchronously; submission does not imply GPU completion or compositor display.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
backend.Draw(target, vertices, batches, clearColor, clearEnabled: true, present: true, time: renderTime);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal abstract string Method { get; }` | [Method](#method) |
| `internal abstract string Driver { get; }` | [Driver](#driver) |
| `internal abstract Vector2i GetPixelSize()` | [Pixel size](#pixel-size) |
| `internal abstract void Draw(CanvasRenderTarget target, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, bool present, double time)` | [Draw](#draw) |
| `internal abstract Image Readback(CanvasRenderTarget target, bool backBuffer = false)` | [Readback](#readback) |
| `internal virtual nint GetNativeHandle(DisplayServer.HandleType type)` | [Native identity](#native-identity) |
| `public abstract void Dispose()` | [Disposal](#disposal) |
| `internal static InvalidOperationException Failure(string operation)` | [Failure](#failure) |
| `internal static void Check(bool result, string operation)` | [Check](#check) |

## Member descriptions

### Method

`internal abstract string Method { get; }`

Returns gpu or compatibility, sampled by the public rendering service.

### Driver

`internal abstract string Driver { get; }`

Reports the native driver selected during creation.

### Pixel size

`internal abstract Vector2i GetPixelSize()`

Queries current framebuffer dimensions in physical pixels; native failure throws InvalidOperationException.

### Draw

`internal abstract void Draw(CanvasRenderTarget target, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, bool present, double time)`

Prepares resources, clears or preserves the selected owned RGBA8 target and submits ordered triangles/copy/group passes. present controls copying/presenting to the window. time is the current scaled, wrapped renderer clock in seconds; GPU forwards its float32 value to optional shader TIME, while compatibility has no programmable shader support. Unsupported features reject explicitly; exceptions propagate to Engine.Run cleanup.

### Readback

`internal abstract Image Readback(CanvasRenderTarget target, bool backBuffer = false)`

Returns a caller-owned RGBA8 copy after a successful frame; it may block and allocate. Before any completed target frame it throws InvalidOperationException. The optional backBuffer flag selects native screen storage for explicit internal verification; public ViewportTexture.GetImage selects its completed target.

### Native identity

`internal virtual nint GetNativeHandle(DisplayServer.HandleType type)`

By default throws NotSupportedException. Compatibility supplies verified borrowed Linux GL/EGL/GLX identities under ADR 0042; no SDL-owned handle becomes public.

### Disposal

`public abstract void Dispose()`

Releases owned native state before the display window can close. Invoked on the owner thread.

### Failure

`internal static InvalidOperationException Failure(string operation)`

Captures the current SDL error with an engine operation description.

### Check

`internal static void Check(bool result, string operation)`

Throws Failure(operation) when a native boolean result is false.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees.

Offscreen canvases now use independently owned completed/write target pairs. Native caches span every selected target in one frame. Viewport textures use their native image and dimensions directly; GetImage readback remains an explicit cold operation. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets).

CreateTarget accepts optional native mip-level storage; BackBuffer lazily allocates/upgrades one shared screen image per CanvasRenderTarget. Readback can explicitly select completed or screen storage for internal native verification. Capability gates precede native drawing. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

### Target storage and frame cache

`internal abstract RenderHandle CreateTarget(Vector2i size, Color clear, bool mipmaps = false)` initializes native target storage. `internal CanvasRenderTarget Target(Viewport viewport, Vector2i size, Color clear)` validates 1..16384 dimensions, initializes both replacement images before committing, and preserves a same-size allocation. `internal RenderHandle BackBuffer(CanvasRenderTarget target, bool mipmaps)` lazily creates/upgrades shared screen storage. `internal CanvasRenderTarget? FindTarget(Viewport viewport)` returns borrowed active storage, and `internal void ReleaseTarget(Viewport viewport)` disposes one target. `protected void ReleaseTargets()` releases every owned target at shutdown. `internal virtual void BeginFrame()` / `EndFrame()` scope native resource caches over the entire multi-target frame. `internal virtual void SetWindowVisible(DisplayServer display, bool visible)` delegates native visibility; the GPU override coordinates presentation storage and its exact remap gate.
