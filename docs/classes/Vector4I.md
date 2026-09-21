# Vector4I

Last updated: 2026-09-21

## Declaration

- Source: [`Vector4I.cs`](../../src/Core/Math/Vector4I.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Vector4I : IEquatable<Vector4I>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Vector4I` is an engine-owned mutable four-component 32-bit integer tuple for masks, packed channels, integer parameter groups, and future typed GPU boundaries. It does not represent 3D or 4D scene geometry. Sequential X/Y/Z/W layout is verified as 16 bytes and owns no resources or lifecycle.

## Complete public and nested API

| Family | Members and behavior |
| --- | --- |
| Storage | Mutable `X`, `Y`, `Z`, `W`; mutable `this[int]`; nested `Axis.X/Y/Z/W = 0/1/2/3` |
| Constants | `MinValue`, `MaxValue`, `Zero`, `One` |
| Construction | `Vector4I(int, int, int, int)`, `Vector4I(Vector4)`, four-output `Deconstruct` |
| Numeric methods | `Abs`, vector/scalar `Clamp`, `DistanceSquaredTo`, `DistanceTo`, `Length`, `LengthSquared`, vector/scalar `Max`, vector/scalar `Min`, `MaxAxisIndex`, `MinAxisIndex`, `Sign`, vector/scalar `Snapped` |
| Integer arithmetic | Binary `+`/`-`, unary `+`/`-`, integer scalar multiplication in either order, component multiplication, scalar/component division and remainder |
| Float arithmetic | Multiplication by `float` in either order and division by `float` return `Vector4` |
| Conversion | Implicit `Vector4I` to `Vector4`; explicit `Vector4` to `Vector4I` |
| Ordering and identity | `==`, `!=`, lexicographic `<`, `<=`, `>`, `>=`, both `Equals` overloads, `GetHashCode` |
| Formatting | `ToString()` and `ToString(string?)` use invariant culture |

## Numeric invariants and error behavior

- Ordinary integer addition, subtraction, multiplication, and negation wrap explicitly. Squared length and distance also use wrapping 32-bit arithmetic; negative overflow can make their square roots NaN.
- Division truncates toward zero and remainder has the dividend's sign. Zero scalar/component divisors throw `DivideByZeroException`; `int.MinValue / -1` and `int.MinValue % -1` throw `OverflowException`. Float division retains IEEE behavior.
- `Abs(int.MinValue)` throws `OverflowException`. Reversed clamp bounds throw `ArgumentException`; invalid indices throw `ArgumentOutOfRangeException`.
- Maximum-axis ties choose X and minimum-axis ties choose W. Snapping uses double intermediate arithmetic, ties toward positive infinity, preserves zero-step components, and throws `OverflowException` when the result is outside `int`.
- Float conversion truncates toward zero and rejects non-finite or out-of-range values with `ArgumentOutOfRangeException`; widening can lose precision above 2^24.
- Invalid formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent values are safe across threads; concurrent shared mutation is not synchronized.

The type depends on canonical scalar [`Mathf`](Mathf.md) for snapping and scalar operations, plus formatting/layout primitives and [`Vector4`](Vector4.md). [`ConfigFile`](ConfigFile.md) persists exact four-field 32-bit integer objects; [`PackedScene`](PackedScene.md) stores it directly.

## Coverage, verification, and limitations

The complete implementable pure-value surface is present. Universal-value truth conversion is permanently excluded, and there is no 3D projection or external numerics integration.

The executable harness covers layout, constants, indexing, conversions, every method/operator family, wraparound, overflow and zero-division failures, axis ties, snapping, formatting, strict persistence, packed-scene storage, and warmed zero-allocation math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
