# ControlGrowDirection

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/ControlGrowDirection.cs`](../../src/Scene/GUI/ControlGrowDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlGrowDirection`

Controls how a [`Control`](Control.md) moves when its resolved size is smaller than its minimum size.

| Value | Number | Behavior |
| --- | ---: | --- |
| `Begin` | 0 | Moves the leading edge, keeping the trailing edge fixed. |
| `End` | 1 | Keeps the leading edge fixed; the default. |
| `Both` | 2 | Moves both edges equally around the center. |

Horizontal and vertical directions are independent. `ControlLayoutTests` verifies all three values against live minimum-size reflow. Maximum-size behavior remains a separate coverage gap.
