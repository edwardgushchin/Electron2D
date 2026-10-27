# Marker

Last updated: 2026-10-27

**Namespace:** `Electron2D`. **Declaration:** `public class Marker : Entity`. **Inherits:** [Entity](Entity.md). **Source:** [Marker.cs](../../src/Scene/2D/Marker.cs). **Component:** [Editor canvas guides](../components/editor-canvas-guides.md).

Marks an authored spatial location with an editor-only cross. It retains the complete inherited Entity and CanvasItem behavior and becomes an ordinary invisible anchor during gameplay. The cross requires scene membership and `Engine.IsEditorHint`; edited-scene ownership is not an additional requirement. See the component for colors, retained redraw, preview integration and verification limits.

## Constructors

| Signature | Description |
| --- | --- |
| `public Marker()` | Ten-unit editor half-axes and inherited spatial defaults. |

## Properties

| Signature | Default |
| --- | --- |
| `public float GizmoExtents { get; set; }` | `10` |

### GizmoExtents

Finite local half-axis length. Zero hides the cross; negative lengths reverse color placement. Transform, visibility and modulation follow ordinary canvas behavior; the hairline remains one framebuffer pixel wide. Assignments queue redraw, including equal values. Nonfinite input throws `ArgumentException` without changing state. Attached access requires the scene owner thread, authored writes during capture throw `InvalidOperationException`, and disposed access throws `ObjectDisposedException`.

The typed property and exact concrete factory persist through duplication, PackedScene and file loading. Subclasses provide an exact scene factory. Drawing uses inherited NotificationDraw before user drawing callbacks.
