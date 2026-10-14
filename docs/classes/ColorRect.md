# ColorRect

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public class ColorRect : Control`. **Inherits:** [Control](Control.md). **Inherited By:** —. **Source:** [ColorRect.cs](../../src/Scene/GUI/ColorRect.cs). **Component:** [GUI primitives](../components/gui-primitives.md).

Draws a solid rectangle from local zero through `Size`. It has no intrinsic minimum, texture or padding. The inherited transform, visibility, clipping, materials, canvas modulation and ordering apply normally. Pointer input stops by default; choose `MouseFilter.Ignore` for a background that should let input pass. Color and inherited layout/theme state persist in scenes. Subclasses provide an exact scene factory.

Attached reads require the owner thread. Authored writes are forbidden off-owner or during capture. Equal colors are silent. Nonfinite channels fail before changing state. Finite signed/HDR channels are retained; render target and blending rules determine final pixels.

## Example

```csharp
var root = new Window { Size = new(320, 180) };
var background = new ColorRect
{
    Size = new(320, 180),
    Color = new Color(.1f, .15f, .2f),
    MouseFilter = MouseFilter.Ignore
};
root.AddChild(background);
Engine.Run(root);
```

## Constructors

| Signature | Description |
| --- | --- |
| `public ColorRect()` | Opaque white fill and inherited control defaults. |

## Properties

| Signature | Default |
| --- | --- |
| `public Color Color { get; set; }` | `Colors.White` |

## Property Descriptions

### Color

Fill color independent of inherited `Modulate` and `SelfModulate`. A changed value queues retained drawing. A nonfinite channel throws `ArgumentException`; disposed access throws `ObjectDisposedException`; off-owner/capture mutation throws `InvalidOperationException`. Typed scene storage copies the value without converting it to a texture.

## Protected extension points

| Signature | Description |
| --- | --- |
| `protected override void OnNotification(int what)` | Records the solid rectangle in the normal draw notification. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds the typed stored color field. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Creates the exact built-in type; subclasses supply their own factory. |

## Verification and limits

GUIPrimitivesTests covers defaults, signed/HDR/invalid color, stored scenes, input policy, owner/capture/disposal guards and zero warmed recording. GUIPrimitivesRenderingTests checks actual saved/loaded fill, live color and visibility on GPU/compatibility. See the component for allocation boundaries and native/platform/accessibility/owner limits.
