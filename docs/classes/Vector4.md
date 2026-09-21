# Vector4

Last updated: 2026-09-21

## Declaration

- Source: [`Vector4.cs`](../../src/Core/Math/Vector4.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Vector4 : IEquatable<Vector4>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Vector4` is an engine-owned mutable four-component single-precision numeric tuple. It is suitable for generic four-value data and future typed GPU boundaries without creating 3D or 4D scene geometry. Sequential X/Y/Z/W layout is verified as 16 bytes. It owns no identity, handle, callback, or lifecycle.

## Complete public and nested API

| Family | Members and behavior |
| --- | --- |
| Storage | Mutable fields `X`, `Y`, `Z`, `W`; mutable `this[int]` for indices 0 through 3; nested `Axis.X/Y/Z/W = 0/1/2/3` |
| Constants | `Zero`, `One`, `Inf` |
| Construction | `Vector4(float, float, float, float)`, `Vector4(Vector4I)`, four-output `Deconstruct` |
| Component math | `Abs`, `Ceil`, vector/scalar `Clamp`, `Floor`, `Inverse`, vector/scalar `Max`, vector/scalar `Min`, `Round`, `Sign`, vector/scalar `Snapped` |
| Geometry-like numeric operations | `DirectionTo`, `DistanceSquaredTo`, `DistanceTo`, `Dot`, `Length`, `LengthSquared`, `Normalized` |
| Interpolation | `CubicInterpolate`, `CubicInterpolateInTime`, `Lerp` |
| Predicates and axes | `IsEqualApprox`, `IsFinite`, `IsNormalized`, `IsZeroApprox`, `MaxAxisIndex`, `MinAxisIndex` |
| Modulus | Scalar/vector `PosMod`; scalar/vector `%` uses truncated remainder |
| Arithmetic | Binary `+`/`-`, unary `+`/`-`, scalar multiplication in either order, component multiplication, scalar/component division |
| Ordering and identity | `==`, `!=`, lexicographic `<`, `<=`, `>`, `>=`, both `Equals` overloads, `GetHashCode` |
| Formatting | `ToString()` and `ToString(string?)` use invariant culture |

`Vector4I` converts implicitly to `Vector4` with possible precision loss above 2^24. Explicit reverse conversion truncates toward zero and rejects non-finite or out-of-range components with `ArgumentOutOfRangeException`.

## Numeric invariants and error behavior

- Arithmetic retains IEEE 754 behavior. Zero scalar/component division produces infinity or NaN; zero remainder or positive modulus produces NaN; reciprocal preserves signed zero through signed infinity.
- Exact zero normalization and equal-point direction return `Zero`. Non-finite normalization follows ordinary managed floating-point propagation. `IsNormalized` uses tolerance `0.001`; approximate component predicates use [`Mathf.Epsilon`](Mathf.md) (`1e-6f`) with exact equality first.
- Maximum-axis ties choose the first maximum; minimum-axis ties choose the last minimum. NaN is skipped by ordered comparisons, and an initial NaN therefore keeps X.
- Relational operators compare X, then Y, then Z, then W directly. If the first differing component is NaN, all four relational results are false; no artificial total ordering is introduced.
- `Round` is midpoint-to-even. `Snapped` uses `floor(value / step + 0.5) * step`; zero steps preserve components. `Sign` throws `ArithmeticException` for NaN.
- Clamp throws `ArgumentException` for reversed bounds; the indexer throws `ArgumentOutOfRangeException`; invalid formats throw `FormatException`.

## Lifecycle, threading, dependencies, and allocation

Copies are independent, with no state transition. Numeric operations allocate no managed memory after warmup; formatting allocates. Independent copies are thread-safe to read or mutate independently; shared writes are unsynchronized.

The type depends on canonical scalar [`Mathf`](Mathf.md), formatting/layout primitives, and [`Vector4I`](Vector4I.md). [`ConfigFile`](ConfigFile.md) accepts only finite values and persists exact `X/Y/Z/W` fields. [`PackedScene`](PackedScene.md) stores the value directly.

## Coverage, verification, and limitations

All pure value behavior from the audited reference API is implemented. Multiplication by a 3D projection matrix is permanently excluded because Electron2D has no 3D projection type. Universal-value truth conversion is excluded by the typed C# architecture. No renderer or shader binding exists yet, so this type makes no GPU integration claim.

The executable harness covers layout, constants, indexing, conversions, every method/operator family, interpolation, zero/non-finite behavior, NaN ordering, axis ties, formatting, strict persistence, packed-scene storage, and warmed allocation-free math. Execution is Linux/.NET 8 only.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
