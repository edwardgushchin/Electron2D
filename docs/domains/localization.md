# Localization domain

Last updated: 2026-09-21

## Responsibility

Localization owns process-wide translation catalogs, UI culture selection, domain/context lookup, parent-culture fallback, and caller-supplied plural selection for the Linux, Windows, macOS, Android, and iOS runtime targets.

Its production source lives under `src/Core/String/`, matching its low-level engine module while the living architecture retains Localization as a separate logical domain. The public namespace remains `Electron2D`.

## Current state

| Component | Responsibility | State |
| --- | --- | --- |
| [Translation](../components/localization.md) | Thread-safe translation registration and resolution | Implemented and verified |

The production type is [`TranslationServer`](../classes/TranslationServer.md). `ElectronObject` delegates `Tr` and `TrN` to this domain when per-object translation is enabled.

## Public surface

`TranslationServer` exposes the global enabled flag and culture, singular/plural registration, singular/plural lookup, and complete catalog clearing. `ElectronObject` supplies the per-instance translation entry points from Core.

## Dependency direction

- Localization depends only on the .NET Base Class Library.
- Core references Localization through the static `TranslationServer` API.
- Localization does not depend on Scene, SDL3-CS, rendering, or assets.
- Future asset loading may populate catalogs but must not move resolution into the asset domain.

## Invariants

- Catalog access and culture changes are lock-protected.
- Exact culture is checked before parent cultures and invariant culture.
- Domains, contexts, and messages are ordinal and case-sensitive.
- Missing singular translations return the source message.
- Missing plural translations use singular only when count equals one; registered selectors own language-specific plural rules.
- Catalog lookup semantics are platform-independent; platform locale discovery, when added, remains an external host input.

## Not implemented

- No `.po`, `.mo`, `.resx`, JSON, or binary catalog loader.
- No locale discovery, pseudo-localization, formatting, interpolation, or bidirectional-text handling.
- No built-in CLDR plural-rule database; registration supplies the selector.
- No native locale-discovery or five-platform localization verification exists yet.

## Verification

`tests/Electron2D.Tests/Program.cs` verifies parent-culture fallback, source fallback, custom plural selection, domains, and per-object translation disabling.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0007: Typed localization](../decisions/localization.md#adr-0007)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Cross-platform runtime target matrix](../decisions/product.md#adr-0021)
