# Electron2D core math decisions

Last updated: 2026-09-21

This bounded log owns the complete architectural records for core math. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0024](#adr-0024), [0025](#adr-0025), [0026](#adr-0026), [0029](#adr-0029).

<a id="adr-0024"></a>
## ADR 0024: Typed color values and portable quantization

Last updated: 2026-09-21

### Status

Accepted.

### Context

Electron2D needs the foundational color value before rendering, images, themes, and concrete visual resources can be designed. The high-level reference API supplies a mature RGBA surface with HSV, OKHSL, HTML/named parsing, packed integers, arithmetic, and language-specific operators. The repository additionally requires typed C#, one Electron2D-owned assembly, portable behavior, no hidden native dependency, complete XML/living documentation, and allocation-free numeric hot paths.

The official 4.7.2 stable reference was audited across `Color.xml`, native `color.h`/`color.cpp`, the typed C# `Color.cs`/`Colors.cs`, the color-name table, and the published `ok_color.h`. The native and typed C# surfaces differ at several edges: default alpha, byte-scaled input range, clamp failure behavior, negative HSV hue, midpoint conversion, and direct empty HTML parsing. Electron2D must choose one explicit executable contract instead of accidentally mixing them.

### Decision

`Electron2D.Color` is a mutable `[Serializable]`, sequential, 16-byte `struct` implementing `IEquatable<Color>`, with public `float R/G/B/A` fields and the complete stable typed C# surface. It is not an `ElectronObject`, has no lifetime protocol, and has no backend handle. `Electron2D.Colors` separately exposes all 146 current PascalCase named values.

The following contracts are fixed:

- zero initialization and `new Color()` are transparent black; opaque black and transparent white remain explicit named values;
- ordinary float construction/arithmetic retain HDR, infinity, and NaN; RGB is assumed sRGB except at explicitly linear conversion/luminance boundaries, and alpha is linear;
- the integer-scale `R8/G8/B8/A8` views follow the typed C# binding and do not clamp to byte range; NaN maps to zero and overflow saturates at an `int` endpoint;
- OKHSL uses an in-assembly managed port of Björn Ottosson's MIT-licensed reference formulas, with the original license retained; no native call is required;
- named lookup is case-insensitive after removing spaces, hyphens, underscores, apostrophes, and periods, and reads an immutable frozen table;
- packed and HTML output use a portable Electron2D quantizer: NaN becomes zero, infinities/out-of-range values clamp to an endpoint, and finite values use midpoint-to-even rounding;
- configuration persistence accepts only finite colors and uses an exact `R/G/B/A` JSON object, rejecting missing, duplicate, or unknown fields;
- typed property descriptors and packed scenes store/copy `Color` directly because it contains no managed references;
- no boolean truth conversion, `System.Drawing` dependency, native name-index API, speculative renderer conversion, or public RGBE encoder is added.

The typed C# empty-span behavior of `FromHtml` returning opaque black is retained, even though `HtmlIsValid` reports empty input as invalid. String constructors do not route invalid input through that edge: they try names and throw for unknown values. Null is rejected explicitly with `ArgumentNullException`.

### Consequences

- Core gains a complete backend-independent color vocabulary before renderer work without creating a renderer abstraction.
- Numeric operations are value-only and allocation-free after JIT warmup; named/string operations may allocate.
- `Color` is naturally usable in existing typed configuration and packed-scene mechanisms, and their wire/capture contracts are verified rather than assumed.
- Public OKHSL getters clamp coordinates to `0..1` as required; at a narrow saturated gamut boundary the reference algorithm can compute saturation slightly above one, so reconstruction from the clamped public triple is knowingly lossy and explicitly tested.
- Public mutable fields are an intentional API/layout choice, not a general encapsulation precedent.
- Packed HDR/non-finite output is deterministic across supported runtimes but deliberately safer than unchecked casts in the reference C# binding.
- Future SDL/render backends must convert explicitly and cannot assume ABI equivalence merely because `Color` is sequential.
- `System.Drawing.Color` or UI-framework `Colors` imports may require normal C# aliases; Electron2D will not rename the public types to avoid namespace ambiguity.

### Rejected alternatives

- **Wait for a renderer:** rejected because color math, persistence, and scene storage are independent foundational behavior.
- **Use `System.Drawing.Color`:** rejected because it is byte-oriented, does not provide the required API, and introduces an inappropriate platform/library identity.
- **Use HSV as an OKHSL approximation:** rejected because its perceptual and gamut behavior is materially different.
- **Call a future native backend for OKHSL:** rejected because it would make core value math unavailable or platform-dependent before host initialization.
- **Generate named properties at runtime/reflection:** rejected because the public compile-time API and XML documentation require real properties.
- **Add generic parsing/formatting interfaces now:** rejected because the required stable API already supplies span HTML parsing and invariant formatting; extra interfaces add unsupported contract surface.

### Verification

`tests/Electron2D.Tests/Program.cs` covers 16-byte layout/default semantics; constructors and mutable components; HSV/OKHSL anchors and round trips; color-space transfer; source-over blend; unbounded operations; failures; integer/HTML formats including non-finite quantization; all 146 public named properties and concurrent lookup; arithmetic, exact/approximate/NaN comparisons; invariant formatting; strict `ConfigFile` schema/rollback; `PackedScene` stored-property restoration; and zero allocations in a warmed numeric loop.

Verification is currently Linux/.NET 8. Renderer/native pixel equivalence and the full target platform matrix remain unavailable until their respective domains and hosts exist.

<a id="adr-0025"></a>
## ADR 0025: Typed axis-aligned rectangle geometry

Last updated: 2026-09-21

### Status

Accepted.

### Context

Electron2D needs a foundational floating-point rectangle before rendering, UI, culling, images, collision, and navigation are designed. The official 4.7.2 stable `Rect2` contract and typed C# implementation were audited together with the native `rect2.h` behavior and global `Side` values. The relevant surface includes mutable position/size/end, signed area, normalization, containment, intersection, enclosure, growth, support mapping, merge, finite and approximate checks, formatting, four edge identities, an integer-rectangle conversion, transform multiplication, and a language-specific boolean conversion.

The repository already uses `System.Numerics.Vector2` throughout unified 2D nodes. Adding a second vector type solely for rectangle parity would duplicate arithmetic and introduce conversion noise. Conversely, substituting `System.Drawing.RectangleF` would bring an unsuitable framework identity and different semantics.

### Decision

`Electron2D.Rect2` is a mutable `[Serializable]`, sequential, 16-byte `struct` implementing `IEquatable<Rect2>`. It stores two private `System.Numerics.Vector2` fields and exposes the complete stable typed C# rectangle surface that does not depend on missing Electron2D types. `Electron2D.Side` is a separate four-value enum with stable numeric identities.

The following contracts are fixed:

- ordinary construction/mutation preserves negative, zero, NaN, and infinite components; normalization is explicit through `Abs()`;
- methods that rely on ordered edges document non-negative size rather than normalizing silently;
- point containment is half-open at the right and bottom edges;
- outer border-only contact is excluded by default, while `Intersects` has an explicit border-inclusion option; a zero-size rectangle strictly inside another retains its position as an empty intersection;
- algebraic growth may over-shrink and create non-positive size;
- an undefined `Side` passed to `GrowSide` is a no-op, matching the audited typed implementation;
- exact equality exposes IEEE NaN behavior; approximate equality checks exact equality first and otherwise uses the repository's scale-aware `0.00001` tolerance;
- numeric formatting is invariant-culture;
- `ConfigFile` accepts only finite rectangles and persists exactly nested `Position.X/Y` and `Size.X/Y` fields, rejecting missing, duplicate, unknown, nonnumeric, or non-finite input;
- typed property descriptors and packed scenes store/copy `Rect2` directly because it contains no managed references;
- a `Rect2I` constructor is deferred until that complete engine type exists; transform multiplication remains deferred to the explicit Node/Rect2 migration recorded by ADR 0026 and ADR 0029;
- language-specific boolean truth conversion is permanently excluded.

No renderer, UI, physics, or platform abstraction is created by this decision.

### Consequences

- Core gains allocation-free rectangle geometry usable by future domains without acquiring dependencies on them.
- Existing `Vector2` transforms and positions interoperate directly with rectangle positions, sizes, centers, support points, and query points.
- Negative sizes remain representable and observable; callers must decide when normalization is appropriate.
- Configuration persistence rejects non-finite values even though ordinary runtime geometry retains them.
- A future `Rect2I` implementation and the pending migration to the now-implemented standalone `Transform2D` must add and verify the deferred rectangle members rather than retrofitting unrelated .NET types.
- Sequential layout is useful for predictable managed storage but does not promise native backend ABI equivalence.

### Rejected alternatives

- **Wait for a renderer or physics backend:** rejected because rectangle math and typed persistence are independently useful foundational behavior.
- **Create a custom `Vector2`:** rejected because `System.Numerics.Vector2` already supplies the required portable value contract and is established throughout the repository.
- **Use `System.Drawing.RectangleF`:** rejected because its API/semantics differ and the dependency is inappropriate for the runtime target matrix.
- **Normalize on every construction:** rejected because it destroys intentional negative-size values and diverges from the audited contract.
- **Add placeholder `Rect2I` or `Transform2D` types:** rejected because empty compatibility shells would violate the repository definition of done. ADR 0026 required a real standalone `Transform2D` vertical slice, now fulfilled by ADR 0029.
- **Serialize every public property automatically:** rejected because computed `End` and `Area` would create a redundant, unstable, ambiguous schema.

### Verification

`tests/Electron2D.Tests/Program.cs` covers layout/defaults, every implemented constructor/member/operator, negative/zero size boundaries, all side values and undefined input, half-open containment, overlap and border behavior, IEEE values, invariant formatting, strict configuration shape and failure rollback, packed-scene copying, and zero allocations in a warmed geometry loop.

Verification is currently Linux/.NET 8. Native structure equivalence, renderer/physics use, integer rectangles, transform multiplication, and the full five-platform matrix remain unavailable.

### Related decision

- [0026: Separate Transform2D type](core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)

<a id="adr-0026"></a>
## ADR 0026: Separate Transform2D foundational type

Last updated: 2026-09-21

### Status

Accepted and fulfilled for the standalone value by ADR 0029. This decision partially supersedes ADR 0008 only where that ADR rejected a separate `Transform2D` type. Migration of `Node` and `Rect2` remains pending; the unified `Node` hierarchy and all other ADR 0008 decisions remain accepted.

### Context

ADR 0008 chose `System.Numerics.Matrix3x2` directly for the first complete unified `Node` implementation and explicitly avoided a speculative transform wrapper. That kept the initial scene slice executable, but it does not express the final engine API direction: Electron2D requires a dedicated 2D transform value with a stable engine-owned contract and direct integration with geometry types such as `Rect2`.

At the time of this decision, the requirement was architectural rather than a claim that the type already existed. ADR 0029 has since delivered the standalone production type. `Node.Transform` and `Node.GlobalTransform` still remain `Matrix3x2` values.

### Decision

Electron2D provides a separate public foundational value type named `Transform2D`. ADR 0029 records its audited public surface, XML/living documentation, error/threading/allocation contracts, persistence, executable tests, and post-implementation audit.

The type owns the engine-facing 2D affine-transform vocabulary, including basis/origin representation, composition, forward and inverse point/vector transformation, affine inversion, and finite/approximate checks. Rectangle transformation belongs to the pending integration slice because it changes `Rect2`; its absence does not make the standalone transform a placeholder.

Until the migration slice is complete:

- `Matrix3x2` remains the truthful current `Node` API and implementation;
- `Rect2` transform multiplication remains explicitly dependency-blocked;
- no implicit compatibility shim or misleading operator is added;
- current behavior must not be documented as already migrated.

The separate migration must update `Node`, `Rect2`, tests, XML documentation, class/component/domain documents, inventory, and the ADR chain atomically. Packed-scene/property/config persistence for standalone `Transform2D` values is already implemented and verified by ADR 0029.

Electron2D remains 2D-only. No generic 3D-capable `Transform` or `Transform3D` type is authorized by this decision.

### Consequences

- The final public geometry vocabulary will not expose `Matrix3x2` as the permanent engine abstraction.
- Current code stays complete and honest while the remaining migration is tracked explicitly rather than represented by a shim.
- `Rect2` ships all independent geometry and can add transform operators in the deliberate migration now that both operand contracts exist.
- The future migration may be source-breaking for current `Node` transform consumers and therefore requires an explicit compatibility and release decision during implementation.
- Standard-library numerics may still be used internally or through explicit conversions, but they do not replace the required engine-owned type.

### Rejected alternatives

- **Keep `Matrix3x2` permanently:** rejected by the required engine API direction and missing direct geometry contract.
- **Add an empty wrapper now:** rejected because it would be a misleading compatibility stub and violate the definition of done.
- **Implement `Transform2D` inside the `Rect2` task:** rejected because it is an independent foundational type with a substantial API, invariants, and migration impact that required its own complete vertical slice, now recorded by ADR 0029.
- **Add a generic or 3D transform family:** rejected because Electron2D is exclusively 2D.

### Verification boundary

ADR 0029 fulfills and verifies the standalone value requirement. Current tests verify that value independently alongside `Node`'s existing `Matrix3x2` contract and `Rect2`'s transform-independent geometry. They do not claim that Node migration or rectangle transformation is complete.

### Related decision

- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)

