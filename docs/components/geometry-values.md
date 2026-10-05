# Geometry values component

Last updated: 2026-10-05

## Scope

This Core component owns the engine's backend-independent value mathematics and pure 2D geometry queries: two-, three-, and four-component floating-point/integer vectors, floating-point and integer 2D axis-aligned rectangles, the 2D affine transform, and rectangle side/corner identities. It contains no renderer, physics, input, asset, scene ownership, native handles, or global state.

## Owned types

| Type | Role | Source |
| --- | --- | --- |
| [`Geometry`](../classes/Geometry.md) | Stateless raster, nearest-point, polygon, hull, triangulation, atlas, clipping and offset queries | [`Geometry.cs`](../../src/Core/Math/Geometry.cs) |
| [`Vector2`](../classes/Vector2.md) | Canonical two-component floating-point spatial and numeric value | [`Vector2.cs`](../../src/Core/Math/Vector2.cs) |
| [`Vector2i`](../classes/Vector2i.md) | Canonical two-component integer grid and numeric value | [`Vector2i.cs`](../../src/Core/Math/Vector2i.cs) |
| [`Vector3`](../classes/Vector3.md) | Three-component floating-point numeric value | [`Vector3.cs`](../../src/Core/Math/Vector3.cs) |
| [`Vector3i`](../classes/Vector3i.md) | Three-component integer numeric value | [`Vector3i.cs`](../../src/Core/Math/Vector3i.cs) |
| [`Vector4`](../classes/Vector4.md) | Four-component floating-point numeric tuple | [`Vector4.cs`](../../src/Core/Math/Vector4.cs) |
| [`Vector4i`](../classes/Vector4i.md) | Four-component integer numeric tuple | [`Vector4i.cs`](../../src/Core/Math/Vector4i.cs) |
| [`Rect2`](../classes/Rect2.md) | Mutable sequential rectangle and typed geometry operations | [`Rect2.cs`](../../src/Core/Math/Rect2.cs) |
| [`Rect2i`](../classes/Rect2i.md) | Mutable sequential integer rectangle and typed geometry operations | [`Rect2i.cs`](../../src/Core/Math/Rect2i.cs) |
| [`Transform`](../classes/Transform.md) | Mutable sequential affine value, composition, inversion, and bounds transformation | [`Transform.cs`](../../src/Core/Math/Transform.cs) |
| [`Corner`](../classes/Corner.md) | Clockwise 2D corner identity for rounded style geometry | [`Corner.cs`](../../src/Core/Math/Corner.cs) |
| [`Side`](../classes/Side.md) | Stable identity for the four rectangle edges | [`Side.cs`](../../src/Core/Math/Side.cs) |

## Runtime flow

1. Callers construct or copy mutable values; zero-initialized structs retain normal all-zero C# state.
2. Vector operations route shared scalar formulas through [`Mathf`](../classes/Mathf.md) and return results without global state or steady-state allocation.
3. Floating-point and integer rectangle operations preserve stored position/size and normalize negative sizes only when `Abs()` is called explicitly.
4. Transform operations use X/Y basis columns plus Origin; callers choose general affine or orthonormal inverse behavior explicitly.
5. `Transform` composes `Entity` local/global state and transforms `Rect2` corners into axis-aligned bounds.
6. Typed persistence accepts only finite floating-point vectors, rectangles, and transforms; integer vectors and rectangles retain all `int` values. Packed scenes copy every value directly.
7. `Geometry` performs pure point/segment and polygon queries; clipping and offsets route through internally compiled Clipper2 and return caller-owned arrays.

## Dependencies

- Canonical scalar [`Mathf`](../classes/Mathf.md), plus .NET globalization, serialization, and interop-layout primitives.
- Existing typed `ConfigFile`, property descriptor, packed-scene, and `Entity` integration boundaries.
- No public external numerics dependency or native library. Clipper2 2.0.1 C# source is [compiled internally](../../src/Vendor/Clipper2/UPSTREAM.txt) into the single runtime assembly.

## Invariants

