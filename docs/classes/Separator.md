# Separator

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Separator : Control`. **Inherits:** [Control](Control.md). **Inherited By:** [HSeparator](HSeparator.md), [VSeparator](VSeparator.md). **Source:** [Separator.cs](../../src/Scene/GUI/Separator.cs). **Component:** [GUI primitives](../components/gui-primitives.md).

Abstract fixed-axis visual separator. It borrows the `separator` style from inherited typed theme lookup and draws it along the control's long axis. It leaves children and pointer routing to Control. The ordinary stopping pointer policy remains; decorative callers can select Ignore.

The intrinsic long-axis minimum is three pixels. The short-axis minimum is the signed theme `separation` value; `GetCombinedMinimumSize` clamps combined bounds to zero. Style margins determine the centered drawing rectangle, independently of separation. Control and style minimum dimensions truncate to integer pixels before signed integer centering. An oversized style may extend beyond the short axis. This placement does not rotate the style: a vertical StyleBoxLine must itself select `Vertical = true`.

Theme resource edits reuse Control's owner/deferred invalidation and minimum-size notifications. Querying or disposing a separator does not dispose the borrowed style. Exact concrete factories and inherited theme overrides persist in packed/file scenes. The abstract base has no file factory.

## Example

```csharp
var group = new VBoxContainer { Size = new(260, 180) };
group.AddChild(new Label { Text = "Options" });
group.AddChild(new HSeparator { MouseFilter = MouseFilter.Ignore });
group.AddChild(new Button("Apply"));
```

Attach `group` to an application viewport/window to arrange and render it.

## Protected extension points

| Signature | Description |
| --- | --- |
| `protected Separator(bool vertical)` | Constructs a subclass with a fixed drawing axis. |
| `protected override Vector2 OnGetMinimumSize()` | Three pixels on the long axis; theme separation on the short axis. |
| `protected override void OnNotification(int what)` | Records the borrowed centered style during drawing. |

## Method Descriptions

### Separator(bool vertical)

True selects a vertical line; false selects horizontal. Axis choice remains fixed for that instance. A custom subclass supplies its own exact scene factory rather than falling back to a built-in concrete type.

### OnGetMinimumSize()

Horizontal returns `(3, separation)`; vertical returns `(separation, 3)`. Attached theme queries require the owner thread and disposed queries fail. Missing separation uses the ordinary zero constant fallback.

### OnNotification(int what)

Runs inherited notification handling first. During Draw, it obtains the live `separator` style, truncates `Size` and the style's minimum, then records the centered rectangle. Horizontal uses `(0, (height-styleHeight)/2, width, styleHeight)`; vertical uses `((width-styleWidth)/2, 0, styleWidth, height)`, with integer division toward zero. Intermediate subtraction uses widened integers. Missing style throws `InvalidOperationException`; dimensions beyond integer bounds throw `OverflowException` before recording. The style's own expansion, clipping, color and geometry rules remain in effect.

## Themes

`separation` is an integer, with zero fallback. `separator` is a borrowed StyleBox. Built-in HSeparator/VSeparator defaults supply separation four and gray one-pixel StyleBoxLine resources with matching orientation. Horizontal content margins are `(4,0,4,0)`; vertical margins `(0,4,0,4)`. Standard theme type variations and local overrides work normally.

## Verification and limits

GUIPrimitivesTests exercises exact fractional and oversized centering, signed spacing, native type variations, worker theme/style changes, borrowed lifetime, stored concrete factories and warm recording. GUIPrimitivesRenderingTests checks both real axes and live thickness/visibility. The component records actual backend scope and unverified native/platform/accessibility/owner gates.
