# ADR 0029: Typed Transform2D value and affine semantics

Last updated: 2026-09-21

## Status

Accepted. This decision fulfills ADR 0026's requirement for a standalone `Transform2D` value. The separate migration of `Node` and `Rect2` remains pending.

## Context

ADR 0026 established that `System.Numerics.Matrix3x2` is not the permanent engine-facing transform vocabulary. The official 4.7.2 stable [`Transform2D` documentation](https://docs.godotengine.org/en/stable/classes/class_transform2d.html), [typed C# implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Transform2D.cs), and [native implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/core/math/transform_2d.cpp) were audited together because they differ in default construction, available methods, local-scale behavior, scalar operations, and failure handling.

Electron2D already uses `System.Numerics.Vector2`, strict typed configuration snapshots, and reference-free packed-scene values. A foundational affine transform can therefore be complete without adding a renderer, native dependency, custom vector family, or dynamic value container.

## Decision

`Electron2D.Transform2D` is a mutable `[Serializable]`, sequential, 24-byte `struct` implementing `IEquatable<Transform2D>`. Its public fields `X`, `Y`, and `Origin` are column vectors backed by `System.Numerics.Vector2`.

The following contracts are fixed:

- `default(Transform2D)` and `new Transform2D()` are the all-zero C# value; `Identity` is explicit;
- columns are ordered X, Y, Origin and use column-vector affine mathematics, while positive screen-space rotation is clockwise;
- `left * right` applies the right/child transform first and the left/parent transform second;
- `AffineInverse` supports any exactly nonsingular basis and throws `InvalidOperationException` for an exact zero determinant;
- `Inverse`, `BasisXformInv`, and reverse vector/array multiplication are documented orthonormal-only shortcuts and perform no hidden validation;
- global operations use left-multiplication semantics; local operations use right-multiplication semantics;
- `ScaledLocal` scales basis columns by their respective scalar components and preserves Origin, matching the documented/native contract rather than the audited typed binding discrepancy;
- decomposition carries reflection in the Y scale, interpolation decomposes and recomposes with shortest-path rotation/skew angles, and weights outside zero through one extrapolate;
- `IsConformal` and `LookingAt` are implemented because they belong to the stable general contract even though the audited typed binding omits them;
- zero or dependent axes orthonormalize to zero instead of becoming non-finite;
- exact equality exposes IEEE NaN behavior; approximate equality uses the repository-wide scale-aware epsilon `0.00001` and exact-equality fast path;
- finite `ConfigFile` values use exactly three nested vectors named `X`, `Y`, and `Origin`, each containing exactly finite numeric `X` and `Y` fields;
- typed stored-property capture and packed scenes copy the reference-free value directly;
- array operations allocate a new result and never mutate their source; warmed scalar math remains allocation-free;
- redundant integer scalar overloads use normal C# numeric conversion, copy construction uses value assignment, and boolean truth conversion is excluded.

No implicit or explicit `Matrix3x2` conversion is added. The two types use compatible scalar storage but opposite multiplication conventions because `Matrix3x2` follows row-vector composition. Conversions will be considered only with the explicit `Node` migration and its source-compatibility decision.

`Transform2D`/`Rect2` operators and migration of `Node.Transform`/`GlobalTransform` remain a separate atomic slice under ADR 0026. This standalone change deliberately does not alter either existing public type.

## Consequences

- Core now owns a backend-independent affine vocabulary suitable for later scene, rendering, UI, camera, culling, and physics integration.
- Callers can perform complete transform math without allocating or depending on SDL.
- Zero initialization remains honest C# storage rather than silently becoming identity; consumers must request `Identity` when that semantic is required.
- Configuration persistence rejects non-finite transforms even though ordinary runtime math retains IEEE values.
- The current `Node` API temporarily continues to expose `Matrix3x2`; the existence of `Transform2D` alone does not claim that migration is complete.
- Rectangle transformation is still absent and must not be inferred from the standalone type.

## Rejected alternatives

- **Wrap `Matrix3x2` internally and forward multiplication:** rejected because its row-vector composition order would make the public parent/child contract easy to reverse accidentally.
- **Make zero initialization identity:** rejected because C# default arrays, generic storage, and uninitialized fields must retain normal zero-value semantics.
- **Copy the typed binding's local-scale implementation:** rejected because it conflicts with the documented global/local distinction and native column semantics for rotated bases.
- **Validate every orthonormal shortcut:** rejected because validation would add hot-path work and change the explicit precondition into a different failure contract; the general affine path is already available.
- **Add a transform interface or abstract backend:** rejected because a pure value has no polymorphic behavior or backend ownership.
- **Migrate `Node` and `Rect2` in the same commit:** rejected by ADR 0026's staged migration boundary and because those source-breaking API changes require their own tests and compatibility audit.

## Verification

The executable harness covers every implemented member family, matrix/composition order, reflection/skew decomposition, singular and malformed failures, config and packed-scene integration, IEEE boundaries, and warmed allocation behavior. Release compilation also emits XML documentation with warnings treated as errors.

Verification is Linux/.NET 8 only. No renderer, native backend, visual output, five-platform build, `Node` migration, `Rect2` transformation, or user acceptance is established by these checks.

## Related decisions

- [0001: Typed C# without Variant](0001-typed-csharp-without-variant.md)
- [0014: Managed Resource lifetime and realtime allocation](0014-managed-resource-lifetime.md)
- [0017: Source-tree module layout](0017-source-tree-layout.md)
- [0018: Typed configuration files](0018-typed-config-files.md)
- [0023: Typed in-memory packed scenes](0023-typed-packed-scenes.md)
- [0025: Typed axis-aligned rectangle geometry](0025-typed-rectangle-geometry.md)
- [0026: Separate Transform2D foundational type](0026-separate-transform2d-type.md)
