# StyleBoxEmpty

Last updated: 2026-09-27

**Inherits:** [StyleBox](StyleBox.md), [Resource](Resource.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class StyleBoxEmpty : StyleBox` · **Source:** [StyleBox.cs](../../src/Scene/Resources/StyleBox.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description and example

A real content-spacing resource without visible decoration. Its four inherited raw margins start at -1 and resolve to zero; positive content overrides contribute to minimum size. It can reserve space for a consumer while producing no retained geometry.

```csharp
using var style = new StyleBoxEmpty();
style.SetContentMarginAll(4);
var contentOffset = style.GetOffset(); // (4, 4)
var minimum = style.GetMinimumSize();  // (8, 8)
```

## API summary

| Signature | Contract |
| --- | --- |
| `public StyleBoxEmpty()` | Initializes inherited negative overrides and zero fallback margins. |
| `protected override void OnDraw(CanvasItem canvasItem, Rect2 rect)` | Records no commands. |
| `protected override Resource CreateDuplicateInstance()` | Creates an exact StyleBoxEmpty duplicate. |

## Member descriptions

<a id="styleboxempty"></a>
**Constructor:** creates a detached reusable resource. It does not create a Theme, control or renderer object.

<a id="ondraw"></a>
**OnDraw:** intentionally has no geometry. Public Draw still checks finite input, style/target lifetime and the target's recording scope before dispatch. GetDrawRect returns its supplied rectangle; TestMask returns true by inherited default.

<a id="createduplicateinstance"></a>
**CreateDuplicateInstance:** preserves exact identity and uses inherited margin copying. A further subclass must provide its own exact factory under Resource. Resources are owned by the caller, or by the existing scene-local duplication mechanism when instantiated from a packed scene.

## Verification and limitations

[Managed tests](../../tests/Electron2D.Tests/StyleBoxTests.cs) verify margins/defaults, hook dispatch, Changed timing, integer strip geometry, recording context and failure cleanup, exact copies, scene-local resource policy, lifetime and lock boundaries. Sixty-four warmed line mutation/recording/replay cycles allocate zero managed bytes. [Native tests](../../tests/Electron2D.Tests/StyleBoxRenderingTests.cs) verify all nine axis combinations, fractional borders/expansion, center suppression, source regions, modulation, atlas ordering, line and empty drawing on Linux Wayland GPU/compatibility; each backend also passes 64 warmed style mutation/recording/render frames with zero managed bytes measured from ProcessFrameStarted through FramePostDraw. Native allocator counts, broad GUI performance, other platforms and owner acceptance remain unverified. Empty is not a substitute for the unavailable default Panel skin. See [StyleBox](StyleBox.md), [coverage](../coverage/classes/StyleBoxEmpty.md) and [ADR 0082](../decisions/rendering.md#adr-0082).
