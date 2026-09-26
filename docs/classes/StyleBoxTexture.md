# StyleBoxTexture

Last updated: 2026-09-27

**Inherits:** [StyleBox](StyleBox.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class StyleBoxTexture : StyleBox` · **Source:** [StyleBoxTexture.cs](../../src/Scene/Resources/StyleBoxTexture.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Reusable nine-patch decoration with separate content, texture-border and outward drawing margins. The caller owns the resource and its borrowed texture. It records through the same retained nine-patch geometry as NinePatchRect, with independent Stretch/Tile/TileFit axes and optional center.

```csharp
// texture is a live caller-owned Texture; keep it alive through rendering.
using var style = new StyleBoxTexture { Texture = texture };
style.SetTextureMarginAll(4);
style.SetExpandMarginAll(2);
style.ContentMarginLeft = 8;
// In a CanvasItem drawing callback:
// DrawStyleBox(style, new Rect2(10, 10, 80, 30));
```

The example has left content margin eight, other content margins falling back to texture borders four, and drawing expanded outward by two. A consumer requests redraw and any required minimum refresh after style changes.

## API summary

| Signature | Contract/default |
| --- | --- |
| `public StyleBoxTexture()` | No texture, zero border/expand margins, white tint, center enabled. |
| `public Texture? Texture { get; set; }` | Borrowed drawing resource; null initially. |
| `public Rect2 RegionRect { get; set; }` | Finite source rectangle; empty initially selects current texture extent. |
| `public Color ModulateColor { get; set; }` | Finite tint; white initially. |
| `public bool DrawCenter { get; set; }` | True initially. |
| `public AxisStretchMode AxisStretchHorizontal { get; set; }` | Stretch initially. |
| `public AxisStretchMode AxisStretchVertical { get; set; }` | Stretch initially. |
| `public float TextureMarginLeft { get; set; }` | Signed source border width; zero initially. |
| `public float TextureMarginTop { get; set; }` | Signed source border width; zero initially. |
| `public float TextureMarginRight { get; set; }` | Signed source border width; zero initially. |
| `public float TextureMarginBottom { get; set; }` | Signed source border width; zero initially. |
| `public float ExpandMarginLeft { get; set; }` | Signed outward drawing expansion; zero initially. |
| `public float ExpandMarginTop { get; set; }` | Signed outward drawing expansion; zero initially. |
| `public float ExpandMarginRight { get; set; }` | Signed outward drawing expansion; zero initially. |
| `public float ExpandMarginBottom { get; set; }` | Signed outward drawing expansion; zero initially. |
| `public float GetTextureMargin(Side margin)` | Returns the stored border width. |
| `public void SetTextureMargin(Side margin, float size)` | Sets one border and publishes Changed. |
| `public void SetTextureMarginAll(float size)` | Atomically sets all borders and publishes Changed once. |
| `public float GetExpandMargin(Side margin)` | Returns the stored outward expansion. |
| `public void SetExpandMargin(Side margin, float size)` | Sets one expansion and publishes Changed. |
| `public void SetExpandMarginAll(float size)` | Atomically sets all expansions and publishes Changed once. |
| `protected override Rect2 OnGetDrawRect(Rect2 rect)` | Expands the requested rectangle. |
| `protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)` | Records resolved atlas/nine-patch geometry. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the fourteen texture-style properties. |
| `protected override Resource CreateDuplicateInstance()` | Preserves exact StyleBoxTexture identity. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies style state and applies subresource policy to Texture. |
| `protected override void Dispose(bool disposing)` | Releases the borrowed reference without disposing its texture. |
| `public enum AxisStretchMode` | [Stretch=0, Tile=1, TileFit=2](StyleBoxTexture.AxisStretchMode.md). |

## Property descriptions

<a id="texture"></a>
**Texture:** null produces no decoration after ordinary Draw validation. An equal live reference is silent. Assigning a disposed texture throws ObjectDisposedException before mutation; an externally disposed stored texture remains readable, but drawing fails until it is replaced. The style does not subscribe to Texture.Changed. Recorded commands use the existing live texture-pixel cache; texture region/dimension or style configuration changes that alter recorded geometry require a redraw request by the consumer.

<a id="regionrect"></a><a id="modulatecolor"></a>
**RegionRect and ModulateColor:** equal values are silent; actual changes commit before Changed. Empty region selects the current source extent. Nonempty regions use texture-pixel coordinates and the existing atlas mapping. ModulateColor multiplies texture pixels before inherited canvas modulation. Nonfinite input throws ArgumentException.

<a id="drawcenter"></a><a id="axisstretchhorizontal"></a><a id="axisstretchvertical"></a>
**Center and axis policies:** DrawCenter omits only the center when false. Horizontal and vertical policies independently stretch or tile the middle source portions. Every valid assignment publishes Changed, including equal values. Undefined enum values throw ArgumentOutOfRangeException before mutation. See [AxisStretchMode](StyleBoxTexture.AxisStretchMode.md).

<a id="texturemarginleft"></a><a id="texturemargintop"></a><a id="texturemarginright"></a><a id="texturemarginbottom"></a>
**Texture margins:** signed fractional source-border widths determine the 3×3 split and supply fallback content margins when inherited content overrides are negative. They do not apply outward expansion. Negative and fractional values are stored without clamping; concrete geometry follows the shared nine-patch contract.

<a id="expandmarginleft"></a><a id="expandmargintop"></a><a id="expandmarginright"></a><a id="expandmarginbottom"></a>
**Expand margins:** signed fractional outward drawing expansion. Left/top subtract from the destination position, and opposing margins add to destination size. Negative values shrink the decoration. They do not change content margins or minimum size.

## Method descriptions

<a id="gettexturemargin"></a><a id="settexturemargin"></a><a id="settexturemarginall"></a><a id="getexpandmargin"></a><a id="setexpandmargin"></a><a id="setexpandmarginall"></a>
**Margin methods:** Side selects one of the four named properties. Every setter emits Changed even for equal values; each all-sides call changes four values atomically and emits once. Invalid Side and nonfinite size throw ArgumentOutOfRangeException before mutation. Disposed access throws ObjectDisposedException.

<a id="ongetdrawrect"></a><a id="ondraw"></a>
**Draw hooks:** the bounds query simply expands the requested rectangle by stored expand margins. Drawing first resolves nested AtlasTexture source/destination regions, then expands the resolved destination and records one nine-patch command with border margins, axis policies, center and tint. This order preserves atlas margins/clipping before outward expansion. A null or empty atlas source emits no geometry. GetDrawRect remains a query of the requested bounds and does not apply atlas clipping. Nonfinite resulting geometry fails before recording a command.

<a id="getpropertydescriptors"></a><a id="createduplicateinstance"></a><a id="copycustomstateto"></a><a id="dispose"></a>
**Storage, copying and cleanup:** raw content, texture-border and expand margins remain distinct. Shallow duplicates share the texture; deep duplicates use Resource's selected subresource graph policy, preserving identity/aliases where required. Instantiating a scene-local style clones its built-in or scene-local texture subresources; a nonlocal external texture remains shared under that existing policy. The built-in factory returns exact StyleBoxTexture; further subclasses require an exact factory of their own. Dispose clears only this style's texture reference and then performs ordinary Resource cleanup.

## Lifecycle and verification

The inherited style lock, synchronous post-commit Changed delivery, custom-hook and owner-thread drawing rules apply. Current draw context and mask/minimum semantics remain inherited. [Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. See [coverage](../coverage/classes/StyleBoxTexture.md) and [ADR 0082](../decisions/rendering.md#adr-0082); global Theme/default-skin lookup and complete skinned-control behavior are separate.

Native nearest-sampling checks compare interior texel samples; exact UV texel boundaries are omitted because raster interpolation can choose the adjacent texel on different backends. This is a precision verification limit, not an extra drawing mode.
