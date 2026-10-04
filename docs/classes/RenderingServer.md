# RenderingServer

Last updated: 2026-10-04

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
| [`public Image? Texture2DGet(RID texture)`](#texture2dget) | Returns a caller-owned copy of original backing pixels and mipmaps, or null for a live proxy whose source has been released. |
| [`public void Texture2DUpdate(RID texture, Image image, int layer = 0)`](#texture2dupdate) | Copies and validates matching source width, height, format and mipmap state before publication. |
| [`public RID TextureProxyCreate(RID baseTexture)`](#textureproxycreate) | Owned alias of a borrowed source, including nested proxies. |
| [`public void TextureProxyUpdate(RID texture, RID proxyTo)`](#textureproxyupdate) | Retargets an owned proxy to a non-proxy source. |
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

`public Image? Texture2DGet(RID texture)`

Returns a caller-owned copy of original backing pixels and mipmaps, or null for a live proxy whose source has been released. Atlas RIDs expose their full backing image, while object-based atlas drawing retains the view. Empty resource RIDs return the 4×4 rendering placeholder without initializing the resource or changing its own GetImage result. Readback uses the authoritative managed snapshot, not a GPU stall.

### Texture2DUpdate

`public void Texture2DUpdate(RID texture, Image image, int layer = 0)`

Copies and validates matching source width, height, format and mipmap state before publication. A failure preserves prior pixels. Logical size overrides do not change required source dimensions. Compatible updates retain the allocation token for backend reuse; retained commands sample new pixels without QueueRedraw. Only layer zero is integrated; nonzero layers throw ArgumentOutOfRangeException until the 2D array resource/renderer slice.

### TextureProxyCreate

`public RID TextureProxyCreate(RID baseTexture)`

Creates a distinct owned RID aliasing a live borrowed or owned texture, including another proxy. No pixel copy or native texture allocation is created for the alias: canvas replay resolves the current source into ordinary backend batches. Nested resolution is iterative. The proxy itself remains alive if its source or an intermediate proxy is freed, disposed or collected; it then returns null image data and draws nothing. Its last format/size/path metadata remains available. Freeing an alias never frees its source. Empty ordinary resources supply their rendering checkerboard when sampled through a proxy. Wrong-kind/stale inputs reject before registration. The alias retains RID links, not ownership of borrowed source resources.

A partial snippet inside an active node’s OnReady callback (retain both RIDs in the node and draw the alias in OnDraw):

```csharp
var server = RenderingServer.Instance!;
using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
image.Fill(Colors.Red);
RID source = server.Texture2DCreate(image);
RID alias = server.TextureProxyCreate(source);
// Later, on the owner thread: server.TextureProxyUpdate(alias, anotherSource);
// OnDraw: DrawTexture(alias, Vector2.Zero);
// Explicit cleanup before shutdown: server.FreeRID(alias); server.FreeRID(source);
```

### TextureProxyUpdate

`public void TextureProxyUpdate(RID texture, RID proxyTo)`

Requires a renderer-owned proxy and a live non-proxy source. Rejects ordinary destinations, borrowed destinations, self/proxy targets and wrong-kind/stale identities before mutation. Source callbacks during metadata acquisition may throw; the old target remains unchanged. Retargeting keeps the proxy RID, updates its source, resets a proxy-local size override and copies the new diagnostic path. Future retained replay samples the current root image/format/size without OnDraw; recorded destination geometry and normalized source regions remain fixed. Source replacement redirects aliases before consuming the replacement RID. A disconnected proxy can be retargeted and resume drawing. Owner/submission/shutdown boundaries match texture mutations.

### TextureReplace

`public void TextureReplace(RID texture, RID byTexture)`

Both identities must be live and owned by this renderer. Transfers replacement pixels, logical dimensions and path to the destination object, preserving its RID and retained references. Proxies of the consumed source redirect to the destination before source disposal; destination proxies continue sampling its replacement pixels. Proxy RIDs cannot be replacement operands. Different pixel configurations are allowed. Consumes byTexture; any commands referring to that consumed object stop drawing. Equal validated RIDs do nothing. Disposal callback errors propagate after publication and removal of the consumed identity; destination state remains committed.

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

Before ordering/traversing a canvas branch, RenderingServer independently tests CanvasItem.VisibilityLayer against the selected Viewport.CanvasCullMask. Nested Y-sort collection prunes rejected intermediaries before flattening. Separate roots and CanvasLayer groups retain their normal ordering. Pending drawing still records, and zero masks still clear/present frames. Only submitted batches participate in shader capability rejection. See [mask semantics and native checks](../components/canvas-rendering.md#canvas-visibility-masks).

## Canvas render time

After FramePreDraw, RenderingServer advances its per-run clock by the captured scaled process step and wraps it by the active [RenderingTimeRolloverSeconds](ProjectSettings.md#renderingtimerolloverseconds). CanvasItem evaluates ordered interval/transform commands against that value each frame. Disabled rendering or an invisible root skips both submission and clock advancement. Tree pause leaves time advancing; TimeScale zero freezes it. The GPU backend sends the same clock to optional reserved TIME uniforms; see [shader render time](../components/shader-materials.md#render-time). See [canvas timing verification](../components/canvas-rendering.md#animation-intervals-and-rectangles).

Retained screen regions now sample the same actual render transforms, layer/mask/clip/repetition and inherited alpha as submitted canvases. All states commit before queued screen events; failures continue later nodes and membership epochs reject stale delivery. [VisibleOnScreenNotifier](../classes/VisibleOnScreenNotifier.md) and [VisibleOnScreenEnabler](../classes/VisibleOnScreenEnabler.md) provide the current runtime API. Both Linux Wayland backends and 64 warmed active neutral-target transitions are verified by [ScreenVisibilityRenderingTests](../../tests/Electron2D.Tests/ScreenVisibilityRenderingTests.cs), under [ADR 0078](../decisions/rendering.md#adr-0078). Native allocations, other platforms, layered offscreen targets and editor gizmo drawing remain outside this verification; single-layer offscreen integration has separate SubViewportTests evidence.

## Texture RID verification

[RenderingTextureRIDTests](../../tests/Electron2D.Tests/RenderingTextureRIDTests.cs) checks stable resource identity before startup, independent duplicates, weak collection/sweeping, wrong-kind/stale/thread/disposal guards, placeholder pixels, copied input/output, update configuration rollback, logical size bounds, replacement/consumption, retained drawing and shutdown after callback failure. Native Linux Wayland GPU and compatibility pixel checks verify red → blue → green → freed destinations, atlas source identity/drawing and the checkerboard; dummy/software executes the same baseline. Each backend has 20 warmup frames, then 64 frames with RID lookup/redraw/submission and 64 retained frames with no managed allocation on the owner thread between FramePreDraw and FramePostDraw. This excludes the host event loop, resource creation/update/replacement, image copying/readback and native/backend allocations. Other platforms and owner visual acceptance remain unverified. Layered/drawable/native-device textures and material/shader/canvas server identities remain separately tracked in coverage.

## Proxy verification and storage

[RenderingTextureProxyTests](../../tests/Electron2D.Tests/RenderingTextureProxyTests.cs) checks 256-level iterative chains and warm lookup, source edits/disposal, failed source callback rollback, owner/type/stale/proxy operand guards, nested aliases, source replacement redirection, disconnected images/draws, retarget recovery, empty-resource checkerboard and independent copied outputs. Native pixels run on Linux Wayland GPU/compatibility and dummy/software. After 20 warmup frames, 64 active source retarget/replay/submission frames and 64 unchanged retained frames allocate no managed bytes between FramePreDraw and FramePostDraw on the owner thread. Native source handles remain stable; cache checks show no per-alias texture storage. Owned source textures retain prepared cache allocations until free/shutdown, so alternating already prepared owned sources does not allocate another native texture. First use of a new source and changed pixel configuration may allocate. Backend-internal/native allocator totals, other platforms and owner acceptance remain unverified. Layered/external/device/drawable alias sampling enters those storage families’ first integration slices.

## Owned mesh identities

MeshCreate returns an owned empty ArrayMesh identity. MeshAddSurfaceFromArrays, MeshClear, MeshSurfaceRemove, MeshSurfaceUpdateVertexRegion/AttributeRegion and MeshSurfaceSetMaterial require this renderer's ownership before mutation. Resource-owned Mesh.GetRID is borrowed: getters and drawing resolve it, mutations/free reject it. MeshGetSurfaceCount, MeshSurfaceGetArrays, MeshSurfaceGetFormat/PrimitiveType/ArrayLen/ArrayIndexLen and MeshSurfaceGetMaterial expose typed current fields; queries return caller-owned arrays or borrowed materials. Packed stride helpers report eight-byte positions and present four-byte color/eight-byte UV attributes. Owned identities expire at FreeRID/shutdown; mesh cleanup and texture/backend cleanup are attempted separately. Further raw surface/deformation/LOD/extra-channel data remains Partial with exact dependencies. [Mesh component](../components/meshes.md) records runtime and current limits.

## Mesh methods

All methods require a live object; RenderingServer operations require the renderer owner thread. Server mutations additionally reject during submission or shutdown. Unknown/stale identities and surface indices reject before mutation; resource identities are borrowed and cannot be freed or mutated through the server.

| Complete signature | Contract |
| --- | --- |
| `public System.Void MeshAddSurfaceFromArrays(Electron2D.RID mesh, Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.ArrayFormat flags = None)` | Adds copied typed surface channels to an owned mesh. |
| `public System.Void MeshClear(Electron2D.RID mesh)` | Removes every surface from an owned mesh. |
| `public Electron2D.RID MeshCreate()` | Creates an owned empty two-dimensional mesh. |
| `public System.Int32 MeshGetSurfaceCount(Electron2D.RID mesh)` | Gets a live mesh's surface count. |
| `public System.Int32 MeshSurfaceGetArrayIndexLen(Electron2D.RID mesh, System.Int32 surface)` | Gets the explicit surface index count. |
| `public System.Int32 MeshSurfaceGetArrayLen(Electron2D.RID mesh, System.Int32 surface)` | Gets the surface vertex count. |
| `public Electron2D.MeshSurfaceData MeshSurfaceGetArrays(Electron2D.RID mesh, System.Int32 surface)` | Gets copied typed channels from one mesh surface. |
| `public Electron2D.Mesh.ArrayFormat MeshSurfaceGetFormat(Electron2D.RID mesh, System.Int32 surface)` | Gets a live surface's channel and policy mask. |
| `public System.Int32 MeshSurfaceGetFormatAttributeStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)` | Gets the packed color/UV stride for a supported format. |
| `public System.Int32 MeshSurfaceGetFormatVertexStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)` | Gets the primary two-dimensional vertex stride for a supported surface format. |
| `public Electron2D.Material MeshSurfaceGetMaterial(Electron2D.RID mesh, System.Int32 surface)` | Gets a borrowed material from a live surface. |
| `public Electron2D.Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(Electron2D.RID mesh, System.Int32 surface)` | Gets one surface's primitive topology. |
| `public System.Void MeshSurfaceRemove(Electron2D.RID mesh, System.Int32 surface)` | Removes one surface from an owned mesh. |
| `public System.Void MeshSurfaceSetMaterial(Electron2D.RID mesh, System.Int32 surface, Electron2D.Material material)` | Assigns a borrowed surface material on an owned mesh. |
| `public System.Void MeshSurfaceUpdateAttributeRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates packed attribute bytes, including partial records, in an owned mesh. |
| `public System.Void MeshSurfaceUpdateVertexRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)` | Updates packed position bytes, including partial records, in an owned mesh. |

## Mesh method descriptions

### MeshAddSurfaceFromArrays

`public System.Void MeshAddSurfaceFromArrays(Electron2D.RID mesh, Electron2D.Mesh.PrimitiveType primitive, Electron2D.MeshSurfaceData arrays, Electron2D.Mesh.ArrayFormat flags = None)`

Summary: Adds copied typed surface channels to an owned mesh.

mesh: Owned mesh identity.

primitive: Primitive topology.

arrays: Borrowed arrays copied before mutation.

flags: Supported two-dimensional update policies.

System.ArgumentException: The identity or geometry is invalid.

System.InvalidOperationException: The mesh is borrowed or owned by another renderer.


### MeshClear

`public System.Void MeshClear(Electron2D.RID mesh)`

Summary: Removes every surface from an owned mesh.

mesh: Owned mesh identity.


### MeshCreate

`public Electron2D.RID MeshCreate()`

Summary: Creates an owned empty two-dimensional mesh.

Returns: A logical mesh identity owned until FreeRID or renderer shutdown.

System.InvalidOperationException: The renderer is off-owner or submitting.


### MeshGetSurfaceCount

`public System.Int32 MeshGetSurfaceCount(Electron2D.RID mesh)`

Summary: Gets a live mesh's surface count.

mesh: Borrowed or owned mesh identity.

Returns: The current surface count.


### MeshSurfaceGetArrayIndexLen

`public System.Int32 MeshSurfaceGetArrayIndexLen(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets the explicit surface index count.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: Zero for sequential vertices.


### MeshSurfaceGetArrayLen

`public System.Int32 MeshSurfaceGetArrayLen(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets the surface vertex count.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The vertex count.


### MeshSurfaceGetArrays

`public Electron2D.MeshSurfaceData MeshSurfaceGetArrays(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets copied typed channels from one mesh surface.

mesh: Borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: Independent caller-owned channels.


### MeshSurfaceGetFormat

`public Electron2D.Mesh.ArrayFormat MeshSurfaceGetFormat(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets a live surface's channel and policy mask.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The supported surface format.


### MeshSurfaceGetFormatAttributeStride

`public System.Int32 MeshSurfaceGetFormatAttributeStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)`

Summary: Gets the packed color/UV stride for a supported format.

format: Surface format mask.

vertexCount: Nonnegative surface vertex count.

Returns: Four bytes for color plus eight bytes for primary UV when present.

System.NotSupportedException: The format requires unsupported channels or non-2D vertices.


### MeshSurfaceGetFormatVertexStride

`public System.Int32 MeshSurfaceGetFormatVertexStride(Electron2D.Mesh.ArrayFormat format, System.Int32 vertexCount)`

Summary: Gets the primary two-dimensional vertex stride for a supported surface format.

format: Surface format mask.

vertexCount: Nonnegative surface vertex count.

Returns: Eight bytes for positions, zero when no vertex channel exists.

System.NotSupportedException: The format requires unsupported channels or non-2D vertices.


### MeshSurfaceGetMaterial

`public Electron2D.Material MeshSurfaceGetMaterial(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets a borrowed material from a live surface.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The borrowed material or null.


### MeshSurfaceGetPrimitiveType

`public Electron2D.Mesh.PrimitiveType MeshSurfaceGetPrimitiveType(Electron2D.RID mesh, System.Int32 surface)`

Summary: Gets one surface's primitive topology.

mesh: Live borrowed or owned mesh identity.

surface: Existing zero-based surface index.

Returns: The topology.


### MeshSurfaceRemove

`public System.Void MeshSurfaceRemove(Electron2D.RID mesh, System.Int32 surface)`

Summary: Removes one surface from an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.


### MeshSurfaceSetMaterial

`public System.Void MeshSurfaceSetMaterial(Electron2D.RID mesh, System.Int32 surface, Electron2D.Material material)`

Summary: Assigns a borrowed surface material on an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based surface index.

material: Borrowed live material or null.


### MeshSurfaceUpdateAttributeRegion

`public System.Void MeshSurfaceUpdateAttributeRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates packed attribute bytes, including partial records, in an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.

offset: Byte offset into color/UV records.

data: Present RGBA8 UNORM bytes followed by present little-endian UV floats.


### MeshSurfaceUpdateVertexRegion

`public System.Void MeshSurfaceUpdateVertexRegion(Electron2D.RID mesh, System.Int32 surface, System.Int32 offset, System.ReadOnlySpan<System.Byte> data)`

Summary: Updates packed position bytes, including partial records, in an owned mesh.

mesh: Owned mesh identity.

surface: Existing zero-based index.

offset: Byte offset into two-float positions.

data: Little-endian X/Y records.

## Instance integration

Created MultiMesh identities own instance storage until FreeRID or renderer shutdown and borrow child Mesh identities. Reads enforce renderer owner thread/lifetime and accept live owned or borrowed instance identities; mutations additionally enforce identity ownership. Resource RIDs may be drawn but cannot be mutated/freed through these owned methods. Ordinary fixed 2D allocation executes atomically; useIndirect=true rejects before mutation. Device buffer RIDs remain absent. Canonical owned MeshCreate identities survive linked MultiMeshGetMesh queries.

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `public System.Void MultiMeshAllocateData(Electron2D.RID multiMesh, System.Int32 instances, System.Boolean useColors = false, System.Boolean useCustomData = false, System.Boolean useIndirect = false)` | Allocates copied packed instance records for an owned resource. |
| `public Electron2D.RID MultiMeshCreate()` | Creates owned empty two-dimensional instance storage. |
| `public Electron2D.Rect2 MultiMeshGetAABB(Electron2D.RID multiMesh)` | Gets the two-dimensional local visibility rectangle of a live instance resource. |
| `public System.Single[] MultiMeshGetBuffer(Electron2D.RID multiMesh)` | Returns copied packed instance records. |
| `public Electron2D.Rect2 MultiMeshGetCustomAABB(Electron2D.RID multiMesh)` | Gets the authored manual local visibility rectangle. |
| `public System.Int32 MultiMeshGetInstanceCount(Electron2D.RID multiMesh)` | Returns the allocated instance count. |
| `public Electron2D.RID MultiMeshGetMesh(Electron2D.RID multiMesh)` | Gets the borrowed mesh identity used by an instance resource. |
| `public System.Int32 MultiMeshGetVisibleInstances(Electron2D.RID multiMesh)` | Gets the stored visible-prefix policy. |
| `public Electron2D.Color MultiMeshInstanceGetColor(Electron2D.RID multiMesh, System.Int32 index)` | Gets one stored instance color multiplier. |
| `public Electron2D.Color MultiMeshInstanceGetCustomData(Electron2D.RID multiMesh, System.Int32 index)` | Gets one stored four-component shader value. |
| `public Electron2D.Transform MultiMeshInstanceGetTransform2D(Electron2D.RID multiMesh, System.Int32 index)` | Gets one instance's current local transform. |
| `public System.Void MultiMeshInstanceResetPhysicsInterpolation(Electron2D.RID multiMesh, System.Int32 index)` | Resets one owned instance's previous presentation record. |
| `public System.Void MultiMeshInstanceSetColor(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color color)` | Sets one owned instance color multiplier. |
| `public System.Void MultiMeshInstanceSetCustomData(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color customData)` | Sets one owned instance's raw shader components. |
| `public System.Void MultiMeshInstanceSetTransform2D(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Transform transform)` | Sets one owned instance's finite local transform. |
| `public System.Void MultiMeshInstancesResetPhysicsInterpolation(Electron2D.RID multiMesh)` | Resets all previous presentation records in owned storage. |
| `public System.Void MultiMeshSetBuffer(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> buffer)` | Copies whole finite packed records into owned storage. |
| `public System.Void MultiMeshSetBufferInterpolated(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)` | Copies explicit current and previous packed presentation records. |
| `public System.Void MultiMeshSetCustomAABB(Electron2D.RID multiMesh, Electron2D.Rect2 aabb)` | Sets the manual visibility rectangle of owned instance storage. |
| `public System.Void MultiMeshSetMesh(Electron2D.RID multiMesh, Electron2D.RID mesh)` | Assigns a borrowed mesh to owned instance storage. |
| `public System.Void MultiMeshSetPhysicsInterpolated(Electron2D.RID multiMesh, System.Boolean interpolated)` | Enables or disables presentation interpolation of owned packed records. |
| `public System.Void MultiMeshSetPhysicsInterpolationQuality(Electron2D.RID multiMesh, Electron2D.MultiMesh.PhysicsInterpolationQuality quality)` | Changes the basis interpolation quality of owned storage. |
| `public System.Void MultiMeshSetVisibleInstances(Electron2D.RID multiMesh, System.Int32 visible)` | Changes the visible prefix of an owned resource without reallocating. |

## Methods and protected extension points descriptions

<a id="member-488a40ea3b4c"></a>
### MultiMeshAllocateData

`public System.Void MultiMeshAllocateData(Electron2D.RID multiMesh, System.Int32 instances, System.Boolean useColors = false, System.Boolean useCustomData = false, System.Boolean useIndirect = false)`

Allocates copied packed instance records for an owned resource.

multiMesh: Owned instance identity.

instances: Nonnegative capacity.

useColors: Include four-float color multipliers.

useCustomData: Include four-float shader data.

useIndirect: False for retained canvas storage; true requires a future writable GPU command-buffer backend.

System.NotSupportedException: Indirect GPU instance commands are requested.

Remarks: Transforms are always two-dimensional. Equal capacity and flags preserve storage.

<a id="member-a3d75d5b55ec"></a>
### MultiMeshCreate

`public Electron2D.RID MultiMeshCreate()`

Creates owned empty two-dimensional instance storage.

Returns: A logical identity valid until FreeRID or renderer teardown.

<a id="member-0182b5f42f6c"></a>
### MultiMeshGetAABB

`public Electron2D.Rect2 MultiMeshGetAABB(Electron2D.RID multiMesh)`

Gets the two-dimensional local visibility rectangle of a live instance resource.

multiMesh: Live owned or borrowed instance identity.

Returns: The manual or computed rectangle of its visible prefix.

<a id="member-764c29b14aac"></a>
### MultiMeshGetBuffer

`public System.Single[] MultiMeshGetBuffer(Electron2D.RID multiMesh)`

Returns copied packed instance records.

multiMesh: Live instance identity.

Returns: Caller-owned eight-float transforms and optional channels.

<a id="member-2a82a59532cf"></a>
### MultiMeshGetCustomAABB

`public Electron2D.Rect2 MultiMeshGetCustomAABB(Electron2D.RID multiMesh)`

Gets the authored manual local visibility rectangle.

multiMesh: Live instance identity.

Returns: The stored rectangle; zero selects computed bounds.

<a id="member-15d670887101"></a>
### MultiMeshGetInstanceCount

`public System.Int32 MultiMeshGetInstanceCount(Electron2D.RID multiMesh)`

Returns the allocated instance count.

multiMesh: Live owned or borrowed instance identity.

Returns: The allocated capacity.

<a id="member-704bbb24d3c7"></a>
### MultiMeshGetMesh

`public Electron2D.RID MultiMeshGetMesh(Electron2D.RID multiMesh)`

Gets the borrowed mesh identity used by an instance resource.

multiMesh: Live instance identity.

Returns: The live mesh RID or an empty identity.

<a id="member-065d2ac29e72"></a>
### MultiMeshGetVisibleInstances

`public System.Int32 MultiMeshGetVisibleInstances(Electron2D.RID multiMesh)`

Gets the stored visible-prefix policy.

multiMesh: Live instance identity.

Returns: Minus one for all instances, or the visible prefix count.

<a id="member-ed902624b78c"></a>
### MultiMeshInstanceGetColor

`public Electron2D.Color MultiMeshInstanceGetColor(Electron2D.RID multiMesh, System.Int32 index)`

Gets one stored instance color multiplier.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The raw current color.

<a id="member-13c66376fd4e"></a>
### MultiMeshInstanceGetCustomData

`public Electron2D.Color MultiMeshInstanceGetCustomData(Electron2D.RID multiMesh, System.Int32 index)`

Gets one stored four-component shader value.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The raw current shader data.

<a id="member-97a26c8d2530"></a>
### MultiMeshInstanceGetTransform2D

`public Electron2D.Transform MultiMeshInstanceGetTransform2D(Electron2D.RID multiMesh, System.Int32 index)`

Gets one instance's current local transform.

multiMesh: Live instance identity.

index: Existing zero-based index.

Returns: The logical current transform.

<a id="member-9d81d734d1a4"></a>
### MultiMeshInstanceResetPhysicsInterpolation

`public System.Void MultiMeshInstanceResetPhysicsInterpolation(Electron2D.RID multiMesh, System.Int32 index)`

Resets one owned instance's previous presentation record.

multiMesh: Owned instance identity.

index: Existing zero-based index.

<a id="member-6bbb88d48f1a"></a>
### MultiMeshInstanceSetColor

`public System.Void MultiMeshInstanceSetColor(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color color)`

Sets one owned instance color multiplier.

multiMesh: Owned instance identity.

index: Existing zero-based index.

color: Finite four-component multiplier.

<a id="member-ef077dbe19b6"></a>
### MultiMeshInstanceSetCustomData

`public System.Void MultiMeshInstanceSetCustomData(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Color customData)`

Sets one owned instance's raw shader components.

multiMesh: Owned instance identity.

index: Existing zero-based index.

customData: Finite four-component value.

<a id="member-f26f760fe681"></a>
### MultiMeshInstanceSetTransform2D

`public System.Void MultiMeshInstanceSetTransform2D(Electron2D.RID multiMesh, System.Int32 index, Electron2D.Transform transform)`

Sets one owned instance's finite local transform.

multiMesh: Owned instance identity.

index: Existing zero-based index.

transform: Finite two-dimensional transform.

<a id="member-24bfe046419d"></a>
### MultiMeshInstancesResetPhysicsInterpolation

`public System.Void MultiMeshInstancesResetPhysicsInterpolation(Electron2D.RID multiMesh)`

Resets all previous presentation records in owned storage.

multiMesh: Owned instance identity.

<a id="member-d26ab6fef659"></a>
### MultiMeshSetBuffer

`public System.Void MultiMeshSetBuffer(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> buffer)`

Copies whole finite packed records into owned storage.

multiMesh: Owned instance identity.

buffer: Whole packed buffer matching capacity and flags.

<a id="member-664d1773c69f"></a>
### MultiMeshSetBufferInterpolated

`public System.Void MultiMeshSetBufferInterpolated(Electron2D.RID multiMesh, System.ReadOnlySpan<System.Single> bufferCurrent, System.ReadOnlySpan<System.Single> bufferPrevious)`

Copies explicit current and previous packed presentation records.

multiMesh: Owned instance identity.

bufferCurrent: Whole current buffer.

bufferPrevious: Whole previous buffer.

<a id="member-7e35d1bb9fb4"></a>
### MultiMeshSetCustomAABB

`public System.Void MultiMeshSetCustomAABB(Electron2D.RID multiMesh, Electron2D.Rect2 aabb)`

Sets the manual visibility rectangle of owned instance storage.

multiMesh: Owned instance identity.

aabb: Finite nonnegative two-dimensional local rectangle.

<a id="member-2446e561e02e"></a>
### MultiMeshSetMesh

`public System.Void MultiMeshSetMesh(Electron2D.RID multiMesh, Electron2D.RID mesh)`

Assigns a borrowed mesh to owned instance storage.

multiMesh: Owned instance identity.

mesh: Live mesh RID or an empty identity to clear it.

<a id="member-c697869bba9d"></a>
### MultiMeshSetPhysicsInterpolated

`public System.Void MultiMeshSetPhysicsInterpolated(Electron2D.RID multiMesh, System.Boolean interpolated)`

Enables or disables presentation interpolation of owned packed records.

multiMesh: Owned instance identity.

interpolated: Whether scene physics snapshots contribute to rendering.

Remarks: Changing the policy resets previous records to current values without changing logical data.

<a id="member-0dcf1b00d68c"></a>
### MultiMeshSetPhysicsInterpolationQuality

`public System.Void MultiMeshSetPhysicsInterpolationQuality(Electron2D.RID multiMesh, Electron2D.MultiMesh.PhysicsInterpolationQuality quality)`

Changes the basis interpolation quality of owned storage.

multiMesh: Owned instance identity.

quality: Fast component or High angular interpolation.

<a id="member-29017b0529ec"></a>
### MultiMeshSetVisibleInstances

`public System.Void MultiMeshSetVisibleInstances(Electron2D.RID multiMesh, System.Int32 visible)`

Changes the visible prefix of an owned resource without reallocating.

multiMesh: Owned instance identity.

visible: Minus one or a count through capacity.

## Offscreen target integration

[Offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) use independent native images and stable borrowed identities. Native root presentation remains owned by Engine.Run.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.RID ViewportGetTexture(Electron2D.RID viewport)` | Returns the borrowed live texture identity associated with a scene viewport. |

## Method Descriptions

<a id="member-f6270f86144d"></a>
### ViewportGetTexture

`public Electron2D.RID ViewportGetTexture(Electron2D.RID viewport)`

Returns the borrowed live texture identity associated with a scene viewport.

viewport: A live identity returned by Viewport.GetViewportRID.

Returns: The stable resource texture RID.

System.ArgumentException: The identity does not resolve to a live viewport.

System.InvalidOperationException: The call is off the renderer owner thread.

System.ObjectDisposedException: The renderer is disposed.


Native canvas texture/program/material caches now span the complete multi-target frame. Texture2DGet resolves completed ViewportTexture images explicitly; ordinary viewport canvas/material sampling uses native storage. Readback/native submission mutation guards remain separate. See [offscreen targets](../components/canvas-rendering.md#offscreen-canvas-targets) for scope, tests and remaining prerequisites.

Its private CanvasFrame owns reusable per-target node/order/geometry/batch buffers, transform maps and sampler scratch. Dependency recursion therefore preserves consumer buffers; frame completion clears borrowed references while retaining capacity.
