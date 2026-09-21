# Vector2I

Last updated: 2026-09-21

## Declaration

- Source: [`Vector2I.cs`](../../src/Core/Math/Vector2I.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Vector2I : IEquatable<Vector2I>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Vector2I` is the engine-owned mutable two-component 32-bit integer value for pixels, grid and tile coordinates, texture dimensions, chunk addresses, integer pairs, and [`RectI`](RectI.md) geometry. Sequential X/Y layout is verified as 8 bytes. It owns no resources or lifecycle.

## Complete public and nested API

| Family | Members and behavior |
| --- | --- |
| Storage | Mutable fields `X`, `Y`; mutable `this[int]`; nested `Axis.X = 0`, `Axis.Y = 1` |
| Constants | `MinValue`, `MaxValue`, `Zero`, `One`, `Up`, `Down`, `Right`, `Left` |
| Construction | `Vector2I(int, int)`, `Vector2I(Vector2)`, `Deconstruct(out int, out int)` |
| Numeric methods | `Abs`, `Aspect`, vector/scalar `Clamp`, `DistanceSquaredTo`, `DistanceTo`, `Length`, `LengthSquared`, vector/scalar `Max`, vector/scalar `Min`, `MaxAxisIndex`, `MinAxisIndex`, `Sign`, vector/scalar `Snapped` |
| Integer arithmetic | Binary `+`/`-`, unary `+`/`-`, integer scalar multiplication in either order, component multiplication, scalar/component integer division, scalar/component remainder |
| Float arithmetic | Multiplication by `float` in either order and division by `float` return `Vector2` and retain IEEE behavior |
| Conversion | Implicit `Vector2I` to `Vector2`; explicit `Vector2` to `Vector2I` |
| Ordering and identity | `==`, `!=`, lexicographic `<`, `<=`, `>`, `>=`, `Equals(object?)`, `Equals(Vector2I)`, `GetHashCode` |
| Formatting | `ToString()` and `ToString(string?)` use invariant culture |

## Numeric invariants and error behavior

- Addition, subtraction, multiplication, and negation explicitly wrap in 32-bit two's-complement arithmetic. Squared length and squared distance also wrap, so their negative overflow can make `Length()` or `DistanceTo()` return NaN.
- Integer division truncates toward zero; remainder has the dividend's sign. A zero scalar or component divisor throws `DivideByZeroException`; `int.MinValue / -1` and `int.MinValue % -1` throw `OverflowException`.
- `Abs` throws `OverflowException` for `int.MinValue`. Clamp overloads throw `ArgumentException` for reversed bounds. The indexer throws `ArgumentOutOfRangeException`.
- Maximum-axis ties choose X; minimum-axis ties choose Y. Snapping uses double intermediate arithmetic and ties toward positive infinity; zero steps preserve the value and an out-of-range snapped result throws `OverflowException`.
- Float conversion truncates toward zero and rejects non-finite or out-of-range input with `ArgumentOutOfRangeException`. Conversion to `Vector2` can lose integer precision above 2^24.
- Invalid numeric formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent copies can be used concurrently; shared mutation is unsynchronized.

The type depends on canonical scalar [`Mathf`](Mathf.md) for snapping and scalar operations, plus formatting/layout primitives and its paired [`Vector2`](Vector2.md). [`RectI`](RectI.md) uses it for position, size, and integer geometry. [`ConfigFile`](ConfigFile.md) persists exactly two 32-bit integer fields; [`PackedScene`](PackedScene.md) stores it directly.

## Coverage, verification, and limitations

The complete implementable reference surface is present. Universal-value truth conversion is permanently excluded. There is no external-numerics conversion or 3D counterpart.

The executable harness covers layout, constants, indexing, construction/conversion, every method and operator family, wraparound, overflow and zero-division failures, ordering, snapping, invariant formatting, strict persistence, packed-scene storage, and allocation-free warmed math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
