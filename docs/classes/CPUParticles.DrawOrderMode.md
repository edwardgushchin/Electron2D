# CPUParticles.DrawOrderMode

Last updated: 2026-10-07

- Declaration: `public enum CPUParticles.DrawOrderMode`
- Source: [CPUParticles.cs](../../src/Scene/2D/CPUParticles.cs)
- Owner: [CPUParticles](CPUParticles.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

Chooses quad order without changing simulation indices. Lifetime puts older particles first; equal ages have no stable ordering guarantee.

## Values

| Declaration | Meaning |
| --- | --- |
| `public const Electron2D.CPUParticles.DrawOrderMode Index = 0` | Draw increasing particle indices. |
| `public const Electron2D.CPUParticles.DrawOrderMode Lifetime = 1` | Draw older particles before younger particles. |

Invalid selections throw ArgumentOutOfRangeException before state changes. These types do not expose backend enums or dynamic selector values. Executable guards and corresponding behavior are checked by CPUParticlesTests.
