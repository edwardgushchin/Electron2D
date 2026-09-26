# ScreenEnableMode

Last updated: 2026-09-26

**Declaration:** `public enum ScreenEnableMode` · **Source:** [VisibleOnScreenEnabler.cs](../../src/Scene/2D/VisibleOnScreenEnabler.cs)

Selects [VisibleOnScreenEnabler.EnableMode](VisibleOnScreenEnabler.md#enablemode) while its region is on screen.

| Value | Integer | Node processing |
| --- | ---: | --- |
| `Inherit` | 0 | Inherit the target's ancestor policy. |
| `Always` | 1 | Process regardless of pause. |
| `WhenPaused` | 2 | Process only while paused. |

Off-screen policy is Disabled for every selection. Invalid values reject before assignment. A namespace enum avoids the C# property/nested-type naming collision under [ADR 0078](../decisions/rendering.md#adr-0078); runtime and numeric values are checked by [ScreenVisibilityTests](../../tests/Electron2D.Tests/ScreenVisibilityTests.cs).
