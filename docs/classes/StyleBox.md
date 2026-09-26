# StyleBox

Last updated: 2026-09-27

**Inherits:** [Resource](Resource.md), ElectronObject · **Inherited By:** [StyleBoxTexture](StyleBoxTexture.md), [StyleBoxEmpty](StyleBoxEmpty.md), [StyleBoxLine](StyleBoxLine.md)

**Declaration:** `public abstract class StyleBox : Resource` · **Source:** [StyleBox.cs](../../src/Scene/Resources/StyleBox.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

Reusable local-pixel decoration and content margins, with custom drawing, bounds, minimum and mask hooks. A style does not position content or attach itself to a control. A consumer borrows the style, requests redraw/minimum refresh when needed, and calls DrawStyleBox during its own canvas recording. Theme lookup and complete skinned controls remain separate capabilities.

The following custom style can be called from a CanvasItem recording callback:

```csharp
sealed class OutlineStyle : StyleBox
{
    protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)
        => canvasItem.DrawRect(rect, Colors.White, false, 1);
}
// During a CanvasItem.OnDraw callback, with a live borrowed style:
// DrawStyleBox(style, new Rect2(0, 0, 80, 30));
```

The caller owns the style's lifetime. A subclass that supports duplication must provide an exact-type factory under the Resource contract.

## API summary

| Signature | Contract/default |
| --- | --- |
| `protected StyleBox()` | Four raw content margins set to -1. |
| `public float ContentMarginLeft { get; set; }` | Signed finite override, -1 initially. |
| `public float ContentMarginTop { get; set; }` | Signed finite override, -1 initially. |
| `public float ContentMarginRight { get; set; }` | Signed finite override, -1 initially. |
| `public float ContentMarginBottom { get; set; }` | Signed finite override, -1 initially. |
| `public float GetContentMargin(Side margin)` | Returns the raw override. |
| `public void SetContentMargin(Side margin, float offset)` | Sets one override; publishes Changed even if equal. |
| `public void SetContentMarginAll(float offset)` | Atomically sets four overrides; publishes Changed once. |
| `public float GetMargin(Side margin)` | Resolves a negative override to the concrete style fallback. |
| `public Vector2 GetMinimumSize()` | Componentwise maximum of resolved margin sums and custom minimum. |
| `public Vector2 GetOffset()` | Resolved left/top margins. |
| `public Rect2 GetDrawRect(Rect2 rect)` | Queries finite draw bounds through OnGetDrawRect. |
| `public bool TestMask(Vector2 point, Rect2 rect)` | Queries the mask hook. |
| `public void Draw(CanvasItem canvasItem, Rect2 rect)` | Records on the target during its own draw scope. |
| `public CanvasItem? GetCurrentItemDrawn()` | Current recording item on this thread, or null. |
| `protected abstract void OnDraw(CanvasItem canvasItem, Rect2 rect)` | Records concrete geometry after target validation. |
| `protected virtual Vector2 OnGetMinimumSize()` | Additional minimum, zero by default. |
| `protected virtual Rect2 OnGetDrawRect(Rect2 rect)` | Returns the input by default. |
| `protected virtual bool OnTestMask(Vector2 point, Rect2 rect)` | True by default, without containment testing. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stores the four raw content overrides. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies base style state through the existing resource graph mechanism. |

## Property descriptions

<a id="contentmarginleft"></a><a id="contentmargintop"></a><a id="contentmarginright"></a><a id="contentmarginbottom"></a>
**Content margins:** positive values reserve inward content space. Any negative override selects the concrete fallback: zero for Empty, texture borders for Texture, half thickness on the perpendicular sides for Line. Read GetMargin when consuming resolved content space; the properties and GetContentMargin intentionally expose raw values. Every valid write commits before Changed, including equal values. Subscribers run outside the style lock; an exception from a subscriber does not undo state.

## Method descriptions

<a id="getcontentmargin"></a><a id="setcontentmargin"></a><a id="setcontentmarginall"></a><a id="getmargin"></a>
**Margin access:** Side selects Left, Top, Right or Bottom. Undefined sides or nonfinite assigned values throw ArgumentOutOfRangeException. SetContentMarginAll commits all four values atomically and emits one Changed event. The style-specific fallback can itself be signed; resolution does not clamp it to zero.

<a id="getminimumsize"></a><a id="getoffset"></a><a id="ongetminimumsize"></a>
**Minimum and offset:** opposing resolved margins are summed for each axis, then compared componentwise with OnGetMinimumSize. The zero default hook prevents a negative default minimum. A custom finite minimum may increase either axis independently. GetOffset returns only the resolved left/top pair. A nonfinite sum or hook result throws InvalidOperationException.

<a id="getdrawrect"></a><a id="ongetdrawrect"></a>
**Draw bounds:** GetDrawRect validates its input and the protected hook's result. It is a query and never changes the rectangle passed to Draw. Texture styles expand the requested rectangle; Empty and Line inherit unchanged bounds. Nonfinite input throws ArgumentException; nonfinite custom output throws InvalidOperationException.

<a id="testmask"></a><a id="ontestmask"></a>
**Mask:** finite point and rectangle are passed to OnTestMask without an implicit rectangle intersection. The default returns true even for a point outside rect. No automatic GUI hit-mask integration is claimed. Nonfinite coordinates throw ArgumentException.

<a id="draw"></a><a id="ondraw"></a>
**Drawing:** validates a live target, finite rectangle and the target's own recording scope before invoking OnDraw. All geometry uses the existing retained command path. Null target throws ArgumentNullException; a disposed style/target or consumed borrowed texture throws ObjectDisposedException; invalid owner/scope or strip geometry outside integer pixel range throws InvalidOperationException. Nonfinite texture-derived geometry follows the existing canvas/atlas ArgumentException boundary. The same validation applies to an empty style. Custom-hook failure propagates through normal recording cleanup; the canvas discards partial commands from a failed recording.

<a id="getcurrentitemdrawn"></a>
**Current item:** returns the calling thread's active CanvasItem during NotificationDraw, Draw event and OnDraw. Outside recording, and from another thread, it returns null. The prior context is restored after every recording, including callback failure. The query does not acquire ownership of the returned item.

<a id="getpropertydescriptors"></a><a id="copycustomstateto"></a>
**Storage/copy hooks:** stored raw margins retain their negative fallback identity. CopyCustomStateTo snapshots them before applying the existing Resource copy policy. Concrete factories preserve exact resource types. Scene-local style instantiation clones built-in or scene-local texture children and shares nonlocal external textures under the existing graph policy. No canvas item or recording context is stored.

## Lifecycle, threading and verification

Built-in resource state uses a private style lock; custom hooks and Changed callbacks execute outside it. Drawing still requires the canvas owner thread, and caller hooks own their own thread-safety. Retained commands capture draw values, so changing a style requires the consumer to queue redraw; content layout likewise needs an explicit minimum refresh. Style and texture ownership remain separate from canvas lifetime.

[Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. See [coverage](../coverage/classes/StyleBox.md), [Resource](Resource.md) and [ADR 0082](../decisions/rendering.md#adr-0082). Global Theme/default-skin lookup, StyleBoxFlat and complete skinned controls remain separate.
