# Electron2D core math decisions

Last updated: 2026-09-24

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

The typed C# empty-span behavior of `FromHTML` returning opaque black is retained, even though `HTMLIsValid` reports empty input as invalid. String constructors do not route invalid input through that edge: they try names and throw for unknown values. Null is rejected explicitly with `ArgumentNullException`.

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

Last updated: 2026-09-24

### Status

Accepted for rectangle semantics. [ADR 0032](core-math.md#adr-0032) confirms `Rect2` as the canonical name, and [ADR 0033](core-math.md#adr-0033) replaces the initial external vector dependency with engine-owned `Vector2`. ADR 0034 supersedes the historical `0.00001` approximate-comparison tolerance. ADR 0035 supersedes the consumer-gated deferral and has delivered the complete `Rect2i` sibling.

### Context

Electron2D needs a foundational floating-point rectangle before rendering, UI, culling, images, collision, and navigation are designed. The official 4.7.2 stable `Rect2` contract and typed C# implementation were audited together with the native `rect2.h` behavior and global `Side` values. The relevant surface includes mutable position/size/end, signed area, normalization, containment, intersection, enclosure, growth, support mapping, merge, finite and approximate checks, formatting, four edge identities, an integer-rectangle conversion, transform multiplication, and a language-specific boolean conversion.

The initial rectangle slice used System.Numerics.Vector2; ADR 0033 now requires engine-owned Vector2 throughout geometry and spatial nodes. Adding a second vector type solely for rectangle parity would duplicate arithmetic and introduce conversion noise. Conversely, substituting `System.Drawing.RectangleF` would bring an unsuitable framework identity and different semantics.

### Decision

`Electron2D.Rect2` is a mutable `[Serializable]`, sequential, 16-byte `struct` implementing `IEquatable<Rect2>`. It stores two `Electron2D.Vector2` values and exposes the applicable stable rectangle surface. `Electron2D.Side` is a separate four-value enum with stable numeric identities.

The following contracts are fixed:

- ordinary construction/mutation preserves negative, zero, NaN, and infinite components; normalization is explicit through `Abs()`;
- methods that rely on ordered edges document non-negative size rather than normalizing silently;
- point containment is half-open at the right and bottom edges;
- outer border-only contact is excluded by default, while `Intersects` has an explicit border-inclusion option; a zero-size rectangle strictly inside another retains its position as an empty intersection;
- algebraic growth may over-shrink and create non-positive size;
- an undefined `Side` passed to `GrowSide` is a no-op, matching the audited typed implementation;
- exact equality exposes IEEE NaN behavior; approximate equality checks exact equality first and otherwise uses the repository-wide tolerance defined by ADR 0034;
- numeric formatting is invariant-culture;
- `ConfigFile` accepts only finite rectangles and persists exactly nested `Position.X/Y` and `Size.X/Y` fields, rejecting missing, duplicate, unknown, nonnumeric, or non-finite input;
- typed property descriptors and packed scenes store/copy `Rect2` directly because it contains no managed references;
- `Rect2i` conversion and `Transform` multiplication are implemented under ADRs 0035 and 0029;
- language-specific boolean truth conversion is permanently excluded.

No renderer, UI, physics, or platform abstraction is created by this decision.

### Consequences

- Core gains allocation-free rectangle geometry usable by future domains without acquiring dependencies on them.
- Existing `Vector2` transforms and positions interoperate directly with rectangle positions, sizes, centers, support points, and query points.
- Negative sizes remain representable and observable; callers must decide when normalization is appropriate.
- Configuration persistence rejects non-finite values even though ordinary runtime geometry retains them.
- `Rect2i` conversions and `Transform` multiplication use the same engine-owned value family without external numeric types.
- Sequential layout is useful for predictable managed storage but does not promise native backend ABI equivalence.

### Rejected alternatives

- **Wait for a renderer or physics backend:** rejected because rectangle math and typed persistence are independently useful foundational behavior.
- **Create a custom `Vector2`:** rejected at the time because `System.Numerics.Vector2` supplied the immediate portable value contract and was established throughout the repository. ADR 0032 supersedes this rejection and requires an engine-owned `Vector` throughout the runtime.
- **Use `System.Drawing.RectangleF`:** rejected because its API/semantics differ and the dependency is inappropriate for the runtime target matrix.
- **Normalize on every construction:** rejected because it destroys intentional negative-size values and diverges from the audited contract.
- **Add placeholder integer-rectangle or `Transform2D` types:** rejected because empty compatibility shells would violate the repository definition of done. ADR 0026 required a real standalone `Transform2D` vertical slice, now fulfilled by ADR 0029.
- **Serialize every public property automatically:** rejected because computed `End` and `Area` would create a redundant, unstable, ambiguous schema.

### Verification

`tests/Electron2D.Tests/Program.cs` covers layout/defaults, every implemented constructor/member/operator, negative/zero size boundaries, all side values and undefined input, half-open containment, overlap and border behavior, IEEE values, invariant formatting, strict configuration shape and failure rollback, packed-scene copying, and zero allocations in a warmed geometry loop.

Verification is currently Linux/.NET 8. ADR 0029 has since delivered transform multiplication, and ADR 0035 has delivered the integer rectangle and typed conversions. Native structure equivalence, renderer/physics use, and the full six-target matrix remain unavailable.

### Related decision

- [0026: Separate Transform2D type](core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)
- [0035: Foreseeable public type-family completeness](core-math.md#adr-0035)

<a id="adr-0026"></a>
## ADR 0026: Separate foundational Transform value

Last updated: 2026-09-24

- Status: Accepted and implemented; affine semantics are defined in ADR 0029 and final names/integration in ADRs 0032 and 0033.
- Scope: Engine-owned 2D affine values and their scene/geometry integration.

### Decision

Electron2D provides the engine-owned `Transform` value with Vector2 basis/origin storage, composition, forward/inverse transformation, finite checks and approximate comparison. Entity.Transform and Entity.GlobalTransform use this value; Rect2 transformation and typed packed/property/config storage are implemented under ADRs 0029 and 0033. No Matrix3x2 wrapper or public external numerics dependency substitutes for this contract.

This value-type decision does not combine scene responsibilities. [ADR 0008](scene.md#adr-0008) separately requires Node for the neutral tree, CanvasItem for drawing/shared transform queries and Entity for the concrete spatial model. Timer and Viewport remain neutral. No 3D or speculative dimension-neutral transform family is authorized.

### Consequences and verification

The spatial surface uses one engine-owned affine vocabulary and documented canonical decomposition. Source XML, class/component/domain pages, inventory and coverage accompany any changes. Managed geometry, scene and persistence checks cover this integration; native mixed-tree canvas checks exercise Entity/CanvasItem composition on Wayland. Those checks do not imply complete rendering, UI or other-platform acceptance.

### Rejected alternatives

- Expose Matrix3x2 as the permanent scene API: it lacks the required engine-owned contract.
- Add an empty wrapper or speculative 3D family: it violates the complete-slice and strictly 2D boundaries.
- Treat removal of a dimensional suffix as permission to merge scene base classes: contradicted by ADR 0008.

### Related decisions

- [0029: Affine value semantics](#adr-0029)
- [0033: Engine-owned vector integration](#adr-0033)
- [0008: Scene inheritance](scene.md#adr-0008)

<a id="adr-0029"></a>
## ADR 0029: Typed Transform value and affine semantics

Last updated: 2026-09-24

### Status

Accepted for affine semantics. This decision fulfills ADR 0026's standalone value requirement. ADRs 0032 and 0033 supersede the old type name and external vector dependency; the complete `Vector2`/`Rect2`/`Transform`/`Entity` migration is now implemented. ADR 0034 supersedes the historical `0.00001` approximate-comparison tolerance.

### Context

ADR 0026 established that `System.Numerics.Matrix3x2` is not the permanent engine-facing transform vocabulary. The official 4.7.2 stable [`Transform2D` documentation](https://docs.godotengine.org/en/stable/classes/class_transform2d.html), [typed C# implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Transform2D.cs), and [native implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/core/math/transform_2d.cpp) were audited together because they differ in default construction, available methods, local-scale behavior, scalar operations, and failure handling.

Electron2D already uses `System.Numerics.Vector2`, strict typed configuration snapshots, and reference-free packed-scene values. A foundational affine transform can therefore be complete without adding a renderer, native dependency, custom vector family, or dynamic value container.

### Decision

`Electron2D.Transform` is a mutable `[Serializable]`, sequential, 24-byte `struct` implementing `IEquatable<Transform>`. Its public fields `X`, `Y`, and `Origin` are column vectors backed by `Electron2D.Vector2`.

The following contracts are fixed:

- `default(Transform)` and `new Transform()` are the all-zero C# value; `Identity` is explicit;
- columns are ordered X, Y, Origin and use column-vector affine mathematics, while positive screen-space rotation is clockwise;
- `left * right` applies the right/child transform first and the left/parent transform second;
- `AffineInverse` supports any exactly nonsingular basis and throws `InvalidOperationException` for an exact zero determinant;
- `Inverse`, `BasisXformInv`, and reverse vector/array multiplication are documented orthonormal-only shortcuts and perform no hidden validation;
- global operations use left-multiplication semantics; local operations use right-multiplication semantics;
- `ScaledLocal` scales basis columns by their respective scalar components and preserves Origin, matching the documented/native contract rather than the audited typed binding discrepancy;
- decomposition carries reflection in the Y scale, interpolation decomposes and recomposes with shortest-path rotation/skew angles, and weights outside zero through one extrapolate;
- `IsConformal` and `LookingAt` are implemented because they belong to the stable general contract even though the audited typed binding omits them;
- `LookingAt` accepts the zero vector as its default target, as in the pinned class reference;
- zero or dependent axes orthonormalize to zero instead of becoming non-finite;
- exact equality exposes IEEE NaN behavior; approximate equality uses the repository-wide tolerance defined by ADR 0034 and an exact-equality fast path;
- finite `ConfigFile` values use exactly three nested vectors named `X`, `Y`, and `Origin`, each containing exactly finite numeric `X` and `Y` fields;
- typed stored-property capture and packed scenes copy the reference-free value directly;
- array operations allocate a new result and never mutate their source; warmed scalar math remains allocation-free;
- redundant integer scalar overloads use normal C# numeric conversion, copy construction uses value assignment, and boolean truth conversion is excluded.

No implicit or explicit `Matrix3x2` conversion is added. The two types use compatible scalar storage but opposite multiplication conventions because `Matrix3x2` follows row-vector composition. Entity uses the engine-owned Transform directly under ADR 0033; no external-matrix compatibility shim is part of that integration.

The final Transform/Rect2 operators and Entity.Transform/GlobalTransform integration are implemented under ADRs 0026 and 0033. Scene naming and inheritance follow ADR 0008.

### Consequences

- Core now owns a backend-independent affine vocabulary suitable for later scene, rendering, UI, camera, culling, and physics integration.
- Callers can perform complete transform math without allocating or depending on SDL.
- Zero initialization remains honest C# storage rather than silently becoming identity; consumers must request `Identity` when that semantic is required.
- Configuration persistence rejects non-finite transforms even though ordinary runtime math retains IEEE values.
- Entity local/global transforms use Transform; Rect2 transformation is implemented under ADR 0033. The standalone value and its scene integration remain distinct responsibilities.

### Rejected alternatives

- **Wrap `Matrix3x2` internally and forward multiplication:** rejected because its row-vector composition order would make the public parent/child contract easy to reverse accidentally.
- **Make zero initialization identity:** rejected because C# default arrays, generic storage, and uninitialized fields must retain normal zero-value semantics.
- **Copy the typed binding's local-scale implementation:** rejected because it conflicts with the documented global/local distinction and native column semantics for rotated bases.
- **Validate every orthonormal shortcut:** rejected because validation would add hot-path work and change the explicit precondition into a different failure contract; the general affine path is already available.
- **Add a transform interface or abstract backend:** rejected because a pure value has no polymorphic behavior or backend ownership.

### Verification

The executable harness covers every implemented member family, matrix/composition order, reflection/skew decomposition, singular and malformed failures, config and packed-scene integration, IEEE boundaries, and warmed allocation behavior. Release compilation also emits XML documentation with warnings treated as errors.

Value-math verification is Linux/.NET 8. ADR 0033 and the current geometry/scene documents cover Rect2 transformation and Entity integration. Value checks do not establish native rendering, cross-platform builds or visual user acceptance.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0017: Source-tree module layout](product.md#adr-0017)
- [0018: Typed configuration files](core-data-io.md#adr-0018)
- [0023: Typed in-memory packed scenes](scene.md#adr-0023)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](core-math.md#adr-0026)

<a id="adr-0032"></a>
## ADR 0032: Own the complete 2D math vocabulary

Last updated: 2026-09-24

### Status

Accepted; [ADR 0033](core-math.md#adr-0033) extends the vector family beyond two components. Engine ownership, the `Vector2`/`Rect2`/`Transform` names, removal of external public numerics, prohibition of dual APIs, and the completed dependent migration remain accepted.

### Context

Electron2D is exclusively two-dimensional. The earlier public geometry surface mixed engine-owned `Rect2` and `Transform2D` values with `System.Numerics.Vector2`, while `Entity` additionally exposed `Matrix3x2`. That made an external numerics library part of the engine contract and prevented Electron2D from defining one complete vector API and its exact numeric semantics. `Rect2` and `Rect2i` keep a matching dimensioned rectangle vocabulary; the affine `Transform` has no 3D scene counterpart.

The engine needs one coherent math vocabulary shared by scene state, rendering, input, physics, UI, resources, serialization, editor tooling, and games. Renaming only a rectangle or transform while retaining the external vector would preserve the largest public dependency and force another source-breaking migration later.

### Decision

Electron2D owns three canonical single-precision 2D value types named `Vector2`, `Rect2`, and `Transform`. ADR 0033 extends the vector family with numeric three- and four-component values.

- `Vector2` is a complete production type rather than a naming wrapper. Its implementation audits the applicable reference vector contract and defines sequential two-float storage, arithmetic and geometric operations, comparison, normalization, finite checks, formatting, persistence, packed-scene storage, XML documentation, and allocation-free hot paths.
- Every Electron2D domain uses `Vector2` for engine-owned 2D coordinates, sizes, directions, velocities, axes, offsets, input values, render geometry, physics values, and public/protected parameters, properties, fields, and return values.
- `Rect2` and `Transform` use `Vector2` for all stored components and API. `Entity` transform and spatial members use `Transform` and `Vector2`.
- `System.Numerics.Vector2` and `Matrix3x2` may exist only inside narrow internal adapters at an external package, native API, or host boundary. They must not become engine-owned state or cross a public/protected Electron2D API boundary.
- No compatibility aliases, duplicate wrappers, or implicit dual APIs ship. The project is pre-release, so the migration favors one canonical contract over compatibility debt.
- Three-dimensional scene rectangles, transforms, overloads, adapters, and reserved abstractions are outside scope. ADR 0033 allows ordinary numeric three-component vectors without adding 3D scene geometry.

The completed migration moved rectangle, affine-transform, node, configuration, packed-scene, property, test, example, and documentation usage to the engine-owned values while preserving the accepted rectangle and affine mathematics.

### Consequences

- Engine consumers receive one self-contained, consistently named 2D math API instead of a mixture of Electron2D and BCL contracts.
- Later rendering, input, physics, UI, editor, and asset domains start directly on the permanent vector type and do not need their own conversion conventions.
- Integration adapters pay explicit value conversions at their boundary. They remain allocation-free struct conversions and are measured before any unsafe ABI shortcut is considered.
- The migration is intentionally source-breaking. Configuration field names such as `X`, `Y`, `Position`, `Size`, and `Origin` may remain stable, but CLR type identity changes require explicit regression tests.
- Consumers can qualify an imported library's same-named numeric type without duplicating Electron2D's API.

### Rejected alternatives

- **Keep `System.Numerics.Vector2` permanently:** rejected because it leaks an external contract through every spatial subsystem and cannot supply the complete engine-owned vector behavior.
- **Expose both vector types:** rejected because every API would need conversion policy and consumers could create mixed-type graphs.
- **Add implicit public conversions to numerics types:** rejected because they would preserve the external type as a de facto second public math API and can hide conversions at hot-path call sites.
- **Migrate future domains only:** rejected because existing Core and Scene APIs would permanently divide the engine into incompatible math generations.

### Verification boundary

The implemented values and their local managed checks are recorded in the class pages and coverage register. Sequential managed layout does not establish native ABI or other-platform behavior under ADR 0021.

### Related decisions

- [0004: 2D scene-oriented API in one Electron2D-owned assembly](product.md#adr-0004)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate Transform2D foundational type](core-math.md#adr-0026)
- [0029: Typed Transform2D value and affine semantics](core-math.md#adr-0029)

<a id="adr-0033"></a>
## ADR 0033: Dimensioned engine-owned vector family

Last updated: 2026-09-24

### Status

Accepted and fulfilled. This decision extends ADR 0032's engine-owned 2D vector contract to the dimensioned numeric vector family. It retains the one-canonical-API rule, `Rect2` and `Transform` names, dependent migration, and prohibition on public external-numerics leakage.

### Context

ADR 0032 established engine-owned `Vector2` for 2D spatial values. The shader architecture and general numeric APIs also need three- and four-component floating-point and integer values. Every vector name states its component count to keep the numeric family consistent.

The rectangle family uses explicit `Rect2`/`Rect2i` dimensional names for its floating and integer values. `Transform` remains unsuffixed because it has no 3D scene counterpart. Vectors are generic numeric tuples whose component count changes their storage and operations.

### Decision

Electron2D owns six canonical vector values: `Vector2`, `Vector2i`, `Vector3`, `Vector3i`, `Vector4`, and `Vector4i`.

Integer-vector type names use the lowercase `i` suffix. This naming rule applies to types; ADR 0045's uppercase-acronym rule applies to functions, methods, and properties. No alternate integer-vector type names or aliases ship.

- `Vector2` is the engine's single-precision 2D spatial and numeric pair. `Vector2i` is its 32-bit integer counterpart for pixels, grids, tiles, dimensions, and integer pairs.
- `Vector3` and `Vector3i` are three-component floating-point and integer numeric values. `Vector3` carries arbitrary `vec3`/`float3` data; `Color` carries values with RGB semantics. A numeric three-component value does not add three-dimensional nodes, scenes, transforms, cameras, physics, rendering paths, or assets.
- `Vector4` and `Vector4i` are four-component numeric tuples. Their existence does not create 3D or 4D scene geometry, transforms, cameras, physics, rendering paths, or assets.
- `Rect2`, `Transform`, and `Entity` use `Electron2D.Vector2` throughout their public/protected API and engine-owned state.
- The previously accepted `Vector` name and the temporary `VectorI` name do not ship. No aliases, forwarding wrappers, duplicate overloads, or compatibility conversions are provided.
- Typed shader parameters use `Vector3` as the canonical `vec3`/`float3` descriptor and accept `Color` when the components represent RGB. Signed and unsigned three-component integer uniforms use `Vector3i` with preserved component bits.
- External numerics types may appear only inside future localized integration adapters. They do not cross a public/protected Electron2D boundary.
- Every vector is a sequential mutable value with explicit float/integer arithmetic, edge behavior, invariant formatting, strict typed configuration persistence, packed-scene storage, and allocation-free warmed numeric operations.
- Universal-value truth conversion is permanently excluded by ADR 0001. Four-component projection operators are excluded because Electron2D has no 3D projection type.

### Consequences

- The vector family states component count explicitly across two-, three-, and four-component values.
- The completed migration is source-breaking from both the former external numerics surface and ADR 0032's unimplemented `Vector` spelling. The repository is pre-release and retains only the final contract.
- `Vector2i`, `Vector3i`, and `Vector4i` use explicit managed integer behavior: ordinary component arithmetic wraps, invalid division throws, and float-to-integer conversion rejects non-finite or out-of-range components. Their squared length and distance return signed 64-bit values after widening before multiplication; they throw `OverflowException` if the exact result exceeds `long.MaxValue`. Distance widens coordinate differences before subtraction. Length and distance use widened floating-point arithmetic independently of the squared-return limit and remain finite for every 32-bit input. This corrects the pre-release 32-bit squared wrap, which made moderate lengths NaN.
- Their floating-scalar division returns a floating vector with IEEE 754 results, including infinity or NaN for a zero divisor. The typed API has no dynamic `Variant` error state under ADR 0001.
- The current executable verification is Linux/.NET 8 only. Sequential managed layout is verified, but native ABI and the full five-target matrix are not.

### Rejected alternatives

- **Keep `Vector` beside `Vector4`:** rejected because one name hides component count while the other exposes it.
- **Add only floating-point four-component data:** rejected after the integer counterpart was explicitly required and provides a symmetric typed parameter family.
- **Use `Color` for every three-component numeric value:** rejected because arbitrary numeric triples do not imply RGB semantics; `Color` remains available for explicitly color-valued data.
- **Retain `Vector`/`VectorI` aliases:** rejected because aliases create a second public vocabulary and compatibility debt before release.
- **Reuse external numerics vectors:** rejected by ADR 0032's retained engine-ownership decision.

### Verification

The executable harness covers all six layouts, constants, index failures, methods and operators, float/integer conversions, interpolation, IEEE values, NaN ordering, integer wrap/overflow/division failures, invariant formatting, strict configuration schemas, direct packed-scene storage, and warmed allocation-free numeric loops. Existing rectangle, transform, and node tests exercise the completed `Vector2` migration.

The post-implementation checks also audit production/test sources for old vector, rectangle, transform, and external-numerics names. Passing local checks do not establish native ABI, rendering/shader integration, visual behavior, mobile/desktop packaging, or six-target acceptance.

### Related decisions

- [0001: Typed C# without Variant](product.md#adr-0001)
- [0004: 2D scene-oriented API](product.md#adr-0004)
- [0014: Managed lifetime and realtime allocation](resources.md#adr-0014)
- [0025: Typed axis-aligned rectangle geometry](core-math.md#adr-0025)
- [0026: Separate affine-transform foundation](core-math.md#adr-0026)
- [0029: Typed affine semantics](core-math.md#adr-0029)
- [0032: Own the complete 2D math vocabulary](core-math.md#adr-0032)

<a id="adr-0034"></a>
## ADR 0034: Canonical scalar mathematics and pre-release correction

Last updated: 2026-09-24

### Status

Accepted and fulfilled. This decision supersedes only the former `0.00001` component-approximation clauses in ADRs 0024, 0025, and 0029. Their remaining color, rectangle, and affine contracts stay accepted.

### Context

Scalar formulas were duplicated across vectors, colors, rectangles, transforms, and nodes. The duplicates could drift in angle wrapping, interpolation, snapping, positive modulus, and approximate comparison. The current official 4.7.2 stable C# `Mathf.cs` and `MathfEx.cs` expose seven constants and 127 typed overloads, including float/double behavior and managed-only conveniences.

Earlier Electron2D values used a `0.00001f` component tolerance. The audited current scalar contract uses `1e-6f` for float comparison and `1e-14` for double comparison. Electron2D has no first public release or compatibility users, so retaining a known-wrong tolerance would create compatibility debt before compatibility exists.

### Decision

`Electron2D.Mathf` is the canonical public scalar-math type. The `Mathf` spelling follows the accepted reference API and keeps it distinct from the .NET `System.MathF` type.

- It implements the complete audited typed surface: `Tau`, `Pi`, `Inf`, `NaN`, `E`, `Sqrt2`, `Epsilon`, and 127 integer/float/double/decimal method overloads.
- Single precision remains the engine's primary scalar. The public epsilon is exactly `1e-6f`; double approximate operations use `1e-14` internally.
- API behavior follows typed C# and .NET semantics explicitly: radians by default, midpoint-to-even `Round`, unchecked integer-returning float conversion, managed exceptions for invalid integer operations, and ordinary IEEE NaN/infinity propagation.
- Matching formulas in `Vector2`, `Vector4`, `Rect2`, `Transform`, `Color`, their integer snapping paths, internal color math, and `Entity` degree conversion route through `Mathf`. Engine code uses this public type for equivalent scalar operations. Its implementation calls `System.MathF` and `System.Math`; operations without an audited member, such as cube root and truncation, continue to use the BCL directly.
- The old component tolerance is corrected rather than preserved. Geometry and color approximate predicates now share `Mathf.Epsilon`; strict threshold behavior is verified directly.
- Before Electron2D's first public release, known incorrect behavior is not retained solely for compatibility. An audited correction replaces it, updates tests/XML/living documents in the same change, and is recorded in the appropriate ADR. This does not authorize unrelated source breakage or silent semantic changes.
- No generic numeric facade, injectable math provider, compatibility switch, second epsilon, vector overload layer, SIMD abstraction, or dependency is introduced.

### Consequences

- Engine/game code receives one complete and documented scalar vocabulary without a name collision with `System.MathF`.
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

Last updated: 2026-09-24

### Status

Accepted and fulfilled. This decision supersedes only ADR 0025's rule that the integer rectangle waits for a current consumer. `Rect2i`, its typed `Rect2` conversions, persistence, packed-scene storage, documentation, and executable coverage are implemented.

### Context

Some paired value types have distinct storage and behavior but form one predictable public vocabulary. Requiring an existing caller before implementing the sibling leaves a known hole that later domains must retrofit. Conversely, implementing every imaginable symmetric type would create speculative API. Electron2D therefore needs a narrower criterion based on an accepted future engine role.

`Rect2i` has foreseeable 2D uses in pixel rectangles, texture and atlas regions, image buffers, tile/grid bounds, and integer viewport or UI regions. Those domains are not implemented yet, but they are within the accepted product direction. Absence of a current caller is therefore not a valid reason to omit `Rect2i` when completing the rectangle family.

### Decision

- A production-type implementation audits its corresponding 2D sibling family, not only immediate call sites.
- A sibling is included in the same production-ready vertical slice when its future role is concrete and belongs to an accepted Electron2D domain, even if no current consumer exists.
- The sibling receives its full own API, XML/living documentation, persistence and integration where supported, positive/negative/boundary tests, and post-implementation audit. Empty shells and compatibility aliases remain forbidden.
- Pure symmetry is insufficient: speculative types, 3D families, and concepts outside the accepted architecture remain excluded.
- `Rect2i` is the required integer sibling of `Rect2`. Its complete implementation includes the audited integer-rectangle contract and typed conversions in both rectangle types.
- The integer sibling retains the explicit two-dimensional `Rect2i` name. The floating-point rectangle remains `Rect2` under ADR 0032. No alternate integer-rectangle type name or compatibility alias ships.
- The delivered type uses `Vector2i`, explicit normalization, unchecked ordinary integer arithmetic, strict typed persistence, and direct reference-free packed-scene storage without adding an absent consumer domain.

### Consequences

- Future `реализуй X` scopes can include a foreseeable sibling even without a current consumer.
- Rectangle-family work includes `Rect2i`, so later image, atlas, grid, renderer, and UI work receives a stable integer geometry primitive instead of inventing one locally.
- Current inventory and class/component/domain documents list both `Rect2` and `Rect2i` as implemented while leaving their absent consumer domains explicit.

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
