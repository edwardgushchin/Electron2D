# Canvas rendering

Last updated: 2026-09-22

## Scope and owned types

The component owns [RenderingServer](../classes/RenderingServer.md) and its internal GPU and compatibility backends. [Node](../classes/Node.md) records rectangle, line and texture commands; [Sprite](../classes/Sprite.md) supplies the ready-to-use texture/frame/region node; [Texture](../classes/Texture.md) and [shader materials](shader-materials.md) provide borrowed resources. Engine.Run owns the root Window and the renderer lifetime. This is an executable part of the rendering vertical slice, with broader API coverage still incomplete.

## Runtime flow

1. Open the backend selected by typed ProjectSettings. GPU initialization can fall back only when enabled; shader-dependent draws fail on compatibility.
2. Deliver FramePreDraw, capture visible nodes and invoke OnDraw when first visible or after QueueRedraw. Clear the command list and reset draw transform before that callback. Redraw requests inside the callback schedule a later frame.
3. Recheck node membership and visibility, sort by effective Z with stable scene order, and transform retained local geometry into framebuffer pixels. Inherited Modulate and local SelfModulate multiply command colors.
4. Batch adjacent commands only when material, texture and repeat mode match. Resolve current immutable pixel snapshots and preflight native resources before clearing/drawing. Pixel updates do not require OnDraw.
5. Upload and submit to the RGBA8 target, copy it to the native window and deliver FramePostDraw. Submission is not display completion. Callback failures trigger Engine.Run cleanup.

## Drawing contract

Filled rectangles, centered outlines and flat-cap lines support local widths, one-pixel negative widths and optional one-pixel antialias fringes. Zero-area or zero-width geometry draws nothing. Nonfinite geometry, transforms or resulting colors fail explicitly.

Texture drawing stretches, repeats or selects a source region. Negative destination sizes flip without relocating the origin; negative source sizes toggle the corresponding flip. Transpose exchanges UV axes and destination dimensions. Nearest sampling is fixed in this integration. Source clipping clamps half-texel borders while preserving interior interpolation. Texture size overrides affect coordinates at command recording; later pixel replacement changes the sampled image without rewriting geometry.

GPU consumes vertex position/color/UV and the imported fragment interface. Built-in TEXTURE is supplied per command, using white for untextured geometry. Compatibility rejects arbitrary shaders, uploads the base mip level and checks unsupported high-precision formats and repeat capabilities. The software driver receives textured triangles separately because SDL 3.4.16's rectangle shortcut loses transposed and constant UVs; native vendored code remains unchanged.

Sprite borrows its texture, records through the texture's virtual region draw method and rebuilds on frame, region, layout or texture changes. Its resource notification callback only marks an atomic redraw request; it cannot run scene code on a worker. Node consumes that request atomically before OnDraw, retaining notifications that arrive during recording for the next frame.

## Ownership and limits

Nodes borrow materials and textures; native texture caches belong to the backend. Updates reuse compatible allocations; replacement recreates them. Unused cached resources are released, and shutdown releases all backend state. A disposed or unreadable texture fails when its retained drawing is consumed. A custom Texture may override drawing with ordinary Node geometry instead of providing an image.

Current framebuffer and blending precision is RGBA8. GPU samples byte and supported floating-point images, including stored mips. Compatibility support depends on the native driver; the tested drivers reject float textures explicitly. The component has no lights, clipping hierarchy, polygon/mesh API, public offscreen targets, GUI drawing, independent window renderers or device-loss recovery. Other targets remain unverified under [ADR 0021](../decisions/product.md#adr-0021).

## Verification

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) check native framebuffer pixels, retained redraw behavior, transforms/Z/modulation, failure cleanup, texture regions/flips/transpose/repeat, Update/SetImage, virtual overrides, disposed resources and both imported languages. UV readback checks the half-texel clipping boundary and interior interpolation. Warmed texture geometry replay allocates zero managed bytes in 1,000 iterations.

A second allocation check measures the complete interval from FramePreDraw to FramePostDraw, including capture, stable Z sorting, retained geometry, resource preparation and native submission. Eight mixed texture/geometry nodes, including HLSL and GLSL materials on GPU, run twenty warmup frames and twenty measured frames. It exposed 64 managed bytes per frame in the IComparer sorting overload; a cached static comparison removes that allocation. The measured interval now allocates zero managed bytes on Linux Wayland GPU/Vulkan, compatibility and dummy/software.

Linux Wayland GPU/Vulkan and compatibility runs passed, plus dummy/software rendering. This is local native and pixel verification; it does not establish visual owner acceptance, other platforms, allocations outside the measured rendering interval or frame-time guarantees. [ADR 0028](../decisions/rendering.md#adr-0028) and the [coverage roadmap](../coverage/roadmap.md) retain the remaining requirements.

The self-contained linux-x64 test publish also passed the complete canvas texture sequence on Wayland and dummy/software from `/tmp` with `LD_LIBRARY_PATH` unset. The publish includes the updated embedded vertex/fragment programs and both imported canvas fixtures. This verifies package delivery for these operations, not AOT or other platforms.

The compatibility backend captures borrowed graphics identities immediately after creating its SDL renderer, checking the current context belongs to the owned window. On Linux EGL it validates the display/config against that context; on X11 GLX it resolves the exact framebuffer config and visual by native context config ID. DisplayServer exposes these through WindowGetNativeHandle while the renderer lives. Queries use the captured identities without changing current context or graphics state. GPU/software expose no GL identity. Tests in RenderingNativeHandleTests verify native identities, foreign context/config isolation, pixel readback, cleanup and reopen on Wayland EGL and XWayland GLX. Other platforms and X11/EGL creation remain unverified.

The same graphics-handle checks pass in a self-contained linux-x64 test publish launched from `/tmp` with `LD_LIBRARY_PATH` unset and `PATH=/usr/bin:/bin`. The host GTK icon loader needs ordinary system utilities such as `bwrap`; an empty PATH aborted GTK before the handle checks. This is a desktop environment requirement, not a compiler or development-runtime dependency.

[SpriteTests](../../tests/Electron2D.Tests/SpriteTests.cs) verifies the texture-backed scene node's properties, events, grid changes, opacity, PackedScene storage/ownership and atomic redraw from worker notifications. [SpriteRenderingTests](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) passed twelve successive image/flip/frame/region/layout/update/replacement/visibility/clear frames on Linux Wayland GPU/Vulkan and compatibility, and dummy/software; GPU also passed with the imported GLSL material. This is native pixel verification, without separate visual owner acceptance.

The self-contained linux-x64 publish also passed the Sprite managed checks and these complete rendering sequences from `/tmp`, with `LD_LIBRARY_PATH` unset and `PATH=/usr/bin:/bin`. No development shader compiler or `dotnet` host was used to run that package.
