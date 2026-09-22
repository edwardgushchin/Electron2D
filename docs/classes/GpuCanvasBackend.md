# GpuCanvasBackend

Last updated: 2026-09-22

- Declaration: `internal sealed unsafe class GpuCanvasBackend : CanvasBackend`
- Source: [GpuCanvasBackend.cs](../../src/Servers/Rendering/GpuCanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The SDL GPU implementation of CanvasBackend. Construction retains the display window, creates a device, claims the window, and builds the default pipeline. Construction failure unwinds the completed steps. The backend owns RGBA8 target/vertex/upload buffers, shader pipelines, samplers and per-Texture GPU caches. All calls require the renderer owner thread.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
using CanvasBackend backend = new GpuCanvasBackend(nativeWindow);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal GpuCanvasBackend(SafeHandle window)` | [Construction](#construction) |
| `internal override string Method { get; }` | [Method](#method) |
| `internal override string Driver { get; }` | [Driver](#driver) |
| `internal override Vector2I GetPixelSize()` | [Pixel size](#pixel-size) |
| `internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present)` | [Draw](#draw) |
| `internal override Image Readback()` | [Readback](#readback) |
| `public override void Dispose()` | [Disposal](#disposal) |

## Member descriptions

### Construction

`internal GpuCanvasBackend(SafeHandle window)`

Creates the device for formats reported by ShaderCompiler, claims window, loads the built-in vertex shader and default fragment pipeline. Native zero/false results fail explicitly.

### Method

`internal override string Method { get; }`

Always gpu.

### Driver

`internal override string Driver { get; }`

SDL device driver name captured after creation; unknown if SDL supplies no name.

### Pixel size

`internal override Vector2I GetPixelSize()`

Uses the window physical pixel dimensions.

### Draw

`internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present)`

Prepares program/texture resources before encoding, prunes unused caches, resizes target/buffers, uploads changed pixels and vertices, and emits ordered render batches. Uniforms serialize with material updates. TEXTURE binds the command texture or owned white pixel. Submission commits texture upload versions only on success. An unsubmitted command is cancelled unless a swapchain was acquired, in which case it must be submitted for release. Runtime backend switching/device recovery are absent.

### Readback

`internal override Image Readback()`

Copies the last target through a download transfer buffer and fence, waits for completion and returns a caller-owned tightly packed RGBA8 Image. Native staging rows are padded to 64 pixels. Temporary handles are released on failure.

### Disposal

`public override void Dispose()`

Idempotently waits for device idle, releases caches, samplers, target/buffers/shaders, releases the window claim and disposes the device. An idle-wait failure is reported after cleanup. Inherited native-context queries reject GL identities.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [shader import checks](../../tools/shaders/check.py) exercise the supported interface, bad inputs and resource lifecycle. GPU output is verified on Linux Wayland/Vulkan; broader shader features and other backends remain incomplete.
