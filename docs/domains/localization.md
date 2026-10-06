# Localization domain

Last updated: 2026-10-06

## Responsibility

Localization owns process-wide direct and resource-backed translation catalogs, UI culture selection, domain/context lookup, scored resource-locale and configured fallback selection, and caller-supplied plural selection for the Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web runtime targets.

Its production source lives under `src/Core/String/`, matching its low-level engine module while the living architecture retains Localization as a separate logical domain. The public namespace remains `Electron2D`.

## Current state

| Component | Responsibility | State |
| --- | --- | --- |
| [Translation](../components/localization.md) | Thread-safe direct and resource-backed translation registration and resolution | Implemented for in-memory lookup; locale-rule and asset gaps remain |

Production types are [`TranslationServer`](../classes/TranslationServer.md), [`TranslationDomain`](../classes/TranslationDomain.md), [`Translation`](../classes/Translation.md), and [`OptimizedTranslation`](../classes/OptimizedTranslation.md). `ElectronObject` delegates `Tr` and `TrN` to this domain when per-object translation is enabled. Scene-owned [`Node`](../classes/Node.md) adds inherited domains and automatic translation policy. [`Control`](../classes/Control.md) consults the selected culture and registered catalog for locale-derived rectangle direction; exact native locale aliases and global scene refresh remain gaps.

## Public surface

`TranslationServer` exposes the culture, locale comparison, tool-locale selection, direct singular/plural registration, named domain registry, main-domain resource queries, lookup, pseudolocalization, typed project-option reload and catalog clearing. `TranslationDomain` manages borrowed catalogs, locale override, enablement and singular pseudolocalization options. `Translation` stores contextual messages and plural forms. `OptimizedTranslation` generates and resolves compressed singular values without retaining source keys. Both support Resource duplication. `ElectronObject` supplies per-instance translation enablement and lookup entry points from Core. `Node` exposes `AutoTranslateMode`, `CanAutoTranslate`, `Atr`, `AtrN` and `SetTranslationDomainInherited`. The service's global enabled flag is internal.

## Dependency direction

- Localization depends on the Core Resource and typed ProjectSettings contracts and the .NET Base Class Library.
- Core references Localization through the static `TranslationServer` API.
- Localization does not depend on Scene, SDL3-CS, rendering, or assets.
- Future asset loading may populate catalogs but must not move resolution into the asset domain.

## Invariants

- Catalog access and culture changes are lock-protected.
- Selected culture, its parents and invariant culture precede nearby resource locales; the configured fallback locale is tried afterward.
- Domains, contexts, and messages are ordinal and case-sensitive.
- Missing singular translations return the source message.
- Missing plural translations use singular only when count equals one; registered selectors own language-specific plural rules. Resource catalogs without a selector use English rules only for English locales and fail explicitly for other locales with multiple forms.
- Catalog lookup semantics are platform-independent; platform locale discovery, when added, remains an external host input.

## Not implemented

- No `.po`, `.mo`, `.resx`, JSON, or binary catalog loader.
- Optimized translation payloads use an internal Brotli representation; reference Smaz file compatibility and editor-only generation gating remain absent.
- No operating-system locale discovery, number formatting, interpolation, or full Unicode bidirectional-text implementation. Pseudolocalization includes only test direction-control marks; locale alias/default-script tables and exact Unicode parity remain partial.
- No built-in CLDR plural-rule database or textual plural-rule evaluator; non-English resource catalogs with multiple forms require a typed selector.
- No native locale-discovery or localization verification on all targets exists yet.

## Verification

`tests/Electron2D.Tests/Program.cs`, `TranslationDomainTests.cs`, `LocalizationProjectSettingsTests.cs`, and `NodeLocalizationTests.cs` verify scored resource-locale and configured fallback selection, source fallback, direct and resource plural selection, contextual and optimized catalogs, duplication, disposal, domain lifecycle and locale override, singular pseudolocalization, per-object translation disabling, inherited node domains and policies, root setting persistence/sampling, managed Engine startup sampling, and live transform reload. `WindowRuntimeTests` verifies Run sampling before scene ready with the SDL dummy driver; other native platforms remain unverified for this setting.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0007: Typed localization](../decisions/localization.md#adr-0007)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)

[Numeric input](../components/numeric-input.md) adds SpinBox formula/text/arrow/repeat/relative-drag authoring through shared Range and LineEdit, fresh scene factories and generated numeral localization. Current Wayland GPU/compatibility capture/input/pixels and prepared active rendering are exercised; precise pointer warp, inherited semantic/editor and foreign target gates remain separate.
