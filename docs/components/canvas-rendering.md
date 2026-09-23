# Canvas rendering

Last updated: 2026-09-23

## Scope and owned types

The component owns [RenderingServer](../classes/RenderingServer.md) and its internal GPU and compatibility backends. [CanvasItem](../classes/CanvasItem.md) records rectangle, line and texture commands; [Sprite](../classes/Sprite.md) supplies the ready-to-use texture/frame/region node; [AnimatedSprite](../classes/AnimatedSprite.md) supplies timed playback and consumes [SpriteFrames](../classes/SpriteFrames.md) and [SpriteFrames.LoopMode](../classes/SpriteFrames.LoopMode.md) from Resources; [Texture](../classes/Texture.md) and [shader materials](shader-materials.md) provide borrowed resources. Engine.Run owns the root Window and the renderer lifetime. This is an executable part of the rendering vertical slice, with broader API coverage still incomplete.

## Runtime flow

1. Open the backend selected by typed ProjectSettings. GPU initialization can fall back only when enabled; shader-dependent draws fail on compatibility.
2. Deliver FramePreDraw and capture visible nodes. For pending recording, clear commands/reset the draw transform, then deliver NotificationDraw, synchronous Draw handlers and OnDraw in order. All three may draw. QueueRedraw inside this recording coalesces; internal resource changes remain pending for the next frame. Failure clears partial commands, retains the dirty flag and closes the recording scope before host cleanup.
3. Recapture after drawing callbacks. Resolve canvas roots in scene order, behind-parent subtrees and nested local Y groups; sort globally by effective Z, preserving the resolved order at equal Z. Transform retained local geometry into framebuffer pixels. Inherited Modulate and local SelfModulate multiply command colors.
4. Batch adjacent commands only when material, texture, filter, repeat and anisotropy limit match. Resolve current immutable pixel snapshots and preflight native resources before clearing/drawing. Pixel updates do not require OnDraw.
5. Upload and submit to the RGBA8 target, copy it to the native window and deliver FramePostDraw. Submission is not display completion. Callback failures trigger Engine.Run cleanup.

## Drawing contract

Filled rectangles, centered outlines and flat-cap lines support local widths, one-pixel negative widths and optional one-pixel antialias fringes. Zero-area or zero-width geometry draws nothing. Nonfinite geometry, transforms or resulting colors fail explicitly.

Texture drawing stretches, repeats or selects a source region. Negative destination sizes flip without relocating the origin; negative source sizes toggle the corresponding flip. Transpose exchanges UV axes and destination dimensions. Canvas and viewport sampling policies select filtering and addressing as described below. Source clipping clamps half-texel borders while preserving interior interpolation. Texture size overrides affect coordinates at command recording; later pixel replacement changes the sampled image without rewriting geometry.

GPU consumes vertex position/color/UV and the imported fragment interface. Built-in TEXTURE is supplied per command, using white for untextured geometry. Compatibility rejects arbitrary shaders, uploads the base mip level and checks unsupported high-precision formats and repeat capabilities. The software driver receives textured triangles separately because SDL 3.4.16's rectangle shortcut loses transposed and constant UVs; native vendored code remains unchanged.

Sprite borrows its texture, records through the texture's virtual region draw method and rebuilds on frame, region, layout or texture changes. Its resource notification callback only marks an atomic redraw request; it cannot run scene code on a worker. CanvasItem consumes that request atomically before OnDraw, retaining notifications that arrive during recording for the next frame. ItemRectChanged is a separate synchronous geometry event: Sprite delivers it after texture identity/centering/offset/active-region/frame/grid changes, with the exact order in its [class contract](../classes/Sprite.md#itemrectchanged). Resource content notifications and Entity transforms do not emit it. Hidden and detached items still deliver; callback failures retain committed geometry and redraw.

## Pixel snapping

Viewport.SnapTransformsToPixel and SnapVerticesToPixel are independent stored flags, false by default. The explicit root Window samples their active ProjectSettings overrides at construction; caller property assignments take precedence. Settings must be loaded before constructing that window. Existing viewports do not track later project changes.

