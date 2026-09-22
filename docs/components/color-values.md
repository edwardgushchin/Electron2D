# Color values component

Last updated: 2026-09-22

## Scope

This Core component provides allocation-free floating-point RGBA math, sRGB/linear transfer, HSV and perceptual OKHSL conversion, straight-alpha composition, stable integer/text encoding, and the standard named-color catalog. It is a backend-independent value layer used by configuration and scene storage and intended for future rendering APIs.

## Owned types

| Type | Role |
| --- | --- |
| [`Color`](../classes/Color.md) | Mutable sequential RGBA value, conversions, arithmetic, packing, parsing, and comparisons |
| [`Colors`](../classes/Colors.md) | Complete 146-entry named color catalog and internal immutable name lookup |

Production sources are [`src/Core/Math/Color.cs`](../../src/Core/Math/Color.cs), [`src/Core/Math/Colors.cs`](../../src/Core/Math/Colors.cs), and the internal licensed OKHSL implementation [`src/Core/Math/OkColor.cs`](../../src/Core/Math/OkColor.cs).

## Runtime flow

The API preserves uppercase acronyms under [ADR 0045](../decisions/product.md#adr-0045), including HTML, SRGB, HSV, OKHSL, RGBE and packed channel order. OKHSL properties retain the component suffix (`OKHSLH`, `OKHSLS`, `OKHSLL`).

1. Callers construct/copy RGBA values or obtain a named value from `Colors`.
2. Numeric operations return new values except explicit field/property/indexer mutation.
3. HSV/OKHSL property setters reconstruct RGB while retaining alpha; OKHSL construction clamps the final RGBA value.
4. Text parsing chooses valid hexadecimal syntax before normalized name lookup.
5. Packed/HTML output applies deterministic clamped midpoint-to-even quantization.
6. Existing typed persistence captures `Color` by value; `ConfigFile` uses a strict finite four-field JSON schema.

## Dependencies

- Canonical scalar [`Mathf`](../classes/Mathf.md), plus .NET span, immutable/frozen collection, globalization, and interop-layout primitives.
- Internal managed OKHSL formulas with retained MIT license.
- No external package, native library, Scene, renderer, SDL, asset, input, audio, physics, scripting, or editor dependency.

## Invariants

- `Color` remains a sequential four-`float`, 16-byte value; zero initialization is transparent black.
- Ordinary math preserves HDR and IEEE 754 behavior; output quantization is separately clamped and deterministic.
- RGB is treated as nonlinear sRGB unless a method explicitly states linear space; alpha remains linear.
- Exact comparisons do not hide NaN; approximate equality uses exact equality first and then a strict scale-aware internal tolerance (`1e-6f`).
- Named lookup is immutable after type initialization, thread-safe, and normalized without culture-sensitive casing.
- Numeric hot paths do not allocate; strings and normalized name parsing may allocate.
- Public OKHSL coordinates are clamped to `0..1`; a narrow saturated gamut edge whose raw reference saturation exceeds one cannot round-trip exactly through only those clamped coordinates.

## Current implementation status

Implemented and verified. The delivered public surface contains the complete typed C# color contract: 14 component/property members, all constructors and conversions, arithmetic/equality/relational operators, 146 standard named properties, strict parsing/error behavior, OKHSL, and integration with the already implemented typed configuration and packed-scene storage layers.

## Exclusions and deferred integration

- No boolean truth conversion is exposed; it belongs to a different language model and has no idiomatic C# contract.
- No native-only color-name index/count functions or RGBE9995 encoder are added to the public API.
- No renderer texture/pixel formats, gradients, images, theme colors, color picker/editor UI, native structure conversion, or display-color-management pipeline exists yet. Those integrations require their owning domains and are not represented by placeholders.
- The type intentionally has no `System.Drawing` dependency or conversion.

## Verification

The executable harness covers construction, mutation, conversions, primary/interior/saturated-boundary OKHSL fixtures, arithmetic, the strict internal tolerance comparison boundaries, exhaustive byte and packing/HTML boundary cases, the complete named-property count and lookup mapping, concurrency, strict JSON persistence, packed-scene value copying, and warmed allocation behavior. Verification is Linux/.NET 8 only; native rendering and six-target runtime output are not yet testable.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0014: Managed lifetime and realtime allocation](../decisions/resources.md#adr-0014)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0024: Typed color values and portable quantization](../decisions/core-math.md#adr-0024)
- [0034: Canonical scalar mathematics and pre-release correction](../decisions/core-math.md#adr-0034)
