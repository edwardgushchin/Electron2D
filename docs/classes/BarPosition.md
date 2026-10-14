# BarPosition

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public enum BarPosition`. **Source:** [BarPosition.cs](../../src/Scene/GUI/BarPosition.cs). **Component:** [Tab panels](../components/tab-panels.md).

Places a tab header or foldable title above or below its content. Assign `tabs.TabsPosition = BarPosition.Bottom;` to place it below. This position domain is distinct from tab alignment and is shared with FoldableContainer under ADR 0051.

| Declaration | Value | Contract |
| --- | --- | --- |
| `Top = 0` | 0 | Header above content; default. |
| `Bottom = 1` | 1 | Header below content. |
| `Max = 2` | 2 | Exclusive upper bound; assignment throws ArgumentOutOfRangeException. |

Top/Bottom are stored typed scene values. Headless and actual GPU/compatibility layout/input tests exercise both positions and RTL; source XML and generated wiki retain the same values.
