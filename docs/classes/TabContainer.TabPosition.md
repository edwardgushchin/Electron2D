# TabContainer.TabPosition

Last updated: 2026-10-06

**Namespace:** `Electron2D`. **Declaration:** `public enum TabContainer.TabPosition`. **Owner:** [TabContainer](TabContainer.md). **Source:** [TabContainer.cs](../../src/Scene/GUI/TabContainer.cs). **Component:** [Tab panels](../components/tab-panels.md).

Places the header above or below page content. Assign `tabs.TabsPosition = TabContainer.TabPosition.Bottom;` to place it below. This position domain is distinct from tab alignment and retains its declaring owner under ADR 0051.

| Declaration | Value | Contract |
| --- | --- | --- |
| `Top = 0` | 0 | Header above content; default. |
| `Bottom = 1` | 1 | Header below content. |
| `Max = 2` | 2 | Exclusive upper bound; assignment throws ArgumentOutOfRangeException. |

Top/Bottom are stored typed scene values. Headless and actual GPU/compatibility layout/input tests exercise both positions and RTL; source XML and generated wiki retain the same values.
