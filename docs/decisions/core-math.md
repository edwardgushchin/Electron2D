# Electron2D core math decisions

Last updated: 2026-09-22

This bounded log owns the complete architectural records for core math. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0024](#adr-0024), [0025](#adr-0025), [0026](#adr-0026), [0029](#adr-0029), [0032](#adr-0032), [0033](#adr-0033), [0034](#adr-0034).

<a id="adr-0024"></a>
## ADR 0024: Typed color values and portable quantization

Last updated: 2026-09-21

### Status

Accepted. ADR 0034 supersedes only the former implicit component-approximation tolerance by making `Mathf` authoritative.

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

Last updated: 2026-09-22

### Status

Accepted for rectangle semantics. The old `Rect2` name and external vector dependency are superseded by [ADR 0032](core-math.md#adr-0032) and [ADR 0033](core-math.md#adr-0033); the production type is now `Rect` over `Vector2`. ADR 0034 supersedes the historical `0.00001` approximate-comparison tolerance. ADR 0035 supersedes the consumer-gated deferral and has delivered the complete `RectI` sibling.

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
- **Create a custom `Vector2`:** rejected at the time because `System.Numerics.Vector2` supplied the immediate portable value contract and was established throughout the repository. ADR 0032 supersedes this rejection and requires an engine-owned `Vector` throughout the runtime.
- **Use `System.Drawing.RectangleF`:** rejected because its API/semantics differ and the dependency is inappropriate for the runtime target matrix.
- **Normalize on every construction:** rejected because it destroys intentional negative-size values and diverges from the audited contract.
- **Add placeholder `Rect2I` or `Transform2D` types:** rejected because empty compatibility shells would violate the repository definition of done. ADR 0026 required a real standalone `Transform2D` vertical slice, now fulfilled by ADR 0029.
- **Serialize every public property automatically:** rejected because computed `End` and `Area` would create a redundant, unstable, ambiguous schema.

### Verification

`tests/Electron2D.Tests/Program.cs` covers layout/defaults, every implemented constructor/member/operator, negative/zero size boundaries, all side values and undefined input, half-open containment, overlap and border behavior, IEEE values, invariant formatting, strict configuration shape and failure rollback, packed-scene copying, and zero allocations in a warmed geometry loop.

Verification is currently Linux/.NET 8. ADR 0029 has since delivered transform multiplication, and ADR 0035 has delivered the integer rectangle and typed conversions. Native structure equivalence, renderer/physics use, and the full six-target matrix remain unavailable.

### Related decision

- [0026: Separate Transform2D type](core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)
- [0035: Foreseeable public type-family completeness](core-math.md#adr-0035)

<a id="adr-0026"></a>
## ADR 0026: Separate Transform2D foundational type

Last updated: 2026-09-21

### Status

Accepted and fulfilled for affine semantics by ADR 0029. This decision partially supersedes ADR 0008 only where that ADR rejected a separate engine-owned transform type. ADRs 0032 and 0033 complete the rename to `Transform`, its `Vector2` storage, and the `Node`/rectangle migration; the unified `Node` hierarchy and all other ADR 0008 decisions remain accepted.

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

Last updated: 2026-09-22

### Status

Accepted for affine semantics. This decision fulfills ADR 0026's standalone value requirement. ADRs 0032 and 0033 supersede the old type name and external vector dependency; the complete `Vector2`/`Rect`/`Transform`/`Node` migration is now implemented. ADR 0034 supersedes the historical `0.00001` approximate-comparison tolerance.

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

Verification is Linux/.NET 8 only. No renderer, native backend, visual output, six-target build, `Node` migration, `Rect2` transformation, or user acceptance is established by these checks.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0017: Source-tree module layout](product.md#adr-0017)
- [0018: Typed configuration files](core-data-io.md#adr-0018)
- [0023: Typed in-memory packed scenes](scene.md#adr-0023)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](core-math.md#adr-0026)

<a id="adr-0032"></a>
## ADR 0032: Own the complete unsuffixed 2D math vocabulary

Last updated: 2026-09-21

### Status

Partially superseded by [ADR 0033](core-math.md#adr-0033). Engine ownership, the `Rect`/`Transform` names, removal of external public numerics, prohibition of dual APIs, and the completed dependent migration remain accepted. The unsuffixed `Vector` name is superseded by the dimensioned vector family.

### Context

Electron2D is exclusively two-dimensional. The existing public geometry surface nevertheless mixes engine-owned `Rect2` and `Transform2D` values with `System.Numerics.Vector2`, while `Node` additionally exposes `Matrix3x2`. That makes an external numerics library part of the engine contract, prevents Electron2D from defining one complete vector API and its exact numeric semantics, and leaves dimensions encoded redundantly in names even though no three-dimensional family exists.

The engine needs one coherent math vocabulary shared by scene state, rendering, input, physics, UI, resources, serialization, editor tooling, and games. Introducing only a renamed rectangle or transform while retaining `Vector2` would preserve the largest external public dependency and force another source-breaking migration later.

### Decision

Electron2D will own three canonical single-precision 2D value types named `Vector`, `Rect`, and `Transform`.

- `Vector` will be a complete production type rather than a naming wrapper. Its implementation must audit the applicable reference vector contract, define sequential two-float storage, arithmetic and geometric operations, exact and approximate comparison, normalization and zero-length behavior, finite checks, formatting, persistence, packed-scene storage, XML documentation, and allocation-free hot paths.
- Every Electron2D domain must use `Vector` for engine-owned 2D coordinates, sizes, directions, velocities, axes, offsets, input values, render geometry, physics values, and public/protected parameters, properties, fields, and return values.
- `Rect` and `Transform` must use `Vector` for all stored components and API. `Node` transform and spatial members must use `Transform` and `Vector`.
- `System.Numerics.Vector2` and `Matrix3x2` may exist only inside narrow internal adapters at an external package, native API, or host boundary. They must not become engine-owned state or cross a public/protected Electron2D API boundary.
- Old `Vector2`, `Rect2`, and `Transform2D` compatibility aliases, duplicate wrappers, and implicit dual APIs will not be shipped. The project is pre-release, so the migration favors one canonical contract over compatibility debt.
- No three-dimensional vector, rectangle, transform, overload, adapter, or reserved abstraction is introduced.

The migration is one production vertical slice: implement `Vector`; migrate existing rectangle, affine-transform, node, configuration, packed-scene, property, test, example, and documentation usage; preserve the already accepted rectangle and affine mathematics; then run a repository-wide public-surface audit. Until that slice is complete, current documents must identify `Vector` and the dependent names as accepted but not implemented.

### Consequences

- Engine consumers receive one self-contained, consistently named 2D math API instead of a mixture of Electron2D and BCL contracts.
- Later rendering, input, physics, UI, editor, and asset domains start directly on the permanent vector type and do not need their own conversion conventions.
- Integration adapters pay explicit value conversions at their boundary. They remain allocation-free struct conversions and are measured before any unsafe ABI shortcut is considered.
- The migration is intentionally source-breaking. Configuration field names such as `X`, `Y`, `Position`, `Size`, and `Origin` may remain stable, but CLR type identity changes require explicit regression tests.
- The unqualified `Vector` name can conflict with another imported library's type; ordinary C# namespace qualification or aliases resolve that consumer-side ambiguity without duplicating Electron2D's API.

### Rejected alternatives

- **Keep `System.Numerics.Vector2` permanently:** rejected because it leaks an external contract through every spatial subsystem and cannot supply the complete engine-owned vector behavior.
- **Expose both vector types:** rejected because every API would need conversion policy and consumers could create mixed-type graphs.
- **Use `Vector2` as the new engine-owned name:** rejected because Electron2D has no 3D type family and the accepted public vocabulary deliberately removes redundant dimensional suffixes.
- **Add implicit public conversions to numerics types:** rejected because they would preserve the external type as a de facto second public math API and can hide conversions at hot-path call sites.
- **Migrate future domains only:** rejected because existing Core and Scene APIs would permanently divide the engine into incompatible math generations.

### Verification boundary

This ADR records the required architecture only. It does not make `Vector` a production type, does not complete the pending `Rect`/`Transform`/`Node` migration, and does not verify performance or any target platform. Those claims require the full implementation, XML and living documentation, positive/negative/boundary/allocation tests, post-implementation audit, full repository checks, and an atomic implementation commit.

### Related decisions

- [0004: 2D scene-oriented API in one Electron2D-owned assembly](product.md#adr-0004)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)

<a id="adr-0033"></a>
## ADR 0033: Dimensioned engine-owned vector family

Last updated: 2026-09-22

### Status

Accepted and fulfilled. This decision supersedes ADR 0032 only for vector naming and family scope. It retains ADR 0032's engine ownership, one-canonical-API rule, `Rect` and `Transform` names, dependent migration, and prohibition on public external-numerics leakage.

### Context

ADR 0032 chose the unsuffixed `Vector` name because the engine has no 3D spatial family. The shader architecture and general numeric APIs also need four-component floating-point and integer values. Once both two- and four-component values coexist, an unsuffixed `Vector` becomes ambiguous and makes the family inconsistent.

The existing rectangle and affine-transform names do not have this ambiguity: Electron2D has only 2D rectangles and transforms. Their dimensionality is inherent in the 2D-only engine contract, while vectors are also generic numeric tuples whose component count changes their storage and operations.

### Decision

Electron2D owns four canonical vector values: `Vector2`, `Vector2I`, `Vector4`, and `Vector4I`.

- `Vector2` is the engine's single-precision 2D spatial and numeric pair. `Vector2I` is its 32-bit integer counterpart for pixels, grids, tiles, dimensions, and integer pairs.
- `Vector4` and `Vector4I` are four-component numeric tuples. Their existence does not create 3D or 4D scene geometry, transforms, cameras, physics, rendering paths, or assets.
- `Rect`, `Transform`, and `Node` use `Electron2D.Vector2` throughout their public/protected API and engine-owned state.
- The previously accepted `Vector` name and the temporary `VectorI` name do not ship. No aliases, forwarding wrappers, duplicate overloads, or compatibility conversions are provided.
- `Vector3` and `Vector3I` are absent because the engine has no three-dimensional spatial domain and no implemented subsystem currently requires three generic components.
- External numerics types may appear only inside future localized integration adapters. They do not cross a public/protected Electron2D boundary.
- Every vector is a sequential mutable value with explicit float/integer arithmetic, edge behavior, invariant formatting, strict typed configuration persistence, packed-scene storage, and allocation-free warmed numeric operations.
- Universal-value truth conversion is permanently excluded by ADR 0001. Four-component projection operators are excluded because Electron2D has no 3D projection type.

### Consequences

- The vector family states component count explicitly and remains coherent when two- and four-component values coexist.
- The completed migration is source-breaking from both the former external numerics surface and ADR 0032's unimplemented `Vector` spelling. The repository is pre-release and retains only the final contract.
- `Vector4` and `Vector4I` can later cross a typed GPU boundary without requiring placeholder shader or renderer APIs now.
- `Vector2I` and `Vector4I` use explicit managed integer behavior: ordinary arithmetic wraps, invalid division throws, squared values can wrap, and float-to-integer conversion rejects non-finite or out-of-range components.
- The current executable verification is Linux/.NET 8 only. Sequential managed layout is verified, but native ABI and the full five-target matrix are not.

### Rejected alternatives

- **Keep `Vector` beside `Vector4`:** rejected because one name hides component count while the other exposes it.
- **Add only floating-point four-component data:** rejected after the integer counterpart was explicitly required and provides a symmetric typed parameter family.
- **Add `Vector3` for family completeness:** rejected because no current 2D subsystem needs it and a speculative type would blur the 2D-only boundary.
- **Retain `Vector`/`VectorI` aliases:** rejected because aliases create a second public vocabulary and compatibility debt before release.
- **Reuse external numerics vectors:** rejected by ADR 0032's retained engine-ownership decision.

### Verification

The executable harness covers all four layouts, constants, index failures, methods and operators, float/integer conversions, interpolation, IEEE values, NaN ordering, integer wrap/overflow/division failures, invariant formatting, strict configuration schemas, direct packed-scene storage, and warmed allocation-free numeric loops. Existing rectangle, transform, and node tests exercise the completed `Vector2` migration.

The post-implementation checks also audit production/test sources for old vector, rectangle, transform, and external-numerics names. Passing local checks do not establish native ABI, rendering/shader integration, visual behavior, mobile/desktop packaging, or six-target acceptance.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0004: 2D scene-oriented API](product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](resources.md#adr-0014)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate affine-transform foundation](core-math.md#adr-0026)
- [0029: Typed affine semantics](core-math.md#adr-0029)
- [0032: Own the complete unsuffixed 2D math vocabulary](core-math.md#adr-0032)

<a id="adr-0034"></a>
## ADR 0034: Canonical scalar mathematics and pre-release correction

Last updated: 2026-09-21

### Status

Accepted and fulfilled. This decision supersedes only the former `0.00001` component-approximation clauses in ADRs 0024, 0025, and 0029. Their remaining color, rectangle, and affine contracts stay accepted.

### Context

Scalar formulas were duplicated across vectors, colors, rectangles, transforms, and nodes. The duplicates could drift in angle wrapping, interpolation, snapping, positive modulus, and approximate comparison. The current official 4.7.2 stable C# `Mathf.cs` and `MathfEx.cs` expose seven constants and 127 typed overloads, including float/double behavior and managed-only conveniences.

Earlier Electron2D values used a `0.00001f` component tolerance. The audited current scalar contract uses `1e-6f` for float comparison and `1e-14` for double comparison. Electron2D has no first public release or compatibility users, so retaining a known-wrong tolerance would create compatibility debt before compatibility exists.

### Decision

`Electron2D.Mathf` is the canonical public scalar-math type.

- It implements the complete audited typed surface: `Tau`, `Pi`, `Inf`, `NaN`, `E`, `Sqrt2`, `Epsilon`, and 127 integer/float/double/decimal method overloads.
- Single precision remains the engine's primary scalar. The public epsilon is exactly `1e-6f`; double approximate operations use `1e-14` internally.
- API behavior follows typed C# and .NET semantics explicitly: radians by default, midpoint-to-even `Round`, unchecked integer-returning float conversion, managed exceptions for invalid integer operations, and ordinary IEEE NaN/infinity propagation.
- Matching formulas in `Vector2`, `Vector4`, `Rect`, `Transform`, `Color`, their integer snapping paths, internal color math, and `Node` degree conversion route through `Mathf`. Operations without an audited member, such as cube root and IEEE remainder normalization, continue to use the BCL directly.
- The old component tolerance is corrected rather than preserved. Geometry and color approximate predicates now share `Mathf.Epsilon`; strict threshold behavior is verified directly.
- Before Electron2D's first public release, known incorrect behavior is not retained solely for compatibility. An audited correction replaces it, updates tests/XML/living documents in the same change, and is recorded in the appropriate ADR. This does not authorize unrelated source breakage or silent semantic changes.
- No generic numeric facade, injectable math provider, compatibility switch, second epsilon, vector overload layer, SIMD abstraction, or dependency is introduced.

### Consequences

- Engine/game code receives one complete and documented scalar vocabulary.
- Duplicate interpolation, modulus, snapping, angle, and comparison helpers are removed; future fixes land once.
- The approximation correction can change results for differences from `1e-6f` through `1e-5f`. This is an intentional pre-release correctness change, not a behavior-preserving part of the mechanical migration.
- All other migrated formulas preserve their prior executable behavior and exception surface.
- Methods are stateless and thread-safe; normal nonthrowing paths are allocation-free after warmup. Transcendental implementations still come from the target .NET runtime.

### Rejected alternatives

- **Keep `1e-5f` for compatibility:** rejected because the project has no released compatibility baseline and the value conflicts with the audited canonical contract.
- **Expose configurable or per-type epsilon:** rejected because it fragments core semantics; callers needing another tolerance already have explicit-tolerance overloads.
- **Leave duplicated helpers in each value:** rejected because identical formulas would drift and make audits repeat the same work.
- **Wrap every BCL math operation throughout unrelated domains:** rejected because centralization applies where `Mathf` is the engine-facing contract, not as a ban on ordinary implementation primitives.
- **Add generic math or SIMD now:** rejected because no measured requirement justifies a second API or abstraction.

### Verification

The executable harness reflects exactly seven constants and 127 method overloads; exercises every family across float and double paths; verifies positive, negative, NaN, infinity, exception, overflow, degenerate, angle-tie, and strict-epsilon cases; verifies migrated downstream values; and measures zero warmed allocations. Release XML generation and repository-wide identity checks remain part of the full gate.

Verification is Linux/.NET 8 only. It does not establish bit-identical transcendental results, AOT behavior, or native execution across Windows, macOS, Android, or iOS.

### Related decisions

- [0004: 2D scene-oriented API](product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](resources.md#adr-0014)
- [0024: Typed color values and portable quantization](core-math.md#adr-0024)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0029: Typed affine semantics](core-math.md#adr-0029)
- [0033: Dimensioned engine-owned vector family](core-math.md#adr-0033)

<a id="adr-0035"></a>
## ADR 0035: Foreseeable public type-family completeness

Last updated: 2026-09-21

### Status

Accepted and fulfilled. This decision supersedes only ADR 0025's rule that the integer rectangle waits for a current consumer. `RectI`, its typed `Rect` conversions, persistence, packed-scene storage, documentation, and executable coverage are implemented.

### Context

Some paired value types have distinct storage and behavior but form one predictable public vocabulary. Requiring an existing caller before implementing the sibling leaves a known hole that later domains must retrofit. Conversely, implementing every imaginable symmetric type would create speculative API. Electron2D therefore needs a narrower criterion based on an accepted future engine role.

`RectI` has foreseeable 2D uses in pixel rectangles, texture and atlas regions, image buffers, tile/grid bounds, and integer viewport or UI regions. Those domains are not implemented yet, but they are within the accepted product direction. Absence of a current caller is therefore not a valid reason to omit `RectI` when completing the rectangle family.

### Decision

- A production-type implementation audits its corresponding 2D sibling family, not only immediate call sites.
- A sibling is included in the same production-ready vertical slice when its future role is concrete and belongs to an accepted Electron2D domain, even if no current consumer exists.
- The sibling receives its full own API, XML/living documentation, persistence and integration where supported, positive/negative/boundary tests, and post-implementation audit. Empty shells and compatibility aliases remain forbidden.
- Pure symmetry is insufficient: speculative types, 3D families, and concepts outside the accepted architecture remain excluded.
- `RectI` is the required integer sibling of `Rect`. Its complete implementation includes the audited integer-rectangle contract and typed conversions in both rectangle types.
- The delivered type uses `Vector2I`, explicit normalization, unchecked ordinary integer arithmetic, strict typed persistence, and direct reference-free packed-scene storage without adding an absent consumer domain.

### Consequences

- Future `реализуй X` scopes can include a foreseeable sibling even without a current consumer.
- Rectangle-family work includes `RectI`, so later image, atlas, grid, renderer, and UI work receives a stable integer geometry primitive instead of inventing one locally.
- Current inventory and class/component/domain documents list both `Rect` and `RectI` as implemented while leaving their absent consumer domains explicit.

### Rejected alternatives

- **Require an existing consumer:** rejected because it knowingly leaves predictable foundational holes.
- **Implement every matching name:** rejected because symmetry alone does not establish product need.
- **Reserve only the name or add a stub:** rejected because it creates misleading public surface.

### Verification

The executable harness covers the complete audited member surface, signed and overflowing arithmetic, minimum-integer normalization failure, edge and empty intersections, both conversions and invalid inputs, invariant formatting, exact configuration schema and malformed data, packed-scene restoration, and warmed allocation behavior. Verification is currently Linux/.NET 8; native ABI and the full host matrix remain unverified.

### Related decisions

- [0004: 2D scene-oriented API](product.md#adr-0004)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0033: Dimensioned engine-owned vector family](core-math.md#adr-0033)
