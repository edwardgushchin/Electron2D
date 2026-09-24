# CompatibilityCanvasBackend

Last updated: 2026-09-23

- Declaration: `internal sealed class CompatibilityCanvasBackend : CanvasBackend`
- Source: [CompatibilityCanvasBackend.cs](../../src/Servers/Rendering/CompatibilityCanvasBackend.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Description

The SDL_Renderer implementation of CanvasBackend. It supports baseline geometry and textures and rejects every shader material before drawing. A retained RGBA8 target supports readback and window presentation. Native resources are owner-thread state; the renderer retains its window through RenderHandle. Unsupported float texture or repeat capabilities are rejected before drawing.

Texture filtering and addressing are set per batch, so a shared texture can have distinct modes in one frame. Preflight rejects mipmaps, anisotropy and mirrored repeat; software triangles additionally reject linear filtering. Non-power-of-two repeat requires native wrapping support. Unsupported requests fail before clearing/drawing.

## Internal usage

This fragment belongs inside the runtime and requires the surrounding owner state.

```csharp
using CanvasBackend backend = new CompatibilityCanvasBackend(nativeWindow);
```

## Member summary

| Declaration | Contract |
| --- | --- |
| `internal CompatibilityCanvasBackend(SafeHandle window)` | [Construction](#construction) |
| `internal override string Method { get; }` | [Method](#method) |
| `internal override string Driver { get; }` | [Driver](#driver) |
| `internal override Vector2i GetPixelSize()` | [Pixel size](#pixel-size) |
| `internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)` | [Draw](#draw) |
| `internal override Image Readback()` | [Readback](#readback) |
| `internal override nint GetNativeHandle(DisplayServer.HandleType type)` | [Native identity](#native-identity) |
| `public override void Dispose()` | [Disposal](#disposal) |

## Member descriptions

### Construction

`internal CompatibilityCanvasBackend(SafeHandle window)`

Creates the native renderer, captures supported Linux context identities while association is known, and configures source-alpha blending. Constructor failure disposes the renderer.

### Method

`internal override string Method { get; }`

Always compatibility.

### Driver

`internal override string Driver { get; }`

Captures the selected SDL_Renderer driver name.

### Pixel size

`internal override Vector2i GetPixelSize()`

Queries physical render output dimensions.

### Draw

`internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)`

Validates shader absence, prepares base-level texture copies and prunes unused resources. Converts vertices into reusable SDL storage, applies clamp/repeat and nearest filtering, and submits triangles. The software driver gets separate textured triangles to avoid its incorrect quad shortcut. Restores the window target in finally. present controls final window copy/presentation.

### Readback

`internal override Image Readback()`

Temporarily binds the owned target, copies and converts the native surface to caller-owned RGBA8 pixels, disposes temporary surfaces and restores the window target.

### Native identity

`internal override nint GetNativeHandle(DisplayServer.HandleType type)`

Returns captured borrowed GL/EGL/GLX identities only for the actual Linux GL/GLES backend. EGL config identity is validated against the owned context; GLX config/visual are queried with temporary enumeration storage released by XFree. Later queries neither change current context nor sample a foreign context. Unsupported categories/drivers throw.

### Disposal

`public override void Dispose()`

Clears exposed identities and releases texture cache, target and renderer. SafeHandle disposal is idempotent. Borrowed resources held by nodes/materials are not disposed.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees. [RenderingNativeHandleTests](../../tests/Electron2D.Tests/RenderingNativeHandleTests.cs) verifies identities and foreign-context isolation on Wayland and XWayland; other platforms remain unverified.

The software driver truncates fractional vertex positions before rasterization. Transform snapping does not eliminate every fraction introduced by scale/rotation; final vertex snapping rounds primitive corners in shared geometry before SDL receives them. Edge precision remains backend-specific; see [pixel snapping](../components/canvas-rendering.md#pixel-snapping).

The pinned SDL software triangle input truncates source UVs to integer texels as well as destination vertices. Half-texel clipping boundaries can therefore shift the boundary between adjacent atlas colors: AtlasTextureTests explicitly checks the software/hardware difference and the shared edge sample. This is a fallback precision limit under ADR 0028; it does not promise identical nearest-sampling pixels across drivers. See [SDL software geometry input](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_render_sw.c).
