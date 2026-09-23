# ParallaxBackground

Last updated: 2026-09-23

- Declaration: `public sealed class ParallaxBackground : CanvasLayer`
- Source: [ParallaxBackground.cs](../../src/Scene/2D/ParallaxBackground.cs)
- Inherits: [CanvasLayer](CanvasLayer.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#parallax-scrolling)
- Coverage: [ParallaxBackground](../coverage/classes/ParallaxBackground.md)

## Description

A separate canvas behind the default canvas (`Layer = -100`) that updates direct [ParallaxLayer](ParallaxLayer.md) children from the current Camera or a manual scroll offset. It inherits CanvasLayer ordering, visibility, transforms and viewport selection, but does not draw itself. Its children remain scene-owned; it borrows the containing viewport and camera. Attached access belongs to the scene owner thread. A camera update replaces the manual `ScrollOffset` on its next tracking update.

## Example

```csharp
var background = new ParallaxBackground { ScrollBaseScale = new Vector2(0.5f, 1f) };
var layer = new ParallaxLayer { MotionMirroring = new Vector2(256, 0) };
layer.AddChild(new Sprite { Texture = texture }); // Existing caller-owned texture.
background.AddChild(layer);
window.AddChild(background); // Existing root Window, before Engine.Run.
```

## API summary

| Declaration | Contract |
| --- | --- |
| `public ParallaxBackground()` | Creates a detached background at canvas layer -100. |
| `public Vector2 ScrollOffset { get; set; }` | Manual offset, replaced by an active camera update. |
| `public Vector2 ScrollBaseOffset { get; set; }` | Base displacement. |
| `public Vector2 ScrollBaseScale { get; set; }` | Per-axis multiplier on ScrollOffset. |
| `public Vector2 ScrollLimitBegin { get; set; }` | Lower scroll bound. |
| `public Vector2 ScrollLimitEnd { get; set; }` | Upper scroll bound. |
| `public bool ScrollIgnoreCameraZoom { get; set; }` | Excludes camera zoom from child positions and scales at the next scroll update. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores typed settings and the layer-specific inherited default. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Reconstructs the exact type from PackedScene. |

All inherited canvas-layer and tree members remain available on [CanvasLayer](CanvasLayer.md) and [Node](Node.md). There are no declared events, methods, constants or enums beyond the extension overrides.

## Property descriptions

`ScrollOffset` and `ScrollBaseOffset` start at zero. `ScrollBaseScale` starts at `(1,1)`. Changing any of these while attached recalculates direct layer transforms. An active camera sends its canvas origin and zoom to this background; a later camera update overwrites `ScrollOffset`. Each vector setter rejects nonfinite values before storage.

`ScrollLimitBegin` and `ScrollLimitEnd` both start at zero, disabling clamping. An axis is limited only when begin is below end. The negative combined scroll is clamped against begin or end minus visible viewport size, then negated for layer positioning. The limits change the child layers at once while attached and do not move the Camera.

`ScrollIgnoreCameraZoom` starts false. When true, the next scroll update uses unit scale for child layers and compensates their offset with the camera's screen offset. Setting only this flag does not recalculate existing layers. A zero camera scale with this mode is rejected as an invalid resulting transform.

The inherited `Layer` starts at -100 instead of CanvasLayer's zero. Other inherited layer settings keep their normal contracts. `CustomViewport` changes update the camera registration and layers, while independent/offscreen viewport rendering remains outside the verified root Window path.

## Lifecycle, errors and verification

Tree entry registers with the selected viewport, computes current child transforms, and tree exit unregisters. Updates traverse a snapshot of direct children; if a transform callback fails, remaining layers still update and errors are reported together. Each child's original position and scale are captured on entry and restored on exit. `PackedScene` stores the scroll settings and inherited layer order, not the borrowed camera or viewport. Nonfinite input, derived coordinate overflow, disposed access, off-owner access and capture-time mutation fail explicitly.

[LegacyParallaxTests](../../tests/Electron2D.Tests/LegacyParallaxTests.cs) checks defaults, camera/manual movement, zoom policy, packing, validation, lifecycle and owner-thread guards. Native pixel checks cover the root Window on Linux Wayland compatibility/GPU and SDL dummy compatibility. Editor canvas behavior, independent/offscreen viewports, other native platforms and physics interpolation remain unverified or absent as recorded in [coverage](../coverage/classes/ParallaxBackground.md).

Inheritance and naming follow [ADR 0008](../decisions/scene.md#adr-0008) and [ADR 0004](../decisions/product.md#adr-0004); rendering follows [ADR 0028](../decisions/rendering.md#adr-0028).
