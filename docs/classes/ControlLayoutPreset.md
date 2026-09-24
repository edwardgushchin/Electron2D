# ControlLayoutPreset

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/ControlLayoutPreset.cs`](../../src/Scene/GUI/ControlLayoutPreset.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlLayoutPreset`

The sixteen numeric values identify the arrangement applied by [`Control.SetAnchorsPreset`](Control.md), `SetOffsetsPreset` and `SetAnchorsAndOffsetsPreset`. Corner and center values place all four anchors at one point. `LeftWide`, `RightWide` and `VCenterWide` span the parent's height; `TopWide`, `BottomWide` and `HCenterWide` span its width. `FullRect` anchors to all four parent edges.

| Values | Numeric identities |
| --- | --- |
| `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight` | 0, 1, 2, 3 |
| `CenterLeft`, `CenterTop`, `CenterRight`, `CenterBottom`, `Center` | 4, 5, 6, 7, 8 |
| `LeftWide`, `TopWide`, `RightWide`, `BottomWide` | 9, 10, 11, 12 |
| `VCenterWide`, `HCenterWide`, `FullRect` | 13, 14, 15 |

All values and the resulting four-anchor positions are checked in `ControlLayoutTests`. The [coverage page](../coverage/classes/Control.md) records the pinned API identities.
