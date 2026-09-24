# Side

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/Math/Side.cs`](../../src/Core/Math/Side.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public enum Side`

> Identifies one side of an axis-aligned rectangle.

## Description

Identifies one side of an axis-aligned rectangle.

`Side` is the stable typed identity of one edge of an axis-aligned rectangle. It is a value enum with no owned state or lifecycle.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var value = Side.Left;
```

## Constants

| Member | Description |
| --- | --- |
| [`Left = 0`](#f-electron2d-side-left) | The left side. |
| [`Top = 1`](#f-electron2d-side-top) | The top side. |
| [`Right = 2`](#f-electron2d-side-right) | The right side. |
| [`Bottom = 3`](#f-electron2d-side-bottom) | The bottom side. |

## Constant Descriptions

<a id="f-electron2d-side-left"></a>
### `Left = 0`

The left side.

<a id="f-electron2d-side-top"></a>
### `Top = 1`

The top side.

<a id="f-electron2d-side-right"></a>
### `Right = 2`

The right side.

<a id="f-electron2d-side-bottom"></a>
### `Bottom = 3`

The bottom side.

## Invariants and error behavior

The four numeric values are stable public API. The enum is not marked as flags. C# permits casting other integers to the type; [`Rect2.GrowSide`](Rect2.md) and [`Rect2i.GrowSide`](Rect2i.md) treat such an undefined value as a no-op. No standalone enum operation throws.

## Threading, dependencies, and integration

Enum values are immutable, allocation-free, and safe to copy or read on any thread. `Side` has no dependency beyond the runtime enum representation and is currently consumed by `Rect2` and `Rect2i`.

## Verification and limitations

`tests/Electron2D.Tests/Program.cs` verifies all numeric identities, all four floating-point and integer rectangle `GrowSide` branches, and undefined-value behavior. No directional aliases or 3D faces are provided.

## Decision

- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
