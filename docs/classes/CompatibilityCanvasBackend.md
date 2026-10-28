# CompatibilityCanvasBackend

Last updated: 2026-10-26

- Declaration: `internal sealed partial class CompatibilityCanvasBackend : CanvasBackend`
- Source: [CompatibilityCanvasBackend.cs](../../src/Servers/Rendering/CompatibilityCanvasBackend.cs), [CompatibilityCanvasBackend.GL.cs](../../src/Servers/Rendering/CompatibilityCanvasBackend.GL.cs), [CompatibilityCanvasBackend.GeometryLines.cs](../../src/Servers/Rendering/CompatibilityCanvasBackend.GeometryLines.cs)
- Component: [canvas-rendering](../components/canvas-rendering.md)
- Visibility: internal; unavailable to engine consumers.

## Viewport render time

The private timing subclass owns reusable native queries for the authoritative canvas Draw stream. Its [measurement and retirement contract](../components/viewport-render-time.md) excludes secondary compatibility replay and presentation. Source: [CompatibilityCanvasBackend.Timing.cs](../../src/Servers/Rendering/CompatibilityCanvasBackend.Timing.cs).

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
| `internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting)` | [Draw](#draw) |
| `internal override Image Readback(CanvasRenderTarget target, bool backBuffer = false)` | [Readback](#readback) |
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

`internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting)`

Validates shader absence, prepares base-level texture copies and prunes unused resources. Converts vertices into reusable SDL storage, applies clamp/repeat and nearest filtering, and submits triangles. Untextured points use SDL.RenderPoint. Uniform-color thin lines use SDL.RenderLines with endpoint snapping; connected strips retain reusable point storage and suppress consecutive equal endpoints. A strip uses one native call, preserving translucent joins. Software and the default hardware point-line method use [attributed native pixel coverage](../components/canvas-rendering.md#attributed-point-line-method-2026-10-26) for interpolated-color/textured lines, with reusable point/triangle buffers and unclipped endpoint projection. [Native attributed GL lines](../components/canvas-rendering.md#native-attributed-gl-lines-2026-10-26) use SDL flush cooperation for hint 2, retaining SDL state and a reusable GLES buffer. [Geometry mode](../components/canvas-rendering.md#attributed-geometry-lines-2026-10-26) retains native endpoint squares, connecting triangles and vertex attributes in reusable storage. [Other hardware native-line modes](../components/canvas-rendering.md#native-attribute-composition-2026-10-26) draw actual SDL coverage into two renderer-owned reusable GPU targets and apply projected endpoint attributes with the existing sampler/blend; geometry calls count, copies and clears do not. Textured points align their quad origin to software/OpenGL/OpenGL ES/Vulkan/SDL GPU native point coverage; other drivers retain the centered footprint. Native draw accounting follows actual submissions. The software driver gets separate textured triangles to avoid its incorrect quad shortcut. Restores the window target in finally. present controls final window copy/presentation.

### Readback

`internal override Image Readback(CanvasRenderTarget target, bool backBuffer = false)`

Temporarily binds the owned target, copies and converts the native surface to caller-owned RGBA8 pixels, disposes temporary surfaces and restores the window target.

### Native identity

`internal override nint GetNativeHandle(DisplayServer.HandleType type)`

Returns captured borrowed GL/EGL/GLX identities only for the actual Linux GL/GLES backend. EGL config identity is validated against the owned context; GLX config/visual are queried with temporary enumeration storage released by XFree. Later queries neither change current context nor sample a foreign context. Unsupported categories/drivers throw.

### Disposal

`public override void Dispose()`

Clears exposed identities and releases texture cache, targets, the owned GLES line buffer in its owning context, and renderer. SafeHandle disposal is idempotent. Borrowed resources held by nodes/materials are not disposed.

## Verification and limits

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) exercise this path through retained drawing and native readback on Linux Wayland and dummy/software. Pixel and allocation checks cover the documented baseline; they do not establish other platforms or frame-time guarantees. [RenderingNativeHandleTests](../../tests/Electron2D.Tests/RenderingNativeHandleTests.cs) verifies identities and foreign-context isolation on Wayland and XWayland; other platforms remain unverified.

The software driver truncates fractional vertex positions before rasterization. Transform snapping does not eliminate every fraction introduced by scale/rotation; final vertex snapping rounds primitive corners in shared geometry before SDL receives them. Edge precision remains backend-specific; see [pixel snapping](../components/canvas-rendering.md#pixel-snapping).

The pinned SDL software triangle input truncates source UVs to integer texels as well as destination vertices. Half-texel clipping boundaries can therefore shift the boundary between adjacent atlas colors: AtlasTextureTests explicitly checks the software/hardware difference and the shared edge sample. This is a fallback precision limit under ADR 0028; it does not promise identical nearest-sampling pixels across drivers. See [SDL software geometry input](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_render_sw.c).

Offscreen canvases now use independently owned completed/write target pairs. Native caches span every selected target in one frame. Viewport textures use their native image and dimensions directly; GetImage readback remains an explicit cold operation. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets).

Baseline copies and group composition use native SDL targets and preserve operation/clip boundaries. Arbitrary shaders and mipmapped groups reject before drawing; software also rejects unsupported premultiplied group blending. Internal screen readback verifies the actual copied pixels. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

ClipChildren uses a lazy native alpha MaskBuffer: ordinary owner primitives accumulate alpha, screen RGB uses destination-alpha blending, then premultiplied composition restores the main target. Draw target/clip restoration is deterministic. Software rejects advanced mask blending before drawing. See [alpha masks](../components/canvas-rendering.md#canvas-alpha-masks).

Texture cache entries retain both source snapshot identity and its uploaded revision. A prepared in-place video publication triggers another upload without changing the Texture or native allocation.

CreateTarget accepts the common HDR argument but rejects true explicitly. Normal byte rendering multiplies each batch's separate canvas color into its borrowed source vertices without retaining a preceding mask/color pass factor. Built-in group/Only-mask owners are unshaded, and AndDraw shares geometry while retaining separate pass factors. Native GPU/Compatibility modulation profiles verify RGB/alpha and warm allocation behavior.

`internal override void Present(ReadOnlySpan<CanvasPresentation> viewports)` composes completed native images at clipped client rectangles after drawing, replacing destination RGBA and clearing uncovered pixels black. The shared contract declares this operation abstract. An empty list clears the implicit zero-raster root presentation. ReadbackScreen returns the latest completed composition independently of source texture updates. See [screen attachment](../components/canvas-rendering.md#viewport-screen-attachment).

The [independent native window path](../components/window-runtime.md#independent-native-windows) retains one scene renderer and display service. GPU surfaces share the authoritative device/targets; compatibility surfaces use the main driver and replay frozen submissions after cold history import. Native window IDs expire on child hide/detachment; viewport resources survive hiding.

## Transparent native presentation

Native compatibility windows capture their transparency role when creating the renderer. Fitted presentation margins clear to transparent for that role, and viewport/presentation copies preserve alpha. Ordinary opaque windows keep black opaque margins. NativePopupWindowTests verifies fitted content and margin alpha through actual native presentation readback; desktop compositor screenshot acceptance remains separate.

[Loading-image presentation](../components/boot-images.md) reuses ordinary color targets and native presentation without mutating scene viewport targets. ReleaseTemporaryTexture removes a cold loading image from the native image cache immediately after submission, including failed attempts; no caller-owned image resource is disposed.
