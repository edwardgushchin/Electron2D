# Vector2

Last updated: 2026-09-21

## Declaration

- Source: [`Vector2.cs`](../../src/Core/Math/Vector2.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Vector2 : IEquatable<Vector2>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Vector2` is the engine-owned mutable two-component single-precision value used for 2D positions, sizes, directions, velocities, axes, offsets, and numeric pairs. Its sequential layout is exactly two `float` fields in X/Y order and is verified as 8 bytes on the current .NET 8 host. It owns no identity, reference, callback, handle, or lifecycle.

## Complete public and nested API

| Family | Members and behavior |
| --- | --- |
| Storage | Mutable fields `X`, `Y`; mutable `this[int]` for indices 0 and 1; nested `Axis.X = 0`, `Axis.Y = 1` |
| Constants | `Zero`, `One`, `Inf`, `Up`, `Down`, `Right`, `Left`; screen coordinates use negative Y for up |
| Construction | `Vector2(float, float)`, `Vector2(Vector2I)`, and `Deconstruct(out float, out float)` |
| Component math | `Abs`, `Ceil`, vector/scalar `Clamp`, `Floor`, `Inverse`, vector/scalar `Max`, vector/scalar `Min`, `Round`, `Sign`, vector/scalar `Snapped` |
| Geometry | `Angle`, `AngleTo`, `AngleToPoint`, `Aspect`, `Cross`, `DirectionTo`, `DistanceSquaredTo`, `DistanceTo`, `Dot`, `Length`, `LengthSquared`, `Normalized`, `Orthogonal`, `Project`, `Reflect`, `Bounce`, `Rotated`, `Slide` |
| Interpolation | `BezierDerivative`, `BezierInterpolate`, `CubicInterpolate`, `CubicInterpolateInTime`, `Lerp`, `LimitLength`, `MoveToward`, `Slerp` |
| Predicates | `IsEqualApprox`, `IsFinite`, `IsNormalized`, `IsZeroApprox`, `MaxAxisIndex`, `MinAxisIndex` |
| Modulus | Scalar/vector `PosMod`; scalar/vector `%` retains truncated-remainder semantics |
| Arithmetic | Binary `+`/`-`, unary `+`/`-`, scalar multiplication in either order, component multiplication, scalar/component division |
| Ordering and identity | `==`, `!=`, lexicographic `<`, `<=`, `>`, `>=`, `Equals(object?)`, `Equals(Vector2)`, `GetHashCode` |
| Formatting | `ToString()` and `ToString(string?)` use invariant culture |

`Vector2I` converts implicitly to `Vector2`; integer components above the exact single-precision range can lose low-order bits. The reverse conversion is explicit, truncates toward zero, and rejects a non-finite or out-of-`int` component with `ArgumentOutOfRangeException`.

## Numeric invariants and error behavior

- Ordinary arithmetic, division, remainder, reciprocal, projection, and interpolation retain IEEE 754 NaN and infinity propagation.
- `Normalized()` and `DirectionTo()` return `Zero` for an exactly zero squared length. `IsNormalized()` uses the engine's `0.001` unit-length tolerance; approximate component comparisons use [`Mathf.Epsilon`](Mathf.md) (`1e-6f`) with exact equality first.
- Angles are radians; positive angles rotate positive X toward positive Y and therefore appear clockwise in screen coordinates.
- `Reflect` follows the line-reflection convention `2 * Dot(normal) * normal - value`; `Bounce` negates it. `Reflect`, `Bounce`, and `Slide` require a normalized normal and do not validate it.
- `Project(Zero)` produces NaN components. `PosMod` with a nonzero divisor uses the divisor's sign; zero divisors produce IEEE NaN.
- `Round` uses midpoint-to-even. `Snapped` uses `floor(value / step + 0.5) * step`; a zero step leaves the component unchanged.
- Clamp overloads throw `ArgumentException` for reversed bounds. The indexer throws `ArgumentOutOfRangeException`; `Sign` throws `ArithmeticException` for NaN; an invalid format throws `FormatException`.
- Exact equality treats signed zeros as equal and NaN as unequal. Relational operators remain unordered when the first differing component is NaN.

## Lifecycle, threading, dependencies, and allocation

Copies are independent values. Concurrent reads of independent copies are safe; concurrent writes to the same storage location are an ordinary unsynchronized data race. Numeric operations allocate no managed memory after JIT warmup; string formatting allocates.

The type depends on canonical scalar [`Mathf`](Mathf.md) plus formatting and layout primitives. [`Rect`](Rect.md), [`Transform`](Transform.md), and [`Node`](Node.md) use it throughout their public 2D API. [`ConfigFile`](ConfigFile.md) persists finite values as exactly `X` and `Y`; [`PackedScene`](PackedScene.md) stores it directly through typed property descriptors.

## Coverage, verification, and limitations

The reference API audit classifies the numeric/geometric surface as implemented. Universal-value truth conversion is permanently excluded by the typed C# architecture. No automatic conversion to an external numerics type is exposed. No 3D vector or spatial API is present.

The executable harness covers layout, constants, index failures, construction and integer conversion failures, every method and operator family, interpolation, zero/non-finite behavior, ordering, formatting, strict persistence, packed-scene copying, and warmed zero-allocation math. Execution is Linux/.NET 8 only; native ABI and the full five-target matrix remain unverified.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
