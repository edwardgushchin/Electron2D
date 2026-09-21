# Mathf

Last updated: 2026-09-21

## Declaration

- Source: [`Mathf.cs`](../../src/Core/Math/Mathf.cs)
- Namespace: `Electron2D`
- Declaration: `public static class Mathf`
- Domain: [Core](../domains/core.md)
- Component: [Scalar math](../components/scalar-math.md)

## Responsibility and ownership

`Mathf` is the engine's stateless scalar-math surface. It centralizes single- and double-precision transcendental functions, angle conversion and shortest-path arithmetic, interpolation, approximation, rounding, wrapping, audio-scale conversion, and integer helpers. It owns no mutable state, resource, handle, callback, or lifecycle.

Single precision is the primary engine scalar contract. Double overloads exist where the audited typed API provides them. `Pi`, `Tau`, `E`, `Sqrt2`, `Inf`, `NaN`, and `Epsilon` are single-precision constants; `Epsilon` is exactly `1e-6f`. Double approximate comparisons use an internal `1e-14` threshold.

## Complete public API

All listed float/double pairs are separate overloads. Weights are not clamped unless stated.

| Family | Members and current behavior |
| --- | --- |
| Constants | `Tau`, `Pi`, `Inf`, `NaN`, `E`, `Sqrt2`, `Epsilon` |
| Absolute and ordering | `Abs(int/float/double)`, `Min(int/float/double)`, `Max(int/float/double)`, `Clamp(int/float/double)`, `Sign(int/float/double)` |
| Circular trigonometry | `Sin`, `Cos`, `Tan`, `Asin`, `Acos`, `Atan`, `Atan2`, and `SinCos`, each for float and double |
| Hyperbolic trigonometry | `Sinh`, `Cosh`, `Tanh`, `Asinh`, `Acosh`, and `Atanh`, each for float and double |
| Exponential math | `Exp`, `Log`, `Pow`, and `Sqrt`, each for float and double |
| Angle math | float/double `DegToRad`, `RadToDeg`, `AngleDifference`, `LerpAngle`, and `RotateToward`; opposite-angle ties follow the documented signed-pi rule |
| Linear interpolation | float/double `Lerp`, `InverseLerp`, `Remap`, and `MoveToward` |
| Cubic interpolation | float/double `CubicInterpolate`, `CubicInterpolateInTime`, `CubicInterpolateAngle`, and `CubicInterpolateAngleInTime`; timed overloads use Barry-Goldman interpolation and handle coincident times explicitly |
| Bezier curves | float/double `BezierInterpolate` and `BezierDerivative` for one-dimensional cubic curves |
| Easing and smoothing | float/double `Ease` clamps its input to `0..1`; `SmoothStep` clamps the normalized edge position and returns `from` when the two edges are approximately equal |
| Rounding | float/double `Ceil`, `Floor`, and midpoint-to-even `Round`; `CeilToInt`, `FloorToInt`, and `RoundToInt` convert float/double results through unchecked managed conversion; float/double `Snapped` uses `floor(value / step + 0.5) * step` and preserves a zero-step value |
| Approximation and classification | float/double `IsEqualApprox` with calculated tolerance, float/double overloads with explicit strict absolute tolerance, plus `IsZeroApprox`, `IsFinite`, `IsInf`, and `IsNaN` |
| Modulus and periodic values | `PosMod(int/float/double)`, `Wrap(int/float/double)`, and float/double `PingPong` |
| Integer/decimal helpers | `NearestPo2(int)`, `StepDecimals(double)`, and `DecimalCount(double/decimal)` |
| Audio scale | float/double `DbToLinear` and `LinearToDb` |

The public surface contains 127 method overloads and seven constants. There are no constructors, properties, fields, events, indexers, operators, virtual members, or protected extension points.

## Numeric invariants and error behavior

- Floating-point operations retain normal IEEE 754 behavior. Domain errors generally produce NaN; zero logarithms produce negative infinity; floating modulus by zero produces NaN.
- Calculated approximate equality first accepts exact equality so equal infinities pass, then compares a strict absolute difference against `max(epsilon * abs(left), epsilon)`. NaN never compares approximately equal. Explicit negative or NaN tolerances allow only exact equality.
- Float zero approximation is strict `abs(value) < 1e-6f`; double zero approximation is strict `abs(value) < 1e-14`. A value exactly on the threshold is false.
- `Abs(int.MinValue)` throws `OverflowException`. Reversed `Clamp` bounds throw `ArgumentException`.
- `Sign(float/double)` throws `ArithmeticException` for NaN. `MoveToward` exposes the same exception when its signed difference is NaN.
- `PosMod(int)` throws `DivideByZeroException` for zero and `OverflowException` for `int.MinValue % -1`. Floating overloads do not throw for a zero divisor.
- `DecimalCount(double)` throws `OverflowException` for NaN, infinity, or values outside the decimal range. The decimal overload reports retained scale, including trailing zeroes.
- Integer-returning rounding overloads deliberately use unchecked managed conversion for NaN, infinity, and out-of-range values; callers must validate when those inputs are possible.
- `NearestPo2` and integer `Wrap` deliberately retain unchecked 32-bit arithmetic. Power-of-two overflow wraps; the extreme `int.MinValue % -1` wrap case can throw `OverflowException`.
- `StepDecimals` is the audited threshold estimator, not a general decimal parser: it returns zero through eight and also returns zero for integral or smaller-than-table fractions.

## Lifecycle, threading, and allocation

There is no lifecycle or state transition. Every call is independent and safe across threads. Methods do not retain references or mutate process state.

Normal nonthrowing calls allocate zero managed memory after JIT warmup; thrown managed exceptions allocate normally. Tuple results are value tuples and decimal inspection uses stack storage. Runtime transcendental accuracy and unchecked floating-to-integer sentinel values remain properties of the target .NET runtime; the current executable verification covers Linux/.NET 8 only.

## Dependencies and interactions

Implementation uses only .NET scalar mathematics and decimal bit access. [`Vector2`](Vector2.md), [`Vector4`](Vector4.md), their integer counterparts, [`Color`](Color.md), [`Rect`](Rect.md), [`Transform`](Transform.md), and [`Node`](Node.md) route matching scalar operations through `Mathf`. Internal color math continues to call the BCL directly only for cube root because no audited `Mathf` member exists for it.

The migration intentionally corrected the former `1e-5f` component-comparison tolerance to the canonical `Mathf.Epsilon` contract. Electron2D has no released compatibility baseline, so known incorrect pre-release behavior is corrected rather than preserved behind a second tolerance or compatibility path.

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` reflects the exact seven-constant/127-overload surface and exercises every method family across float and double paths. Tests cover ordinary results, extrapolation, opposite angles, coincident times, NaN/infinity, strict epsilon boundaries, reversed clamps, integer zero division/overflow, unchecked rounding conversion, decimal failures, wrapping, power-of-two overflow, downstream geometry migration, and warmed zero allocation.

The implementation was audited against the official 4.7.2 stable typed C# scalar-math sources and their inheritance-independent global math contract. There is no generic-math facade, random-number generation, vector overload, configurable epsilon, fast-approximation mode, SIMD API, or cube-root member. Those are absent rather than speculative wrappers.

## Decisions

- [0004: 2D scene-oriented API in one assembly](../decisions/product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
