# ScrollContainer.ScrollMode

Last updated: 2026-09-30

**Declaration:** `public enum ScrollContainer.ScrollMode` · **Source:** [ScrollContainer.cs](../../src/Scene/GUI/ScrollContainer.cs) · **Component:** [Scrolling](../components/scrolling.md)

The axis policy used by `HorizontalScrollMode` and `VerticalScrollMode`. The two properties reject undefined numeric values and default to Auto.

| Value | Numeric value | Effect |
| --- | ---: | --- |
| `Disabled` | 0 | Fit content and disable movement. |
| `Auto` | 1 | Scroll and show a bar only on overflow. |
| `ShowAlways` | 2 | Scroll with a visible bar. |
| `ShowNever` | 3 | Scroll without a visible bar. |
| `Reserve` | 4 | Scroll and reserve bar space even when hidden. |
| `MaximizeFirst` | 5 | Prefer bounded content size before scrolling. |

See [ScrollContainer](ScrollContainer.md) for layout and input interactions, and [coverage](../coverage/classes/ScrollContainer.md) for numeric correspondence.
