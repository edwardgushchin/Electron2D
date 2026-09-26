# CharacterMotionMode

Last updated: 2026-09-26

**Declaration:** `public enum CharacterMotionMode` · **Source:** [CharacterBody.cs](../../src/Scene/2D/CharacterBody.cs)

Selects the [CharacterBody](CharacterBody.md) contact classifier. The enum is separate from the `MotionMode` property because C# cannot give a nested enum and property the same identifier.

| Value | Numeric ID | Behavior |
| --- | ---: | --- |
| `Grounded` | 0 | Classify floor and ceiling against UpDirection/FloorMaxAngle. |
| `Floating` | 1 | Treat all contacts as walls and use WallMinSlideAngle. |

An undefined value rejects before changing the body. [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks both branches and PackedScene. See [ADR 0067](../decisions/physics.md#adr-0067).