Transform snapping rounds local and accumulated parent origins with floor(value + 0.5) before each normal hierarchy composition. Local basis/rotation/scale remain unchanged. Flattened Y groups compose snapped local origins within the group's coordinate system before sorting; they do not insert extra parent-origin rounding within the flattened chain. Neutral parents and TopLevel retain independent canvas roots. Public node transforms, input coordinates and processing remain fractional.

Vertex snapping rounds final primitive corners after node/draw/framebuffer transforms. The shared geometry path supplies both backends and fragment materials. Texture clipping cells are split on the original diagonal and interpolated within the two snapped triangles; artificial subdivision vertices are not rounded again. This preserves continuous interpolation even when rounding makes a quad non-parallelogram. Texture UVs use the 0.00001 precision adjustment before region clipping. Collapsed triangles draw nothing. Untextured strokes and antialiasing fringes round after tessellation.

Attached Sprite.GetRect, IsPixelOpaque and OnDraw use the rounded local offset under transform snapping. Queries read the current flag, while retained commands keep the offset from their last recording. A viewport flag change does not QueueRedraw or emit ItemRectChanged; request sprite redraw to refresh that recorded offset. Detached sprites have no active viewport offset policy. Vertex snapping does not affect those local queries.

The pinned SDL [software triangle input](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_render_sw.c) truncates fractional vertex coordinates before rasterization. Transform snapping can still leave fractional vertices after scale/rotation, so its edge pixels can differ from hardware. Native checks record that difference and verify the common integer boundary when vertex snapping is also enabled. This is a fallback precision limit under ADR 0028, not identical rasterization.

The audit follows the pinned [canvas culling source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_canvas_cull.cpp), [canvas vertex shader](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/renderer_rd/shaders/canvas.glsl), [Sprite source](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/sprite_2d.cpp) and Viewport properties. The root project-setting initialization maps to explicit Window construction under the existing typed host lifecycle.

[CanvasPixelSnapTests](../../tests/Electron2D.Tests/CanvasPixelSnapTests.cs) verifies defaults, positive/negative ties, queries versus retained commands, descriptor packing, active project overrides, owner/disposal guards, clipping geometry and warmed allocations. [CanvasPixelSnapRenderingTests](../../tests/Electron2D.Tests/CanvasPixelSnapRenderingTests.cs) exercises both flags independently/together, local versus accumulated translation, neutral/TopLevel boundaries, snapped Y ties, nested scaled Y groups, clipped textures and HLSL/GLSL. Rendering allocation checks also run with both policies enabled. GUI control snapping, offscreen/nested viewport rendering and interpolation remain separate incomplete capabilities. Native checks passed on Linux Wayland GPU/compatibility (including HLSL/GLSL) and dummy/software. Both snapping modes retain zero managed allocations in warmed frame submission. Other platforms, visual owner acceptance and a fresh self-contained publish were not checked in this slice.

## Texture sampling

CanvasItem.TextureFilter and TextureRepeat default to ParentNode. A direct canvas parent supplies the inherited policy; a neutral Node or TopLevel breaks that chain. At submission the containing Viewport supplies unresolved defaults (linear/clamp initially). `DrawTextureRect(..., tile: true)` always selects ordinary repeat for that command, even under a mirrored policy. Separate batches retain the resolved filter, repeat and anisotropy limit, so objects sharing a Texture may use different policies in one frame.

Actual canvas-property changes update inheriting canvas descendants, queue redraw under the normal attached/coalescing rules and then raise PropertyListChanged on the changed item. Overrides stop propagation; neutral nodes stop canvas propagation. Viewport defaults are resolved during submission for independent canvas roots. Entry/reparent/TopLevel refresh inherited state; detached policy writes become effective upon entry. Hidden items retain redraw requests. State remains committed if a property-list callback throws. Thread, capture and disposal guards apply before mutation.

Viewport.CanvasItemDefaultTextureFilter defaults to Linear and CanvasItemDefaultTextureRepeat to Disabled. Their ParentNode choice inherits a direct canvas/viewport parent or falls back to those defaults. The current public runtime supports only a root Window; nested/offscreen viewport activation remains unavailable. Viewport.AnisotropicFilteringLevel starts from the active ProjectSettings anisotropy override, normally four samples. The limit affects only anisotropic canvas modes and can change between frames. Disabled preserves ordinary mipmap filtering.

