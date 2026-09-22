# RenderingServer

Last updated: 2026-09-23

- Declaration: `public sealed class RenderingServer : ElectronObject`
- Source: [RenderingServer.cs](../../src/Servers/Rendering/RenderingServer.cs)
- Inherits: [ElectronObject](ElectronObject.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Renders the active root Window's retained CanvasItem commands. Engine.Run creates the service after acquiring the native window, drives it on the scene owner thread and closes it during cleanup. There is no public constructor or independent lifetime. Consumers draw through CanvasItem and Texture; owned SDL handles remain internal. DisplayServer exposes supported borrowed native context identities under ADR 0042.

The service supports rectangles, lines and textures using source-alpha blending into an RGBA8 framebuffer. Shader materials require GPU rendering. Startup settings select `gpu` or `compatibility` and whether GPU initialization may fall back. This does not implement live device migration or recovery.

## Example

Inside a node's OnReady callback during Engine.Run:

```csharp
RenderingServer.Instance!.SetDefaultClearColor(new Color(0.1f, 0.1f, 0.15f));
```

## API summary

| Declaration | Contract |
| --- | --- |
| `static RenderingServer? Instance { get; }` | Active service, or null outside its Engine.Run lifetime. |
| `bool RenderLoopEnabled { get; set; }` | Enables frame submission; initially true. |
| `string GetCurrentRenderingMethod()` | Actual `gpu` or `compatibility` method after fallback. |
| `string GetCurrentRenderingDriverName()` | Actual native driver name. |
| `Color GetDefaultClearColor()` | Current clear color. |
| `void SetDefaultClearColor(Color color)` | Sets a finite clear color for later frames. |
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

## Event descriptions

### FramePreDraw

Runs synchronously within the scene execution barrier before capturing visible nodes and invoking pending OnDraw callbacks. A subscriber failure aborts the frame and propagates through Engine.Run cleanup. Frame and native-pump re-entry are rejected.

### FramePostDraw

Runs synchronously after submission. Submission does not prove GPU completion or compositor presentation. A subscriber failure propagates through Engine.Run cleanup. No event is delivered for a disabled or invisible root frame.

## Internal frame records

The private readonly `RenderEntry(CanvasItem Node, int Z, int Order)` record borrows a CanvasItem, its effective Z index and resolved canvas order. A reused list sorts by Z first and that resolved order second. The private readonly `YSortEntry(CanvasItem Node, float Y, int Order)` record holds a borrowed item, its Y in the current group's coordinate system and its original order for approximate ties. Nested independent groups append temporary ranges to a shared reused list; sorting its Span range uses a cached static comparison. No span survives recursive list growth. Both lists release borrowed references after submission or failure; they never dispose nodes.

## Lifecycle, errors and dependencies

Command preparation follows scene order. The renderer captures the tree again after drawing callbacks, so parenting and sibling-order changes affect this submission. Canvas roots (TopLevel items and items without direct canvas parents) retain scene order, with each root's canvas subtree resolved before the next root. Within a subtree, behind-parent children precede the parent and ordinary children follow. YSortEnabled flattens nested enabled groups in local coordinates; unsorted child subtrees stay grouped at their root's Y. Global Z sorting preserves this resolved order for equal Z. Visibility is checked before ordering. CanvasItem transforms and modulation apply to retained local commands each frame. Shader capabilities, texture availability and backend format requirements are checked before drawing. Geometry and material resources remain borrowed.

Public instance methods and RenderLoopEnabled reject calls off the owner thread with InvalidOperationException. Calling inherited Dispose directly is rejected because Engine.Run owns teardown. The protected disposal overrides implement internal shutdown only; the sealed type has no user extension point. Native handles, buffers, textures and programs are released before detaching the service.

RenderingRuntimeTests measures zero managed allocation from FramePreDraw through FramePostDraw over twenty warmed frames with mixed geometry, textures and GPU HLSL/GLSL materials on native Wayland and dummy/software. The check includes both captures, stable Z sorting and nested Y groups; it excludes the host event loop, resource construction, readback and arbitrary user callbacks. It is not a frame-time bound.

Depends on Window, SceneTree, Node, CanvasItem, typed material/texture resources and the internal SDL3-CS backends. [Canvas rendering](../components/canvas-rendering.md) records native verification and current limits. Lights, clipping, polygons, public offscreen targets, multiwindow rendering, device recovery and the full rendering API remain unfinished.
