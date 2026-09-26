# TextureProgressBar

Last updated: 2026-09-26

**Inherits:** [Range](Range.md), Control, CanvasItem, Node, ElectronObject

**Declaration:** `public partial class TextureProgressBar : Range` · **Source:** [TextureProgressBar.cs](../../src/Scene/GUI/TextureProgressBar.cs), [Geometry](../../src/Scene/GUI/TextureProgressBar.Geometry.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

Draws borrowed under, progress and over textures in that order with independent color multipliers. The Range ratio selects one of nine fill policies. Ordinary layers use their native texture size, independent of larger Control.Size; NinePatchStretch changes minimum size to margin sums and draws layers using the control size. Radial progress in that mode scales its polygon without border UV patches.

Resources remain caller-owned. Same texture in multiple slots retains a listener for each slot; clearing one slot leaves the others active. Resource changes redraw/update minimum size; disposal clears all matching slots. Geometry, input, owner/capture, clipping, materials and sampling use inherited Control/CanvasItem contracts. The control needs no fonts or theme skin.

## Example

Partial rendered scene snippet; caller owns `texture`:

```csharp
var bar = new TextureProgressBar
{
    TextureProgress = texture, MaxValue = 100, Value = 50,
    FillMode = TextureProgressFillMode.Clockwise,
    RadialInitialAngle = 270, RadialFillDegrees = 180
};
```

## API summary

| Signature | Contract/default |
| --- | --- |
| `public TextureProgressBar()` | Step 1, MouseFilter.Pass, no textures. |
| `public Texture? TextureUnder { get; set; }` | Borrowed null background. |
| `public Texture? TextureProgress { get; set; }` | Borrowed null fill. |
| `public Texture? TextureOver { get; set; }` | Borrowed null foreground. |
| `public Color TintUnder { get; set; }` | Finite white multiplier. |
| `public Color TintProgress { get; set; }` | Finite white multiplier. |
| `public Color TintOver { get; set; }` | Finite white multiplier. |
| `public TextureProgressFillMode FillMode { get; set; }` | LeftToRight=0. |
| `public bool NinePatchStretch { get; set; }` | False. |
| `public Vector2 TextureProgressOffset { get; set; }` | Finite zero offset. |
| `public Vector2 RadialCenterOffset { get; set; }` | Finite zero original-texture center offset. |
| `public float RadialInitialAngle { get; set; }` | 0 degrees; outside [0,360] wraps. |
| `public float RadialFillDegrees { get; set; }` | 360 degrees; clamps [0,360]. |
| `public int StretchMarginLeft/Top/Right/Bottom { get; set; }` | Four independent signed pixel widths, zero. |
| `public int GetStretchMargin(Side margin)` | Returns one defined side. |
| `public void SetStretchMargin(Side margin, int value)` | Sets one side and invalidates minimum/draw. |

## Property descriptions

<a id="textureunder"></a><a id="textureprogress"></a><a id="textureover"></a>
**TextureUnder / Progress / Over:** borrow resources, track content/disposal and invalidate current minimum/draw. Null slots draw nothing. Equal assignment is silent; disposed assignment rejects. Minimum without stretching is max(Vector2.One, all assigned sizes).

<a id="tintunder"></a><a id="tintprogress"></a><a id="tintover"></a>
**Tints:** finite RGBA multipliers, white initially; actual changes redraw.

<a id="fillmode"></a><a id="ninepatchstretch"></a>
**FillMode / NinePatchStretch:** [TextureProgressFillMode](TextureProgressFillMode.md) supplies six linear/centered and three radial policies. Invalid modes reject. Nine-patch linear partial progress adjusts source/destination borders using ratio; under/over use full ratio. Signed border sums determine stretched minimum. Radial progress uses Control.Size and a polygon rather than nine-patch texture mapping.

<a id="textureprogressoffset"></a><a id="radialcenteroffset"></a><a id="radialinitialangle"></a><a id="radialfilldegrees"></a>
**Offsets and angles:** progress offset moves fill geometry. In nine-patch rendering, a layer sharing the exact progress texture identity also receives that offset, matching source identity behavior. Radial center starts at the source texture center plus pixel offset and clamps into normalized [0,1]. Angle is clockwise from twelve o'clock; exactly 360 remains 360, outside values wrap. Sweep clamps to [0,360]. Empty/full radial paths are explicit; partial sectors include boundary rays, square corners and center with source UVs.

<a id="stretchmarginleft"></a><a id="stretchmargintop"></a><a id="stretchmarginright"></a><a id="stretchmarginbottom"></a>
**Stretch margins:** signed Side values, zero initially; equal writes are silent, changed writes update minimum and draw. Source bilateral mapping's singular drift uses a finite fallback instead of invalid native geometry, under ADR 0080.

## Methods, lifecycle and verification

<a id="getstretchmargin"></a><a id="setstretchmargin"></a>
Side accessors require Left/Top/Right/Bottom. Public access uses owner/lifetime/capture guards. [Range](Range.md) supplies shared double value/ratio/signals and policy timing. Descriptors restore config before Value, preserving fractional values despite the default Step=1; exact type is retained. Sharing and resource ownership are not serialized as native runtime state.

[Managed tests](../../tests/Electron2D.Tests/RangeProgressTests.cs) verify resources/modes/angles/minimum/config/errors/packing and shared state. [Native tests](../../tests/Electron2D.Tests/TextureProgressRenderingTests.cs) compare every pixel of nine half/empty/full masks and six linear nine-patch masks, inspect readback grids and measure 64 warmed active radial frames with zero managed allocation on GPU/compatibility Linux Wayland. Native allocators, broad GUI cost, other platforms and owner acceptance are unverified. Inherited vertical size flags remain Blocked until the real Control/Container layout slice; class [coverage](../coverage/classes/TextureProgressBar.md) remains Partial. See [ADR 0080](../decisions/rendering.md#adr-0080).
