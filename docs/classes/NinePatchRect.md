# NinePatchRect

Last updated: 2026-09-26

**Inherits:** [Control](Control.md), CanvasItem, Node, ElectronObject

**Declaration:** `public class NinePatchRect : Control` · **Source:** [NinePatchRect.cs](../../src/Scene/GUI/NinePatchRect.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

Draws a borrowed texture with fixed corner borders and independent center-axis stretch/tiling. It records one command and resolves current image/atlas dimensions at submission. Normal transforms, modulation, sampling, material and clipping are inherited. The node does not consume pointer input by default. It needs no theme or font.

The intrinsic minimum is (left+right, top+bottom), including signed configured margins; Control combines this with custom minimum and zero. Source border widths use pixels independently of destination center size. An all-zero region size selects the full base texture. Atlas region/margin mapping clips destination/source before splitting. Negative source extents follow texture-region flipping. A zero-width source center in Stretch samples one constant coordinate; border priority remains deterministic for overlapping margins. TileFit uses a rounded complete repeat count, not ceil.

## Example

Partial snippet; the host owns `texture` and attaches the panel:

```csharp
var panel = new NinePatchRect
{
    Texture = texture,
    Size = new Vector2(200, 80),
    PatchMarginLeft = 8, PatchMarginRight = 8,
    PatchMarginTop = 8, PatchMarginBottom = 8,
    AxisStretchHorizontal = NinePatchRect.AxisStretchMode.TileFit
};
```

## API summary

| Full signature | Contract/default |
| --- | --- |
| `public NinePatchRect()` | Null texture, true center, zero region/margins, Stretch axes; MouseFilter.Ignore. |
| `public Texture? Texture { get; set; }` | Borrowed texture; null initially. |
| `public Rect2 RegionRect { get; set; }` | Finite logical source rectangle, zero initially. |
| `public bool DrawCenter { get; set; }` | True initially. |
| `public AxisStretchMode AxisStretchHorizontal { get; set; }` | Stretch initially. |
| `public AxisStretchMode AxisStretchVertical { get; set; }` | Stretch initially. |
| `public int PatchMarginLeft { get; set; }` | Signed pixels, zero. |
| `public int PatchMarginTop { get; set; }` | Signed pixels, zero. |
| `public int PatchMarginRight { get; set; }` | Signed pixels, zero. |
| `public int PatchMarginBottom { get; set; }` | Signed pixels, zero. |
| `public int GetPatchMargin(Side margin)` | Returns the stored side value. |
| `public void SetPatchMargin(Side margin, int value)` | Updates one side, drawing and minimum size. |
| `public event Action? TextureChanged` | Actual replacement/borrowed disposal clearing, not content updates. |

## Property descriptions

<a id="texture"></a>
**Texture:** commits subscriptions and binding before notifications. Equal references do nothing; caller ownership is retained. Content changes redraw without TextureChanged. Disposal clears the binding. An assigned disposed texture rejects; a notification failure reports after the new value has committed. Live submission also resolves dimensions/atlas mappings independently of callback order.

<a id="regionrect"></a>
**RegionRect:** finite source coordinates/endpoints; a changed value invalidates item bounds and drawing. All-zero size is a full-source sentinel. Atlas clipping follows the view's region and transparent margins before border subdivision.

<a id="drawcenter"></a>
**DrawCenter:** false omits only pieces that lie in both center axes; corners and edge strips remain.

<a id="axisstretchhorizontal"></a>
<a id="axisstretchvertical"></a>
**AxisStretchHorizontal / AxisStretchVertical:** select [AxisStretchMode](NinePatchRect.AxisStretchMode.md) independently. Undefined values reject before assignment. Tile may end with a clipped fragment; TileFit rounds repeat count and uses complete resized tiles.

<a id="patchmarginleft"></a>
<a id="patchmargintop"></a>
<a id="patchmarginright"></a>
<a id="patchmarginbottom"></a>
**PatchMarginLeft / Top / Right / Bottom:** exact Side projection, including signed values; update intrinsic margin sums. Positive border width is not scaled with destination center. Equal assignments are silent. Editor range hints are not imposed on stored integer values.

## Method and event descriptions

<a id="getpatchmargin"></a>
<a id="setpatchmargin"></a>
**GetPatchMargin / SetPatchMargin:** Side must be Left, Top, Right or Bottom. Invalid values throw ArgumentOutOfRangeException; attached owner thread and normal scene capture/disposal guards apply.

<a id="texturechanged"></a>
**TextureChanged:** synchronous parameterless event after committed binding, redraw and minimum invalidation. Errors are aggregated after attempted required notifications. A content edit does not represent a texture replacement.

## Lifecycle, errors and verification

Borrowed texture subscriptions end on replacement or node disposal; borrowed resources are never disposed by this control. PackedScene stores all nine configuration properties, preserves exact type and overrides the inherited pointer-filter descriptor default to Ignore. Existing Control minimum-size/layout, visibility, clipping and input ownership remain authoritative.

CPU geometry is proportional to repeat count with limits of 1,048,574 axis tiles and 1,048,576 piece pairs. Exceeding them throws InvalidOperationException before submission. Errors do not prove a successful frame; capacity preparation and steady-state allocation are separate. [NinePatchTests](../../tests/Electron2D.Tests/NinePatchTests.cs) checks state/resources/packing and independent axis equations. [Native tests](../../tests/Electron2D.Tests/NinePatchRenderingTests.cs) compare every pixel for all nine combinations, center suppression, signed-region flip, atlas/constant-center mapping and 64 warmed actively resized frames with zero managed bytes on GPU/compatibility Linux Wayland. Readback grid images were inspected; native allocator counts, other platforms, dense-panel cost and owner visual acceptance are unverified. [ADR 0079](../decisions/rendering.md#adr-0079) owns the contract and exact remaining RID/progress/theme dependencies.

[Range](../classes/Range.md) and [TextureProgressBar](../classes/TextureProgressBar.md) now execute shared double value policy and textured linear/centered/radial fills. Nine-patch partial progress reuses the real retained geometry/tint path. Their inherited vertical size flag remains Blocked for the first Control.SizeFlags/Container layout consumer; no inert flag is exposed. [RangeProgressTests](../../tests/Electron2D.Tests/RangeProgressTests.cs) and [native tests](../../tests/Electron2D.Tests/TextureProgressRenderingTests.cs) verify the current scope and allocation/platform limits under [ADR 0080](../decisions/rendering.md#adr-0080).
