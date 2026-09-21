# Side

Last updated: 2026-09-21

## Declaration

- Source: [`Side.cs`](../../src/Core/Math/Side.cs)
- Namespace: `Electron2D`
- Declaration: `public enum Side`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Side` is the stable typed identity of one edge of an axis-aligned rectangle. It is a value enum with no owned state or lifecycle.

## Complete public API

| Value | Numeric value | Meaning |
| --- | ---: | --- |
| `Left` | `0` | Left edge |
| `Top` | `1` | Top edge |
| `Right` | `2` | Right edge |
| `Bottom` | `3` | Bottom edge |

## Invariants and error behavior

The four numeric values are stable public API. The enum is not marked as flags. C# permits casting other integers to the type; [`Rect2.GrowSide`](Rect2.md) treats such an undefined value as a no-op. No standalone enum operation throws.

## Threading, dependencies, and integration

Enum values are immutable, allocation-free, and safe to copy or read on any thread. `Side` has no dependency beyond the runtime enum representation and is currently consumed by `Rect2`.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies all numeric identities, all four `Rect2.GrowSide` branches, and undefined-value behavior. No directional aliases or 3D faces are provided.

## Decision

- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
