# ADR 0024: Typed color values and portable quantization

Last updated: 2026-09-21

## Status

Accepted.

## Context

Electron2D needs the foundational color value before rendering, images, themes, and concrete visual resources can be designed. The high-level reference API supplies a mature RGBA surface with HSV, OKHSL, HTML/named parsing, packed integers, arithmetic, and language-specific operators. The repository additionally requires typed C#, one Electron2D-owned assembly, portable behavior, no hidden native dependency, complete XML/living documentation, and allocation-free numeric hot paths.

The official 4.7.2 stable reference was audited across `Color.xml`, native `color.h`/`color.cpp`, the typed C# `Color.cs`/`Colors.cs`, the color-name table, and the published `ok_color.h`. The native and typed C# surfaces differ at several edges: default alpha, byte-scaled input range, clamp failure behavior, negative HSV hue, midpoint conversion, and direct empty HTML parsing. Electron2D must choose one explicit executable contract instead of accidentally mixing them.

## Decision

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

## Consequences

- Core gains a complete backend-independent color vocabulary before renderer work without creating a renderer abstraction.
- Numeric operations are value-only and allocation-free after JIT warmup; named/string operations may allocate.
- `Color` is naturally usable in existing typed configuration and packed-scene mechanisms, and their wire/capture contracts are verified rather than assumed.
- Public OKHSL getters clamp coordinates to `0..1` as required; at a narrow saturated gamut boundary the reference algorithm can compute saturation slightly above one, so reconstruction from the clamped public triple is knowingly lossy and explicitly tested.
- Public mutable fields are an intentional API/layout choice, not a general encapsulation precedent.
- Packed HDR/non-finite output is deterministic across supported runtimes but deliberately safer than unchecked casts in the reference C# binding.
- Future SDL/render backends must convert explicitly and cannot assume ABI equivalence merely because `Color` is sequential.
- `System.Drawing.Color` or UI-framework `Colors` imports may require normal C# aliases; Electron2D will not rename the public types to avoid namespace ambiguity.

## Rejected alternatives

- **Wait for a renderer:** rejected because color math, persistence, and scene storage are independent foundational behavior.
- **Use `System.Drawing.Color`:** rejected because it is byte-oriented, does not provide the required API, and introduces an inappropriate platform/library identity.
- **Use HSV as an OKHSL approximation:** rejected because its perceptual and gamut behavior is materially different.
- **Call a future native backend for OKHSL:** rejected because it would make core value math unavailable or platform-dependent before host initialization.
- **Generate named properties at runtime/reflection:** rejected because the public compile-time API and XML documentation require real properties.
- **Add generic parsing/formatting interfaces now:** rejected because the required stable API already supplies span HTML parsing and invariant formatting; extra interfaces add unsupported contract surface.

## Verification

`tests/Electron2D.Tests/Program.cs` covers 16-byte layout/default semantics; constructors and mutable components; HSV/OKHSL anchors and round trips; color-space transfer; source-over blend; unbounded operations; failures; integer/HTML formats including non-finite quantization; all 146 public named properties and concurrent lookup; arithmetic, exact/approximate/NaN comparisons; invariant formatting; strict `ConfigFile` schema/rollback; `PackedScene` stored-property restoration; and zero allocations in a warmed numeric loop.

Verification is currently Linux/.NET 8. Renderer/native pixel equivalence and the full target platform matrix remain unavailable until their respective domains and hosts exist.
