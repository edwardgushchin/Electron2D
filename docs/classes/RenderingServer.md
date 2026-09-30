# RenderingServer

Last updated: 2026-10-01

- Declaration: `public sealed partial class RenderingServer : ElectronObject`
- Source: [RenderingServer.cs](../../src/Servers/Rendering/RenderingServer.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Renders the active root Window's retained CanvasItem commands. Engine.Run creates the service after acquiring the native window, drives it on the scene owner thread and closes it during cleanup. There is no public constructor or independent lifetime. Consumers draw through CanvasItem and Texture, including live texture RIDs; owned SDL handles remain internal. DisplayServer exposes supported borrowed native context identities under ADR 0042.

The service supports rectangles, lines, polygons, short primitives and textures using source-alpha blending into an RGBA8 framebuffer. Shader materials require GPU rendering. Startup settings select `gpu` or `compatibility` and whether GPU initialization may fall back. This does not implement live device migration or recovery.

CanvasLayer grouping precedes item Z/Y ordering. Default-canvas and layer roots use their own viewport transform; retained commands survive layer motion, camera following and order changes. [Parallax](Parallax.md) and [ParallaxLayer](ParallaxLayer.md) repeat descendant retained commands within that ordering without duplicating nodes or draw callbacks. Transform snapping prepares canvas translations separately from item translations. See [canvas layers](../components/canvas-rendering.md#canvas-layers).

When the scene tree enables physics interpolation, the renderer composes previous/current canvas-item transforms and the current camera's viewport transform with the Engine's fractional tick progress. Both GPU and compatibility backends receive the same prepared vertices. Logical node, viewport and input coordinates are never rewritten for rendering. A first tick or explicit reset presents the current pose.

Control descendant clipping adds a framebuffer scissor to each affected batch after order and transforms are resolved. Parent drawing stays outside that clip; top-level and neutral canvas boundaries break its inheritance. The same batch contract runs on GPU and compatibility. See [Control clipping](../components/canvas-rendering.md#control-descendant-clipping).

## Example

Inside a node's OnReady callback during Engine.Run:

```csharp
RenderingServer.Instance!.SetDefaultClearColor(new Color(0.1f, 0.1f, 0.15f));
```

## Constants

| Declaration | Contract |
| --- | --- |
| [`public const int CanvasLayerMin = int.MinValue`](#canvaslayermin) | Smallest layer index. |
| [`public const int CanvasLayerMax = int.MaxValue`](#canvaslayermax) | Largest layer index. |

### CanvasLayerMin

`public const int CanvasLayerMin = -2147483648`

Smallest accepted CanvasLayer.Layer value. It draws before every larger layer index, independently of per-item Z. This bound is inclusive.

### CanvasLayerMax

`public const int CanvasLayerMax = 2147483647`

Largest accepted CanvasLayer.Layer value. It draws after every smaller layer index, independently of per-item Z. This bound is inclusive; equal layers retain their independent canvas groups.

## API summary

| Declaration | Contract |
| --- | --- |
| `static RenderingServer? Instance { get; }` | Active service, or null outside its Engine.Run lifetime. |
| `bool RenderLoopEnabled { get; set; }` | Enables frame submission; initially true. |
| `string GetCurrentRenderingMethod()` | Actual `gpu` or `compatibility` method after fallback. |
| `string GetCurrentRenderingDriverName()` | Actual native driver name. |
| `Color GetDefaultClearColor()` | Current clear color. |
| `void SetDefaultClearColor(Color color)` | Sets a finite clear color for later frames. |
| [`public RID Texture2DCreate(Image image)`](#texture2dcreate) | Copies a live nonempty image, validates axes 1..16384 and supported sampling formats, and returns a renderer-owned identity. |
| [`public RID Texture2DPlaceholderCreate()`](#texture2dplaceholdercreate) | Creates a drawable 4×4 RGBA8 magenta/black checkerboard. |
| [`public Image Texture2DGet(RID texture)`](#texture2dget) | Returns a caller-owned copy of original backing pixels and mipmaps. |
| [`public void Texture2DUpdate(RID texture, Image image, int layer = 0)`](#texture2dupdate) | Copies and validates matching source width, height, format and mipmap state before publication. |
| [`public void TextureReplace(RID texture, RID byTexture)`](#texturereplace) | Both identities must be live and owned by this renderer. |
| [`public Image.Format TextureGetFormat(RID texture)`](#texturegetformat) | Reports original backing format, including the full atlas source format. |
| [`public void TextureSetSizeOverride(RID texture, int width, int height)`](#texturesetsizeoverride) | Sets both logical drawing axes in 1..16384 without reallocating or resampling pixels. |
| [`public void TextureSetPath(RID texture, string path)`](#texturesetpath) | Sets diagnostic metadata for a server-owned texture. |
| [`public string TextureGetPath(RID texture)`](#texturegetpath) | Returns owned diagnostic metadata or a borrowed resource’s ResourcePath. |
| [`public void FreeRID(RID rid)`](#freerid) | Removes a live server-owned texture RID, releases its managed payload and disposes the internal resource. |
| `event Action? FramePreDraw` | Before capture and command preparation. |
| `event Action? FramePostDraw` | After submitting the canvas frame. |

## Property descriptions

### Instance

An atomic reference, null before startup and after shutdown. Keeping the reference does not prolong its Engine.Run lifetime. Calls on a disposed service throw ObjectDisposedException.

### RenderLoopEnabled

False skips capture and submission while retaining existing commands and pending redraw requests. It does not stop input or scene processing. Both reading and writing require the scene owner thread.

## Method descriptions

### GetCurrentRenderingMethod

Reports the backend actually opened. A requested GPU method may report compatibility if the configured startup fallback succeeded. Requires the owner thread.

### GetCurrentRenderingDriverName

Reports the backend's native driver string. This is diagnostic information and does not grant access to native resources. Requires the owner thread.

### GetDefaultClearColor

Returns the current color, initialized from ProjectSettings.DefaultClearColor. Requires the owner thread.

### SetDefaultClearColor

Validates all channels before changing state. Nonfinite values throw ArgumentException without mutation. Normalized framebuffer channels clamp to zero through one; this is not an HDR framebuffer. Requires the owner thread.

### Texture2DCreate

`public RID Texture2DCreate(Image image)`

Copies a live nonempty image, validates axes 1..16384 and supported sampling formats, and returns a renderer-owned identity. Input disposal or later image edits do not affect the new texture. Compressed/integer formats fail explicitly. Creation/upload allocation is outside the warmed replay guarantee.

### Texture2DPlaceholderCreate

`public RID Texture2DPlaceholderCreate()`

Creates a drawable 4×4 RGBA8 magenta/black checkerboard. It supports the same update, size/path, replacement and free operations as an ordinary server texture.

### Texture2DGet

`public Image Texture2DGet(RID texture)`

Returns a caller-owned copy of original backing pixels and mipmaps. Atlas RIDs expose their full backing image, while object-based atlas drawing retains the view. Empty resource RIDs return the 4×4 rendering placeholder without initializing the resource or changing its own GetImage result. Readback uses the authoritative managed snapshot, not a GPU stall.

### Texture2DUpdate

`public void Texture2DUpdate(RID texture, Image image, int layer = 0)`

Copies and validates matching source width, height, format and mipmap state before publication. A failure preserves prior pixels. Logical size overrides do not change required source dimensions. Compatible updates retain the allocation token for backend reuse; retained commands sample new pixels without QueueRedraw. Only layer zero is integrated; nonzero layers throw ArgumentOutOfRangeException until the 2D array resource/renderer slice.

### TextureReplace

`public void TextureReplace(RID texture, RID byTexture)`

Both identities must be live and owned by this renderer. Transfers replacement pixels, logical dimensions and path to the destination object, preserving its RID and retained references. Different pixel configurations are allowed. Consumes byTexture; any commands referring to that consumed object stop drawing. Equal validated RIDs do nothing. Disposal callback errors propagate after publication and removal of the consumed identity; destination state remains committed.

### TextureGetFormat

`public Image.Format TextureGetFormat(RID texture)`

Reports original backing format, including the full atlas source format. Empty resource RIDs report RGBA8 for the rendering placeholder; the resource itself retains its empty metadata.

### TextureSetSizeOverride

`public void TextureSetSizeOverride(RID texture, int width, int height)`

Sets both logical drawing axes in 1..16384 without reallocating or resampling pixels. Unlike ImageTexture.SetSizeOverride, zero is invalid. Existing recorded geometry/normalized source coordinates remain fixed; QueueRedraw records the new logical size. Invalid dimensions leave state unchanged.

### TextureSetPath

`public void TextureSetPath(RID texture, string path)`

Sets diagnostic metadata for a server-owned texture. Null is rejected; empty and ordinary strings are accepted. It performs no file loading and does not register a ResourcePath cache entry.

### TextureGetPath

`public string TextureGetPath(RID texture)`

Returns owned diagnostic metadata or a borrowed resource’s ResourcePath. An empty path is valid.

### FreeRID

`public void FreeRID(RID rid)`

Removes a live server-owned texture RID, releases its managed payload and disposes the internal resource. Later retained commands referencing it draw nothing; a second free or lookup throws ArgumentException. Borrowed resource RIDs require resource disposal and throw InvalidOperationException here. Identity removal commits before disposal callbacks run. This method currently handles textures; canvas, material and shader RID ownership are separate incomplete families.

All texture instance methods require the active scene owner thread and a live renderer. Empty, stale and wrong-kind RIDs throw ArgumentException; mutation/free/replace of borrowed or foreign identities throw InvalidOperationException. Writes are allowed during scene callbacks, canvas recording and FramePreDraw/FramePostDraw, but rejected during geometry replay/native submission and shutdown. Getter calls remain available at frame events. Renderer shutdown drains every owned texture even if disposal callbacks fail, attempts backend cleanup independently, detaches Instance and then propagates collected errors. Borrowed resource identities survive renderer shutdown.

## Event descriptions

### FramePreDraw

Runs synchronously within the scene execution barrier after [AnimatedTexture](AnimatedTexture.md) resources advance and emit frame-change notifications, and before capturing visible nodes and delivering pending NotificationDraw, Draw and OnDraw recording stages. A failed stage aborts recording and clears its partial commands before propagating. A subscriber failure aborts the frame and propagates through Engine.Run cleanup. Frame and native-pump re-entry are rejected.

### FramePostDraw

Runs synchronously after submission. Submission does not prove GPU completion or compositor presentation. A subscriber failure propagates through Engine.Run cleanup. No event is delivered for a disabled or invisible root frame.

## Internal frame records

The private readonly `RenderEntry(CanvasItem Node, int Z, int Order, Transform Transform)` record borrows a CanvasItem, its effective Z index and resolved canvas order. A reused list sorts by Z first and that resolved order second. The private readonly `YSortEntry(CanvasItem Node, Transform Transform, int Order)` record holds a borrowed item, its transform in the current group's coordinate system (origin Y is the sorting key) and its original order for approximate ties. Nested independent groups append temporary ranges to a shared reused list; sorting its Span range uses a cached static comparison. No span survives recursive list growth. Both lists release borrowed references after submission or failure; they never dispose nodes.

## Lifecycle, errors and dependencies

Command preparation follows scene order. The renderer captures the tree again after drawing callbacks, so parenting and sibling-order changes affect this submission. Canvas roots (TopLevel items and items without direct canvas parents) retain scene order, with each root's canvas subtree resolved before the next root. Within a subtree, behind-parent children precede the parent and ordinary children follow. YSortEnabled flattens nested enabled groups in local coordinates; unsorted child subtrees stay grouped at their root's Y. Global Z sorting preserves this resolved order for equal Z. Visibility is checked before ordering. CanvasItem transforms and modulation apply to retained local commands each frame. Ordering carries the rendered transform through the hierarchy, rounding local and accumulated parent origins when enabled. Flattened Y groups round each local origin before composing their group transform, without inserting extra parent rounding inside that group. Final primitive vertex snapping happens during geometry replay. Public node transform queries retain fractional values. Shader capabilities, texture availability and backend format requirements are checked before drawing. Geometry and material resources remain borrowed.

Public instance methods and RenderLoopEnabled reject calls off the owner thread with InvalidOperationException. Calling inherited Dispose directly is rejected because Engine.Run owns teardown. The protected disposal overrides implement internal shutdown only; the sealed type has no user extension point. Native handles, buffers, textures and programs are released before detaching the service.

RenderingRuntimeTests measures zero managed allocation from FramePreDraw through FramePostDraw over twenty warmed frames with mixed geometry, textures and GPU HLSL/GLSL materials on native Wayland and dummy/software. The check includes both captures, stable Z sorting and nested Y groups; it excludes the host event loop, resource construction, readback and arbitrary user callbacks. It is not a frame-time bound.

Depends on Window, SceneTree, Node, CanvasItem, typed material/texture resources and the internal SDL3-CS backends. [Canvas rendering](../components/canvas-rendering.md) records native verification and current limits. Lights, general canvas masks, public offscreen targets, multiwindow rendering, device recovery and the full rendering API remain unfinished.

Root canvas replay starts with `framebufferScale * window.GetFinalTransform() * window.CanvasTransform`, then composes the existing canvas hierarchy/drawing transforms. Both compatibility and GPU paths, including custom materials, consume that same transform. Live viewport changes do not rerecord retained commands. See [canvas coordinates](../components/canvas-rendering.md#viewport-coordinates) for input/query semantics and verification.

## Canvas mask submission

Before ordering/traversing a canvas branch, RenderingServer independently tests CanvasItem.VisibilityLayer against the root Viewport.CanvasCullMask. Nested Y-sort collection prunes rejected intermediaries before flattening. Separate roots and CanvasLayer groups retain their normal ordering. Pending drawing still records, and zero masks still clear/present frames. Only submitted batches participate in shader capability rejection. See [mask semantics and native checks](../components/canvas-rendering.md#canvas-visibility-masks).

## Canvas render time

After FramePreDraw, RenderingServer advances its per-run clock by the captured scaled process step and wraps it by the active [RenderingTimeRolloverSeconds](ProjectSettings.md#renderingtimerolloverseconds). CanvasItem evaluates ordered interval/transform commands against that value each frame. Disabled rendering or an invisible root skips both submission and clock advancement. Tree pause leaves time advancing; TimeScale zero freezes it. The GPU backend sends the same clock to optional reserved TIME uniforms; see [shader render time](../components/shader-materials.md#render-time). See [canvas timing verification](../components/canvas-rendering.md#animation-intervals-and-rectangles).

Retained screen regions now sample the same actual render transforms, layer/mask/clip/repetition and inherited alpha as submitted canvases. All states commit before queued screen events; failures continue later nodes and membership epochs reject stale delivery. [VisibleOnScreenNotifier](../classes/VisibleOnScreenNotifier.md) and [VisibleOnScreenEnabler](../classes/VisibleOnScreenEnabler.md) provide the current runtime API. Both Linux Wayland backends and 64 warmed active neutral-target transitions are verified by [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs), under [ADR 0078](../decisions/rendering.md#adr-0078). Native allocations, other platforms, independent offscreen viewports and editor gizmo drawing remain outside this verification.

## Texture RID verification

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) checks stable resource identity before startup, independent duplicates, weak collection/sweeping, wrong-kind/stale/thread/disposal guards, placeholder pixels, copied input/output, update configuration rollback, logical size bounds, replacement/consumption, retained drawing and shutdown after callback failure. Native Linux Wayland GPU and compatibility pixel checks verify red → blue → green → freed destinations, atlas source identity/drawing and the checkerboard; dummy/software executes the same baseline. Each backend has 20 warmup frames, then 64 frames with RID lookup/redraw/submission and 64 retained frames with no managed allocation on the owner thread between FramePreDraw and FramePostDraw. This excludes the host event loop, resource creation/update/replacement, image copying/readback and native/backend allocations. Other platforms and owner visual acceptance remain unverified. Proxy/layered/drawable/native-device textures and material/shader/canvas server identities remain separately tracked in coverage.
