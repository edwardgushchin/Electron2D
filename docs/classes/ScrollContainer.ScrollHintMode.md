# ScrollContainer.ScrollHintMode

Last updated: 2026-09-30

**Declaration:** `public enum ScrollContainer.ScrollHintMode` · **Source:** [ScrollContainer.cs](../../src/Scene/GUI/ScrollContainer.cs) · **Component:** [Scrolling](../components/scrolling.md)

The directional hint policy stored by `ScrollContainer.HintMode`. An edge still must have hidden content in that direction; no hint appears at a reached boundary.

| Value | Numeric value | Effect |
| --- | ---: | --- |
| `Disabled` | 0 | No hints. |
| `All` | 1 | Both eligible edges. |
| `TopAndLeft` | 2 | Top and leading side. |
| `BottomAndRight` | 3 | Bottom and trailing side. |

The horizontal edges exchange physical sides under RTL. See [ScrollContainer](ScrollContainer.md) and [coverage](../coverage/classes/ScrollContainer.md).
