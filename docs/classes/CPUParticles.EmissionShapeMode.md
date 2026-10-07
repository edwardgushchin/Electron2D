# CPUParticles.EmissionShapeMode

Last updated: 2026-10-07

- Declaration: `public enum CPUParticles.EmissionShapeMode`
- Source: [CPUParticles.cs](../../src/Scene/2D/CPUParticles.cs)
- Owner: [CPUParticles](CPUParticles.md)
- Component: [CPU particles](../components/cpu-particles.md)

## Description

Chooses the next birth distribution; normals/colors apply only when their array length matches points. Radius/extents are in pixels; Max is not selectable.

## Values

| Declaration | Meaning |
| --- | --- |
| `public const Electron2D.CPUParticles.EmissionShapeMode DirectedPoints = 5` | Emit at copied points with optional matching normal bases. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Max = 7` | Nonselectable bound. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Point = 0` | Emit at the origin. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Points = 4` | Emit at copied points, with optional matching colors. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Rectangle = 3` | Emit inside a rectangle. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Ring = 6` | Emit uniformly by area between two radii. |
| `public const Electron2D.CPUParticles.EmissionShapeMode Sphere = 1` | Emit inside a circular disk with uniform radius. |
| `public const Electron2D.CPUParticles.EmissionShapeMode SphereSurface = 2` | Emit the projected surface of a sphere into the disk. |

Invalid selections throw ArgumentOutOfRangeException before state changes. These types do not expose backend enums or dynamic selector values. Executable guards and corresponding behavior are checked by CPUParticlesTests.
