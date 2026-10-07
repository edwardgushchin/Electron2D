# CPUParticles.Parameter

Last updated: 2026-10-07

- Declaration: `public enum CPUParticles.Parameter`
- Source: [CPUParticles.cs](../../src/Scene/2D/CPUParticles.cs)
- Owner: [CPUParticles](CPUParticles.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

Twelve applicable CPU simulation channels share GetParamMin/Max/Curve and paired setters. Values and samples feed actual forces/appearance; Max is not selectable. InitialLinearVelocity accepts its generic curve even though it has no dedicated curve property.

## Values

| Declaration | Meaning |
| --- | --- |
| `public const Electron2D.CPUParticles.Parameter Angle = 7` | Controls degrees. |
| `public const Electron2D.CPUParticles.Parameter AngularVelocity = 1` | Controls degrees per second. |
| `public const Electron2D.CPUParticles.Parameter AnimOffset = 11` | Controls normalized animation offset. |
| `public const Electron2D.CPUParticles.Parameter AnimSpeed = 10` | Controls animation cycles over particle lifetime. |
| `public const Electron2D.CPUParticles.Parameter Damping = 6` | Controls pixels per second squared. |
| `public const Electron2D.CPUParticles.Parameter HueVariation = 9` | Controls hue turns. |
| `public const Electron2D.CPUParticles.Parameter InitialLinearVelocity = 0` | Controls pixels per second. |
| `public const Electron2D.CPUParticles.Parameter LinearAccel = 3` | Controls pixels per second squared. |
| `public const Electron2D.CPUParticles.Parameter Max = 12` | Nonselectable bound. |
| `public const Electron2D.CPUParticles.Parameter OrbitVelocity = 2` | Controls clockwise turns per second. |
| `public const Electron2D.CPUParticles.Parameter RadialAccel = 4` | Controls pixels per second squared. |
| `public const Electron2D.CPUParticles.Parameter Scale = 8` | Controls scale multiplier. |
| `public const Electron2D.CPUParticles.Parameter TangentialAccel = 5` | Controls pixels per second squared. |

Invalid selections throw ArgumentOutOfRangeException before state changes. These types do not expose backend enums or dynamic selector values. Executable guards and corresponding behavior are checked by CPUParticlesTests.
