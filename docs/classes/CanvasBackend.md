# CanvasBackend

Last updated: 2026-09-23

- Declaration: `internal abstract class CanvasBackend : IDisposable`
- Source: [CanvasBackend.cs](../../src/Servers/Rendering/CanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The shared internal contract for [GpuCanvasBackend](GpuCanvasBackend.md) and [CompatibilityCanvasBackend](CompatibilityCanvasBackend.md). RenderingServer exclusively owns one backend and serializes access on the scene thread. Games cannot construct it. Draw consumes prepared borrowed data synchronously; submission does not imply GPU completion or compositor display.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
backend.Draw(vertices, batches, clearColor, present: true, renderTime);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal abstract string Method { get; }` | [Method](#method) |
| `internal abstract string Driver { get; }` | [Driver](#driver) |
| `internal abstract Vector2I GetPixelSize()` | [Pixel size](#pixel-size) |
| `internal abstract void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)` | [Draw](#draw) |
| `internal abstract Image Readback()` | [Readback](#readback) |
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

`internal abstract Vector2I GetPixelSize()`

Queries current framebuffer dimensions in physical pixels; native failure throws InvalidOperationException.

### Draw

`internal abstract void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)`

Prepares resources, clears its owned RGBA8 target and submits ordered triangles. present controls copying/presenting to the window. time is the current scaled, wrapped renderer clock in seconds; GPU forwards its float32 value to optional shader TIME, while compatibility has no programmable shader support. Zero-size output skips work. Unsupported features reject explicitly; exceptions propagate to Engine.Run cleanup.

### Readback

`internal abstract Image Readback()`

Returns a caller-owned RGBA8 copy after a successful frame; it may block and allocate. Before any completed target frame it throws InvalidOperationException. This is internal verification support, not a public offscreen API.

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
