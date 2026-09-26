# StyleBoxFlat

Last updated: 2026-09-27

**Inherits:** [StyleBox](StyleBox.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public partial class StyleBoxFlat : StyleBox` · **Source:** [StyleBoxFlat.cs](../../src/Scene/Resources/StyleBoxFlat.cs), [StyleBoxFlat.Geometry.cs](../../src/Scene/Resources/StyleBoxFlat.Geometry.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

A reusable filled or hollow rounded rectangle with independent borders, corner radii, outward expansion, border blending, shadows, skew and anti-aliasing. It records untextured colored triangles through the existing canvas path. The caller owns the resource, draws it through CanvasItem.DrawStyleBox during recording, and requests redraw or content layout refresh after changes.

```csharp
using var style = new StyleBoxFlat
{
    BGColor = new Color(0.15f, 0.2f, 0.3f),
    BorderColor = Colors.White,
    ShadowSize = 4,
    ShadowOffset = new Vector2(2, 2)
};
style.SetBorderWidthAll(2);
style.SetCornerRadiusAll(8);
// Inside a CanvasItem drawing callback:
// DrawStyleBox(style, new Rect2(10, 10, 120, 40));
```

Border widths are the fallback content margins when inherited content overrides are negative. Expansion, shadow, corner radius and skew affect drawing but do not add content space. Global Theme/default-skin lookup and automatic panel/control integration remain separate capabilities.

## API summary

| Signature | Contract/default |
| --- | --- |
| `public StyleBoxFlat()` | Initializes the defaults below and inherited content margins -1. |
| `public Color BGColor { get; set; }` | `(0.6, 0.6, 0.6, 1)`. |
| `public Color BorderColor { get; set; }` | `(0.8, 0.8, 0.8, 1)`. |
| `public bool BorderBlend { get; set; }` | False. |
| `public int BorderWidthLeft { get; set; }` | Signed integer, zero. |
| `public int BorderWidthTop { get; set; }` | Signed integer, zero. |
| `public int BorderWidthRight { get; set; }` | Signed integer, zero. |
| `public int BorderWidthBottom { get; set; }` | Signed integer, zero. |
| `public int CornerRadiusTopLeft { get; set; }` | Signed integer, zero. |
| `public int CornerRadiusTopRight { get; set; }` | Signed integer, zero. |
| `public int CornerRadiusBottomRight { get; set; }` | Signed integer, zero. |
| `public int CornerRadiusBottomLeft { get; set; }` | Signed integer, zero. |
| `public int CornerDetail { get; set; }` | Eight; assignments clamp to 1..20. |
| `public bool DrawCenter { get; set; }` | True. |
| `public float ExpandMarginLeft { get; set; }` | Signed finite pixels, zero. |
| `public float ExpandMarginTop { get; set; }` | Signed finite pixels, zero. |
| `public float ExpandMarginRight { get; set; }` | Signed finite pixels, zero. |
| `public float ExpandMarginBottom { get; set; }` | Signed finite pixels, zero. |
| `public Color ShadowColor { get; set; }` | `(0, 0, 0, 0.6)`. |
| `public Vector2 ShadowOffset { get; set; }` | Zero. |
| `public int ShadowSize { get; set; }` | Signed integer, zero; positive values enable shadow geometry. |
| `public Vector2 Skew { get; set; }` | Zero. |
| `public bool AntiAliasing { get; set; }` | True; also publishes PropertyListChanged. |
| `public float AntiAliasingSize { get; set; }` | One; finite assignments clamp to 0.01..10. |
| `public int GetBorderWidth(Side margin)` | Raw signed border width. |
| `public int GetBorderWidthMin()` | Smallest of the four raw signed border widths. |
| `public void SetBorderWidth(Side margin, int width)` | Sets one border width. |
| `public void SetBorderWidthAll(int width)` | Sets all widths and emits Changed once. |
| `public int GetCornerRadius(Corner corner)` | Raw signed corner radius. |
| `public void SetCornerRadius(Corner corner, int radius)` | Sets one corner radius. |
| `public void SetCornerRadiusAll(int radius)` | Sets all radii and emits Changed once. |
| `public float GetExpandMargin(Side margin)` | Raw signed expansion. |
| `public void SetExpandMargin(Side margin, float size)` | Sets one finite expansion. |
| `public void SetExpandMarginAll(float size)` | Sets all finite expansions and emits Changed once. |
| `protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)` | Records rounded fill/border/shadow triangles. |
| `protected override Rect2 OnGetDrawRect(Rect2 rect)` | Queries expansion plus positive shadow bounds. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the 23 own properties and inherited raw content margins. |
| `protected override Resource CreateDuplicateInstance()` | Creates an exact StyleBoxFlat duplicate. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies all value state through Resource graph copying. |

## Property descriptions

All valid property assignments publish Changed after state commitment, even when the stored value is unchanged. AntiAliasing then publishes PropertyListChanged if the style remains live, including after a Changed observer fails. The existing Resource error combiner rethrows a single error or aggregates both errors after both phases have been attempted. The three all-sides/all-corners methods each commit four values and publish one Changed event. Callback failures do not undo committed state.

<a id="bgcolor"></a><a id="bordercolor"></a><a id="borderblend"></a><a id="drawcenter"></a>
**BGColor, BorderColor, BorderBlend and DrawCenter:** the background fills the interior only when DrawCenter is true. At least one positive border width enables the border. BorderBlend interpolates its inner edge toward BGColor for a filled style, or toward transparent border color for a hollow style. Colors remain independent of inherited canvas modulation. Nonfinite colors throw ArgumentException before mutation.

<a id="borderwidthleft"></a><a id="borderwidthtop"></a><a id="borderwidthright"></a><a id="borderwidthbottom"></a>
**Border widths:** signed integer widths remain unmodified in resource state and supply each side's fallback content margin. Draw-time adaptation limits opposing borders to the destination dimensions to avoid overlap; this does not rewrite the stored widths. Inherited GetMinimumSize uses resolved content margins with the default zero minimum hook.

<a id="cornerradiustopleft"></a><a id="cornerradiustopright"></a><a id="cornerradiusbottomright"></a><a id="cornerradiusbottomleft"></a><a id="cornerdetail"></a>
**Corner radii and detail:** radii use [Corner](Corner.md)'s clockwise order, retain signed integer input, and are adapted to adjacent dimensions/borders while drawing. CornerDetail chooses arc subdivisions and clamps to 1..20, with eight by default. A non-rounded ring uses its sharp-corner geometry instead of unnecessary arc subdivisions. Radius does not change content margins or minimum size.

<a id="expandmarginleft"></a><a id="expandmargintop"></a><a id="expandmarginright"></a><a id="expandmarginbottom"></a>
**Expansion:** signed finite local pixels. Left/top subtract from the requested position; opposing margins add to size. Negative expansion shrinks the decoration. Border/radius adaptation and shadow geometry operate on this expanded rectangle. Content spacing stays independent.

<a id="shadowcolor"></a><a id="shadowoffset"></a><a id="shadowsize"></a>
**Shadow:** positive ShadowSize draws a fading ring around the offset style rectangle before the main decoration; DrawCenter also controls the solid shadow interior. Nonpositive size suppresses the shadow but remains stored. ShadowOffset moves only shadow geometry. GetDrawRect merges the positive shadow's bounds regardless of shadow color alpha. Shadow colors/offsets must be finite and never change content minimums.

<a id="skew"></a>
**Skew:** a finite two-component shear about the expanded style center. For each original vertex, X changes by `-Skew.X * (Y - center.Y)` and Y by `-Skew.Y * (X - center.X)`; both offsets use the original coordinates. It applies to fill, borders and shadows. The draw-bound query deliberately excludes skew.

<a id="antialiasing"></a><a id="antialiasingsize"></a>
**Anti-aliasing:** feather bands are generated only for a positive corner radius or non-negligible skew when AntiAliasing is enabled. Sharp unskewed rectangles keep sharp edges. AntiAliasingSize is finite and clamps to 0.01..10 local pixels. The current viewport uses identity content stretch and no font/canvas oversampling override, so the recording factor is one. The first viewport/font oversampling slice must divide feather width by the active factor and invalidate retained recordings when it changes; nonunit oversampling is not implemented here. Neither AA bands nor skew enlarge GetDrawRect's result.

## Method descriptions

<a id="getborderwidth"></a><a id="getborderwidthmin"></a><a id="setborderwidth"></a><a id="setborderwidthall"></a><a id="getcornerradius"></a><a id="setcornerradius"></a><a id="setcornerradiusall"></a><a id="getexpandmargin"></a><a id="setexpandmargin"></a><a id="setexpandmarginall"></a>
**Indexed and grouped access:** methods share storage with the corresponding properties. GetBorderWidthMin returns the signed minimum across all four borders without clamping. Side and Corner must name valid values; undefined casts throw ArgumentOutOfRangeException before indexed access. Nonfinite expansion throws ArgumentOutOfRangeException before mutation. Group setters publish one Changed event including equal assignments.

<a id="ondraw"></a><a id="ongetdrawrect"></a>
**Geometry hooks:** expand the request, adapt opposing border/corner dimensions, record shadow then border/fill and any AA bands, shear vertices and retain the resulting colored triangles. UV coordinates cover the complete expanded rectangle plus AA extent, supporting the existing material path even without a texture. An approximately zero expanded width/height emits no geometry; a style with no positive border/shadow and DrawCenter=false also emits none. Public Draw still validates scope/lifetime/input before dispatch.

GetDrawRect only applies expansion and, for positive ShadowSize, merges the expanded-and-offset shadow rectangle. It does not measure final triangles, skew or feather bands. The inherited finite-result guard applies.

<a id="getpropertydescriptors"></a><a id="createduplicateinstance"></a><a id="copycustomstateto"></a>
**Storage and copying:** typed descriptors preserve exact defaults, signed raw state, clamps and inherited content overrides. Flat owns no texture; shallow and deep duplication both copy its value configuration through Resource's coalesced change mechanism. The factory preserves exact StyleBoxFlat identity; further subclasses supply their own exact factory.

## Lifecycle, errors and verification

Inherited style ownership, lifetime, lock and draw-thread rules apply. Mutation commits before callbacks; custom drawing requires the target's active recording scope. Invalid enum/finite inputs and computed nonfinite geometry fail explicitly. Warm tessellation buffers are reused, with capacity growth outside the measured steady state.

[StyleBoxFlatTests](../../tests/Electron2D.Tests/StyleBoxFlatTests.cs) verifies defaults, Corner identities, equal-write event ordering, clamps/guards, content/draw bounds, hooks, exact resource copies, scene-local behavior, callback failures and concurrency. The [independent C++ fixture](../../tests/Electron2D.Tests/Fixtures/StyleBoxFlatGeometry.json) covers 15 sharp/rounded/unequal/oversized/blended/hollow/AA/shadow/skew/signed/degenerate profiles; triangle ordering, vertex positions, colors and UVs match within 0.00005, and draw rectangles match exactly. Sixty-four warmed mutation/geometry-recording/replay cycles allocate zero managed bytes. [StyleBoxFlatRenderingTests](../../tests/Electron2D.Tests/StyleBoxFlatRenderingTests.cs) verifies three visible mutation states covering rounded corners, borders, center suppression, border blend, offset shadow, skew, AA and expansion on Linux Wayland GPU/compatibility. Each backend also passes 64 warmed mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, nonunit oversampling, other platforms and owner acceptance remain unverified. Global Theme/default-skin lookup, Panel/PanelContainer integration, nonunit oversampling, text shaping and broader platform/owner acceptance remain separate. See [coverage](../coverage/classes/StyleBoxFlat.md) and [ADR 0082](../decisions/rendering.md#adr-0082).
