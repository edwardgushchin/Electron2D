# ControlGrowDirection

Last updated: 2026-09-24

**Inherits:** —

- **Source:** [`src/Scene/GUI/ControlGrowDirection.cs`](../../src/Scene/GUI/ControlGrowDirection.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum ControlGrowDirection`

Controls how a [`Control`](Control.md) moves when its resolved size is outside the minimum or maximum size bounds.

| Value | Number | Behavior |
| --- | ---: | --- |
| `Begin` | 0 | Moves the leading edge, keeping the trailing edge fixed. |
| `End` | 1 | Keeps the leading edge fixed; the default. |
| `Both` | 2 | Moves both edges equally around the center. |

Horizontal and vertical directions are independent. `ControlLayoutTests` verifies all three values against live minimum-size growth and maximum-size shrinkage. RTL mirroring and complete container sizing remain separate coverage gaps.
