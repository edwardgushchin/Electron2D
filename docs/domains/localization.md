# Localization domain

Last updated: 2026-09-23

## Responsibility

Localization owns process-wide direct and resource-backed translation catalogs, UI culture selection, domain/context lookup, parent-culture fallback, and caller-supplied plural selection for the Windows, macOS, Linux (X11/Wayland), Android, iOS, and Web runtime targets.

Its production source lives under `src/Core/String/`, matching its low-level engine module while the living architecture retains Localization as a separate logical domain. The public namespace remains `Electron2D`.

## Current state

| Component | Responsibility | State |
| --- | --- | --- |
| [Translation](../components/localization.md) | Thread-safe direct and resource-backed translation registration and resolution | Implemented for in-memory lookup; locale-rule and asset gaps remain |

Production types are [`TranslationServer`](../classes/TranslationServer.md) and [`Translation`](../classes/Translation.md). `ElectronObject` delegates `Tr` and `TrN` to this domain when per-object translation is enabled.

## Public surface

`TranslationServer` exposes the culture, direct singular/plural registration, resource registration/removal, lookup, and complete catalog clearing. `Translation` stores contextual messages and plural forms and supports independent Resource duplication. `ElectronObject` supplies per-instance translation enablement and lookup entry points from Core. The service's global enabled flag is internal.

## Dependency direction

- Localization depends on the Core Resource contract and the .NET Base Class Library.
- Core references Localization through the static `TranslationServer` API.
- Localization does not depend on Scene, SDL3-CS, rendering, or assets.
- Future asset loading may populate catalogs but must not move resolution into the asset domain.

## Invariants

- Catalog access and culture changes are lock-protected.
- Exact culture is checked before parent cultures and invariant culture.
- Domains, contexts, and messages are ordinal and case-sensitive.
- Missing singular translations return the source message.
- Missing plural translations use singular only when count equals one; registered selectors own language-specific plural rules. Resource catalogs without a selector use English rules only for English locales and fail explicitly for other locales with multiple forms.
- Catalog lookup semantics are platform-independent; platform locale discovery, when added, remains an external host input.

## Not implemented

- No `.po`, `.mo`, `.resx`, JSON, or binary catalog loader.
- No locale discovery, pseudo-localization, formatting, interpolation, or bidirectional-text handling.
- No built-in CLDR plural-rule database or textual plural-rule evaluator; non-English resource catalogs with multiple forms require a typed selector.
- No native locale-discovery or six-target localization verification exists yet.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies parent-culture fallback, source fallback, direct and resource plural selection, contextual catalogs, duplication, disposal, domains, and per-object translation disabling.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0007: Typed localization](../decisions/localization.md#adr-0007)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)
