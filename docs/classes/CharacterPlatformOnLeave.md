# CharacterPlatformOnLeave

Last updated: 2026-09-26

**Declaration:** `public enum CharacterPlatformOnLeave` · **Source:** [CharacterBody.cs](../../src/Scene/2D/CharacterBody.cs)

Selects how [CharacterBody](CharacterBody.md) changes `Velocity` when it leaves a moving floor or wall. The enum is separate from the `PlatformOnLeave` property because C# cannot give a nested enum and property the same identifier.

| Value | Numeric ID | Behavior |
| --- | ---: | --- |
| `AddVelocity` | 0 | Add the last platform point velocity. |
| `AddUpwardVelocity` | 1 | Add platform velocity but drop a downward component. |
| `DoNothing` | 2 | Leave character velocity unchanged. |

An undefined value rejects before mutation. [CharacterBodyTests](../../tests/Electron2D.Tests/CharacterBodyTests.cs) checks all three branches and PackedScene. See [ADR 0067](../decisions/physics.md#adr-0067).
