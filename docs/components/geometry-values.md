# Geometry values component

Last updated: 2026-09-23

## Scope

This Core component owns the engine's backend-independent value mathematics: two-, three-, and four-component floating-point/integer vectors, floating-point and integer 2D axis-aligned rectangles, the 2D affine transform, and rectangle side identities. It contains no renderer, physics, input, asset, scene ownership, native handles, or global state.

## Owned types

| Type | Role | Source |
| --- | --- | --- |
| [`Vector2`](../classes/Vector2.md) | Canonical two-component floating-point spatial and numeric value | [`Vector2.cs`](../../src/Core/Math/Vector2.cs) |
| [`Vector2I`](../classes/Vector2I.md) | Canonical two-component integer grid and numeric value | [`Vector2I.cs`](../../src/Core/Math/Vector2I.cs) |
| [`Vector3`](../classes/Vector3.md) | Three-component floating-point numeric value | [`Vector3.cs`](../../src/Core/Math/Vector3.cs) |
| [`Vector3I`](../classes/Vector3I.md) | Three-component integer numeric value | [`Vector3I.cs`](../../src/Core/Math/Vector3I.cs) |
| [`Vector4`](../classes/Vector4.md) | Four-component floating-point numeric tuple | [`Vector4.cs`](../../src/Core/Math/Vector4.cs) |
| [`Vector4I`](../classes/Vector4I.md) | Four-component integer numeric tuple | [`Vector4I.cs`](../../src/Core/Math/Vector4I.cs) |
| [`Rect`](../classes/Rect.md) | Mutable sequential rectangle and typed geometry operations | [`Rect.cs`](../../src/Core/Math/Rect.cs) |
| [`RectI`](../classes/RectI.md) | Mutable sequential integer rectangle and typed geometry operations | [`RectI.cs`](../../src/Core/Math/RectI.cs) |
| [`Transform`](../classes/Transform.md) | Mutable sequential affine value, composition, inversion, and bounds transformation | [`Transform.cs`](../../src/Core/Math/Transform.cs) |
| [`Side`](../classes/Side.md) | Stable identity for the four rectangle edges | [`Side.cs`](../../src/Core/Math/Side.cs) |

## Runtime flow

1. Callers construct or copy mutable values; zero-initialized structs retain normal all-zero C# state.
2. Vector operations route shared scalar formulas through [`Mathf`](../classes/Mathf.md) and return results without global state or steady-state allocation.
3. Floating-point and integer rectangle operations preserve stored position/size and normalize negative sizes only when `Abs()` is called explicitly.
4. Transform operations use X/Y basis columns plus Origin; callers choose general affine or orthonormal inverse behavior explicitly.
5. `Transform` composes `Entity` local/global state and transforms `Rect` corners into axis-aligned bounds.
6. Typed persistence accepts only finite floating-point vectors, rectangles, and transforms; integer vectors and rectangles retain all `int` values. Packed scenes copy every value directly.

## Dependencies

- Canonical scalar [`Mathf`](../classes/Mathf.md), plus .NET globalization, serialization, and interop-layout primitives.
- Existing typed `ConfigFile`, property descriptor, packed-scene, and `Entity` integration boundaries.
- No public external numerics dependency and no external package or native library.

## Invariants

- `Vector2` and `Vector2I` are sequential X/Y values of 8 bytes; `Vector3` and `Vector3I` are sequential X/Y/Z values of 12 bytes; `Vector4` and `Vector4I` are sequential X/Y/Z/W values of 16 bytes.
- `Rect` is 16 bytes containing `Vector2 Position` then `Vector2 Size`; `RectI` is 16 bytes containing `Vector2I Position` then `Vector2I Size`; `Transform` is 24 bytes containing `Vector2 X`, `Y`, then `Origin`.
- Floating-point ordinary math retains IEEE values; finite persistence validates only at its serialization boundary.
- Floating-point component approximation uses the strict internal tolerance (`1e-6f`) and accepts exact equality first; unit-vector checks retain their separate `0.001` tolerance.
- Integer ordinary component arithmetic wraps explicitly; division and invalid absolute values retain managed exceptions. Integer-vector squared norms widen before multiplication, return `long`, and throw when the exact result exceeds `long.MaxValue`; ordinary lengths/distances remain finite across all 32-bit coordinates.
- Float-to-integer vector conversion truncates toward zero and rejects non-finite or out-of-range components. Integer-to-float conversion can lose low-order precision above 2^24.
- Maximum-axis ties select the first component; minimum-axis ties select the last component.
- Numeric hot paths allocate no managed memory after warmup; formatting, transform array operators, and persistence allocate by contract.
- Floating-point and integer rectangle containment is half-open on right/bottom, and rectangle sizes never normalize implicitly. Integer rectangle arithmetic wraps except for the documented `Abs()` minimum-value failure.
- Transform multiplication applies the right operand first; general inversion rejects an exactly singular basis, while reverse point/rectangle operations have an orthonormal-basis precondition.
- `Side` values remain left `0`, top `1`, right `2`, bottom `3`.

## Current implementation status

Implemented and verified. `Rect`, `Transform`, and `Entity` use the engine-owned `Vector2` directly, and duplicated scalar interpolation/modulus/snapping/angle/approximation helpers have been migrated to `Mathf`. `Vector2I`, `Vector3`, `Vector3I`, `Vector4`, `Vector4I`, and `RectI` provide their complete currently implementable value contracts, including typed conversions within vector and rectangle dimensional pairs. Strict configuration schemas and direct packed-scene storage exist for all six vectors, both rectangles, and transforms.

## Exclusions and limitations

- Universal-value truth conversion is permanently excluded by the typed C# architecture.
- Four-component projection operations are excluded because the engine has no 3D projection type.
- Three-dimensional rectangles, transforms, nodes, and rendering remain outside the 2D product boundary. Numeric `Vector3` and `Vector3I` do not introduce these systems.
- No public external-numerics adapter exists. Future native/package adapters must remain localized at integration boundaries.
- Shader uniforms accept the numeric vector values; these pure values do not add physics, UI layout, atlas, or native ABI integration.

## Verification

The executable harness covers every method/operator family, layouts and constants, index failures, interpolation, the strict internal tolerance migration boundaries, NaN/infinity/signed-zero behavior, integer wrap/overflow/zero division, widened integer norms and their checked limits, conversion boundaries, axis ties, floating-point and integer rectangle boundaries, affine order/inversion/decomposition, Entity integration, strict malformed persistence, packed-scene value copying, invariant formatting, and warmed allocation behavior.

Execution is verified on Linux/.NET 8 only. Native ABI and the Windows/macOS/Linux (X11/Wayland)/Android/iOS/Web build and host matrix remain unverified.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate affine-transform foundation](../decisions/core-math.md#adr-0026)
- [0029: Typed affine semantics](../decisions/core-math.md#adr-0029)
- [0032: Engine-owned math vocabulary](../decisions/core-math.md#adr-0032)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
- [0035: Foreseeable public type-family completeness](../decisions/core-math.md#adr-0035)