GPU uses all stored image mip levels for mipmap modes and restricts Nearest/Linear to level zero. Images without mipmaps use their sole level. Canvas mip interpolation is linear by default; ProjectSettings.UseNearestMipmapFilter changes it to nearest at GPU-renderer startup. Canvas sampler settings also apply to the reserved TEXTURE binding in HLSL and GLSL materials. Other named material texture parameters retain the existing fixed nearest-texel/nearest-mip/clamp profile, including explicit shader LOD sampling.

| Sampling capability | GPU | Hardware compatibility | Software compatibility |
| --- | --- | --- | --- |
| Nearest base-level sampling | Yes | Yes | Yes |
| Linear base-level sampling | Yes | Yes | Explicitly rejected |
| Stored mipmaps / anisotropy | Yes | Explicitly rejected | Explicitly rejected |
| Clamp / ordinary repeat | Yes | Yes, subject to wrapping support | Yes, subject to wrapping support |
| Mirrored repeat | Yes | Explicitly rejected | Explicitly rejected |

SDL_Renderer's [address modes](https://wiki.libsdl.org/SDL3/SDL_TextureAddressMode) do not include mirrored repeat. Its pinned [software triangle path](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_triangle.c) does not implement linear filtering; accepting the scale-mode setter alone is not proof of filtered output. The engine rejects that unsupported request before clearing/drawing. Software consumers must explicitly choose nearest for texture drawing. Non-power-of-two repeat still requires the driver's wrapping capability. No backend silently downgrades an unsupported sampling choice.

Sampler caches belong to the GPU backend, are bounded by the enum/anisotropy combinations and are disposed with it. Texture updates and replacements preserve each command's sampling choice. The warmed resolution, batch construction and submission paths allocate no managed memory.


## Canvas lifecycle

Actual SceneTree entry attaches each canvas parent-first and notifies visible entry; exit detaches child-first without visibility/Hidden signals. TopLevel changes rebind the item with an exit/entry pair and schedule redraw. Local Visible changes always notify the item; effective changes propagate through locally visible direct canvas children, including TopLevel, with Hidden following visibility delivery when becoming hidden. Neutral nodes break inheritance and Window visibility reaches every canvas root. Showing and reattachment request fresh recording. C# notification overrides call base for inherited event dispatch; manual tree notifications do not alter membership.

## Canvas ordering

TopLevel items and items below neutral Node parents are independent canvas roots. A root's canvas subtree precedes the next root at equal effective Z. ShowBehindParent draws a child subtree before its parent. YSortEnabled orders the item itself at Y = 0 and direct canvas children by local Y; nested enabled children join that group, while disabled children keep their subtree together at their own Y. Approximate Y ties keep scene order using the shared Mathf contract. Invisible items are omitted, neutral/TopLevel boundaries end the group, and Z takes precedence everywhere. Changes use the next submission without rerecording retained commands and do not reorder processing/input.

## Ownership and limits

Nodes borrow materials and textures; native texture caches belong to the backend. Updates reuse compatible allocations; replacement recreates them. Unused cached resources are released, and shutdown releases all backend state. A disposed or unreadable texture fails when its retained drawing is consumed. A custom Texture may override drawing with ordinary CanvasItem geometry instead of providing an image.

