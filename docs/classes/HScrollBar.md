# HScrollBar

Last updated: 2026-09-30

**Inherits:** [ScrollBar](ScrollBar.md), [Range](Range.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class HScrollBar : ScrollBar` · **Source:** [ScrollBar.cs](../../src/Scene/GUI/ScrollBar.cs) · **Component:** [Scrolling](../components/scrolling.md)

A left-to-right range bar, including under RTL layout. Its initial horizontal size flag is Fill and vertical flag is ShrinkBegin. The `padding_top` and `padding_bottom` theme constants default to zero; they remain ordinary typed theme overrides, not stored built-in constants.

```csharp
var bar = new HScrollBar { MaxValue = 500, Page = 100, Value = 40 };
```

## API summary

| Signature | Contract |
| --- | --- |
| `public HScrollBar()` | Fixes horizontal orientation and the inherited ScrollBar defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates an exact HScrollBar during typed scene instantiation. |

All value, input, theme and lifetime behavior comes from [ScrollBar](ScrollBar.md). A subclass that needs exact scene reconstruction supplies its own factory. Managed and Linux Wayland GPU/compatibility checks are linked from the base page; see [coverage](../coverage/classes/HScrollBar.md).
