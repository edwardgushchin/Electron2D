# Localization domain

Last updated: 2026-10-18

## Responsibility

Localization owns process-wide direct and resource-backed translation catalogs, UI culture selection, domain/context lookup, scored resource-locale and configured fallback selection, and caller-supplied plural selection for the Windows, macOS, Linux (X11/Wayland), Android, iOS, Android TV, tvOS, and Web runtime targets.

Its production source lives under `src/Core/String/`, matching its low-level engine module while the living architecture retains Localization as a separate logical domain. The public namespace remains `Electron2D`.

## Current state

| Component | Responsibility | State |
| --- | --- | --- |
| [Translation](../components/localization.md) | Thread-safe direct and resource-backed translation registration and resolution | Implemented for in-memory lookup; locale-rule and asset gaps remain |

Production types are [`TranslationServer`](../classes/TranslationServer.md), [`TranslationDomain`](../classes/TranslationDomain.md), [`Translation`](../classes/Translation.md), and [`OptimizedTranslation`](../classes/OptimizedTranslation.md). `ElectronObject` delegates `Tr` and `TrN` to this domain when per-object translation is enabled. Scene-owned [`Node`](../classes/Node.md) adds inherited domains and automatic translation policy. [`Control`](../classes/Control.md) consults the selected culture and registered catalog for locale-derived rectangle direction; application culture and pseudolocalization changes now refresh the active MainLoop synchronously after state commits. Exact runtime locale/catalog negotiation and asset remaps remain gaps.

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
- Missing plural translations use singular only when count equals one; registered selectors own language-specific plural rules. Resource catalogs use an imported Plural-Forms definition, then a known automatic locale rule, and fail explicitly for unknown multi-form rules.
- Catalog lookup semantics are platform-independent; platform locale discovery, when added, remains an external host input.

## Not implemented

- `.resx`, JSON catalogs and system-dependent MO extension tables remain outside the current import profile.
- Optimized translation payloads use an internal Brotli representation; reference Smaz file compatibility and editor-only generation gating remain absent.
- No operating-system locale discovery, number formatting, interpolation, or full Unicode bidirectional-text implementation. Pseudolocalization includes only test direction-control marks; locale alias/default-script tables and exact Unicode parity remain partial.
- Full CLDR decimal categories remain absent; fixed integer locale rules and bounded textual catalog definitions execute. Unknown rules require a valid definition or typed caller selector.
- No native locale-discovery or localization verification on all targets exists yet.

## Verification

`tests/Electron2D.Tests/Program.cs`, `TranslationDomainTests.cs`, `LocalizationProjectSettingsTests.cs`, and `NodeLocalizationTests.cs` verify scored resource-locale and configured fallback selection, source fallback, direct and resource plural selection, contextual and optimized catalogs, duplication, disposal, domain lifecycle and locale override, singular pseudolocalization, per-object translation disabling, inherited node domains and policies, root setting persistence/sampling, managed Engine startup sampling, and live transform reload. `WindowRuntimeTests` verifies Run sampling before scene ready with the SDL dummy driver; other native platforms remain unverified for this setting.

## Decisions

- [0001: Typed C# without Variant](../decisions/product.md#adr-0001)
- [0007: Typed localization](../decisions/localization.md#adr-0007)
- [0017: Source-tree module layout](../decisions/product.md#adr-0017)
- [0021: Runtime and editor target platforms](../decisions/product.md#adr-0021)

[Numeric input](../components/numeric-input.md) adds SpinBox formula/text/arrow/repeat/relative-drag authoring through shared Range and LineEdit, fresh scene factories and generated numeral localization. Current Wayland GPU/compatibility capture/input/pixels and prepared active rendering are exercised; precise pointer warp, inherited semantic/editor and foreign target gates remain separate.

The permanent `ProjectSettings.RootNodeLayoutDirection` integer selector accepts only 0 application locale, 1 LTR, 2 RTL and 3 system UI locale (default 0). `ForceRightToLeftLayoutDirection` is a permanent boolean, default false. Both support save/load and feature overrides and are sampled independently for each SceneTree before entry. Explicit node LTR/RTL choices override forced policy; other modes are forced RTL when enabled. Global project edits affect subsequent trees, while detached nodes read current settings. [ProjectLayoutDirectionTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionTests.cs) and native [ProjectLayoutDirectionNativeTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionNativeTests.cs) verify the executable profile.

`Control.TranslationContext` is a stored local string, empty by default. `Atr`/`AtrN` use it when the call's context is null or empty; a nonempty call argument overrides it. `Tr`/`TrN` retain their explicit context. The property is not inherited from a parent. Set it before assigning text: changing the context alone sends no notification and preserves already prepared captions. `Notify(Node.NotificationTranslationChanged)` refreshes that control; `PropagateNotification` can refresh its subtree. Existing automatic mode, object enablement and inherited domain rules still apply.

## PO and MO catalog import

`ResourceLoader.Load<Translation>(path)` recognizes `po` and `mo`, shares cache modes and threaded preparation, and returns a complete contextual catalog. Header Language/X-Language selects Locale (English if absent). Plural-Forms supplies a stored definition overriding the automatic locale rule; an explicit PluralSelector wins over either. Resource copies and `.e2dres` archives preserve imported rules and independent message containers. Invalid public override text falls back to automatic rules; malformed imported headers fail before resource publication.

Input is bounded to 16 MiB, 100,000 messages, 64 forms and 4096 rule characters, with 256 expression nodes and 64 nesting levels. Expressions use unsigned 64-bit integer arithmetic, normal operator precedence, ternary and short-circuit logic; divide-by-zero or an out-of-range result fails lookup. PO supports contexts, continued quoted strings, C single-character escapes and contiguous plural indexes; fuzzy and obsolete entries are skipped. MO checks revisions, tables, terminators and aggregate decoded bytes. Non-UTF-8 encodings and system-dependent MO extensions fail explicitly. Import is read-only; save the resource as `.e2dres`.

`TranslationCatalogTests` exercises import, malformed input, cache/threaded paths, fresh-process archive restoration, override priority, copying and zero-byte warm selection. Native language-menu consumers use an imported rule with reordered forms and check rendered two/five-item pixels on GPU and compatibility renderers. These are Linux x64 checks, not foreign-platform acceptance.

Application notification ownership, reentry, failure handling and measured public physical/native glyph consumers are documented in [translation notifications](../components/localization.md#application-translation-notifications). Independent library trees use explicit notification; active SceneTree delivery requires its owner thread.
