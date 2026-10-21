# RenderingServer

Last updated: 2026-10-21

- Declaration: `public sealed partial class RenderingServer : ElectronObject`
- Source: [RenderingServer.cs](../../src/Servers/Rendering/RenderingServer.cs), [RenderingServer.Threads.cs](../../src/Servers/Rendering/RenderingServer.Threads.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

Public static declarations are in [`RenderingServer.API.cs`](../../src/Servers/Rendering/RenderingServer.API.cs).


## ViewportSetUseDebanding

`public static void ViewportSetUseDebanding(RID viewport, bool enabled)` selects HDR screen dithering for a live scene or caller-owned viewport. The identity must belong to the active rendering session. Undefined/stale identities throw ArgumentException; foreign-session, off-owner, unavailable-service and active-capture/submission mutations throw InvalidOperationException.

The override preserves `Viewport.UseDebanding`; any valid scene assignment republishes authoring. It affects the next presentation without replacing native HDR storage. Only the root HDR viewport affects 2D screen output. Alpha, SDR/offscreen/linear images and movie capture retain their values. Compatibility rejects enabled debanding at target preparation. [HDR screen debanding](../components/hdr-canvas.md#screen-debanding) defines precision, defaults, persistence and verification.

## LCD and MSDF regions

CanvasItemAddLCDTextureRectRegion(RID, Rect2, RID, Rect2, Color) and CanvasItemAddMSDFTextureRectRegion(RID, Rect2, RID, Rect2, Color? = null, int outlineSize = 0, float pixelRange = 1, float scale = 1) append executable retained glyph regions to live borrowed or caller-owned items. They share scene rendering and owner/lifetime guards; [the complete glyph contract](../components/canvas-rendering.md#lcd-and-msdf-texture-regions) records sampling, blending, material order and backend limits.

## Retained canvas presentation

The static retained-item interpolation family now includes CanvasItemSetInterpolated, CanvasItemResetPhysicsInterpolation and CanvasItemTransformPhysicsInterpolation. Native item/child histories are tied to scene physics boundaries; reset is item-local and rebase updates both endpoints and canonical retained pose. Typed owner/finite/lifetime guards and source republishing preserve authored scene properties. See [contract and native verification](../components/canvas-rendering.md#retained-canvas-item-interpolation).

## Viewport MSAA

`ViewportSetMSAA2D(RID viewport, Viewport.MSAA mode)` selects per-viewport GPU geometry coverage independently of scene authoring and other attachments of a shared canvas. Valid scene MSAA2D assignments republish. The next submitted image prepares matching multisample targets and pipelines, preserving logical texture identity; invalid identities/modes and off-owner/capture mutations reject. Native capability and lifecycle details are in [canvas MSAA](../components/canvas-rendering.md#canvas-multisample-antialiasing).


## Viewport canvas configuration

`RenderingServer.ViewportSetCanvasCullMask`, `ViewportSetGlobalCanvasTransform`, `ViewportSetDefaultCanvasItemTextureFilter` and `ViewportSetDefaultCanvasItemTextureRepeat` configure the next submitted image of a live scene or caller-owned viewport. One shared canvas can therefore render with independent transforms, visibility masks and samplers in each attached viewport. The outer matrix applies after the canvas attachment matrix and before window stretch/pixel density, including default scene canvases and CanvasLayer canvases. Finite singular matrices are valid; zero masks hide all canvas items and bit 31 remains usable.

Renderer-session overrides preserve authored properties and input coordinates. Matching scene setters republish, including equal sampling values. Invalid sampler/matrix/identity values reject; unsupported native sampling retains explicit backend gates. See [canvas configuration behavior and verification](../components/canvas-rendering.md#viewport-canvas-configuration).

## Server-owned offscreen viewport targets

The static viewport family creates an inactive zero-size RID target without attaching a node to the user scene. `ViewportCreate`, `ViewportSetActive`, `ViewportSetSize`, `ViewportGetUpdateMode`, `ViewportSetUpdateMode`, `ViewportSetClearMode`, `ViewportSetTransparentBackground`, `ViewportSetDisable2D` and `ViewportSetParentViewport` share the existing native GPU/compatibility compositor. Attach an owned or borrowed canvas, record its items, set dimensions and activate the target. The existing `ViewportGetTexture` and `Texture2DGet` supply stable sampling identity and explicit copied pixel readback.

Defaults are Always clear, WhenVisible update, opaque background, canvas enabled and one image layer. Exact nonnegative dimensions smaller than two or zero layers suspend submission without consuming Once. Active=false and Disabled retain the last completed image; resize retires its allocation and pixels while keeping logical viewport/texture identities. Once policies transition only after successful draw. Never clear retains completed pixels. Disable2D submits only the clear/retention result. WhenVisible follows submitted texture dependencies; WhenParentVisible follows an independently selected active rendering parent, including raw RID parents. Sampling alone does not update a target whose rendering parent is detached, inactive or has suspended dimensions. Parenting rejects cycles and does not alter the authored hierarchy. Freeing a parent detaches its rendering children; freeing an owned viewport also disposes its texture view, removes canvas attachments and retires native images. Shutdown retires leaked owned identities. FreeRID rejects scene-owned viewport and texture identities.

Borrowed attached scene viewports use the same owner/session guards. SubViewport update, clear, transparency and size writes remain observable through their scene properties; low-level exact dimensions can be zero or one, while a subsequent ordinary Size write still clamps to two. Stretched containers continue to own their child size. Low-level Window target sizing does not resize its native window. Mutations are owner-only and reject canvas capture, native submission or shutdown.

The current backend profile implements one RGBA8 image layer. ViewportSetSize rejects counts above one explicitly; layered/multiview targets require layer-aware canvas targets, layer selection and matching texture/device exports and remain Partial in coverage. Native screen attachment, HDR, hardware MSAA and VRS remain separate capabilities. Current verification covers Linux Wayland GPU and hardware compatibility, real root/offscreen pixels, dependent canvases, clear/update/resize/parent policies, stale/session identities and prepared managed allocations. Other platforms, native allocator counts and human visual acceptance remain unverified.

## Borrowed skeletal identity

Skeleton.GetSkeleton supplies a weak scene palette identity consumed by Polygon skin replay. FreeRID rejects it on an active owner rather than disposing a scene node. Caller-created palette storage and actual scene-canvas attachment now execute through the mesh skin API below, with distinct borrowed/owned lifetime and replay callback guards. Caller-owned canvases and Mesh/MultiMesh command authoring now execute through the API below.

## ViewportSetUseHDR2D

`public static void ViewportSetUseHDR2D(RID viewport, bool enabled)` selects linear half-float output for a live scene or caller-owned viewport. The renderer-session override preserves the authored UseHDR2D getter; an equal scene assignment republishes it. Texture metadata/notifications commit immediately, while dependent native storage replaces on the next submission and expires old device aliases. Wrong/stale/foreign identities, off-owner access and capture/submission mutation reject. Compatibility and unsupported native capabilities reject enabled targets. See [HDR canvas targets](../components/hdr-canvas.md).

## Description

`public static bool IsAvailable { get; }` reports whether an active object is published. It is an observation, not a lifetime reservation; false means ordinary service calls and event subscription changes throw `InvalidOperationException`.

Public operations and events use static access to the retained object under [ADR 0095](../decisions/singleton-services.md#adr-0095). Object state, identity, property discovery and the owning domain lifetime rules remain intact.

Public static operations use the active native service. `IsAvailable` reports whether it is published; operations and event subscription changes throw `InvalidOperationException` when absent. Event subscriptions belong to that session and do not transfer to later sessions.

Renders the active root Window's retained CanvasItem commands. Engine.Run creates the service after acquiring the native window, drives it on the scene owner thread and closes it during cleanup. There is no public static constructor or independent lifetime. Consumers draw through CanvasItem and Texture, including live texture RIDs; owned SDL handles remain internal. DisplayServer exposes supported borrowed native context identities under ADR 0042.

The service supports rectangles, lines, polygons, short primitives and textures using source-alpha blending into an RGBA8 framebuffer. Shader materials require GPU rendering. Startup settings select `gpu` or `compatibility` and whether GPU initialization may fall back. This does not implement live device migration or recovery.

CanvasLayer grouping precedes item Z/Y ordering. Default-canvas and layer roots use their own viewport transform; retained commands survive layer motion, camera following and order changes. [Parallax](Parallax.md) and [ParallaxLayer](ParallaxLayer.md) repeat descendant retained commands within that ordering without duplicating nodes or draw callbacks. Transform snapping prepares canvas translations separately from item translations. See [canvas layers](../components/canvas-rendering.md#canvas-layers).

When the scene tree enables physics interpolation, the renderer composes previous/current canvas-item transforms and the current camera's viewport transform with the Engine's fractional tick progress. Both GPU and compatibility backends receive the same prepared vertices. Logical node, viewport and input coordinates are never rewritten for rendering. A first tick or explicit reset presents the current pose.

Control descendant clipping adds a framebuffer scissor to each affected batch after order and transforms are resolved. Parent drawing stays outside that clip; top-level and neutral canvas boundaries break its inheritance. The same batch contract runs on GPU and compatibility. See [Control clipping](../components/canvas-rendering.md#control-descendant-clipping).

## Retained device image bridge

GetRenderingDevice returns the active GPU renderer's retained device, or null when unavailable. Record owner-thread compute/image operations and end lists before canvas rendering; renderer submission and queued-read completion are automatic. TextureRDCreate returns an owned ordinary/array canvas RID borrowing a sampled global-device image. Free only the wrapper through RenderingServer.FreeRID. TextureGetRDTexture resolves nested ordinary/layered aliases into their borrowed source RID; already-sRGB storage is returned directly. A distinct sRGB view needs native view ownership. RenderingServer.TextureGetRDTexture exports actual renderer-managed ordinary uploads, independent arrays and completed viewport images to the retained device without CPU copying. Compatible authored updates keep the RID; replacing allocation, disposing the source, rebinding the viewport or retiring its target expires it. A viewport RID resolves the completed ping-pong image when each command is recorded. FreeRID releases only the borrowed wrapper; queued native operations retain the old allocation through completion. Device writes change GPU storage, while authored CPU snapshots remain unchanged; the next authored update overwrites device changes. Sampling, raw update/copy/read and supported storage roles reflect actual native allocation flags. Prepare changed uploads before starting a draw list. Empty storage, unrendered targets and the compatibility renderer return an empty RID. Distinct sRGB views still require native format-view ownership. See [the executed device bridge](../components/local-compute.md#device-images-in-canvas).

| Declaration | Contract |
| --- | --- |
| `public static RenderingDevice? GetRenderingDevice()` | Retained global device for the active GPU session; never caller-owned. |
| `public static RID TextureRDCreate(RID deviceTexture, TextureLayered.LayeredType layerType = TextureLayered.LayeredType.Array)` | Owns an ordinary or independent-array canvas wrapper, inferred from image allocation. |
| `public static RID TextureGetRDTexture(RID texture, bool SRGB = false)` | Borrowed global image of a canvas resource/alias; exports native uploads/arrays/completed viewports; empty for unavailable storage. |

## Example

Inside a node's OnReady callback during Engine.Run:

```csharp
RenderingServer.SetDefaultClearColor(new Color(0.1f, 0.1f, 0.15f));
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
| `static bool RenderLoopEnabled { get; set; }` | Enables frame submission; initially true. |
| `string GetCurrentRenderingMethod()` | Actual `gpu` or `compatibility` method after fallback. |
| `string GetCurrentRenderingDriverName()` | Actual native driver name. |
| `Color GetDefaultClearColor()` | Current clear color. |
| `void SetDefaultClearColor(Color color)` | Sets a finite clear color for later frames. |
| [`public static RID Texture2DCreate(Image image)`](#texture2dcreate) | Copies a live nonempty image, validates axes 1..16384 and supported sampling formats, and returns a renderer-owned identity. |
| [`public static RID Texture2DPlaceholderCreate()`](#texture2dplaceholdercreate) | Creates a drawable 4×4 RGBA8 magenta/black checkerboard. |
| [`public static Image? Texture2DGet(RID texture)`](#texture2dget) | Returns a caller-owned copy of original backing pixels and mipmaps, or null for a live proxy whose source has been released. |
| [`public static void Texture2DUpdate(RID texture, Image image, int layer = 0)`](#texture2dupdate) | Copies and validates matching source width, height, format and mipmap state before publication. |
| [`public static RID TextureProxyCreate(RID baseTexture)`](#textureproxycreate) | Owned alias of a borrowed source, including nested proxies. |
| [`public static void TextureProxyUpdate(RID texture, RID proxyTo)`](#textureproxyupdate) | Retargets an owned proxy to a non-proxy source. |
| [`public static void TextureReplace(RID texture, RID byTexture)`](#texturereplace) | Both identities must be live and owned by this renderer. |
| [`public static Image.Format TextureGetFormat(RID texture)`](#texturegetformat) | Reports original backing format, including the full atlas source format. |
| [`public static void TextureSetSizeOverride(RID texture, int width, int height)`](#texturesetsizeoverride) | Sets both logical drawing axes in 1..16384 without reallocating or resampling pixels. |
| [`public static void TextureSetPath(RID texture, string path)`](#texturesetpath) | Sets diagnostic metadata for a server-owned texture. |
| [`public static string TextureGetPath(RID texture)`](#texturegetpath) | Returns owned diagnostic metadata or a borrowed resource’s ResourcePath. |
| [`public static void FreeRID(RID rid)`](#freerid) | Removes a live server-owned texture RID, releases its managed payload and disposes the internal resource. |
| `static event Action? FramePreDraw` | Before capture and command preparation. |
| `static event Action? FramePostDraw` | After submitting the canvas frame. |

## Property descriptions

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

Validates all channels before changing state. Nonfinite values throw ArgumentException without mutation. Byte framebuffer channels clamp to zero through one. HDR viewports convert the authored clear color to linear RGB and store half-float values. Requires the owner thread.

### Texture2DCreate

`public static RID Texture2DCreate(Image image)`

Copies a live nonempty image, validates axes 1..16384 and supported sampling formats, and returns a renderer-owned identity. Input disposal or later image edits do not affect the new texture. Compressed/integer formats fail explicitly. Creation/upload allocation is outside the warmed replay guarantee.

### Texture2DPlaceholderCreate

`public static RID Texture2DPlaceholderCreate()`

Creates a drawable 4×4 RGBA8 magenta/black checkerboard. It supports the same update, size/path, replacement and free operations as an ordinary server texture.

### Texture2DGet

`public static Image? Texture2DGet(RID texture)`

Returns a caller-owned copy of original backing pixels and mipmaps, or null for a live proxy whose source has been released. Atlas RIDs expose their full backing image, while object-based atlas drawing retains the view. Empty resource RIDs return the 4×4 rendering placeholder without initializing the resource or changing its own GetImage result. Readback uses the authoritative managed snapshot, not a GPU stall.

### Texture2DUpdate

`public static void Texture2DUpdate(RID texture, Image image, int layer = 0)`

Copies and validates matching source width, height, format and mipmap state before publication. A failure preserves prior pixels. Logical size overrides do not change required source dimensions. Compatible updates retain the allocation token for backend reuse; retained commands sample new pixels without QueueRedraw. Only layer zero is integrated; nonzero layers throw ArgumentOutOfRangeException until the 2D array resource/renderer slice.

### TextureProxyCreate

`public static RID TextureProxyCreate(RID baseTexture)`

Creates a distinct owned RID aliasing a live borrowed or owned texture, including another proxy. No pixel copy or native texture allocation is created for the alias: canvas replay resolves the current source into ordinary backend batches. Nested resolution is iterative. The proxy itself remains alive if its source or an intermediate proxy is freed, disposed or collected; it then returns null image data and draws nothing. Its last format/size/path metadata remains available. Freeing an alias never frees its source. Empty ordinary resources supply their rendering checkerboard when sampled through a proxy. Wrong-kind/stale inputs reject before registration. The alias retains RID links, not ownership of borrowed source resources.

A partial snippet inside an active node’s OnReady callback (retain both RIDs in the node and draw the alias in OnDraw):

```csharp
using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
image.Fill(Colors.Red);
RID source = RenderingServer.Texture2DCreate(image);
RID alias = RenderingServer.TextureProxyCreate(source);
// Later, on the owner thread: RenderingServer.TextureProxyUpdate(alias, anotherSource);
// OnDraw: DrawTexture(alias, Vector2.Zero);
// Explicit cleanup before shutdown: RenderingServer.FreeRID(alias); RenderingServer.FreeRID(source);
```

### TextureProxyUpdate

`public static void TextureProxyUpdate(RID texture, RID proxyTo)`

Requires a renderer-owned proxy and a live non-proxy source. Rejects ordinary destinations, borrowed destinations, self/proxy targets and wrong-kind/stale identities before mutation. Source callbacks during metadata acquisition may throw; the old target remains unchanged. Retargeting keeps the proxy RID, updates its source, resets a proxy-local size override and copies the new diagnostic path. Future retained replay samples the current root image/format/size without OnDraw; recorded destination geometry and normalized source regions remain fixed. Source replacement redirects aliases before consuming the replacement RID. A disconnected proxy can be retargeted and resume drawing. Owner/submission/shutdown boundaries match texture mutations.

### TextureReplace

`public static void TextureReplace(RID texture, RID byTexture)`

Both identities must be live and owned by this renderer. Transfers replacement pixels, logical dimensions and path to the destination object, preserving its RID and retained references. Proxies of the consumed source redirect to the destination before source disposal; destination proxies continue sampling its replacement pixels. Proxy RIDs cannot be replacement operands. Different pixel configurations are allowed. Consumes byTexture; any commands referring to that consumed object stop drawing. Equal validated RIDs do nothing. Disposal callback errors propagate after publication and removal of the consumed identity; destination state remains committed.

### TextureGetFormat

`public static Image.Format TextureGetFormat(RID texture)`

Reports original backing format, including the full atlas source format. Empty resource RIDs report RGBA8 for the rendering placeholder; the resource itself retains its empty metadata.

### TextureSetSizeOverride

`public static void TextureSetSizeOverride(RID texture, int width, int height)`

Sets both logical drawing axes in 1..16384 without reallocating or resampling pixels. Unlike ImageTexture.SetSizeOverride, zero is invalid. Existing recorded geometry/normalized source coordinates remain fixed; QueueRedraw records the new logical size. Invalid dimensions leave state unchanged.

### TextureSetPath

`public static void TextureSetPath(RID texture, string path)`

Sets diagnostic metadata for a server-owned texture. Null is rejected; empty and ordinary strings are accepted. It performs no file loading and does not register a ResourcePath cache entry.

### TextureGetPath

`public static string TextureGetPath(RID texture)`

Returns owned diagnostic metadata or a borrowed resource’s ResourcePath. An empty path is valid.

### FreeRID

`public static void FreeRID(RID rid)`

Removes a live server-owned texture RID, releases its managed payload and disposes the internal resource. Later retained commands referencing it draw nothing; a second free or lookup throws ArgumentException. Borrowed resource RIDs require resource disposal and throw InvalidOperationException here. Identity removal commits before disposal callbacks run. It also releases caller-owned mesh, instance, palette, canvas and canvas-item identities. Canvas/item children survive independently and become drawable again after reparenting. Material and shader RID ownership retain separate prerequisites.

All texture operations require the active scene owner thread and a live renderer. Empty, stale and wrong-kind RIDs throw ArgumentException; mutation/free/replace of borrowed or foreign identities throw InvalidOperationException. Writes are allowed during scene callbacks, canvas recording and FramePreDraw/FramePostDraw, but rejected during geometry replay/native submission and shutdown. Getter calls remain available at frame events. Renderer shutdown drains every owned texture even if disposal callbacks fail, attempts backend cleanup independently, unpublishes the active service and then propagates collected errors. Borrowed resource identities survive renderer shutdown.

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

Depends on Window, SceneTree, Node, CanvasItem, typed material/texture resources and the internal SDL3-CS backends. [Canvas rendering](../components/canvas-rendering.md) records native verification and current limits. Lights, general canvas masks, remaining offscreen controls, native popup rendering, device recovery and the full rendering API remain unfinished.

Root canvas replay starts with `framebufferScale * window.GetFinalTransform() * window.CanvasTransform`, then composes the existing canvas hierarchy/drawing transforms. Both compatibility and GPU paths, including custom materials, consume that same transform. Live viewport changes do not rerecord retained commands. See [canvas coordinates](../components/canvas-rendering.md#viewport-coordinates) for input/query semantics and verification.

## Canvas mask submission

Before ordering/traversing a canvas branch, RenderingServer independently tests CanvasItem.VisibilityLayer against the selected Viewport.CanvasCullMask. Nested Y-sort collection prunes rejected intermediaries before flattening. Separate roots and CanvasLayer groups retain their normal ordering. Pending drawing still records, and zero masks still clear/present frames. Only submitted batches participate in shader capability rejection. See [mask semantics and native checks](../components/canvas-rendering.md#canvas-visibility-masks).

## Canvas render time

After FramePreDraw, RenderingServer advances its per-run clock by the captured scaled process step and wraps it by the active [RenderingTimeRolloverSeconds](ProjectSettings.md#renderingtimerolloverseconds). CanvasItem evaluates ordered interval/transform commands against that value each frame. Disabled rendering or an invisible root skips both submission and clock advancement. Tree pause leaves time advancing; TimeScale zero freezes it. The GPU backend sends the same clock to optional reserved TIME uniforms; see [shader render time](../components/shader-materials.md#render-time). See [canvas timing verification](../components/canvas-rendering.md#animation-intervals-and-rectangles).

Retained screen regions now sample the same actual render transforms, layer/mask/clip/repetition and inherited alpha as submitted canvases. All states commit before queued screen events; failures continue later nodes and membership epochs reject stale delivery. [VisibleOnScreenNotifier](../classes/VisibleOnScreenNotifier.md) and [VisibleOnScreenEnabler](../classes/VisibleOnScreenEnabler.md) provide the current runtime API. Both Linux Wayland backends and 64 warmed active neutral-target transitions are verified by [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs), under [ADR 0078](../decisions/rendering.md#adr-0078). Native allocations, other platforms, layered offscreen targets and editor gizmo drawing remain outside this verification; single-layer offscreen integration has separate SubViewportTests evidence.

## Texture RID verification

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) checks stable resource identity before startup, independent duplicates, weak collection/sweeping, wrong-kind/stale/thread/disposal guards, placeholder pixels, copied input/output, update configuration rollback, logical size bounds, replacement/consumption, retained drawing and shutdown after callback failure. Native Linux Wayland GPU and compatibility pixel checks verify red → blue → green → freed destinations, atlas source identity/drawing and the checkerboard; dummy/software executes the same baseline. Each backend has 20 warmup frames, then 64 frames with RID lookup/redraw/submission and 64 retained frames with no managed allocation on the owner thread between FramePreDraw and FramePostDraw. This excludes the host event loop, resource creation/update/replacement, image copying/readback and native/backend allocations. Other platforms and owner visual acceptance remain unverified. Layered/drawable/native-device textures and material/shader/canvas server identities remain separately tracked in coverage.

## Proxy verification and storage

[RenderingTextureProxyTests](../../tests/Electron2D.Tests/RenderingTextureProxyTests.cs) checks 256-level iterative chains and warm lookup, source edits/disposal, failed source callback rollback, owner/type/stale/proxy operand guards, nested aliases, source replacement redirection, disconnected images/draws, retarget recovery, empty-resource checkerboard and independent copied outputs. Native pixels run on Linux Wayland GPU/compatibility and dummy/software. After 20 warmup frames, 64 active source retarget/replay/submission frames and 64 unchanged retained frames allocate no managed bytes between FramePreDraw and FramePostDraw on the owner thread. Native source handles remain stable; cache checks show no per-alias texture storage. Owned source textures retain prepared cache allocations until free/shutdown, so alternating already prepared owned sources does not allocate another native texture. First use of a new source and changed pixel configuration may allocate. Backend-internal/native allocator totals, other platforms and owner acceptance remain unverified. Layered/external/device/drawable alias sampling enters those storage families’ first integration slices.

## Owned mesh identities

MeshCreate returns an owned empty ArrayMesh identity. MeshAddSurfaceFromArrays, MeshClear, MeshSurfaceRemove, MeshSurfaceUpdateVertexRegion/AttributeRegion and MeshSurfaceSetMaterial require this renderer's ownership before mutation. Resource-owned Mesh.GetRID is borrowed: getters and drawing resolve it, mutations/free reject it. MeshGetSurfaceCount, MeshSurfaceGetArrays, MeshSurfaceGetFormat/PrimitiveType/ArrayLen/ArrayIndexLen and MeshSurfaceGetMaterial expose typed current fields; queries return caller-owned arrays or borrowed materials. Packed stride helpers report eight-byte positions and present four-byte color and separate eight-byte primary/secondary UV attributes. Owned identities expire at FreeRID/shutdown; mesh cleanup and texture/backend cleanup are attempted separately. Further raw surface/deformation/LOD/extra-channel data remains Partial with exact dependencies. [Mesh component](../components/meshes.md) records runtime and current limits.

## Mesh methods

All methods require a live object; RenderingServer operations require the renderer owner thread. Server mutations additionally reject during submission or shutdown. Unknown/stale identities and surface indices reject before mutation; resource identities are borrowed and cannot be freed or mutated through the server.

| Complete signature | Contract |
| --- | --- |
| `public static System.Void MeshAddSurfaceFromArrays(Electron2D.RID mesh, Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.ArrayFormat flags = None)` | Adds copied typed surface channels to an owned mesh. |
| `public static System.Void MeshClear(Electron2D.RID mesh)` | Removes every surface from an owned mesh. |
| `public static Electron2D.RID MeshCreate()` | Creates an owned empty two-dimensional mesh. |
| `public static System.Int32 MeshGetSurfaceCount(Electron2D.RID mesh)` | Gets a live mesh's surface count. |
| `public static System.Int32 MeshSurfaceGetArrayIndexLen(Electron2D.RID mesh, System.Int32 surface)` | Gets the explicit surface index count. |
| `public static System.Int32 MeshSurfaceGetArrayLen(Electron2D.RID mesh, System.Int32 surface)` | Gets the surface vertex count. |
| `public static Electron2D.MeshSurfaceData MeshSurfaceGetArrays(Electron2D.RID mesh, System.Int32 surface)` | Gets copied typed channels from one mesh surface. |
| `public static Electron2D.Mesh.ArrayFormat MeshSurfaceGetFormat(Electron2D.RID mesh, System.Int32 surface)` | Gets a live surface's channel and policy mask. |
| `public static System.Int32 MeshSurfaceGetFormatAttributeStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)` | Gets the packed color/texture-coordinate stride for a supported format. |
| `public static System.Int32 MeshSurfaceGetFormatVertexStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)` | Gets the primary two-dimensional vertex stride for a supported surface format. |
| `public static Electron2D.Material MeshSurfaceGetMaterial(Electron2D.RID mesh, System.Int32 surface)` | Gets a borrowed material from a live surface. |
| `public static Electron2D.Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(Electron2D.RID mesh, System.Int32 surface)` | Gets one surface's primitive topology. |
| `public static System.Void MeshSurfaceRemove(Electron2D.RID mesh, System.Int32 surface)` | Removes one surface from an owned mesh. |
| `public static System.Void MeshSurfaceSetMaterial(Electron2D.RID mesh, System.Int32 surface, Electron2D.Material material)` | Assigns a borrowed surface material on an owned mesh. |
| `public static System.Void MeshSurfaceUpdateAttributeRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates packed attribute bytes, including partial records, in an owned mesh. |
| `public static System.Void MeshSurfaceUpdateVertexRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates packed position bytes, including partial records, in an owned mesh. |

## Mesh method descriptions

### MeshAddSurfaceFromArrays

`public static System.Void MeshAddSurfaceFromArrays(Electron2D.RID mesh, Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.ArrayFormat flags = None)`

Summary: Adds copied typed surface channels to an owned mesh.

mesh: Owned mesh identity.

primitive: Primitive topology.

arrays: Borrowed arrays copied before mutation.

flags: Supported two-dimensional update policies.

System.ArgumentException: The identity or geometry is invalid.

System.InvalidOperationException: The mesh is borrowed or owned by another renderer.


### MeshClear

`public static System.Void MeshClear(Electron2D.RID mesh)`

Summary: Removes every surface from an owned mesh.

mesh: Owned mesh identity.


### MeshCreate

`public static Electron2D.RID MeshCreate()`

Summary: Creates an owned empty two-dimensional mesh.

Returns: A logical mesh identity owned until FreeRID or renderer shutdown.

System.InvalidOperationException: The renderer is off-owner or submitting.


### MeshGetSurfaceCount

`public static System.Int32 MeshGetSurfaceCount(Electron2D.RID mesh)`

Summary: Gets a live mesh's surface count.

mesh: Borrowed or owned mesh identity.

Returns: The current surface count.


### MeshSurfaceGetArrayIndexLen

`public static System.Int32 MeshSurfaceGetArrayIndexLen(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets the explicit surface index count.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: Zero for sequential vertices.


### MeshSurfaceGetArrayLen

`public static System.Int32 MeshSurfaceGetArrayLen(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets the surface vertex count.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The vertex count.


### MeshSurfaceGetArrays

`public static Electron2D.MeshSurfaceData MeshSurfaceGetArrays(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets copied typed channels from one mesh surface.

mesh: Borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: Independent caller-owned channels.


### MeshSurfaceGetFormat

`public static Electron2D.Mesh.ArrayFormat MeshSurfaceGetFormat(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets a live surface's channel and policy mask.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The supported surface format.


### MeshSurfaceGetFormatAttributeStride

`public static System.Int32 MeshSurfaceGetFormatAttributeStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)`

Summary: Gets the packed color/texture-coordinate stride for a supported format.

format: Surface format mask.

vertexCount: Nonnegative surface vertex count.

Returns: Four bytes for color plus eight bytes per present primary or secondary UV channel.

System.NotSupportedException: The format requires unsupported channels or non-2D vertices.


### MeshSurfaceGetFormatVertexStride

`public static System.Int32 MeshSurfaceGetFormatVertexStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)`

Summary: Gets the primary two-dimensional vertex stride for a supported surface format.

format: Surface format mask.

vertexCount: Nonnegative surface vertex count.

Returns: Eight bytes for positions, zero when no vertex channel exists.

System.NotSupportedException: The format requires unsupported channels or non-2D vertices.


### MeshSurfaceGetMaterial

`public static Electron2D.Material MeshSurfaceGetMaterial(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets a borrowed material from a live surface.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The borrowed material or null.


### MeshSurfaceGetPrimitiveType

`public static Electron2D.Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets one surface's primitive topology.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The topology.


### MeshSurfaceRemove

`public static System.Void MeshSurfaceRemove(Electron2D.RID mesh, System.Int32 surface)`

Summary: Removes one surface from an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.


### MeshSurfaceSetMaterial

`public static System.Void MeshSurfaceSetMaterial(Electron2D.RID mesh, System.Int32 surface, Electron2D.Material material)`

Summary: Assigns a borrowed surface material on an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based surface index.

material: Borrowed live material or null.


### MeshSurfaceUpdateAttributeRegion

`public static System.Void MeshSurfaceUpdateAttributeRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates packed attribute bytes, including partial records, in an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.

offset: Byte offset into color/primary-UV/secondary-UV records.

data: Present RGBA8 UNORM bytes followed by present primary and secondary little-endian UV pairs.


### MeshSurfaceUpdateVertexRegion

`public static System.Void MeshSurfaceUpdateVertexRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates packed position bytes, including partial records, in an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.

offset: Byte offset into two-float positions.

data: Little-endian X/Y records.

## Instance integration

Created MultiMesh identities own instance storage until FreeRID or renderer shutdown and borrow child Mesh identities. Reads enforce renderer owner thread/lifetime and accept live owned or borrowed instance identities; mutations additionally enforce identity ownership. Resource RIDs may be drawn but cannot be mutated/freed through these owned methods. Fixed 2D allocation executes atomically; useIndirect=true creates native command storage on the GPU backend and rejects on Compatibility before mutation. Writable device identities follow the contract below. Canonical owned MeshCreate identities survive linked MultiMeshGetMesh queries.

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `public static System.Void MultiMeshAllocateData(Electron2D.RID multiMesh, System.Int32 instances, System.Boolean useColors = false, System.Boolean useCustomData = false, System.Boolean useIndirect = false)` | Allocates copied packed instance records for an owned resource. |
| `public static Electron2D.RID MultiMeshCreate()` | Creates owned empty two-dimensional instance storage. |
| `public static Electron2D.Rect2 MultiMeshGetAABB(Electron2D.RID multiMesh)` | Gets the two-dimensional local visibility rectangle of a live instance resource. |
| `public static System.Single[] MultiMeshGetBuffer(Electron2D.RID multiMesh)` | Returns copied packed instance records. |
| `public static Electron2D.Rect2 MultiMeshGetCustomAABB(Electron2D.RID multiMesh)` | Gets the authored manual local visibility rectangle. |
| `public static System.Int32 MultiMeshGetInstanceCount(Electron2D.RID multiMesh)` | Returns the allocated instance count. |
| `public static Electron2D.RID MultiMeshGetMesh(Electron2D.RID multiMesh)` | Gets the borrowed mesh identity used by an instance resource. |
| `public static System.Int32 MultiMeshGetVisibleInstances(Electron2D.RID multiMesh)` | Gets the stored visible-prefix policy. |
| `public static Electron2D.Color MultiMeshInstanceGetColor(Electron2D.RID multiMesh, System.Int32 index)` | Gets one stored instance color multiplier. |
| `public static Electron2D.Color MultiMeshInstanceGetCustomData(Electron2D.RID multiMesh, System.Int32 index)` | Gets one stored four-component shader value. |
| `public static Electron2D.Transform MultiMeshInstanceGetTransform2D(Electron2D.RID multiMesh, System.Int32 index)` | Gets one instance's current local transform. |
| `public static System.Void MultiMeshInstanceResetPhysicsInterpolation(Electron2D.RID multiMesh, System.Int32 index)` | Resets one owned instance's previous presentation record. |
| `public static System.Void MultiMeshInstanceSetColor(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color color)` | Sets one owned instance color multiplier. |
| `public static System.Void MultiMeshInstanceSetCustomData(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color customData)` | Sets one owned instance's raw shader components. |
| `public static System.Void MultiMeshInstanceSetTransform2D(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Transform transform)` | Sets one owned instance's finite local transform. |
| `public static System.Void MultiMeshInstancesResetPhysicsInterpolation(Electron2D.RID multiMesh)` | Resets all previous presentation records in owned storage. |
| `public static System.Void MultiMeshSetBuffer(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> buffer)` | Copies whole finite packed records into owned storage. |
| `public static System.Void MultiMeshSetBufferInterpolated(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)` | Copies explicit current and previous packed presentation records. |
| `public static System.Void MultiMeshSetCustomAABB(Electron2D.RID multiMesh, Electron2D.Rect2 aabb)` | Sets the manual visibility rectangle of owned instance storage. |
| `public static System.Void MultiMeshSetMesh(Electron2D.RID multiMesh, Electron2D.RID mesh)` | Assigns a borrowed mesh to owned instance storage. |
| `public static System.Void MultiMeshSetPhysicsInterpolated(Electron2D.RID multiMesh, System.Boolean interpolated)` | Enables or disables presentation interpolation of owned packed records. |
| `public static System.Void MultiMeshSetPhysicsInterpolationQuality(Electron2D.RID multiMesh, Electron2D.MultiMesh.PhysicsInterpolationQuality quality)` | Changes the basis interpolation quality of owned storage. |
| `public static System.Void MultiMeshSetVisibleInstances(Electron2D.RID multiMesh, System.Int32 visible)` | Changes the visible prefix of an owned resource without reallocating. |

## Methods and protected extension points descriptions

<a id="member-488a40ea3b4c"></a>
### MultiMeshAllocateData

`public static System.Void MultiMeshAllocateData(Electron2D.RID multiMesh, System.Int32 instances, System.Boolean useColors = false, System.Boolean useCustomData = false, System.Boolean useIndirect = false)`

Allocates copied packed instance records for an owned resource.

multiMesh: Owned instance identity.

instances: Nonnegative capacity.

useColors: Include four-float color multipliers.

useCustomData: Include four-float shader data.

useIndirect: Create native five-uint commands per surface, initially with zero instances; requires GPU rendering.

System.NotSupportedException: Enabled indirect mode is requested on Compatibility.

Remarks: Transforms are always two-dimensional. Equal capacity, schema and indirect mode preserve storage; capacity/schema changes reset records and expire identities; mode changes recreate native storage from retained authored snapshots.

<a id="member-a3d75d5b55ec"></a>
### MultiMeshCreate

`public static Electron2D.RID MultiMeshCreate()`

Creates owned empty two-dimensional instance storage.

Returns: A logical identity valid until FreeRID or renderer teardown.

<a id="member-0182b5f42f6c"></a>
### MultiMeshGetAABB

`public static Electron2D.Rect2 MultiMeshGetAABB(Electron2D.RID multiMesh)`

Gets the two-dimensional local visibility rectangle of a live instance resource.

multiMesh: Live owned or borrowed instance identity.

Returns: The manual or computed rectangle of its visible prefix.

<a id="member-764c29b14aac"></a>
### MultiMeshGetBuffer

`public static System.Single[] MultiMeshGetBuffer(Electron2D.RID multiMesh)`

Returns copied packed instance records.

multiMesh: Live instance identity.

Returns: Caller-owned eight-float transforms and optional channels.

<a id="member-2a82a59532cf"></a>
### MultiMeshGetCustomAABB

`public static Electron2D.Rect2 MultiMeshGetCustomAABB(Electron2D.RID multiMesh)`

Gets the authored manual local visibility rectangle.

multiMesh: Live instance identity.

Returns: The stored rectangle; zero selects computed bounds.

<a id="member-15d670887101"></a>
### MultiMeshGetInstanceCount

`public static System.Int32 MultiMeshGetInstanceCount(Electron2D.RID multiMesh)`

Returns the allocated instance count.

multiMesh: Live owned or borrowed instance identity.

Returns: The allocated capacity.

<a id="member-704bbb24d3c7"></a>
### MultiMeshGetMesh

`public static Electron2D.RID MultiMeshGetMesh(Electron2D.RID multiMesh)`

Gets the borrowed mesh identity used by an instance resource.

multiMesh: Live instance identity.

Returns: The live mesh RID or an empty identity.

<a id="member-065d2ac29e72"></a>
### MultiMeshGetVisibleInstances

`public static System.Int32 MultiMeshGetVisibleInstances(Electron2D.RID multiMesh)`

Gets the stored visible-prefix policy.

multiMesh: Live instance identity.

Returns: Minus one for all instances, or the visible prefix count.

<a id="member-ed902624b78c"></a>
### MultiMeshInstanceGetColor

`public static Electron2D.Color MultiMeshInstanceGetColor(Electron2D.RID multiMesh, System.Int32 index)`

Gets one stored instance color multiplier.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The raw current color.

<a id="member-13c66376fd4e"></a>
### MultiMeshInstanceGetCustomData

`public static Electron2D.Color MultiMeshInstanceGetCustomData(Electron2D.RID multiMesh, System.Int32 index)`

Gets one stored four-component shader value.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The raw current shader data.

<a id="member-97a26c8d2530"></a>
### MultiMeshInstanceGetTransform2D

`public static Electron2D.Transform MultiMeshInstanceGetTransform2D(Electron2D.RID multiMesh, System.Int32 index)`

Gets one instance's current local transform.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The logical current transform.

<a id="member-9d81d734d1a4"></a>
### MultiMeshInstanceResetPhysicsInterpolation

`public static System.Void MultiMeshInstanceResetPhysicsInterpolation(Electron2D.RID multiMesh, System.Int32 index)`

Resets one owned instance's previous presentation record.

multiMesh: Owned instance identity.

index: Existing zero-based index.

<a id="member-6bbb88d48f1a"></a>
### MultiMeshInstanceSetColor

`public static System.Void MultiMeshInstanceSetColor(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color color)`

Sets one owned instance color multiplier.

multiMesh: Owned instance identity.

index: Existing zero-based index.

color: Finite four-component multiplier.

<a id="member-ef077dbe19b6"></a>
### MultiMeshInstanceSetCustomData

`public static System.Void MultiMeshInstanceSetCustomData(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color customData)`

Sets one owned instance's raw shader components.

multiMesh: Owned instance identity.

index: Existing zero-based index.

customData: Finite four-component value.

<a id="member-f26f760fe681"></a>
### MultiMeshInstanceSetTransform2D

`public static System.Void MultiMeshInstanceSetTransform2D(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Transform transform)`

Sets one owned instance's finite local transform.

multiMesh: Owned instance identity.

index: Existing zero-based index.

transform: Finite two-dimensional transform.

<a id="member-24bfe046419d"></a>
### MultiMeshInstancesResetPhysicsInterpolation

`public static System.Void MultiMeshInstancesResetPhysicsInterpolation(Electron2D.RID multiMesh)`

Resets all previous presentation records in owned storage.

multiMesh: Owned instance identity.

<a id="member-d26ab6fef659"></a>
### MultiMeshSetBuffer

`public static System.Void MultiMeshSetBuffer(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> buffer)`

Copies whole finite packed records into owned storage.

multiMesh: Owned instance identity.

buffer: Whole packed buffer matching capacity and flags.

<a id="member-664d1773c69f"></a>
### MultiMeshSetBufferInterpolated

`public static System.Void MultiMeshSetBufferInterpolated(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)`

Copies explicit current and previous packed presentation records.

multiMesh: Owned instance identity.

bufferCurrent: Whole current buffer.

bufferPrevious: Whole previous buffer.

<a id="member-7e35d1bb9fb4"></a>
### MultiMeshSetCustomAABB

`public static System.Void MultiMeshSetCustomAABB(Electron2D.RID multiMesh, Electron2D.Rect2 aabb)`

Sets the manual visibility rectangle of owned instance storage.

multiMesh: Owned instance identity.

aabb: Finite nonnegative two-dimensional local rectangle.

<a id="member-2446e561e02e"></a>
### MultiMeshSetMesh

`public static System.Void MultiMeshSetMesh(Electron2D.RID multiMesh, Electron2D.RID mesh)`

Assigns a borrowed mesh to owned instance storage.

multiMesh: Owned instance identity.

mesh: Live mesh RID or an empty identity to clear it.

<a id="member-c697869bba9d"></a>
### MultiMeshSetPhysicsInterpolated

`public static System.Void MultiMeshSetPhysicsInterpolated(Electron2D.RID multiMesh, System.Boolean interpolated)`

Enables or disables presentation interpolation of owned packed records.

multiMesh: Owned instance identity.

interpolated: Whether scene physics snapshots contribute to rendering.

Remarks: Changing the policy resets previous records to current values without changing logical data.

<a id="member-0dcf1b00d68c"></a>
### MultiMeshSetPhysicsInterpolationQuality

`public static System.Void MultiMeshSetPhysicsInterpolationQuality(Electron2D.RID multiMesh, Electron2D.MultiMesh.PhysicsInterpolationQuality quality)`

Changes the basis interpolation quality of owned storage.

multiMesh: Owned instance identity.

quality: Fast component or High angular interpolation.

<a id="member-29017b0529ec"></a>
### MultiMeshSetVisibleInstances

`public static System.Void MultiMeshSetVisibleInstances(Electron2D.RID multiMesh, System.Int32 visible)`

Changes the visible prefix of an owned resource without reallocating.

multiMesh: Owned instance identity.

visible: Minus one or a count through capacity.

## Offscreen target integration

[Offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) use independent native images and stable borrowed identities. Native root presentation remains owned by Engine.Run.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public static Electron2D.RID ViewportGetTexture(Electron2D.RID viewport)` | Returns the borrowed live texture identity associated with a scene viewport. |

## Method Descriptions

<a id="member-f6270f86144d"></a>
### ViewportGetTexture

`public static Electron2D.RID ViewportGetTexture(Electron2D.RID viewport)`

Returns the borrowed live texture identity associated with a scene viewport.

viewport: A live identity returned by Viewport.GetViewportRID.

Returns: The stable resource texture RID.

System.ArgumentException: The identity does not resolve to a live viewport.

System.InvalidOperationException: The call is off the renderer owner thread.

System.ObjectDisposedException: The renderer is disposed.


Native canvas texture/program/material caches now span the complete multi-target frame. Texture2DGet resolves completed ViewportTexture images explicitly; ordinary viewport canvas/material sampling uses native storage. Readback/native submission mutation guards remain separate. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) for scope, tests and remaining prerequisites.

Its private CanvasFrame owns reusable per-target node/order/geometry/batch buffers, transform maps and sampler scratch. Dependency recursion therefore preserves consumer buffers; frame completion clears borrowed references while retaining capacity.

RenderingServer.Composition.cs collects same-Z group ranges, fits or retains owner geometry, inserts copy/clear/composite boundaries and guards unsupported nested/writable-buffer reads. Its internal screen readback is an explicit native probe, separate from ordinary frame submission. See [composition](../components/canvas-rendering.md#group-composition-and-screen-snapshots).

ClipChildren extends the existing composition stream. Command-bearing mask owners end their captured same-Z range, AndDraw inserts an early ordinary owner draw using already uploaded geometry, and final owner geometry supplies alpha. Parent/internal Y-sort and canvas boundaries follow the same compositor traversal. See [alpha masks](../components/canvas-rendering.md#canvas-alpha-masks).

## World particle composition

For CPUParticles world-coordinate commands, each render entry captures the current framebuffer/viewport/CanvasLayer basis separately from the ordinary emitter transform. Existing particles remain visible after emitter motion or singular scale; normal children, logical transform queries, clips, Z/Y sort and repetition retain their ordinary paths. Native GPU/compatibility checks and prepared measurements are recorded in [CPU particles](../components/cpu-particles.md).

## Mesh skin API

| Complete signature | Contract |
| --- | --- |
| `public const System.Int32 ArrayWeightsSize = 4` | The default number of bone/weight slots per skinned vertex; Use8BoneWeights selects twice this count. |
| `public static System.Void CanvasItemAttachSkeleton(Electron2D.RID item, Electron2D.RID skeleton)` | Attaches a borrowed palette to a live scene canvas item for retained mesh skin replay. |
| `public static System.Int32 MeshSurfaceGetFormatSkinStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)` | Returns the packed skin stride for a supported two-dimensional surface format. |
| `public static System.Void MeshSurfaceUpdateSkinRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates copied bytes in a caller-owned mesh's packed skin channel. |
| `public static System.Void SkeletonAllocateData(Electron2D.RID skeleton, System.Int32 bones)` | Replaces owned palette storage with zero transforms; equal capacity preserves values. |
| `public static Electron2D.Transform SkeletonBoneGetTransform2D(Electron2D.RID skeleton, System.Int32 bone)` | Returns one stored deformation transform from owned or scene storage. |
| `public static System.Void SkeletonBoneSetTransform2D(Electron2D.RID skeleton, System.Int32 bone, Electron2D.Transform transform)` | Sets a finite skin deformation transform in owned storage. |
| `public static Electron2D.RID SkeletonCreate()` | Creates an empty caller-owned two-dimensional skin palette. |
| `public static System.Int32 SkeletonGetBoneCount(Electron2D.RID skeleton)` | Returns the number of transforms in a live owned or scene palette. |
| `public static System.Void SkeletonSetBaseTransform2D(Electron2D.RID skeleton, Electron2D.Transform baseTransform)` | Sets the finite palette base transform in canvas coordinates. |

## Mesh skin member descriptions

### ArrayWeightsSize

`public const System.Int32 ArrayWeightsSize = 4`

The default number of bone/weight slots per skinned vertex; Use8BoneWeights selects twice this count.

### CanvasItemAttachSkeleton

`public static System.Void CanvasItemAttachSkeleton(Electron2D.RID item, Electron2D.RID skeleton)`

Attaches a borrowed palette to a live scene canvas item for retained mesh skin replay.

Remarks: The attachment is transient, does not own the palette and is not inherited by canvas children. Freed palettes leave the original unskinned geometry. Scene palettes must share viewport/canvas layer with their consumer.

`item`: Scene-owned CanvasItem.GetCanvasItem identity.

`skeleton`: Live owned/scene palette, or an empty RID to detach.

Throws `System.ArgumentException`: The item or nonempty palette identity is absent.

Throws `System.InvalidOperationException`: The service is unavailable or owner/capture/submission rules reject mutation.

### MeshSurfaceGetFormatSkinStride

`public static System.Int32 MeshSurfaceGetFormatSkinStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)`

Returns the packed skin stride for a supported two-dimensional surface format.

Returns: Zero without skin; two bytes per present index/weight slot, with four or eight slots.

`format`: Typed channel flags.

`vertexCount`: Nonnegative count; does not affect the fixed skin stride.

### MeshSurfaceUpdateSkinRegion

`public static System.Void MeshSurfaceUpdateSkinRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Updates copied bytes in a caller-owned mesh's packed skin channel.

`mesh`: Caller-owned mesh identity.

`surface`: Existing skinned surface.

`offset`: Byte offset into the skin buffer.

`data`: Little-endian uint16 indices followed by UNORM16 weights per vertex.

### SkeletonAllocateData

`public static System.Void SkeletonAllocateData(Electron2D.RID skeleton, System.Int32 bones)`

Replaces owned palette storage with zero transforms; equal capacity preserves values.

Remarks: All palettes are two-dimensional; no dimensional selector is exposed. Allocation commits after successful preparation.

`skeleton`: Caller-owned palette identity.

`bones`: Nonnegative transform count.

Throws `System.ArgumentOutOfRangeException`: The bone count is negative.

Throws `System.InvalidOperationException`: Ownership or renderer/scene owner/submission rules reject mutation.

### SkeletonBoneGetTransform2D

`public static Electron2D.Transform SkeletonBoneGetTransform2D(Electron2D.RID skeleton, System.Int32 bone)`

Returns one stored deformation transform from owned or scene storage.

Returns: Stored transform; scene palettes supply actual pose multiplied by inverse rest.

`skeleton`: Live palette.

`bone`: Existing zero-based index.

Throws `System.ArgumentException`: The identity is absent or disposed.

Throws `System.ArgumentOutOfRangeException`: The index is absent.

Throws `System.InvalidOperationException`: The renderer is unavailable or read off-owner.

### SkeletonBoneSetTransform2D

`public static System.Void SkeletonBoneSetTransform2D(Electron2D.RID skeleton, System.Int32 bone, Electron2D.Transform transform)`

Sets a finite skin deformation transform in owned storage.

`skeleton`: Caller-owned palette.

`bone`: Existing zero-based index.

`transform`: Finite transform, including singular or reflected bases.

Throws `System.ArgumentException`: The palette is absent or the transform is nonfinite.

Throws `System.ArgumentOutOfRangeException`: The bone index is absent.

Throws `System.InvalidOperationException`: Ownership or renderer owner/submission rules reject mutation.

### SkeletonCreate

`public static Electron2D.RID SkeletonCreate()`

Creates an empty caller-owned two-dimensional skin palette.

Returns: A stable RID released by FreeRID or renderer shutdown.

Throws `System.InvalidOperationException`: The native service is unavailable or called off-owner/during submission.

### SkeletonGetBoneCount

`public static System.Int32 SkeletonGetBoneCount(Electron2D.RID skeleton)`

Returns the number of transforms in a live owned or scene palette.

Returns: Nonnegative count; scene palettes follow actual rig membership.

`skeleton`: Live palette identity.

Throws `System.ArgumentException`: The identity is absent or disposed.

Throws `System.InvalidOperationException`: The renderer is unavailable or read off-owner.

### SkeletonSetBaseTransform2D

`public static System.Void SkeletonSetBaseTransform2D(Electron2D.RID skeleton, Electron2D.Transform baseTransform)`

Sets the finite palette base transform in canvas coordinates.

`skeleton`: Caller-owned palette.

`baseTransform`: Canvas basis; singular bases retain data but omit deformation.

Throws `System.ArgumentException`: The identity is absent or the base is nonfinite.

Throws `System.InvalidOperationException`: Ownership or renderer owner/submission rules reject mutation.

The [mesh component](../components/meshes.md#server-palettes-and-foureight-skin-records) owns the actual storage, coordinate, lifetime, callback, archive and verification contract. New palette API uses the existing native-service availability/owner gate. Headless retained geometry and native rendered/backend acceptance remain separately recorded.

## Caller-owned canvas API

See [canvas lifetime and replay contract](../components/canvas-rendering.md#caller-owned-canvases-and-items). These static operations use the current retained owner.

| Operation | Behavior |
| --- | --- |
| `CanvasCreate` | Creates an empty caller-owned canvas, attachable to multiple active viewports. |
| `CanvasItemCreate` | Creates a caller-owned retained canvas item with no parent or commands. |
| `CanvasItemSetParent` | Sets the native render parent to a live canvas/item RID or detaches with an empty identity; scene Node.Parent is unchanged. |
| `CanvasItemSetTransform` | Sets a finite native local render transform without changing the scene authoring transform. |
| `CanvasItemSetVisible` | Sets native visibility, inherited through render parents. |
| `CanvasItemSetModulate` | Sets finite native modulation inherited by render descendants. |
| `CanvasItemSetSelfModulate` | Sets finite native modulation for this item only. |
| `CanvasItemSetDrawBehindParent` | Selects drawing before the render parent at equal Z. |
| `CanvasItemSetSortChildrenByY` | Selects stable ascending Y order for render children. |
| `CanvasItemSetZIndex` | Sets native Z within the canvas item supported range. |
| `CanvasItemSetZAsRelativeToParent` | Selects clamped render-parent Z accumulation instead of absolute Z. |
| `CanvasItemSetClip` | Clips this item and its render descendants to its local drawing or custom rectangle. |
| `CanvasItemSetCustomRect` | Selects finite custom local bounds or restores automatic command bounds. |
| `DebugCanvasItemGetRect` | Returns current custom or automatic local command bounds. |
| `CanvasItemSetDefaultTextureFilter` | Sets native filtering with the shared typed canvas sampler contract. |
| `CanvasItemSetDefaultTextureRepeat` | Sets native addressing with the shared typed canvas sampler contract. |
| `CanvasItemClear` | Clears retained native commands without freeing resources or the item. |
| `CanvasItemAddMesh` | Appends a retained borrowed mesh command with identity transform and white tint by default. |
| `CanvasItemAddMultiMesh` | Appends a retained borrowed repeated-mesh command, consuming live instance storage. |
| `CanvasSetModulate` | Sets finite canvas-wide native modulation. |
| `CanvasSetItemRepeat` | Repeats any live item and its render descendants in its local basis; nearest nested source overrides inherited repetition. |
| `CanvasSetItemMirroring` | Validates direct canvas parentage and sets one repeat through the shared item state. |
| `ViewportAttachCanvas` | Attaches a live canvas to an active scene viewport; the same canvas can be attached to multiple viewports. |
| `ViewportRemoveCanvas` | Removes one view attachment while retaining canvas/item ownership and other attachments. |
| `ViewportSetCanvasTransform` | Sets one attached canvas view matrix without rewriting scene viewport or layer authoring. |
| `ViewportSetCanvasStacking` | Sets signed layer/sublayer order for one canvas view before item Z/Y sorting. |
| `CanvasItemAddNinePatch` | Records retained nine-patch geometry using shared AxisStretchMode, borrowed source texture or internal white, margins/source regions and optional center. |

## Low-level primitive API

[The primitive command contract](../components/canvas-rendering.md#low-level-primitive-commands) defines copied storage, palette consumption, command state, native/source separation and raster limits.

| Operation | Behavior |
| --- | --- |
| `CanvasItemAddLine` | Records a copied local line with finite color/width and optional antialiasing. |
| `CanvasItemAddPolyline` | Records a connected copied polyline; colors are empty, uniform or per vertex. |
| `CanvasItemAddMultiline` | Records copied endpoint pairs; colors can be empty, uniform, per segment or per endpoint. |
| `CanvasItemAddRect` | Records a filled rectangle with optional antialias feather geometry. |
| `CanvasItemAddCircle` | Records a filled circle through the retained 64-segment ellipse kernel. |
| `CanvasItemAddEllipse` | Records a filled local ellipse with major/minor radii and optional antialiasing. |
| `CanvasItemAddPolygon` | Copies and triangulates a simple contour with white/uniform/per-vertex color and optional borrowed texture. |
| `CanvasItemAddPrimitive` | Copies one through four points; missing color/UV values follow the existing primitive contract. |
| `CanvasItemAddTextureRect` | Records a borrowed texture rectangle with tiling, modulation, flips and transpose. |
| `CanvasItemAddTextureRectRegion` | Records a borrowed texture source region with optional transpose and UV clipping. |
| `CanvasItemAddTriangleArray` | Copies indexed triangles and optional four-slot skin channels; count selects an index prefix in triangles. |
| `CanvasItemAddSetTransform` | Records a finite drawing transform for subsequent visible commands. |
| `CanvasItemAddAnimationSlice` | Records a finite repeating half-open interval controlling subsequent commands. |
| `CanvasItemAddClipIgnore` | Records whether subsequent commands bypass rectangular inherited/own clipping; alpha masks still apply. |
| `CanvasItemSetDrawIndex` | Sets signed stable order among equal-Y/equal-Z render siblings without changing Node order. |
| `CanvasItemSetVisibilityLayer` | Sets the native 32-bit visibility mask used by each destination viewport without changing authored getters. |
| `CanvasItemSetUseParentMaterial` | Selects borrowed material inheritance through the actual render graph, preserving authored flags. |

## Shader and material RID integration

[The program RID contract](../components/shader-materials.md#shader-and-material-identities) documents caller-owned create/set/query/free operations and borrowed resource identities. `CanvasItemSetMaterial` changes native material state independently of authored `Material`; its matching source setter republishes the authored reference. Shader replacement and typed uniform/texture updates reuse retained commands.

| Declaration | Contract |
| --- | --- |
| `static RID ShaderCreate()` / `MaterialCreate()` | Retain a unique owned compiled program or empty programmable material until free/teardown. |
| `static void ShaderSetSPIRV(RID, ReadOnlySpan<byte>)` / `static byte[] ShaderGetSPIRV(RID)` | Validate/copy a compiled canvas program; reads accept borrowed shader RIDs. |
| `static void ShaderSetPathHint(RID, string)` | Diagnostic validation path metadata for an owned shader. |
| `static IReadOnlyList<PropertyDescriptor> GetShaderParameterList(RID)` | Immutable typed descriptors for current reflected user uniforms. |
| `static void ShaderSetDefaultTextureParameter(RID, string, RID, int = 0)` / `static RID ShaderGetDefaultTextureParameter(RID, string, int = 0)` | Borrowed indexed sampler default; empty clears, scalars accept zero and fixed descriptor arrays accept their element range. |
| `static void MaterialSetShader(RID, RID)` | Borrow a shader for an owned material; empty clears the program/value state. |
| `static void MaterialSetParam<T>(RID, string, T)` / `(RID, string, ReadOnlySpan<T>)` | Validate/copy a reflected scalar, vector, matrix or complete fixed array. |
| `static void MaterialSetParam(RID, string, RID)` | Borrow an explicit named texture; empty selects the shader default. |
| `static T MaterialGetParam<T>(RID, string)` | Read a typed scalar/vector/matrix from owned or borrowed programmable material. |
| `static T[] MaterialGetParamArray<T>(RID, string)` / `static void MaterialGetParam<T>(RID, string, Span<T>)` | Independent allocating array or prepared exact-length caller copy. |
| `static RID MaterialGetParam(RID, string)` | Explicit texture override identity; empty means default selection. |
| `static void CanvasItemSetMaterial(RID, RID)` | Bind either live material kind to native canvas state; empty selects ordinary canvas colors. |

Generic parameter operations require `T : unmanaged` and the reflected element contract. Wrong identity/type/name/array length and invalid values reject; owner/submission/shutdown guards cover the complete family. See [RenderingServer.Programs.API.cs](../../src/Servers/Rendering/RenderingServer.Programs.API.cs) for per-member XML and [RenderingProgramTests](../../tests/Electron2D.Tests/RenderingProgramTests.cs) for native evidence.

## Viewport world integration

[Canvas and physics worlds](../components/worlds.md) documents World.Canvas, Viewport.World/FindWorld, nearest-viewport CanvasItem access, shared rendering, independent physics, membership changes and runtime lifetime. Existing server and native kernels remain the implementation path. [WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) supplies direct behavior and actual target-pixel evidence.

## Local compute device creation

`public static RenderingDevice CreateLocalRenderingDevice()` creates a caller-owned [local compute device](RenderingDevice.md) without requiring an active canvas renderer. It establishes native compute capability and owner-thread lifetime, and propagates initialization errors. The caller disposes the device and its RID graph. This factory does not change the active renderer or select a physics backend. See [local compute](../components/local-compute.md) for buffers, shaders, lists and verification.

## Physics contact canvas

The internal contact-canvas table owns one canvas-item RID and recorded revision per
live physics WorldRuntime. Before frame capture it records that world's latest
bounded point sample on its default canvas. Shared-world viewports render the same
item through their own canvas transforms; they do not create duplicate markers.
Empty samples clear old commands. Toggle-off, world detach and renderer shutdown
release ownership. Ordinary canvas storage handles warmed rerecording without new
managed allocations. See [physics diagnostics](../components/physics-debug.md).

## Array sampler and RID operations

See [array resources](../components/texture-arrays.md) for copied storage, ownership, verification and format dependencies. Sampled shape changes clear incompatible overrides/defaults.

| Member | Kind | Summary |
| --- | --- | --- |
| [`public static Electron2D.Image Texture2DLayerGet(Electron2D.RID texture, System.Int32 layer)`](#member-e2ffba22a7ad) | method | Copies an array layer from a live resource-owned or server-owned texture RID. |
| [`public static Electron2D.RID Texture2DLayeredCreate(System.ReadOnlySpan<Electron2D.Image> layers, Electron2D.TextureLayered.LayeredType layeredType)`](#member-efef72e59765) | method | Creates a server-owned array from copied homogeneous image layers. |
| [`public static Electron2D.RID Texture2DLayeredPlaceholderCreate(Electron2D.TextureLayered.LayeredType layeredType)`](#member-baf9bd084533) | method | Creates a server-owned diagnostic image-array placeholder. |

<a id="member-e2ffba22a7ad"></a>
### `Texture2DLayerGet(Electron2D.RID, System.Int32)`

Kind: `method`

```csharp
public static Electron2D.Image Texture2DLayerGet(Electron2D.RID texture, System.Int32 layer)
```

#### Summary

Copies an array layer from a live resource-owned or server-owned texture RID.

#### Returns

A caller-owned image; null for a metadata-only resource.

#### Parameters

- `texture`: A layered texture or layered proxy identity.
- `layer`: The zero-based image layer.

#### Exceptions

- `T:System.ArgumentException`: The RID is stale or belongs to another resource role.
- `T:System.ArgumentOutOfRangeException`: The readable image layer is outside the stored range.
- `T:System.InvalidOperationException`: The service is absent/off-owner, or a proxy source is absent.


<a id="member-efef72e59765"></a>
### `Texture2DLayeredCreate(System.ReadOnlySpan<Electron2D.Image>, Electron2D.TextureLayered.LayeredType)`

Kind: `method`

```csharp
public static Electron2D.RID Texture2DLayeredCreate(System.ReadOnlySpan<Electron2D.Image> layers, Electron2D.TextureLayered.LayeredType layeredType)
```

#### Summary

Creates a server-owned array from copied homogeneous image layers.

#### Returns

A stable RID owned until FreeRID or renderer shutdown.

#### Parameters

- `layers`: Nonempty images sharing size, format and mipmap state.
- `layeredType`: The independent image-array sampling role.

#### Exceptions

- `T:System.ArgumentException`: Image configuration is invalid.
- `T:System.ArgumentOutOfRangeException`: The sampling role is invalid.
- `T:System.NotSupportedException`: A source format is unsupported.
- `T:System.InvalidOperationException`: The native service is absent, off-owner, submitting or closing.


<a id="member-baf9bd084533"></a>
### `Texture2DLayeredPlaceholderCreate(Electron2D.TextureLayered.LayeredType)`

Kind: `method`

```csharp
public static Electron2D.RID Texture2DLayeredPlaceholderCreate(Electron2D.TextureLayered.LayeredType layeredType)
```

#### Summary

Creates a server-owned diagnostic image-array placeholder.

#### Returns

A stable RID for one diagnostic layer until FreeRID or shutdown.

#### Parameters

- `layeredType`: The independent image-array sampling role.

#### Exceptions

- `T:System.ArgumentOutOfRangeException`: The sampling role is invalid.
- `T:System.InvalidOperationException`: The native service is absent, off-owner, submitting or closing.

Layer-generated tile items use existing retained canvas grouping/material/Z/Y routing. Their identities reject caller writes/free/disposal and retire with the layer/session; see [tile drawing](../components/tiles.md#per-tile-materials-and-draw-order).

## Canvas lighting integration

[Canvas lights and occlusion](../components/canvas-lights.md) records this type's receiving/material, resource, occlusion, sampling or static-service consumer, with current GPU and compatibility behavior.

Integer device image consumers now validate float/signed/unsigned sampled scalar types and array roles, migrate reload bindings and use nearest native integer sampling. Cold snapshots expose raw-valued float32 RGBA; byte device readback preserves all integer bits. See [the integer canvas checkpoint](../components/local-compute.md#integer-canvas-sampling-checkpoint) for executable evidence and the connected scalar-aware lighting and alpha-mask behavior.

Static scene/retained item setters, effective getters, declaration defaults and immutable descriptor lists reuse the owner's CanvasItem state. [Per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters) records the compiled source profile and identity/thread checks.

### Instance parameter operations

| Method | Result |
| --- | --- |
| `CanvasItemSetInstanceShaderParameter<T>(RID instance, string parameter, T? value)` | Copies one override; null resets it. |
| `CanvasItemGetInstanceShaderParameter<T>(RID instance, string parameter)` | Effective nullable value. |
| `CanvasItemGetInstanceShaderParameterDefaultValue<T>(RID instance, string parameter)` | Declared zero default ignoring overrides, or null when absent. |
| `CanvasItemGetInstanceShaderParameterList(RID instance)` | Immutable copied typed descriptors for effective item/surface shaders. |

A live rendering session and its owner thread are required. Scene and retained item identities share state. Invalid identities and foreign sessions fail; setter capture/generated-tile ownership guards remain in force.

## Secondary mesh UV channel

[MeshSurfaceData.UV2s](MeshSurfaceData.md#uv2s) supplies copied finite float pairs independent of primary UV and instance data. [The mesh component](../components/meshes.md#secondary-uv-storage-and-shader-consumer) records packing, archives, shader locations and executable verification.

## Mesh normal/tangent stride

`public static int MeshSurfaceGetFormatNormalTangentStride(Mesh.ArrayFormat format, int vertexCount)`

Returns four bytes for each present normal/tangent channel, zero for neither. Requires the renderer owner thread, a supported format and a nonnegative count. Normal/tangent words follow the complete position block in the same vertex buffer; color/UV and skin strides remain independent.

## Numerical mesh custom formats

`public const int ArrayCustomCount = 4`

Four independent typed MeshSurfaceData custom channels use the shared [MeshCustomFormat](MeshCustomFormat.md) enum. MeshSurfaceGetFormatAttributeStride sums color, UV and the selected custom record widths. MeshSurfaceUpdateAttributeRegion validates complete affected records before mutation and refreshes retained shader inputs. See [the packed custom contract](../components/meshes.md#numerical-custom-channels).

## Named mesh targets

| Complete signature | Contract |
| --- | --- |
| `public static System.Void MeshAddSurfaceFromArrays(Electron2D.RID mesh, Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.ArrayFormat flags = None, System.Collections.Generic.IReadOnlyList<Electron2D.MeshSurfaceData> blendShapes = null, System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> lods = null)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |
| `public static Electron2D.RID MeshCreate(System.Int32 blendShapeCount)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |
| `public static System.Int32 MeshGetBlendShapeCount(Electron2D.RID mesh)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |
| `public static Electron2D.BlendShapeMode MeshGetBlendShapeMode(Electron2D.RID mesh)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |
| `public static System.Void MeshSetBlendShapeMode(Electron2D.RID mesh, Electron2D.BlendShapeMode mode)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |
| `public static Electron2D.MeshSurfaceData[] MeshSurfaceGetBlendShapeArrays(Electron2D.RID mesh, System.Int32 surface)` | Copied typed surface/target storage; owned mutations and borrowed queries follow the common mesh identity contract. |

MeshCreate(int) defines the shape count before adding owned surfaces. Shapes receive ordinal index names in the managed owned resource. MeshAddSurfaceFromArrays retains positional flags and adds optional typed blendShapes/lods. Copied LOD indices now execute scale selection. See [named targets and deformation](../components/meshes.md#named-targets-and-instance-deformation).

## LOD behavior

Authored index sets select from the full local-to-framebuffer basis using a one-pixel error threshold. MultiMesh selects each presentation instance independently; MeshInstance morph output retains LODs. Base queries and conservative bounds describe full geometry. Storage/callback ownership and detailed validation are documented in [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).

## Mesh LOD query

| Complete signature | Contract |
| --- | --- |
| `public static System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> MeshSurfaceGetLODs(Electron2D.RID mesh, System.Int32 surface)` | Copied sorted thresholds and indices from an owned or borrowed live mesh. |

### MeshSurfaceGetLODs

`public static System.Collections.Generic.IReadOnlyDictionary<System.Single, System.Int32[]> MeshSurfaceGetLODs(Electron2D.RID mesh, System.Int32 surface)`

Uses the initialized process service on its owner thread. Invalid/disposed identities and surface indices reject through the common mesh registry; each query copies arrays and preserves resource ownership. Thresholds are local error units. See [authored LOD indices](../components/meshes.md#authored-lod-indices-and-scale-selection).


## Compressed attribute storage

`Mesh.ArrayFormat.CompressAttributes` retains float XY positions, four-byte UNORM16 primary/secondary UVs and a six-byte joint normal/tangent axis/angle record. UVs outside [0,1] use copied per-component scale metadata; zero axes remain zero and overflowing scale rejects before mutation. Normal-only imports synthesize a tangent; tangent-only compressed input rejects. Frame packing normalizes/projects numerical directions, so it does not retain arbitrary magnitude/nonorthogonality. Partial updates preserve untouched canonical bytes. Tangent regeneration retains exact UV bytes/scale and reencodes the coupled frame. Copies, saved scenes and morph output retain the format and exact base records. AMS7 persists metadata and canonical bytes and reads AMS1–6. See [compressed attributes](../components/meshes.md#compressed-attributes) for layout, precision and verification limits.

## Explicit frame execution

ForceDraw(bool swapBuffers = true, double frameStep = 0) executes the current native scene even when RenderLoopEnabled is false; non-presenting frames still update viewport textures. ForceSync drains selected render-thread requests, submits pending renderer device work and waits for prior GPU operations. Synchronization itself does not draw; user actions retain their ordinary operations. RequestFrameDrawnCallback(Action) schedules an ordered one-shot owner-thread action after a completed frame, with capture release and error cleanup. See [clock, ownership, failure and actual pixel verification](../components/manual-render-frames.md).


## Retained canvas visibility callbacks

RenderingServer.CanvasItemSetVisibilityNotifier accepts a live canvas-item RID, enable flag, finite local rectangle and optional typed enter/exit callbacks. The rectangle extends command/custom bounds. Parent visibility, inherited alpha threshold, cull layers, clipping and repeated canvas transforms participate in conservative culling; this does not test occlusion. Notifier-only items can participate without drawing commands.

Sampling is OR-combined across all submitted root, scene and server-owned offscreen viewports. States commit once after successful backend frame submission, before callbacks and FramePostDraw. A submitted global frame with no participating view exits a previously visible raw notifier. Skipped rendering supplies no new sample. Disable, free, borrowed Scene node disposal and renderer close clear callbacks silently. Replacing area/callbacks preserves committed state and invalidates stale queued delivery.

Callbacks run on the renderer owner, may mutate/free retained items after submission, and cannot reenter rendering. All states commit first; later callbacks, readbacks and frame completion continue after a callback fails. Committed transitions never replay. This runtime-only callback state is absent from PackedScene storage.

`CanvasSetItemRepeat(item, repeatSize, repeatTimes)` accepts finite signed periods and nonnegative counts. Each nonzero axis draws count plus one copies with offsets starting at minus floor(count/2); negative periods reverse order. Zero count or zero size disables the local source and descendants may inherit an ancestor source. State follows the logical item when reparented and expires with renderer teardown. Existing clipping/custom bounds and submitted-view visibility use repeated geometry; no scene property or PackedScene storage is added. Ordinary invertible drawing culls offscreen repeat indexes conservatively. Singular transforms, programmable materials and world-coordinate commands retain complete expansion; many overlapping copies can therefore remain expensive.

## Retained canvas group modes

`CanvasItemSetCanvasGroupMode(RID item, CanvasGroupMode mode, float clearMargin = 5, bool fitEmpty = false, float fitMargin = 0, bool blurMIPMaps = false)` overrides live composition without changing authored scene properties. The nested CanvasGroupMode values are Disabled=0, ClipOnly=1, ClipAndDraw=2 and Transparent=3. Disabled uses ordinary drawing; ClipOnly masks child output without parent color; ClipAndDraw additionally draws the parent below children; Transparent composites same-Z descendants once from a transparent backbuffer.

Margins are finite nonnegative screen pixels. fitEmpty synthesizes a rectangle around child bounds when the item has no commands; fitMargin expands it, and clearMargin expands preparation bounds. Existing owner geometry remains authoritative. Mipmap generation requires GPU rendering; compatibility rejects the submitted effect before drawing. Nested same-Z groups and screen-reading children retain the shared-backbuffer restrictions. State follows logical reparenting and expires with the renderer. Matching authored setters republish scene policy. CanvasServerGroupTests and existing composition checks exercise native modes, margins, offscreen targets, shaders and warmed allocation-free execution on Linux x64.

Retained screen snapshots can be configured through RenderingServer.CanvasItemSetCopyToBackBuffer on live item identities. BackBufferCopy source assignments republish the authored policy; renderer-only settings stay outside scene getters/storage. See [retained copy contract](../components/canvas-rendering.md#retained-backbuffer-copy-settings).

[UInt16 image texture sources](../components/integer-textures.md) now retain all four original layouts and mip chains, use raw unsigned RGBA16 GPU storage, validate ordinary/layered sampler scalars and reject unsupported integer drawing on Compatibility.

## Viewport screen attachment

`ViewportAttachToScreen(RID viewport, Rect2 rect = default, int screen = 0)` copies completed viewport images into a selected native window without an intermediate Sprite. Zero selects main, positive active IDs select child windows, and minus one detaches. Scene and caller-owned viewports retain their texture dimensions and identities. Finite nonnegative-size rectangles use client pixels and clip with their source mapping preserved. Attachment is renderer-session state; authoring, GUI input and root-viewport movie capture remain independent.

The root initially uses its content layout and WhenVisible updates. A nonzero screen rectangle schedules an active, non-disabled viewport, including WhenParentVisible without a rendering parent. A zero rectangle removes that visibility trigger; another policy or sampled texture may still update it, in which case its whole image copies from client origin. Only images successfully drawn this frame participate. Children copy before rendering parents, with sibling activation order retained; copies replace RGBA, leaving uncovered client pixels black. No selected copies preserves the last screen image. ForceDraw(false) updates textures without screen composition. A zero implicit root raster clears the presentation while exposing no viewport image.

See [screen attachment verification and dependencies](../components/canvas-rendering.md#viewport-screen-attachment). Active native child IDs execute through the same ordered attachment path. Invalid or retired IDs reject before mutation; explicit attachments to a retired foreign window stay detached until reassigned. A recreated scene Window resumes its own content-layout attachment. Direct framebuffer rendering remains separate.

## Paired canvas program access

`static void ShaderSetSPIRV(RID shader, ReadOnlySpan<byte> vertexBytecode, ReadOnlySpan<byte> fragmentBytecode)` validates and atomically replaces both stages of an owned shader under the existing session/owner-thread and submission-mutation rules. Path hints wrap failed stage diagnostics; failed replacement preserves the old pair. `static byte[] ShaderGetSPIRV(RID shader, RenderingDevice.ShaderStage stage)` reads copied Vertex/Fragment bytes from an owned or borrowed live shader. Fragment-only programs return an empty Vertex payload. Stage validation, resources, parameter migration and defaults match Shader; undefined/noncanvas stages and stale identities reject. Parameterless export remains fragment-only.

## Global shader parameters

The static GlobalShaderParameterAdd/Get/GetList/GetType/Remove/Set/SetOverride family controls shared typed values for every consuming canvas material. Matrices and both borrowed image roles have concrete overloads. See the [complete declarations and member contracts](../components/global-shader-parameters.md#public-api), [logical enum](RenderingServer.GlobalShaderParameterType.md) and [execution/ownership rules](../components/global-shader-parameters.md). Base values remain independent of temporary overrides; the registry belongs to the active renderer session.

Scene-authored global shader values now use [ShaderGlobalsOverride](ShaderGlobalsOverride.md). Its typed portable values, tree-order arbitration and renderer-session restoration are described in the [global parameter component](../components/global-shader-parameters.md#scene-owned-overrides).

Registered `shader_globals/name` definitions use [GlobalShaderParameterSettings<T>](GlobalShaderParameterSettings.Generic.md) with exact numeric carriers, copied matrices or image-path strings. Rendering-session preparation samples active features before scene Ready and owns loaded images through shutdown; later setting edits apply to the next session. Headless configuration authoring remains independent of a renderer. See [project global behavior](../components/global-shader-parameters.md#project-authored-globals).

## Rendering change observation

| Complete declaration | Contract |
| --- | --- |
| `public static bool HasChanged()` | Owner-thread observation of pending output or an executing time consumer; does not clear changes. |

<a id="haschanged"></a>
### HasChanged

Returns true for pending retained state, resources, presentation or frame callbacks and active time-dependent output. A new session starts changed. Successful presented submission acknowledges the captured revision; later mutations and failures remain pending. ForceDraw(false) leaves presentation work. Off-owner access or absent renderer throws InvalidOperationException. Conservative invalidation can report equal resulting pixels. See [producer and scheduling details](../components/low-processor-rendering.md).

## Live packed mesh indices

| Declaration | Contract |
| --- | --- |
| `public static void MeshSurfaceUpdateIndexRegion(RID mesh, int surface, int offset, ReadOnlySpan<byte> data)` | Copies partial little-endian index bytes into an existing caller-owned surface, after validating the entire resulting range. |
| `public static int MeshSurfaceGetFormatIndexStride(Mesh.ArrayFormat format, int vertexCount)` | Zero without Index, two bytes for 1–65536 vertices, four otherwise. Negative counts reject. |
| `public static int MeshSurfaceGetFormatOffset(Mesh.ArrayFormat format, int vertexCount, Mesh.ArrayFormat channel)` | Selects one channel presence bit and returns its offset within its own buffer; absent channels return zero. |

All three operations require the active renderer owner. Index mutation also rejects borrowed/invalid identities, active submission, missing index storage, out-of-buffer regions and indices outside the existing vertex set. Validation precedes every write; observer errors retain committed data. Empty valid patches emit Changed consistently with other region updates. Surface primitive/count/material, RID, authored bounds and separate LOD indices are preserved. Retained mesh and repeated-instance commands use the next current topology without caller QueueRedraw.

Offset channels are Vertex, Normal, Tangent, Color, TexUV, TexUV2, Custom0–Custom3, Bones, Weights and Index. Vertex, Index and absent channels return zero. Normal/tangent records follow all eight-byte positions; attribute offsets address one color/UV/custom record, and skin offsets address one four/eight-slot record. Compressed directions use four axis bytes followed by two tangent/angle bytes. Invalid combined/policy channels throw ArgumentOutOfRangeException; an overflowing direction offset throws OverflowException. Unsupported formats keep the existing explicit format errors.

For example, while the renderer is active and `mesh` is a caller-owned indexed surface with at least six vertices:

```csharp
RenderingServer.MeshSurfaceUpdateIndexRegion(mesh, 0, 0, new byte[] { 3, 0, 4, 0, 5, 0 });
```

This moves its first triangle to vertices 3, 4 and 5 when the index stride is two. See [mesh index behavior and verification](../components/meshes.md#live-index-buffer-updates).

## Packed mesh surface family

| Public static member | Contract |
| --- | --- |
| `RID MeshCreateFromSurfaces(IReadOnlyList<MeshSurfaceBuffers> surfaces, int blendShapeCount = 0)` | Validates every surface before publishing a caller-owned identity; explicit zero-through-4096 shape count applies to every surface. |
| `void MeshAddSurface(RID mesh, MeshSurfaceBuffers surface)` | Appends copied packed data to a current-renderer-owned mesh, preserving existing surfaces on validation failure. |
| `MeshSurfaceBuffers MeshGetSurface(RID mesh, int surface)` | Returns independent packed buffers/metadata from an owned or borrowed mesh; material remains borrowed. |

All three require the active renderer owner thread. Creation/append also reject native submission/shutdown and borrowed mutation. Invalid buffers, counts, bounds, topology, shape schema, indices or LODs reject before publication. Disposed materials reject on import. Empty batches are valid and retain their explicit shape count. Custom export rejects recursive/revision-changing/disposed sources and recovers on later valid preparation. The [payload contract](MeshSurfaceBuffers.md) and [component](../components/meshes.md#packed-server-surfaces) define exact packing, independent shape masks, bounds, ownership and verification limits. Queries/imports allocate; warmed retained replay/region updates retain the existing budget. Procedural position-free formats remain an explicit producer dependency.

## Manual mesh visibility bounds

| Signature | Contract |
| --- | --- |
| `public static Rect2 MeshGetCustomAABB(RID mesh)` | Owner-thread query of a live owned or borrowed mesh; zero selects automatic visibility. Other borrowed Mesh roles have no authored override. |
| `public static void MeshSetCustomAABB(RID mesh, Rect2 aabb)` | Sets finite nonnegative mesh-local visibility on a caller-owned mesh; zero restores automatic bounds. Existing submission/shutdown and ownership guards apply. |

These methods share ArrayMesh.CustomAABB state. Source geometry and packed surface descriptions remain independent. Retained commands, group fitting, palettes/morphs and hardware/expanded instances use the override. Resource-owned RIDs permit queries and retain their resource setter; service mutation rejects borrowed identities. See [bounds behavior and native checks](../components/meshes.md#manual-mesh-visibility-bounds).

Paired ShaderSetSPIRV uses the same [original mesh vertex ID contract](../components/shader-materials.md#authored-vertex-stages) as Shader: indexed and expanded geometry, LODs, deformation and instances retain the source index; ordinary non-mesh geometry retains native stream IDs.

## Procedural mesh integration

Typed and packed procedural surfaces execute through owned/borrowed mesh identities with independent counts, source IDs and authored visibility. Existing owner-thread, mutation and disposal guards apply. See [the connected contract](../components/meshes.md#procedural-surfaces-without-positions).

## Writable MultiMesh buffers

| Complete signature | Contract |
| --- | --- |
| `public static RID MultiMeshGetBufferRDRID(RID multiMesh)` | Live borrowed/owned resource to actual canonical writable storage; empty for zero count or Compatibility. |
| `public static RID MultiMeshGetCommandBufferRDRID(RID multiMesh)` | Five uints per surface; empty without indirect mode, surfaces, capacity or a GPU backend. |

Both require the global renderer owner, no active device list and no native submission/readback-memory callback reentry. Invalid logical RIDs throw ArgumentException; byte-budget overflow throws ArgumentOutOfRangeException; invalid thread/list/callback/native state throws InvalidOperationException. The resource owns live device lifetime. CPU getters remain authored; device writes are visible to actual canvas consumers. See [complete record/command layout, replacement and verification](../components/meshes.md#writable-instance-device-buffers).

## Sampled descriptor arrays

| Declaration | Contract |
| --- | --- |
| `static void MaterialSetParam(RID material, string name, ReadOnlySpan<RID> textures)` | Copies exact-length image override identities after full validation; empty entries select indexed defaults. |
| `static RID[] MaterialGetParamTextureArray(RID material, string name)` | Returns copied explicit override identities. |
| `static void MaterialGetParam(RID material, string name, Span<RID> destination)` | Copies all overrides into exact-length caller storage without prepared allocation. |

These operations require a fixed sampled descriptor array, including length one; ordinary/layered and sampled scalar roles follow the reflected parameter. Existing scalar image operations reject arrays. ShaderSet/GetDefaultTextureParameter address individual elements by index for both image roles. Owner-thread, active-session, stale/wrong-kind/borrowed ownership and submission guards remain the existing identity contract. Wrong names/shapes/lengths/identities or disposed images reject before mutation; producer reentry/replaced programs raise InvalidOperationException. Getters return overrides, preserving empty entries independently of shader defaults. The caller owns all images; clearing/freeing a material never frees them. See [SamplerArrayTests](../../tests/Electron2D.Tests/SamplerArrayTests.cs) and [the full behavior and verification limits](../components/shader-materials.md#fixed-sampled-descriptor-arrays).

### MaterialSetParam(RID, string, ReadOnlySpan<RID>)

Sets the entire reflected sampled descriptor array with copied borrowed identities. Empty entries select indexed defaults. Name, length, live identity, sampled shape/scalar and owner/session/submission checks complete before one commit; failed producer validation cannot partially change the array.

### MaterialGetParamTextureArray(RID, string)

Returns a new RID[] of explicit borrowed overrides, preserving empty entries independently of defaults. The name must identify a fixed sampled descriptor array; no resource ownership transfers.

### MaterialGetParam(RID, string, Span<RID>)

Resolves every explicit override into bounded temporary identity storage before copying to the exact-length destination. A bad name, shape, length or stale/disposed resource leaves caller storage untouched. Prepared calls allocate no managed memory and retain ordinary owner/session guards.

Numeric material mat3/mat4 values and fixed arrays execute through `MaterialSetMatrixParam` and copied-array/exact-span `MaterialGetMatrixParam`. They share the ShaderMaterial column-major/finite/identity contract, including reload and paired stages; owned identities permit mutation and borrowed resource identities permit reads. See [numeric material matrices](../components/shader-materials.md#numeric-material-matrices).

The TextureDrawableCreate/TextureDrawableBlitRect/TextureDrawableGenerateMIPMaps/TextureDrawableGetDefaultMaterial family owns writable GPU images and a retained default blit material. Its TextureDrawableFormat values preserve RGBA8, sRGB, half and float storage. Borrowed resource targets, native export leases, matching owned replacement, path/display metadata and shutdown share the existing texture RID services. See [writable textures](../components/drawable-textures.md).

## GPU particle storage and canvas

The static particle family owns planar GPU storage and borrows draw/process resources. All operations require a live GPU renderer and its owner thread. Stale, foreign and wrong-kind identities reject. Mutations cannot reenter native submission or particle preparation; explicit bounds readback cannot run during submission. Compatibility rejects storage creation. See [GPU particles](../components/gpu-particles.md) for defaults, scheduling, native ABI, subemitters, trails, ownership and current verification limits.

### Particle example

This standalone native example uses the public API. The storage and canvas identities belong to the renderer session; the mesh remains caller-owned. This example compiles as a separate public-API consumer; the same workflow executes in ParticleServerTests.

```csharp
using Electron2D;

using var window = new Window { Size = new(160, 120) };
var owner = new Entity();
window.AddChild(owner);
using var mesh = new ArrayMesh();
mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData
{
    Vertices = [new(-4, -4), new(4, -4), new(4, 4), new(-4, 4)],
    Indices = [0, 1, 2, 0, 2, 3]
});
window.Ready += _ =>
{
    var particles = RenderingServer.ParticlesCreate();
    RenderingServer.ParticlesSetAmount(particles, 1);
    RenderingServer.ParticlesSetFixedFPS(particles, 0);
    RenderingServer.ParticlesSetLifetime(particles, 10);
    RenderingServer.ParticlesSetUseLocalCoordinates(particles, true);
    RenderingServer.ParticlesSetDrawPasses(particles, 1);
    RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID());
    var canvas = RenderingServer.CanvasItemCreate();
    RenderingServer.CanvasItemSetParent(canvas, owner.GetCanvasItem());
    RenderingServer.CanvasItemAddParticles(canvas, particles);
    RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(80, 60)),
        Vector2.Zero, Colors.Green, Colors.White, 31);
    RenderingServer.FramePostDraw += () =>
    {
        RenderingServer.FreeRID(canvas);
        RenderingServer.FreeRID(particles);
        window.Tree!.Quit();
    };
};
Engine.Run(window);
```

### Particle method summary

Signatures below are declared on `RenderingServer`; the nested draw policy types are [ParticlesDrawOrder](ParticlesDrawOrder.md), [ParticlesTransformAlign](RenderingServer.ParticlesTransformAlign.md) and [ParticlesTransformAlignAxis](RenderingServer.ParticlesTransformAlignAxis.md).

| C# signature | Contract |
| --- | --- |
| `public static void CanvasItemAddParticles(RID item, RID particles, RID texture = default)` | [Appends a retained particle draw command to a canvas item.](#canvasitemaddparticles) |
| `public static RID ParticlesCreate()` | [Creates owned planar GPU particle storage metadata.](#particlescreate) |
| `public static bool ParticlesGetEmitting(RID particles)` | [Queries whether automatic emission is enabled.](#particlesgetemitting) |
| `public static bool ParticlesIsInactive(RID particles)` | [Queries whether a stopped emitter has drained its renderer activity interval.](#particlesisinactive) |
| `public static void ParticlesRequestProcess(RID particles)` | [Requests one simulation update, including for an emitter without a visible canvas command.](#particlesrequestprocess) |
| `public static void ParticlesRequestProcessTime(RID particles, float processTime, float processTimeResidual = 0)` | [Queues explicit process and residual intervals for the next update.](#particlesrequestprocesstime) |
| `public static void ParticlesRestart(RID particles)` | [Restarts simulation state and its emission phase on the next update.](#particlesrestart) |
| `public static void ParticlesSetAmount(RID particles, int amount)` | [Sets particle capacity and clears prior state when its size changes.](#particlessetamount) |
| `public static void ParticlesSetEmitting(RID particles, bool enabled)` | [Sets automatic emission.](#particlessetemitting) |
| `public static void ParticlesSetOneShot(RID particles, bool enabled)` | [Sets one-cycle automatic emission.](#particlessetoneshot) |
| `public static void ParticlesSetFractionalDelta(RID particles, bool enabled)` | [Sets fractional newborn processing.](#particlessetfractionaldelta) |
| `public static void ParticlesSetInterpolate(RID particles, bool enabled)` | [Sets fixed-step presentation interpolation.](#particlessetinterpolate) |
| `public static void ParticlesSetUseLocalCoordinates(RID particles, bool enabled)` | [Sets emitter-local simulation coordinates.](#particlessetuselocalcoordinates) |
| `public static void ParticlesSetLifetime(RID particles, float value)` | [Sets the nominal particle lifetime.](#particlessetlifetime) |
| `public static void ParticlesSetPreProcessTime(RID particles, float value)` | [Sets the restart preprocessing duration.](#particlessetpreprocesstime) |
| `public static void ParticlesSetSpeedScale(RID particles, float value)` | [Sets the automatic simulation time scale.](#particlessetspeedscale) |
| `public static void ParticlesSetCollisionBaseSize(RID particles, float value)` | [Sets the process collision size.](#particlessetcollisionbasesize) |
| `public static void ParticlesSetAmountRatio(RID particles, float value)` | [Sets the active capacity ratio.](#particlessetamountratio) |
| `public static void ParticlesSetExplosivenessRatio(RID particles, float value)` | [Sets the emission concentration.](#particlessetexplosivenessratio) |
| `public static void ParticlesSetRandomnessRatio(RID particles, float value)` | [Sets the cycle/index emission-time randomness.](#particlessetrandomnessratio) |
| `public static void ParticlesSetInterpToEnd(RID particles, float value)` | [Sets the process interpolation-to-end input.](#particlessetinterptoend) |
| `public static void ParticlesSetFixedFPS(RID particles, int FPS)` | [Selects fixed simulation frequency or variable stepping.](#particlessetfixedfps) |
| `public static void ParticlesSetEmissionTransform(RID particles, Transform transform)` | [Sets the finite planar emission transform.](#particlessetemissiontransform) |
| `public static void ParticlesSetEmitterVelocity(RID particles, Vector2 velocity)` | [Sets the finite planar emitter velocity supplied to process programs.](#particlessetemittervelocity) |
| `public static void ParticlesSetCustomAABB(RID particles, Rect2 bounds)` | [Sets authored visibility bounds without reading GPU state.](#particlessetcustomaabb) |
| `public static Rect2 ParticlesGetCurrentAABB(RID particles)` | [Reads current native particle bounds in emitter-local coordinates.](#particlesgetcurrentaabb) |
| `public static void ParticlesSetProcessMaterial(RID particles, RID material)` | [Sets the borrowed particle process material.](#particlessetprocessmaterial) |
| `public static void ParticlesSetSubemitter(RID particles, RID subemitter)` | [Selects the borrowed target for GPU-generated subemissions.](#particlessetsubemitter) |
| `public static void ParticlesSetDrawPasses(RID particles, int count)` | [Sets the number of borrowed mesh draw passes.](#particlessetdrawpasses) |
| `public static void ParticlesSetDrawPassMesh(RID particles, int pass, RID mesh)` | [Assigns one borrowed planar draw-pass mesh.](#particlessetdrawpassmesh) |
| `public static void ParticlesSetDrawOrder(RID particles, ParticlesDrawOrder value)` | [Selects particle drawing order.](#particlessetdraworder) |
| `public static void ParticlesSetTransformAlign(RID particles, ParticlesTransformAlign value)` | [Selects velocity alignment.](#particlessettransformalign) |
| `public static void ParticlesSetTransformAlignAxis(RID particles, ParticlesTransformAlignAxis value)` | [Selects aligned planar basis.](#particlessettransformalignaxis) |
| `public static void ParticlesSetTrailBindPoses(RID particles, ReadOnlySpan<Transform> bindPoses)` | [Sets copied planar trail bind poses.](#particlessettrailbindposes) |
| `public static void ParticlesSetTrails(RID particles, bool enabled, float lengthSeconds)` | [Configures retained trail history for skinned draw meshes.](#particlessettrails) |
| `public static void ParticlesEmit(RID particles, Transform transform, Vector2 velocity, Color color, Color custom, int emitFlags)` | [Queues one copied manual particle and switches off automatic emission.](#particlesemit) |

### Particle method descriptions

#### CanvasItemAddParticles

`public static void CanvasItemAddParticles(RID item, RID particles, RID texture = default)`

Appends a retained particle draw command to a canvas item.

**item:** A live canvas item identity. **particles:** A particle identity owned by this renderer. **texture:** A borrowed ordinary texture, or empty.

Mesh passes supply geometry; the canvas item's material shades it independently of the particle process material.

#### ParticlesCreate

`public static RID ParticlesCreate()`

Creates owned planar GPU particle storage metadata.

**Returns:** A fresh identity; capacity starts at zero.

Owner-thread operations require an active GPU rendering session. Free with RenderingServer.FreeRID. Particle data and native buffers are allocated on demand; Compatibility rejects this family.

#### ParticlesGetEmitting

`public static bool ParticlesGetEmitting(RID particles)`

Queries whether automatic emission is enabled.

**particles:** A live caller-owned particle identity.

**Returns:** The committed emission flag.

#### ParticlesIsInactive

`public static bool ParticlesIsInactive(RID particles)`

Queries whether a stopped emitter has drained its renderer activity interval.

**particles:** A live caller-owned particle identity.

**Returns:** True only after emission is disabled and the activity interval has elapsed.

#### ParticlesRequestProcess

`public static void ParticlesRequestProcess(RID particles)`

Requests one simulation update, including for an emitter without a visible canvas command.

**particles:** A live caller-owned particle identity.

#### ParticlesRequestProcessTime

`public static void ParticlesRequestProcessTime(RID particles, float processTime, float processTimeResidual = 0)`

Queues explicit process and residual intervals for the next update.

**particles:** A live caller-owned particle identity. **processTime:** Nonnegative emitting process seconds. **processTimeResidual:** Nonnegative residual seconds, with emission disabled.

Owner-thread operations require an active GPU rendering session. Explicit intervals use fixed subdivisions, independently of SpeedScale. A later request replaces both pending intervals. Processing occurs when the renderer next updates this identity.

#### ParticlesRestart

`public static void ParticlesRestart(RID particles)`

Restarts simulation state and its emission phase on the next update.

**particles:** A live caller-owned particle identity.

#### ParticlesSetAmount

`public static void ParticlesSetAmount(RID particles, int amount)`

Sets particle capacity and clears prior state when its size changes.

**particles:** A live caller-owned particle identity. **amount:** Nonnegative record capacity within native byte limits.

#### ParticlesSetEmitting

`public static void ParticlesSetEmitting(RID particles, bool enabled)`

Sets automatic emission.

**particles:** A live caller-owned particle identity. **enabled:** Whether the option is enabled.

#### ParticlesSetOneShot

`public static void ParticlesSetOneShot(RID particles, bool enabled)`

Sets one-cycle automatic emission.

**particles:** A live caller-owned particle identity. **enabled:** Whether the option is enabled.

#### ParticlesSetFractionalDelta

`public static void ParticlesSetFractionalDelta(RID particles, bool enabled)`

Sets fractional newborn processing.

**particles:** A live caller-owned particle identity. **enabled:** Whether the option is enabled.

#### ParticlesSetInterpolate

`public static void ParticlesSetInterpolate(RID particles, bool enabled)`

Sets fixed-step presentation interpolation.

**particles:** A live caller-owned particle identity. **enabled:** Whether the option is enabled.

#### ParticlesSetUseLocalCoordinates

`public static void ParticlesSetUseLocalCoordinates(RID particles, bool enabled)`

Sets emitter-local simulation coordinates.

**particles:** A live caller-owned particle identity. **enabled:** Whether the option is enabled.

#### ParticlesSetLifetime

`public static void ParticlesSetLifetime(RID particles, float value)`

Sets the nominal particle lifetime.

**particles:** A live caller-owned particle identity. **value:** Positive finite value; durations use seconds.

#### ParticlesSetPreProcessTime

`public static void ParticlesSetPreProcessTime(RID particles, float value)`

Sets the restart preprocessing duration.

**particles:** A live caller-owned particle identity. **value:** Nonnegative finite value; durations use seconds.

#### ParticlesSetSpeedScale

`public static void ParticlesSetSpeedScale(RID particles, float value)`

Sets the automatic simulation time scale.

**particles:** A live caller-owned particle identity. **value:** Nonnegative finite value; durations use seconds.

#### ParticlesSetCollisionBaseSize

`public static void ParticlesSetCollisionBaseSize(RID particles, float value)`

Sets the process collision size.

**particles:** A live caller-owned particle identity. **value:** Nonnegative finite collision-size input, initially 0.01.

#### ParticlesSetAmountRatio

`public static void ParticlesSetAmountRatio(RID particles, float value)`

Sets the active capacity ratio.

**particles:** A live caller-owned particle identity. **value:** A finite ratio, clamped to zero through one.

#### ParticlesSetExplosivenessRatio

`public static void ParticlesSetExplosivenessRatio(RID particles, float value)`

Sets the emission concentration.

**particles:** A live caller-owned particle identity. **value:** A finite ratio, clamped to zero through one.

#### ParticlesSetRandomnessRatio

`public static void ParticlesSetRandomnessRatio(RID particles, float value)`

Sets the cycle/index emission-time randomness.

**particles:** A live caller-owned particle identity. **value:** A finite ratio, clamped to zero through one.

#### ParticlesSetInterpToEnd

`public static void ParticlesSetInterpToEnd(RID particles, float value)`

Sets the process interpolation-to-end input.

**particles:** A live caller-owned particle identity. **value:** A finite ratio, clamped to zero through one.

#### ParticlesSetFixedFPS

`public static void ParticlesSetFixedFPS(RID particles, int FPS)`

Selects fixed simulation frequency or variable stepping.

**particles:** A live caller-owned particle identity. **FPS:** Positive frequency; zero or negative selects variable stepping. Changing this option clears numerical state.

#### ParticlesSetEmissionTransform

`public static void ParticlesSetEmissionTransform(RID particles, Transform transform)`

Sets the finite planar emission transform.

**particles:** A live caller-owned particle identity. **transform:** Emitter transform; identity initially. World canvas/bounds and child-local routing require an invertible pose.

#### ParticlesSetEmitterVelocity

`public static void ParticlesSetEmitterVelocity(RID particles, Vector2 velocity)`

Sets the finite planar emitter velocity supplied to process programs.

**particles:** A live caller-owned particle identity. **velocity:** Emitter velocity in pixels per second.

#### ParticlesSetCustomAABB

`public static void ParticlesSetCustomAABB(RID particles, Rect2 bounds)`

Sets authored visibility bounds without reading GPU state.

**particles:** A live caller-owned particle identity. **bounds:** Emitter-local visibility bounds; initially (-4,-4,8,8).

#### ParticlesGetCurrentAABB

`public static Rect2 ParticlesGetCurrentAABB(RID particles)`

Reads current native particle bounds in emitter-local coordinates.

**particles:** A live caller-owned particle identity.

**Returns:** Active state bounds expanded by the longest draw-mesh axis; empty before allocation.

Owner-thread operations require an active GPU rendering session. This is an explicit GPU synchronization/readback query, outside native submission. World coordinates are transformed by the current emission inverse. Mesh callbacks must leave particle state stable.

#### ParticlesSetProcessMaterial

`public static void ParticlesSetProcessMaterial(RID particles, RID material)`

Sets the borrowed particle process material.

**particles:** A live caller-owned particle identity. **material:** A particle ParticleProcessMaterial or ShaderMaterial identity, or empty for the default process.

#### ParticlesSetSubemitter

`public static void ParticlesSetSubemitter(RID particles, RID subemitter)`

Selects the borrowed target for GPU-generated subemissions.

**particles:** A live caller-owned particle identity. **subemitter:** An identity from this rendering session, or empty. Cycles reject.

#### ParticlesSetDrawPasses

`public static void ParticlesSetDrawPasses(RID particles, int count)`

Sets the number of borrowed mesh draw passes.

**particles:** A live caller-owned particle identity. **count:** Nonnegative pass count. New entries are empty.

#### ParticlesSetDrawPassMesh

`public static void ParticlesSetDrawPassMesh(RID particles, int pass, RID mesh)`

Assigns one borrowed planar draw-pass mesh.

**particles:** A live caller-owned particle identity. **pass:** Existing zero-based draw-pass index. **mesh:** Live mesh identity, or empty to disable the pass.

#### ParticlesSetDrawOrder

`public static void ParticlesSetDrawOrder(RID particles, ParticlesDrawOrder value)`

Selects particle drawing order.

**particles:** A live caller-owned particle identity. **value:** A defined planar option.

#### ParticlesSetTransformAlign

`public static void ParticlesSetTransformAlign(RID particles, ParticlesTransformAlign value)`

Selects velocity alignment.

**particles:** A live caller-owned particle identity. **value:** A defined planar option.

#### ParticlesSetTransformAlignAxis

`public static void ParticlesSetTransformAlignAxis(RID particles, ParticlesTransformAlignAxis value)`

Selects aligned planar basis.

**particles:** A live caller-owned particle identity. **value:** A defined planar option.

#### ParticlesSetTrailBindPoses

`public static void ParticlesSetTrailBindPoses(RID particles, ReadOnlySpan<Transform> bindPoses)`

Sets copied planar trail bind poses.

**particles:** A live caller-owned particle identity. **bindPoses:** Copied per-bone poses. Zero or one disables segmented trail history.

#### ParticlesSetTrails

`public static void ParticlesSetTrails(RID particles, bool enabled, float lengthSeconds)`

Configures retained trail history for skinned draw meshes.

**particles:** A live caller-owned particle identity. **enabled:** Whether per-particle history is enabled. **lengthSeconds:** Finite history length of at least 0.01 seconds; capped at ten seconds.

#### ParticlesEmit

`public static void ParticlesEmit(RID particles, Transform transform, Vector2 velocity, Color color, Color custom, int emitFlags)`

Queues one copied manual particle and switches off automatic emission.

**particles:** A live caller-owned particle identity. **transform:** Supplied planar particle pose. **velocity:** Supplied planar velocity. **color:** Supplied particle color. **custom:** Supplied four numeric custom channels. **emitFlags:** Bits 1 position, 2 basis, 4 velocity, 8 color and 16 custom select supplied fields. Remaining bits are ignored.

Owner-thread operations require an active GPU rendering session. If automatic emission was enabled, its previous state is cleared on processing. The pending queue is bounded by capacity; excess records are dropped. No caller-owned data is retained.

All per-member failures also follow the shared owner/identity/preparation contract above. Invalid numeric, enum and capacity values reject before the corresponding operation commits; disposed borrowed resources reject their next use. The native/readback and platform limits are documented in the linked component.

The [independent native window path](../components/window-runtime.md#independent-native-windows) retains one scene renderer and display service. GPU surfaces share the authoritative device/targets; compatibility surfaces use the main driver and replay frozen submissions after cold history import. Native window IDs expire on child hide/detachment; viewport resources survive hiding.

## Render-thread callback bridge

| Declaration | Contract |
| --- | --- |
| [`public static bool IsOnRenderThread()`](#member-render-thread-identity) | Thread-safe active-session observation of the native owner identity. |
| [`public static void CallOnRenderThread(System.Action callback)`](#member-render-thread-callback) | Queues from another thread; drains prior requests and invokes inline on the owner. |

<a id="member-render-thread-identity"></a>
### IsOnRenderThread

Returns true on the current scene/native owner and false on another thread. Requires an active, non-retiring renderer; otherwise throws InvalidOperationException. The observation does not reserve native lifetime or relax access rules. Both current canvas backends share the scene owner. Use IsAvailable for availability and CallOnRenderThread for worker-requested operations.

<a id="member-render-thread-callback"></a>
### CallOnRenderThread

Accepts a nonnull borrowed Action from any thread. Workers return after enqueue; automatic/forced render attempts and ForceSync invoke a FIFO snapshot on the owner, including disabled-render and low-processor loops. Owner callers first drain prior requests, then invoke inline; nested calls are inline. Requests arriving during the selected batch wait for a later drain. Captured objects remain the application's responsibility. Calls after session retirement cannot target a new session implicitly.

Null throws ArgumentNullException; absent/retiring service throws InvalidOperationException. Selected callback failures propagate on the owner after the selected work and native frame/sync attempt; several failures aggregate. All selected actions are tried, including the caller's owner action after a preceding error. Ordinary device/list/submission/lifetime guards still apply. Teardown cancels pending captures. Reuse prepared delegates to avoid application closure allocation. See [the executable workflow, ordering and verification](../components/render-thread-callbacks.md).

## TextureSetForceRedrawIfVisible

`public static void TextureSetForceRedrawIfVisible(RID texture, bool enable)`

Enables continuous low-processor frames while a live ordinary/layered image RID or its proxy is visibly bound in submitted output. Borrowed resources keep their ownership; the weak policy belongs to the current rendering session. It observes logical proxy chains before native image flattening, canvas channels, current animation frames, shader bindings and rendered viewport dependencies. Destination replacement keeps its policy; a consumed replacement source does not transfer its flag. Drawing-only MeshTexture identities require their sampled BaseTexture image instead. Invalid/stale/wrong-role identities reject; calls require the active owner and existing native mutation phase.

Unused, hidden, clipped and offscreen bindings settle to idle; disabled viewports retain their existing update policy. A visible frozen viewport image can have its own policy. RenderLoopEnabled=false still suspends automatic drawing, while ForceDraw explicitly submits. Visibility uses submitted geometry/bounds and clipping; authored/generated vertices and instances without explicit bounds, shader branch selection and pixel occlusion are conservative. See [scheduling and verification](../components/low-processor-rendering.md#visible-texture-policies).

## SetBootImageWithStretch

`public static void SetBootImageWithStretch(Image? image, Color color, RenderingServer.SplashStretchMode stretchMode, bool useFilter = true)`

Immediately presents a copied loading image over a finite background in the main native client. [SplashStretchMode](RenderingServer.SplashStretchMode.md) supplies six centered sizing/crop modes; filtering defaults to linear. Null/empty images preserve output, and the source stays caller-owned. The active native owner may call during readiness or with RenderLoopEnabled=false, outside scene capture/submission. Invalid enum/color, disposed images, off-owner/reentrant/absent service and unsupported native capabilities reject.

The call is cold allocation/upload work. Temporary image storage retires before return; pending particle work and scene caches retain their ordinary frame boundary. Scene viewport contents, scene clear color, clocks/counters and queued frame callbacks remain intact. It emits no frame events, keeps ordinary scene work pending and is replaced by the next normal frame. [Loading-image behavior and verification](../components/boot-images.md) records exact geometry, failure/cleanup and backend limits.

Native thin-stroke batch bounds include half-pixel coverage and an additional half pixel when deferred vertex snapping is active. Group fitting and texture-redraw visibility use that same margin, preserving zero-margin groups with horizontal or vertical lines. Fitted native-line bounds round outward to whole pixels so MSAA group composition does not attenuate a resolved edge a second time. See [stroke commands](../components/canvas-rendering.md#stroke-commands).

## Completed canvas submission counters

[Canvas counters](../components/canvas-counters.md) now publish successful per-viewport/global drawing records, topology/instance budgets and native geometry calls. Empty completed frames reset; skipped or failed frames retain prior values. GPU-authored activity uses explicit capacity/base-geometry budgets without readback. Owner/lifetime, backend granularity and exact verification limits are defined in that component.

| Signature | Contract |
| --- | --- |
| `public static long GetRenderingInfo(RenderingInfo info)` | Allocation-free, any-thread aggregate observation; zero without a native session. |
| `public static long ViewportGetRenderInfo(RID viewport, ViewportRenderInfoType type, ViewportRenderInfo info)` | Same per-viewport fields through a live scene or server-owned RID; native owner required. |

## Native OS feature query

`public static bool HasOSFeature(string feature)` reads the active backend's immutable native ordinary-image upload snapshot. Null throws ArgumentNullException; an absent service throws InvalidOperationException. Unknown names return false. Supported native family tags are s3tc, rgtc and bptc when every required format can be sampled natively. CPU decoding and unrelated device-only format support do not imply this upload capability. Queries can run across threads, allocate no prepared managed data and never create another device. OS.HasFeature observes this same snapshot, returning false outside an active session. See [host platform queries](../components/host-platform.md).

## Shader sampler metadata

| Method | Contract |
| --- | --- |
| `public static void ShaderSetTextureSamplerState(RID shader, string name, RDSamplerState? state, int index = 0)` | Copies a sampler setting onto a live caller-owned shader identity; null restores the ordinary policy. |
| `public static RDSamplerState? ShaderGetTextureSamplerState(RID shader, string name, int index = 0)` | Reads owned or borrowed shader metadata as an independent caller-owned disposable copy, or null. |

Both operations require the live server owner thread. Invalid/stale/wrong-kind identities reject; borrowed mutation throws InvalidOperationException. Name, descriptor element, copied input ownership, validation, Changed publication and backend limits follow [Shader's complete method contract](Shader.md#texture-sampler-state). No sampler RID is borrowed or exposed; configuring metadata precedes native renderer preparation. See [the executable consumer](../../tests/ShaderSamplers.Consumer/ShaderSamplers.Consumer.csproj).

CanvasItemAddPrimitive shares the [native point/line kernel](../components/canvas-rendering.md#native-point-and-line-primitives) with scene drawing, including copied endpoint attributes and snapping. Public server producers participate in the same native pixel and lifetime checks; compatibility native subpixel parity remains incomplete.
