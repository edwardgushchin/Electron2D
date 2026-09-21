# Transform2D

Last updated: 2026-09-21

## Declaration

- Source: [`Transform2D.cs`](../../src/Core/Math/Transform2D.cs)
- Namespace: `Electron2D`
- Declaration: `[Serializable] [StructLayout(LayoutKind.Sequential)] public struct Transform2D : IEquatable<Transform2D>`
- Domain: [Core](../domains/core.md)
- Component: [Geometry values](../components/geometry-values.md)

## Responsibility and ownership

`Transform2D` is a mutable 24-byte affine-transform value composed of three sequential `System.Numerics.Vector2` columns. `X` and `Y` are the basis axes; `Origin` is translation. The value represents clockwise screen-space rotation, translation, non-uniform scale, reflection, and skew without owning a renderer, native handle, identity, callback, or managed reference.

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
| `Transform2D(Vector2, Vector2, Vector2)` | Stores X, Y, and Origin columns |
| `Transform2D(float, float, float, float, float, float)` | Stores the six column-major components |
| `Transform2D(float, Vector2)` | Constructs clockwise rotation plus translation |
| `Transform2D(float, Vector2, float, Vector2)` | Constructs rotation, signed scale, skew, and translation |
| `AffineInverse()` | General inverse for any basis whose determinant is not exactly zero |
| `BasisXform(Vector2)` | Applies the basis while ignoring Origin |
| `BasisXformInv(Vector2)` | Applies the transposed basis; it is an inverse only for an orthonormal basis |
| `Determinant()` | Returns the signed basis determinant |
| `InterpolateWith(Transform2D, float)` | Decomposes, shortest-path interpolates angles, linearly interpolates scale/origin, and recomposes; extrapolation is allowed |
| `Inverse()` | Fast transpose-based inverse under the caller-supplied orthonormal-basis precondition |
| `IsConformal()` | Tests approximate orthogonality and uniform scale, including reflection |
| `IsEqualApprox(Transform2D)` | Per-component scale-aware epsilon `0.00001`, with exact equality first |
| `IsFinite()` | Requires all six components to be neither NaN nor infinity |
| `LookingAt(Vector2)` | Uses the inverse-local target and signed source scale to adjust rotation while retaining Origin and removing scale/skew; a skewed source does not reduce to the raw global target angle |
| `Orthonormalized()` | Applies Gram-Schmidt to the basis and preserves Origin |
| `Rotated`, `Scaled`, `Translated` | Apply global/parent-frame changes through left multiplication semantics |
| `RotatedLocal`, `ScaledLocal`, `TranslatedLocal` | Apply local-frame changes through right multiplication semantics |
| `Transform2D * Transform2D` | Composes parent/left with child/right; right is applied first |
| `Transform2D * Vector2` | Applies the full affine transform to a point |
| `Vector2 * Transform2D` | Applies the inverse orthonormal transform to a point |
| `Transform2D * Vector2[]`, `Vector2[] * Transform2D` | Return newly allocated forward/inverse transformed arrays; input arrays are unchanged |
| `Transform2D * float`, `Transform2D / float` | Apply scalar arithmetic to all six components, including Origin |
| `==`, `!=`, `Equals` | Exact component equality; NaN is unequal |
| `GetHashCode()` | Hashes all three columns |
| `ToString()`, `ToString(string?)` | Invariant-culture X/Y/Origin formatting |

## Matrix and ordering invariants

- Columns are laid out as `X=(xx,xy)`, `Y=(yx,yy)`, and `Origin=(ox,oy)`. On paper, the affine matrix is `[xx yx ox; xy yy oy]`.
- A point becomes `(xx*x + yx*y + ox, xy*x + yy*y + oy)`.
- Positive angles turn positive X toward positive Y, which appears clockwise in the engine's screen coordinate convention.
- `left * right` applies `right` first. This is the parent/child order and must not be replaced by `System.Numerics.Matrix3x2` multiplication without reversing the row-vector composition order.
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

The public type depends only on `System.Numerics.Vector2`, floating-point math, globalization, and interop metadata. [`ConfigFile`](ConfigFile.md) stores finite transforms using exact nested `X.X/Y`, `Y.X/Y`, and `Origin.X/Y` fields. Typed property descriptors and [`PackedScene`](PackedScene.md) preserve the reference-free value directly.

`Node.Transform` and `Node.GlobalTransform` still use `System.Numerics.Matrix3x2`; migration is a separate source-breaking slice required by ADR 0026. [`Rect2`](Rect2.md) transform operators are likewise deferred to that migration slice. There is no implicit or explicit `Matrix3x2` conversion because its row-vector composition convention needs a deliberate migration contract.

## Official reference coverage inventory

The implementation was audited against the official 4.7.2 stable class contract, native math, and typed C# binding.

| Reference family | Electron2D disposition |
| --- | --- |
| X/Y/Origin, constants, constructors, decomposition, index access | Implemented with `System.Numerics.Vector2`; zero-initialized C# value remains a zero matrix and `Identity` is explicit |
| General/orthonormal inverse, determinant, basis transforms | Implemented; singular general inverse is an explicit C# exception |
| Interpolation, conformal/finite/approximate checks, `LookingAt`, orthonormalization | Implemented, including `IsConformal` and `LookingAt` that are present in the general stable API but absent from the audited typed binding |
| Global/local rotation, scale, and translation | Implemented; local scaling follows the documented/native column semantics rather than the audited managed binding's componentwise discrepancy |
| Transform, point, array, scalar, equality, index, and formatting operations | Implemented; integer scalar overloads use normal C# conversion to the float overload, and packed-vector arrays map to `Vector2[]` |
| Rectangle transform operators | Deferred to the explicit `Node`/`Rect2` migration required by ADR 0026; neither existing type is changed in this standalone slice |
| Copy constructor and origin/getter methods | Normal struct assignment and the public fields/properties are the typed C# surface; redundant wrappers are excluded |
| Boolean truth conversion | Permanently excluded because the engine has no universal-value truthiness contract |
| In-place native setters/operators | Expressed through mutable fields, constructors, and returned values rather than duplicated public mutators |

## Verification and known limitations

`tests/Electron2D.Tests/Program.cs` verifies 24-byte layout, zero/identity/reflection values, constructors, both indexers and failures, matrix order, decomposition including negative scale/skew, point/basis/array transforms, general and orthonormal inverse behavior, singular failures, global/local operation differences, shortest-angle interpolation and extrapolation, conformal/finite/exact/approximate behavior including NaN/infinity, degenerate orthonormalization, `LookingAt`, scalar arithmetic, culture-invariant formatting, strict configuration persistence and malformed input, packed-scene storage, and zero warmed allocation for numeric math.

Execution is currently verified only on Linux/.NET 8. The type has not been exercised through a renderer, native SDL backend, five-platform test matrix, migrated `Node`, or rectangle transformation.

## Decisions

- [0001: Typed C# without Variant](../decisions/0001-typed-csharp-without-variant.md)
- [0014: Managed lifetime and realtime allocation](../decisions/0014-managed-resource-lifetime.md)
- [0017: Source-tree module layout](../decisions/0017-source-tree-layout.md)
- [0025: Typed axis-aligned rectangle geometry](../decisions/0025-typed-rectangle-geometry.md)
- [0026: Separate Transform2D foundational type](../decisions/0026-separate-transform2d-type.md)
- [0029: Typed Transform2D value and affine semantics](../decisions/0029-typed-transform2d-value.md)