- `Vector2` and `Vector2i` are sequential X/Y values of 8 bytes; `Vector3` and `Vector3i` are sequential X/Y/Z values of 12 bytes; `Vector4` and `Vector4i` are sequential X/Y/Z/W values of 16 bytes.
- `Rect2` is 16 bytes containing `Vector2 Position` then `Vector2 Size`; `Rect2i` is 16 bytes containing `Vector2i Position` then `Vector2i Size`; `Transform` is 24 bytes containing `Vector2 X`, `Y`, then `Origin`.
- Floating-point ordinary math retains IEEE values; finite persistence validates only at its serialization boundary.
- Floating-point vector normalization and normalized direction return zero for non-finite components or differences.
- Floating-point component approximation uses the strict internal tolerance (`1e-6f`) and accepts exact equality first; unit-vector checks retain their separate `0.001` tolerance.
- `Vector2` and `Vector3` movement use the pinned `1e-5f` destination-proximity threshold independently of scalar approximate equality. `Vector3` length limiting preserves division-before-multiplication rounding; octahedral decoding clamps out-of-square fold correction.
- Integer ordinary component arithmetic wraps explicitly; division and invalid absolute values retain managed exceptions. Integer-vector squared norms widen before multiplication, return `long`, and throw when the exact result exceeds `long.MaxValue`; ordinary lengths/distances remain finite across all 32-bit coordinates.
- Float-to-integer vector conversion truncates toward zero and rejects non-finite or out-of-range components. Integer-to-float conversion can lose low-order precision above 2^24.
- Maximum-axis ties select the first component; minimum-axis ties select the last component.
- Numeric hot paths allocate no managed memory after warmup; formatting, transform array operators, and persistence allocate by contract.
- Floating-point and integer rectangle containment is half-open on right/bottom, and rectangle sizes never normalize implicitly. Integer rectangle arithmetic wraps except for the documented `Abs()` minimum-value failure.
- Transform multiplication applies the right operand first; general inversion rejects an exactly singular basis, while reverse point/rectangle operations have an orthonormal-basis precondition.
- `Corner` values remain top-left `0`, top-right `1`, bottom-right `2`, bottom-left `3`; StyleBoxFlat validates undefined values before indexed access.
- `Side` values remain left `0`, top `1`, right `2`, bottom `3`.
- `Geometry` does not mutate caller values. Raster lines include both endpoints; nearest-pair and raster results are fresh arrays; missing intersections are nullable values.

## Current implementation status

Core values are executable with type-specific semantic coverage. `Rect2`, `Transform`, and `Entity` use the engine-owned `Vector2` directly, and duplicated scalar interpolation/modulus/snapping/angle/approximation helpers have been migrated to `Mathf`. `Vector2i` and `Rect2i` have audited value contracts. `Vector3` now has all 84 applicable members and its type row audited; `Vector3i` now has all 56 declared members and its type row audited; `Vector4` now has all 65 applicable members and its type row audited; `Vector4i` now has all 52 declared members and its type row audited, completing the applicable member audit for all six vector types. The `Vector3` length/movement and octahedral packing slice closed four method rows after fixing source-order rounding, the 1e-5 proximity threshold and out-of-square fold clamping. A core-value audit then closed 34 constructor/constant/component/operator rows with IEEE, copy, ordering and integer-to-float boundary checks. The componentwise scalar audit closed 22 more Vector3 rows, correcting NaN-sensitive MinAxisIndex branches while retaining Mathf rounding, clamp and approximate-equality adaptations. The final geometry/interpolation audit closed the remaining 19 methods and type row; internal matrix-row rotation avoids a public 3D Basis, and source-order reflection also corrects the Vector2 sibling. Strict configuration schemas and direct packed-scene storage exist for all six vectors, both rectangles, and transforms. The Vector2 reference audit covers all 82 members, including integer scalar conversion, the default length limit, the corrected 1e-5 MoveToward proximity boundary and the accepted midpoint-to-even rounding boundary under ADRs 0033/0034. The Transform reference audit covers all 43 declared members, including the zero target default for `LookingAt` and C# integer scalar conversion under ADR 0029.

