# StyleBoxLine

Last updated: 2026-09-27

**Inherits:** [StyleBox](StyleBox.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class StyleBoxLine : StyleBox` · **Source:** [StyleBoxLine.cs](../../src/Scene/Resources/StyleBoxLine.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

A reusable horizontal or vertical filled strip. Thickness supplies the perpendicular size and fallback content margins; signed growth adjusts its two endpoints. It uses the existing retained rectangle path with canvas transforms, modulation and clipping.

```csharp
using var style = new StyleBoxLine { Color = Colors.White, Thickness = 2,
    GrowBegin = 0, GrowEnd = 0 };
// In a CanvasItem drawing callback:
// DrawStyleBox(style, new Rect2(4, 8, 80, 20));
```

A horizontal style replaces the rectangle height with its thickness; it does not center a stroke within the supplied height. The caller owns the style and requests redraw after its state changes.

## API summary

| Signature | Contract/default |
| --- | --- |
| `public StyleBoxLine()` | Black, thickness one, horizontal, endpoint growth one. |
| `public Color Color { get; set; }` | Finite strip color, black initially. |
| `public int Thickness { get; set; }` | Signed perpendicular extent, one initially. |
| `public bool Vertical { get; set; }` | False for horizontal. |
| `public float GrowBegin { get; set; }` | Signed finite leading growth, one initially. |
| `public float GrowEnd { get; set; }` | Signed finite trailing growth, one initially. |
| `protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)` | Records integer strip geometry. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds the five line properties. |
| `protected override Resource CreateDuplicateInstance()` | Preserves exact StyleBoxLine identity. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies line and inherited content state. |

## Property descriptions

<a id="color"></a><a id="thickness"></a><a id="vertical"></a><a id="growbegin"></a><a id="growend"></a>
**Color, Thickness, Vertical, GrowBegin and GrowEnd:** every valid assignment commits and emits Changed, including equal values. Signed thickness and growth are retained. Nonfinite color throws ArgumentException; nonfinite growth throws ArgumentOutOfRangeException before mutation; subscriber failures follow commitment.

Negative inherited content overrides resolve to half Thickness on top/bottom for a horizontal line, or left/right for a vertical line. Along-line fallback margins are zero. Explicit nonnegative content margins override this fallback. The inherited default minimum hook is zero, so signed fallback sums cannot make the default minimum negative.

## Method descriptions

<a id="ondraw"></a>
**OnDraw:** truncates rectangle position and size toward zero to integer pixels. Horizontal drawing then truncates `X - GrowBegin` and `Width + GrowBegin + GrowEnd`, and sets height to Thickness; vertical drawing performs the corresponding Y/height operations and sets width. It records a filled rectangle using Color. Zero/negative extents follow the existing rectangle command policy; inputs outside integer range throw InvalidOperationException. The inherited GetDrawRect query still returns the supplied rectangle, not the grown strip bounds.

<a id="getpropertydescriptors"></a><a id="createduplicateinstance"></a><a id="copycustomstateto"></a>
**Storage and copying:** descriptors preserve line defaults and inherited raw margins. Exact shallow/deep copies use Resource's graph machinery; the line owns no texture. A custom subclass must supply its own exact factory.

## Lifecycle and verification

The inherited style lock, callback, finite-input and drawing-owner rules apply. [Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. See [StyleBox](StyleBox.md), [coverage](../coverage/classes/StyleBoxLine.md) and [ADR 0082](../decisions/rendering.md#adr-0082).
