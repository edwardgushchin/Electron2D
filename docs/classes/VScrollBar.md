# VScrollBar

Last updated: 2026-10-26

**Inherits:** [ScrollBar](ScrollBar.md), [Range](Range.md), [Control](Control.md), CanvasItem, [Node](Node.md), ElectronObject · **Inherited By:** —

**Declaration:** `public class VScrollBar : ScrollBar` · **Source:** [ScrollBar.cs](../../src/Scene/GUI/ScrollBar.cs) · **Component:** [Scrolling](../components/scrolling.md)

A top-to-bottom range bar. Its initial horizontal size flag is ShrinkBegin and vertical flag is Fill. The `padding_left` and `padding_right` theme constants default to zero and can be overridden through inherited typed theme methods.

```csharp
var bar = new VScrollBar { MaxValue = 500, Page = 100, Value = 40 };
```

## API summary

| Signature | Contract |
| --- | --- |
| `public VScrollBar()` | Fixes vertical orientation and the inherited ScrollBar defaults. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Recreates an exact VScrollBar during typed scene instantiation. |

All value, input, theme and lifetime behavior comes from [ScrollBar](ScrollBar.md). A subclass that needs exact scene reconstruction supplies its own factory. Managed and Linux Wayland GPU/compatibility checks are linked from the base page; see [coverage](../coverage/classes/VScrollBar.md).

Native numeric accessibility follows [ADR 0114](../decisions/accessibility.md#adr-0114) and [AccessibilityServer numeric values](AccessibilityServer.md#numeric-values). Automatic output publishes the current value, bounds and semantic step. Native requests reuse the ordinary Range.Value policy; progress displays and non-editable inputs are read-only. Page supplies native page actions for editable producers. Text editing/selection is a separate protocol.