`Geometry` implements twenty-four pure raster, nearest-point, polygon, hull, decomposition, triangulation, atlas, intersection, clipping and offset methods. Its polygon triangulation also serves retained canvas drawing through caller-owned scratch buffers. Convex-part merging adapts PolyPartition ([notice and license](../../licence/PolyPartition-LICENSE.txt)). The declared geometry class is complete under the accepted typed C# projection; clipping and offsets use internally compiled Clipper2 2.0.1 with five decimal digits of internal precision.

`Rect2` now has all 27 mapped members and its type row audited against the pinned floating-point rectangle and ADRs 0025/0029/0034. Source-order `Abs` and growth calculations preserve IEEE edge behavior; half-open containment and border-aware overlap preserve the source's rejection order when NaN is present. `Rect2i` now has all 23 mapped members and its type row audited against the pinned integer rectangle and ADRs 0033/0035. Its `Abs` selects the negative size before position addition, preserving wrapped positions and the explicit `int.MinValue` error. Both rectangle types have typed conversion, strict persistence, packed-scene and warmed allocation checks on Linux/.NET 8.

## Exclusions and limitations

- Universal-value truth conversion is permanently excluded by the typed C# architecture.
- Four-component projection operations are excluded because the engine has no 3D projection type.
- Three-dimensional rectangles, transforms, nodes, and rendering remain outside the 2D product boundary. Numeric `Vector3` and `Vector3i` do not introduce these systems.
- No public external-numerics adapter exists. Future native/package adapters must remain localized at integration boundaries.
- Shader uniforms accept the numeric vector values; these pure values do not add physics, UI layout, atlas, or native ABI integration.

## Verification

The executable harness covers every method/operator family, layouts and constants, index failures, interpolation, the strict internal tolerance migration boundaries, NaN/infinity/signed-zero behavior, integer wrap/overflow/zero division, widened integer norms and their checked limits, conversion boundaries, axis ties, floating-point and integer rectangle boundaries, affine order/inversion/decomposition, Entity integration, strict malformed persistence, packed-scene value copying, invariant formatting, and warmed allocation behavior.

`GeometryTests.Run` covers the pure queries on raster orientation/endpoints, integer limits, degenerate and crossing segments, circle boundaries, nullable intersections, polygon winding/containment, convex hull closure, convex decomposition and area, atlas layout and limits, polygon and Delaunay triangulation, segment/circle contact, polygon set-operation areas and holes, open-line clipping, offset joins/caps, and invalid inputs. `CanvasPolygonTests.Run` covers reuse without warmed redraw allocations.

Execution is verified on Linux/.NET 8 only. Native ABI and the Windows/macOS/Linux (X11/Wayland)/Android/iOS/Web build and host matrix remain unverified.
For the changed `Vector2`/`Vector3` geometric methods, a single-thread .NET 8 probe warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes. `MoveToward` on both vector sizes, `Vector3.LimitLength`, octahedral encode/decode and value accumulation allocated zero managed bytes in every active and idle pass. External callback or native allocations were outside this pure-value probe.
For the componentwise `Vector3` slice, a Linux/.NET 8 single-thread probe warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes of axis selection, absolute value, clamp, maximum, modulus and snapping. Each pass allocated zero managed bytes in both modes. This pure-value probe does not measure native or external callback allocations.
For the completed `Vector3` geometry slice, a Linux/.NET 8 single-thread probe warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes of Vector2/Vector3 reflection and Vector3 bounce, rotation and spherical interpolation. Each active and idle pass allocated zero managed bytes. Native and callback-external allocations were outside this pure-value probe.
For the `Vector3i` audit, the same Linux/.NET 8 single-thread pattern warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes of checked norms, distance, snapping, clamp, axis selection and arithmetic. Every active and idle pass allocated zero managed bytes; native and external allocations were not measured.
For the `Vector4` audit, a single-thread Linux/.NET 8 probe warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes of normalized values, snapping, cubic interpolation, norms, dot and axis selection. All active and idle passes allocated zero managed bytes; native and external allocations were not measured.
For the `Vector4i` audit, the same Linux/.NET 8 single-thread pattern warmed 8,192 active and 8,192 idle cycles, then measured three 4,096-cycle passes of checked norms, distance, snapping, clamp, axis selection and arithmetic. Every active and idle pass allocated zero managed bytes; native and external allocations were not measured.

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
