# ParallaxLayer

Last updated: 2026-09-23

- Declaration: `public sealed class ParallaxLayer : Entity`
- Source: [ParallaxLayer.cs](../../src/Scene/2D/ParallaxLayer.cs)
- Inherits: [Entity](Entity.md)
- Component: [Canvas rendering](../components/canvas-rendering.md#parallax-scrolling)
- Coverage: [ParallaxLayer](../coverage/classes/ParallaxLayer.md)

## Description

A spatial canvas subtree driven only by a direct [ParallaxBackground](ParallaxBackground.md) parent. On tree entry it captures its original position and scale. Background movement computes a temporary position and scale; tree exit restores the originals. One additional retained drawing copy is submitted on each enabled mirroring axis, without duplicating nodes or invoking draw callbacks again. An unrelated parent leaves it as an ordinary Entity and yields a configuration warning.

## Example

```csharp
var layer = new ParallaxLayer
{
    MotionScale = new Vector2(0.5f, 1f),
    MotionMirroring = new Vector2(256, 0),
};
layer.AddChild(new Sprite { Texture = texture }); // Existing caller-owned texture.
background.AddChild(layer); // Existing ParallaxBackground.
```

## API summary

| Declaration | Contract |
| --- | --- |
| `public ParallaxLayer()` | Creates a detached, unit-motion spatial layer. |
| `public Vector2 MotionScale { get; set; }` | Per-axis background scroll multiplier. |
| `public Vector2 MotionOffset { get; set; }` | Offset applied after zoom scaling. |
| `public Vector2 MotionMirroring { get; set; }` | Nonnegative local repeat interval per axis. |
| `public override string[] GetConfigurationWarnings()` | Reports a missing direct ParallaxBackground parent. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores original transforms and typed motion settings. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Reconstructs the exact type from PackedScene. |

Inherited spatial, drawing and tree members remain on [Entity](Entity.md), [CanvasItem](CanvasItem.md) and [Node](Node.md). No events, enums or constants are declared here.

## Property descriptions

`MotionScale` starts at `(1,1)` and multiplies each background scroll axis. `MotionOffset` starts at zero and is multiplied by the current zoom scale. Attached setters immediately recalculate the layer. Nonfinite vectors are rejected before storage.

`MotionMirroring` starts at zero; negative components clamp to zero and nonfinite values are rejected. Enabled axes produce one further copy. The copy interval uses the original local scale and the parent canvas basis, independent of camera zoom. Mirroring wraps the calculated position into the negative period and changes rendering on the next frame. The source does not fill an axis with unlimited copies.

The inherited `Position` and `Scale` are calculated while a direct background parent drives the layer. The original values are restored on tree exit and stored in PackedScene instead of the calculated values. Other Entity transform properties retain their inherited behavior. Changing Position or Scale during active background motion is not a durable redefinition of the captured originals.

## Lifecycle, errors and verification

Entry captures Position and Scale before applying parent movement; exit restores both even when a transform callback fails. Updating either calculated transform attempts the other when a callback throws and aggregates failures. Attached mutations require the scene owner thread; nonfinite input, derived overflow, disposed access and capture-time mutation fail explicitly. `GetConfigurationWarnings()` includes inherited warnings plus the direct-parent requirement.

[LegacyParallaxTests](../../tests/Electron2D.Tests/LegacyParallaxTests.cs) checks defaults, motion, zoom, packing, detach/reattach, validation and owner-thread access. Native retained-pixel checks cover mirroring with camera motion and original scale on Linux Wayland compatibility/GPU and SDL dummy compatibility. Physics interpolation must be added to Node and the canvas renderer before this layer can default it to Off; editor behavior, independent/offscreen viewports and other native platforms remain gaps in [coverage](../coverage/classes/ParallaxLayer.md).

The spatial mapping follows [ADR 0008](../decisions/scene.md#adr-0008) and drawing follows [ADR 0028](../decisions/rendering.md#adr-0028).
