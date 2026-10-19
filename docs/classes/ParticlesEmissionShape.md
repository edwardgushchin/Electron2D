# ParticlesEmissionShape

Last updated: 2026-10-07

- Declaration: `public enum ParticlesEmissionShape`
- Source: [ParticlesEmissionShape.cs](../../src/Scene/Resources/ParticlesEmissionShape.cs)
- Consumers: [CPUParticles](CPUParticles.md), [ParticleProcessMaterial](ParticleProcessMaterial.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

Shared planar birth-distribution identity. CPU emitters consume copied point/normal/color arrays; GPU process materials consume sampled point/normal/color textures. Radius/extents are in pixels; Max is not selectable. CPU Sphere uses a uniform radius; process-material Sphere projects a volume sample. Both share the named distribution domain without merging their random streams.

## Values

| Declaration | Meaning |
| --- | --- |
| `public const Electron2D.ParticlesEmissionShape DirectedPoints = 5` | Emit at copied points with optional matching normal bases. |
| `public const Electron2D.ParticlesEmissionShape Max = 7` | Nonselectable bound. |
| `public const Electron2D.ParticlesEmissionShape Point = 0` | Emit at the origin. |
| `public const Electron2D.ParticlesEmissionShape Points = 4` | Emit at copied points, with optional matching colors. |
| `public const Electron2D.ParticlesEmissionShape Rectangle = 3` | Emit inside a rectangle. |
| `public const Electron2D.ParticlesEmissionShape Ring = 6` | Emit uniformly by area between two radii. |
| `public const Electron2D.ParticlesEmissionShape Sphere = 1` | Emit inside a circular disk with uniform radius. |
| `public const Electron2D.ParticlesEmissionShape SphereSurface = 2` | Emit the projected surface of a sphere into the disk. |

Invalid selections throw ArgumentOutOfRangeException before state changes. These types do not expose backend enums or dynamic selector values. Executable guards and corresponding behavior are checked by CPUParticlesTests.
