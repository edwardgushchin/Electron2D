# ReferenceRect

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public class ReferenceRect : Control`. **Inherits:** [Control](Control.md). **Source:** [ReferenceRect.cs](../../src/Scene/GUI/ReferenceRect.cs). **Component:** [Editor canvas guides](../components/editor-canvas-guides.md).

Draws a centered outline around the inherited local control rectangle. Minimum size, anchors, layout, pointer/focus policy, visibility, clipping, ordering, transform and modulation remain inherited. The border initially appears only while `Engine.IsEditorHint` is true. The component owns preview integration, redraw semantics and qualification limits.

## Constructors

| Signature | Description |
| --- | --- |
| `public ReferenceRect()` | One-unit opaque red editor border and inherited Control defaults. |

## Properties

| Signature | Default |
| --- | --- |
| `public Color BorderColor { get; set; }` | `Colors.Red` |
| `public float BorderWidth { get; set; }` | `1` |
| `public bool EditorOnly { get; set; }` | `true` |

### BorderColor

Finite border color, including signed/HDR channels. A nonfinite channel throws `ArgumentException` before mutation. Target format and inherited blending determine the final pixel color.

### BorderWidth

Finite local outline width. Negative values clamp to zero, which draws no border. Outlines at least as wide as either rectangle dimension follow DrawRect's expanded-fill behavior. A nonfinite width throws `ArgumentException` before mutation.

### EditorOnly

False permits the border during ordinary gameplay. Changes and editor-role transitions take effect on the next submitted canvas frame. The flag does not alter layout or input policy.

All three properties are typed stored scene state. Exact concrete factories support duplicate/packed/file reconstruction; subclasses supply an exact factory. Equal writes are silent. Attached access requires the scene owner thread; authored writes during capture throw `InvalidOperationException` and disposed access throws `ObjectDisposedException`. Size changes invalidate drawing through Control. Built-in drawing runs during NotificationDraw before user drawing callbacks.
