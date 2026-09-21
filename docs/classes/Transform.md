# Transform

Last updated: 2026-09-21

## Declaration

- Source: [`Transform.cs`](../../src/Core/Math/Transform.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Transform : IEquatable<Transform>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Transform` is a mutable 24-byte affine-transform value composed of three sequential `Electron2D.Vector2` columns. `X` and `Y` are the basis axes; `Origin` is translation. The value represents clockwise screen-space rotation, translation, non-uniform scale, reflection, and skew without owning a renderer, native handle, identity, callback, or managed reference.

Zero initialization produces six zero components and is intentionally different from `Identity`. Ordinary value assignment copies all components. The type does not derive from `ElectronObject` and has no disposal lifecycle.

## Complete public API

| Member | Current behavior |
| --- | --- |
| `X`, `Y`, `Origin` | Mutable matrix columns for the basis axes and translation |
| `Identity`, `FlipX`, `FlipY` | Unit transform and horizontal/vertical reflection constants returned by value |
| `Rotation` | Angle of `X`, in clockwise screen-space radians |
| `Scale` | `X` length and determinant-signed `Y` length; reflection sign is carried by Y |
| `Skew` | Angular deviation from an orthogonal basis, accounting for reflection |
| `this[int]` | Mutable column access for indices zero through two |
| `this[int, int]` | Mutable column-major component access for three columns and two rows |
| `Transform(Vector2, Vector2, Vector2)` | Stores X, Y, and Origin columns |
| `Transform(float, float, float, float, float, float)` | Stores the six column-major components |
| `Transform(float, Vector2)` | Constructs clockwise rotation plus translation |
| `Transform(float, Vector2, float, Vector2)` | Constructs rotation, signed scale, skew, and translation |
| `AffineInverse()` | General inverse for any basis whose determinant is not exactly zero |
| `BasisXform(Vector2)` | Applies the basis while ignoring Origin |
| `BasisXformInv(Vector2)` | Applies the transposed basis; it is an inverse only for an orthonormal basis |
| `Determinant()` | Returns the signed basis determinant |
| `InterpolateWith(Transform, float)` | Decomposes, shortest-path interpolates angles, linearly interpolates scale/origin, and recomposes; extrapolation is allowed |
| `Inverse()` | Fast transpose-based inverse under the caller-supplied orthonormal-basis precondition |
| `IsConformal()` | Tests approximate orthogonality and uniform scale, including reflection |
| `IsEqualApprox(Transform)` | Per-component scale-aware [`Mathf.Epsilon`](Mathf.md) (`1e-6f`), with exact equality first |
| `IsFinite()` | Requires all six components to be neither NaN nor infinity |
| `LookingAt(Vector2)` | Uses the inverse-local target and signed source scale to adjust rotation while retaining Origin and removing scale/skew; a skewed source does not reduce to the raw global target angle |
| `Orthonormalized()` | Applies Gram-Schmidt to the basis and preserves Origin |
| `Rotated`, `Scaled`, `Translated` | Apply global/parent-frame changes through left multiplication semantics |
| `RotatedLocal`, `ScaledLocal`, `TranslatedLocal` | Apply local-frame changes through right multiplication semantics |
| `Transform * Transform` | Composes parent/left with child/right; right is applied first |
| `Transform * Vector2` | Applies the full affine transform to a point |
| `Vector2 * Transform` | Applies the inverse orthonormal transform to a point |
| `Transform * Vector2[]`, `Vector2[] * Transform` | Return newly allocated forward/inverse transformed arrays; input arrays are unchanged |
| `Transform * Rect`, `Rect * Transform` | Return forward or inverse-orthonormal axis-aligned bounds of all four rectangle corners |
| `Transform * float`, `Transform / float` | Apply scalar arithmetic to all six components, including Origin |
| `==`, `!=`, `Equals` | Exact component equality; NaN is unequal |
| `GetHashCode()` | Hashes all three columns |
| `ToString()`, `ToString(string?)` | Invariant-culture X/Y/Origin formatting |

## Matrix and ordering invariants

- Columns are laid out as `X=(xx,xy)`, `Y=(yx,yy)`, and `Origin=(ox,oy)`. On paper, the affine matrix is `[xx yx ox; xy yy oy]`.
- A point becomes `(xx*x + yx*y + ox, xy*x + yy*y + oy)`.
- Positive angles turn positive X toward positive Y, which appears clockwise in the engine's screen coordinate convention.
- `left * right` applies `right` first. This is the parent/child order used by [`Node`](Node.md).
- `Scaled` scales matrix rows and Origin componentwise. `ScaledLocal` scales the X and Y columns by `scale.X` and `scale.Y` respectively and preserves Origin.
- `Scale` places a negative-determinant reflection sign on Y. A zero or unordered determinant produces a zero reported Y scale.
- `Inverse`, `BasisXformInv`, and the reverse vector/array operators assume an orthonormal basis and intentionally do not validate it. `AffineInverse` is the general path.
- `AffineInverse` rejects an exactly zero determinant with `InvalidOperationException`; a very small nonzero determinant is accepted and follows IEEE overflow behavior.
- `Orthonormalized` keeps zero or dependent axes as zero rather than creating non-finite values. It does not promise two unit axes for degenerate input.
- Ordinary math retains IEEE values. `IsFinite` is the explicit validation operation.

## Error behavior

- Either indexer throws `ArgumentOutOfRangeException` for an invalid column or row.
- `AffineInverse` and singular `LookingAt` throw `InvalidOperationException` when the determinant is exactly zero.
- Array operators throw `ArgumentNullException` for a null array.
- `ToString(string?)` throws `FormatException` for an invalid numeric format.
- Scalar division by zero does not throw; it produces ordinary IEEE infinity or NaN components.

## Lifecycle, threading, and allocation

There is no lifecycle beyond value construction, mutation, and copying. Independent copies are safe to use on different threads. Concurrent mutation of the same storage location is an ordinary unsynchronized C# data race.

Constructors, scalar math, decomposition, composition, point transformation, inversion, comparison, and hashing allocate no managed memory after JIT warmup. Array operators allocate one destination array. String formatting allocates. Sequential 24-byte layout is verified locally but does not promise ABI equivalence with a future native backend.

## Dependencies and integration

The public type depends on canonical scalar [`Mathf`](Mathf.md), [`Vector2`](Vector2.md), [`Rect`](Rect.md), globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores finite transforms using exact nested `X.X/Y`, `Y.X/Y`, and `Origin.X/Y` fields. Typed property descriptors and [`PackedScene`](PackedScene.md) preserve the reference-free value directly.

`Node.Transform`, `Node.GlobalTransform`, point conversion, reparenting, and relative transforms use this value directly. There is no public implicit or explicit conversion to another numerics library; native or package adapters must remain localized at their future integration boundary.

## Official reference coverage inventory

The implementation was audited against the official 4.7.2 stable class contract, native math, and typed C# binding.

| Reference family | Electron2D disposition |
| --- | --- |
| X/Y/Origin, constants, constructors, decomposition, index access | Implemented with `Electron2D.Vector2`; zero-initialized C# value remains a zero matrix and `Identity` is explicit |
| General/orthonormal inverse, determinant, basis transforms | Implemented; singular general inverse is an explicit C# exception |
| Interpolation, conformal/finite/approximate checks, `LookingAt`, orthonormalization | Implemented, including `IsConformal` and `LookingAt` that are present in the general stable API but absent from the audited typed binding |
| Global/local rotation, scale, and translation | Implemented; local scaling follows the documented/native column semantics rather than the audited managed binding's componentwise discrepancy |
| Transform, point, array, scalar, equality, index, and formatting operations | Implemented; integer scalar overloads use normal C# conversion to the float overload, and packed-vector arrays map to `Vector2[]` |
| Rectangle transform operators | Implemented by transforming all four corners into axis-aligned bounds; reverse multiplication uses the orthonormal inverse contract |
| Copy constructor and origin/getter methods | Normal struct assignment and the public fields/properties are the typed C# surface; redundant wrappers are excluded |
| Boolean truth conversion | Permanently excluded because the engine has no universal-value truthiness contract |
| In-place native setters/operators | Expressed through mutable fields, constructors, and returned values rather than duplicated public mutators |

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies 24-byte layout, zero/identity/reflection values, constructors, both indexers and failures, matrix order, decomposition including negative scale/skew, point/basis/array/rectangle transforms, negative-size rectangle normalization, general and orthonormal inverse behavior, singular failures, global/local operation differences, shortest-angle interpolation and extrapolation, conformal/finite/exact/approximate behavior including NaN/infinity, degenerate orthonormalization, `LookingAt`, scalar arithmetic, culture-invariant formatting, strict configuration persistence and malformed input, packed-scene storage, and zero warmed allocation for numeric math.

Execution is currently verified only on Linux/.NET 8. The type has not been exercised through a renderer, native SDL backend, or five-platform test matrix.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0025: Typed axis-aligned rectangle geometry](../decisions/core-math.md#adr-0025)
- [0026: Separate Transform foundational type](../decisions/core-math.md#adr-0026)
- [0029: Typed Transform value and affine semantics](../decisions/core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](../decisions/core-math.md#adr-0033)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
