# Scalar math component

Last updated: 2026-09-25

## Scope

This Core component owns backend-independent scalar mathematics shared by engine code and games. It covers constants, float/double transcendental functions, angular arithmetic, interpolation, approximation, rounding, modulus, periodic values, and audio-scale conversion. It does not own vectors, random generation, geometry storage, rendering, physics, platform SIMD, or native handles.

## Owned types

| Type | Role | Source |
| --- | --- | --- |
| [`Mathf`](../classes/Mathf.md) | Stateless canonical scalar-math API | [`Mathf.cs`](../../src/Core/Math/Mathf.cs) |

## Runtime flow

1. Callers invoke a static typed overload with integer, single-precision, double-precision, or decimal input.
2. Direct operations delegate to the corresponding .NET scalar primitive.
3. Engine-specific composition implements shortest-path angles, interpolation, approximation, snapping, wrapping, and conversion formulas without shared state.
4. A value or documented managed exception returns synchronously; no work is queued and no process state changes.

## Dependencies

- .NET scalar numeric primitives and stack-based decimal bit inspection.
- No package, native backend, renderer, scene, resource, I/O, editor, or platform-host dependency.
- Geometry, color, and scene transform code depend on this component; dependency never points back from scalar math.

## Invariants and error behavior

- Single precision is the primary engine scalar vocabulary; matching double overloads retain double calculation and use a `1e-14` approximate threshold.
- `Epsilon` is `1e-6f`, and zero/equality thresholds are strict rather than inclusive.
- Floating-point edge behavior remains IEEE 754 unless a method explicitly clamps or returns a documented degenerate value.
- Integer division, absolute-value, and extreme remainder failures remain visible managed exceptions. Reversed clamp bounds are rejected.
- All methods are stateless and thread-safe. Normal nonthrowing calls are deterministic for a fixed runtime numeric implementation and allocation-free after warmup; exception construction follows ordinary managed behavior.
- Before the first public release, an audited correctness defect is fixed directly. No duplicate legacy tolerance or compatibility branch is retained solely to preserve known-wrong behavior.

## Current implementation status

Implemented and verified. The complete audited 4.7.2 stable typed scalar surface is present as seven constants and 127 method overloads. Matching scalar formulas formerly duplicated by vectors and transforms now route through `Mathf`; color, rectangle, transform, vector, integer-vector snapping, and Entity degree conversion use the shared contract. The former `1e-5f` component approximation was corrected to `1e-6f`.

## Exclusions and limitations

- Random generation is a separate [Core component](random-generation.md). This scalar-math component has no noise, generic numeric abstraction, vector overload, configurable global epsilon, approximate fast-trigonometry mode, or public SIMD contract.
- Cube root and truncation remain direct internal `System.MathF` operations because they are not part of the audited public surface.
- Transcendental bit-for-bit equivalence across every supported OS/architecture is not claimed; only contract-level behavior is fixed.
- Verification currently executes on Linux/.NET 10. Windows, macOS, Android, iOS, Android TV, tvOS, and Web remain unverified; this scalar component has no X11/Wayland-specific behavior.

## Verification

The executable harness checks the exact reflected surface, every method family, positive/negative/domain/boundary cases, strict epsilon behavior, documented exceptions, integer overflow, NaN/infinity, angle ties and wrapping, downstream geometry behavior, and zero warmed allocations. Release XML generation treats warnings as errors.

## Decisions

- [0004: 2D scene-oriented API in one assembly](../decisions/product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