Current framebuffer and blending precision is RGBA8. GPU samples byte and supported floating-point images, including stored mips. Compatibility support depends on the native driver; the tested drivers reject float textures explicitly. The component has no lights, clipping hierarchy, polygon/mesh API, public offscreen targets, GUI drawing, independent window renderers or device-loss recovery. Other targets remain unverified under [ADR 0021](../decisions/product.md#adr-0021).

## Sampling verification

[CanvasSamplingTests](../../tests/Electron2D.Tests/CanvasSamplingTests.cs) covers inheritance, neutral/TopLevel/reparent boundaries, detached cache behavior, invalid values, callback failure, owner-thread/disposal guards, PackedScene and project initialization. [CanvasSamplingRenderingTests](../../tests/Electron2D.Tests/CanvasSamplingRenderingTests.cs) checks consecutive frames with shared textures and distinct filters, live inheritance/repeat changes, resource updates, both imported shader languages, all six concrete GPU filter modes, base-level clamping, nearest/linear mip interpolation, and increased stripe contrast across anisotropy limits. Compatibility tests reject unsupported modes before FramePostDraw. Legacy pixel-art fixtures now explicitly select nearest.

The initial native audit caught and corrected a regression in explicit material LOD sampling; the fixed material sampler keeps its earlier mip range and interpolation. It also exposed software linear filtering silently acting as nearest, now converted to an explicit capability failure. Native results and precision apply to tested Linux Wayland GPU/compatibility and dummy/software only; no new self-contained publish or visual owner acceptance is claimed.

## Verification

The ItemRectChanged integration passes managed Sprite event checks. Its full Wayland rendering run passed on an unchanged retry after the first run failed while Vulkan queried surface formats for a new window during the allocation check. This records a native initialization failure, not an event-test failure; the passing retry does not prove initialization is free of intermittent failures.

[CanvasLifecycleTests](../../tests/Electron2D.Tests/CanvasLifecycleTests.cs) checks activation, reattachment, TopLevel rebinding, visibility propagation, Hidden, manual notifications, callback failure continuation and recording recovery. Native six-frame readback sequences verify drawing in the notification, event and override, redraw on showing/rebinding/reattachment and coalescing inside Draw. The suite passes on Wayland GPU/compatibility and dummy/software.

[RenderingRuntimeTests](../../tests/Electron2D.Tests/RenderingRuntimeTests.cs), [RenderingTextureTests](../../tests/Electron2D.Tests/RenderingTextureTests.cs) and [CanvasTextureTests](../../tests/Electron2D.Tests/CanvasTextureTests.cs) check native framebuffer pixels, retained redraw behavior, transforms/Z/modulation, failure cleanup, texture regions/flips/transpose/repeat, Update/SetImage, virtual overrides, disposed resources and both imported languages. UV readback checks the half-texel clipping boundary and interior interpolation. Warmed texture geometry replay allocates zero managed bytes in 1,000 iterations.

A second allocation check measures the complete interval from FramePreDraw to FramePostDraw, including both tree captures, stable Z sorting, nested independent Y groups, retained geometry, resource preparation and native submission. Eight mixed texture/geometry nodes, including HLSL and GLSL materials on GPU, run twenty warmup frames and twenty measured frames. The IComparer range-sorting overload allocated 64 managed bytes per Y group per frame; sorting a Span range with a cached static comparison removes those allocations. The measured interval now allocates zero managed bytes on Linux Wayland GPU/Vulkan, compatibility and dummy/software.

[CanvasOrderingTests](../../tests/Electron2D.Tests/CanvasOrderingTests.cs) passes 31 pixel cases per backend: behind-parent subtrees, Z precedence/clamping, root ordering, visibility boundaries, local/nested Y groups, approximate ties and drawing-callback mutations. Restoring the previous renderer makes the behind-parent assertion fail. Linux Wayland GPU/Vulkan and compatibility runs passed, plus dummy/software rendering. This is local native and pixel verification; it does not establish visual owner acceptance, other platforms, allocations outside the measured rendering interval or frame-time guarantees. [ADR 0028](../decisions/rendering.md#adr-0028) and the [coverage roadmap](../coverage/roadmap.md) retain the remaining requirements.

The self-contained linux-x64 test publish also passed the complete canvas texture sequence on Wayland and dummy/software from `/tmp` with `LD_LIBRARY_PATH` unset. The publish includes the updated embedded vertex/fragment programs and both imported canvas fixtures. This verifies package delivery for these operations, not AOT or other platforms.

The compatibility backend captures borrowed graphics identities immediately after creating its SDL renderer, checking the current context belongs to the owned window. On Linux EGL it validates the display/config against that context; on X11 GLX it resolves the exact framebuffer config and visual by native context config ID. DisplayServer exposes these through WindowGetNativeHandle while the renderer lives. Queries use the captured identities without changing current context or graphics state. GPU/software expose no GL identity. Tests in RenderingNativeHandleTests verify native identities, foreign context/config isolation, pixel readback, cleanup and reopen on Wayland EGL and XWayland GLX. Other platforms and X11/EGL creation remain unverified.

The same graphics-handle checks pass in a self-contained linux-x64 test publish launched from `/tmp` with `LD_LIBRARY_PATH` unset and `PATH=/usr/bin:/bin`. The host GTK icon loader needs ordinary system utilities such as `bwrap`; an empty PATH aborted GTK before the handle checks. This is a desktop environment requirement, not a compiler or development-runtime dependency.

[SpriteTests](../../tests/Electron2D.Tests/SpriteTests.cs) verifies the texture-backed scene node's properties, events, grid changes, opacity, PackedScene storage/ownership and atomic redraw from worker notifications. [SpriteRenderingTests](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) passed twelve successive image/flip/frame/region/layout/update/replacement/visibility/clear frames on Linux Wayland GPU/Vulkan and compatibility, and dummy/software; GPU also passed with the imported GLSL material. This is native pixel verification, without separate visual owner acceptance.

The self-contained linux-x64 publish also passed the Sprite managed checks and these complete rendering sequences from `/tmp`, with `LD_LIBRARY_PATH` unset and `PATH=/usr/bin:/bin`. No development shader compiler or `dotnet` host was used to run that package.

### Atlas views

[AtlasTexture](../classes/AtlasTexture.md) maps a borrowed source region and margins before recording the underlying texture command. Nested views compose those mappings. Sprite listens to view changes and records fresh geometry; custom CanvasItem commands require QueueRedraw after region/margin changes. Source pixel updates remain visible through retained commands. AtlasTexture intentionally ignores DrawRect tiling and controls sampling clipping through FilterClip. AtlasTextureTests verifies both backends and HLSL/GLSL canvas shaders.

The pinned SDL software triangle input truncates source UVs to integer texels as well as destination vertices. Half-texel clipping boundaries can therefore shift the boundary between adjacent atlas colors: AtlasTextureTests explicitly checks the software/hardware difference and the shared edge sample. This is a fallback precision limit under ADR 0028; it does not promise identical nearest-sampling pixels across drivers. See [SDL software geometry input](https://github.com/libsdl-org/SDL/blob/release-3.4.16/src/render/software/SDL_render_sw.c).

## Animated frame playback

AnimatedSprite is a direct Entity subclass and a Sprite sibling. SpriteFrames serializes named frame/rate/loop data and owns independent frame containers while borrowing textures. Existing Resource duplication supplies shallow/deep copies and packed-scene local ownership. It does not forward contained texture events. Frame edits emit Changed; name/rate/loop edits and ClearAll remain silent.

Internal idle notifications advance playback under normal tree pause/process mode. Worker library edits request atomic reconciliation on the owner thread, keeping scene notifications off workers. Retained frame drawing uses virtual Texture.DrawRectRegion, including AtlasTexture, and the existing canvas material/sampling/visibility path. Event ordering, boundary timing, endpoint retention and iteration limits are documented in [AnimatedSprite](../classes/AnimatedSprite.md#timing-contract-and-source-audit); no alternative clock or rendering path is introduced.

AnimatedSpriteTests verifies resource/state/scene/callback contracts. AnimatedSpriteRenderingTests verifies native texture, atlas and blank-frame readback, timed host completion, worker edits and callback-failure cleanup with Wayland compatibility/GPU, HLSL/GLSL, and dummy/software. Other platform and owner visual/performance acceptance remain outside this evidence.

Scene [Path/PathFollow](scene-paths.md) consumers update the inherited Entity transform and require no special draw commands. Global transform notification traversal rents isolated child snapshots from the standard shared array pool and clears them in finally; warm descendant movement avoids the former per-update array allocation. PathTests covers this allocation boundary and PathRenderingTests covers descendant movement, worker curve changes and visibility on Wayland GPU/compatibility and dummy/software.