<a id="adr-0029"></a>
## ADR 0029: Typed Transform2D value and affine semantics

Last updated: 2026-09-21

### Status

Accepted. This decision fulfills ADR 0026's requirement for a standalone `Transform2D` value. The separate migration of `Node` and `Rect2` remains pending.

### Context

ADR 0026 established that `System.Numerics.Matrix3x2` is not the permanent engine-facing transform vocabulary. The official 4.7.2 stable [`Transform2D` documentation](https://docs.godotengine.org/en/stable/classes/class_transform2d.html), [typed C# implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Transform2D.cs), and [native implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/core/math/transform_2d.cpp) were audited together because they differ in default construction, available methods, local-scale behavior, scalar operations, and failure handling.

Electron2D already uses `System.Numerics.Vector2`, strict typed configuration snapshots, and reference-free packed-scene values. A foundational affine transform can therefore be complete without adding a renderer, native dependency, custom vector family, or dynamic value container.

### Decision

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

### Consequences

- Core now owns a backend-independent affine vocabulary suitable for later scene, rendering, UI, camera, culling, and physics integration.
- Callers can perform complete transform math without allocating or depending on SDL.
- Zero initialization remains honest C# storage rather than silently becoming identity; consumers must request `Identity` when that semantic is required.
- Configuration persistence rejects non-finite transforms even though ordinary runtime math retains IEEE values.
- The current `Node` API temporarily continues to expose `Matrix3x2`; the existence of `Transform2D` alone does not claim that migration is complete.
- Rectangle transformation is still absent and must not be inferred from the standalone type.

### Rejected alternatives

- **Wrap `Matrix3x2` internally and forward multiplication:** rejected because its row-vector composition order would make the public parent/child contract easy to reverse accidentally.
- **Make zero initialization identity:** rejected because C# default arrays, generic storage, and uninitialized fields must retain normal zero-value semantics.
- **Copy the typed binding's local-scale implementation:** rejected because it conflicts with the documented global/local distinction and native column semantics for rotated bases.
- **Validate every orthonormal shortcut:** rejected because validation would add hot-path work and change the explicit precondition into a different failure contract; the general affine path is already available.
- **Add a transform interface or abstract backend:** rejected because a pure value has no polymorphic behavior or backend ownership.
- **Migrate `Node` and `Rect2` in the same commit:** rejected by ADR 0026's staged migration boundary and because those source-breaking API changes require their own tests and compatibility audit.

### Verification

The executable harness covers every implemented member family, matrix/composition order, reflection/skew decomposition, singular and malformed failures, config and packed-scene integration, IEEE boundaries, and warmed allocation behavior. Release compilation also emits XML documentation with warnings treated as errors.

Verification is Linux/.NET 8 only. No renderer, native backend, visual output, five-platform build, `Node` migration, `Rect2` transformation, or user acceptance is established by these checks.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0017: Source-tree module layout](product.md#adr-0017)
- [0018: Typed configuration files](core-data-io.md#adr-0018)
- [0023: Typed in-memory packed scenes](scene.md#adr-0023)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](core-math.md#adr-0026)
