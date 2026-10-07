# CPUParticles.ParticleFlags

Last updated: 2026-10-07

- Declaration: `public enum CPUParticles.ParticleFlags`
- Source: [CPUParticles.cs](../../src/Scene/2D/CPUParticles.cs)
- Owner: [CPUParticles](CPUParticles.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

AlignYToVelocity is the only applicable two-dimensional flag. Reserved spatial rotation/depth flags are excluded; Max retains the source bound 3 and is not selectable.

## Values

| Declaration | Meaning |
| --- | --- |
| `public const Electron2D.CPUParticles.ParticleFlags AlignYToVelocity = 0` | Align the Y basis with velocity. |
| `public const Electron2D.CPUParticles.ParticleFlags Max = 3` | Nonselectable bound of the source flag domain. |

Invalid selections throw ArgumentOutOfRangeException before state changes. These types do not expose backend enums or dynamic selector values. Executable guards and corresponding behavior are checked by CPUParticlesTests.
