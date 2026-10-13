# Translation component

Last updated: 2026-10-13

## Scope

This Localization component stores and resolves domain/context translations for a process-wide UI culture.

## Owned type

[`TranslationServer`](../classes/TranslationServer.md) resolves direct registrations and owns named [`TranslationDomain`](../classes/TranslationDomain.md) registrations of borrowed [`Translation`](../classes/Translation.md) resources. [`OptimizedTranslation`](../classes/OptimizedTranslation.md) is a derived catalog that stores compressed values without source keys. `ElectronObject` provides per-instance domain and enablement settings and delegates `Tr`/`TrN` calls to the server. [`Node`](../classes/Node.md) inherits domains from parents and adds automatic translation policy and `Atr`/`AtrN`.

## Current implementation status

Direct registration and contextual, mutable resource catalogs execute through the same lookup path. A registered domain can override culture and enablement and pseudolocalize singular results. Two typed project settings select the startup test culture and fallback locale; nine further settings supply pseudolocalization enablement and main-domain transforms. A twelfth setting selects the initial scene-root auto-translate mode. `TranslationServer.ReloadPseudolocalization` reapplies the eight transforms during a run without toggling enablement. Resource duplication copies independent message containers. Optimized catalogs generate hash-keyed, optionally Brotli-compressed values, skip contextual entries, keep only the first plural form, hide source keys, and duplicate their lookup maps. Typed Translation archives now execute through ResourceLoader; UTF-8 PO/MO catalogs import through ResourceLoader; operating-system locale discovery remains unimplemented.

## Dependencies

The component depends on typed Core project settings, .NET globalization, collections, cryptography and Brotli compression. It has no SDL3-CS dependency.

## Lookup

Singular keys consist of culture, domain, context, and source message. Plural keys additionally contain source singular and plural forms. Resolution tries the selected culture, each parent and invariant culture, then positive-score resource locales, then the configured fallback. Direct registrations precede resources at each exact culture. A later resource wins an equal score.

Direct plural registrations take `Func<long, string>`. Resource catalogs store plural-form lists and select an index through `Func<long, int>`; English has its source fallback, while other locales with multiple forms require a selector.

Scene [`Control`](../classes/Control.md) can use a domain's selected culture and registered catalog when resolving application-locale layout direction. Explicit LTR/RTL remains independent of translation. Managed culture direction and catalog presence drive the current geometric mirror; root/forced project policies now execute in both Window and Control. Native locale alias tables and automatic scene reflow after a global culture change remain incomplete.

## Threading

Direct registration, domain registry changes, clearing, and culture changes use the server lock. Domain and resource state have their own locks; lookup snapshots registered resources and invokes their override hooks and selectors outside the server lock. Direct selector delegates still execute under the server lock and should be short. The global enabled flag uses volatile access.

## Exclusions

The component does not load catalog files, format parameters, infer plural rules, or select culture from the operating system after startup. Reloading pseudolocalization does not reload asset remaps or notify an active scene of a translation change; local node domain and mode changes do notify affected nodes. Optimized catalogs do not implement the source engine's serialized Smaz format or restrict `Generate` to an editor build in the shared assembly.

## Verification

Tests cover parent-culture lookup, source fallback, scored regional selection, project fallback, domain selection, direct and resource plural selectors, contextual edits, optimized compressed lookup and key hiding, independent duplication, removal, disposal, per-object disabling, domain locale override, singular pseudolocalization, typed project-setting persistence, startup sampling and transform reload. SDL dummy `WindowRuntimeTests` verifies startup sampling before scene ready. A warmed 1,024-call direct optimized lookup check measured 0 managed allocated bytes on Linux/.NET 8. Native or external allocations, the complete server lookup route, total catalog memory use, and reference locale-alias/default-script parity remain unmeasured or unaudited.

Generated numeric input now uses TranslationServer.FormatNumber/ParseNumber/GetPercentSign with exact rune/decimal/exponent data for nine systems and 92 locale identities. The SpinBox first consumer respects Control.LocalizeNumeralSystem; ordinary text is unchanged. Independent locale vectors include supplementary-plane digits, unknown-locale identity and hyphen spellings. Global locale notifications and broader CLDR/alias negotiation remain separate prerequisites.

The permanent `ProjectSettings.RootNodeLayoutDirection` integer selector accepts only 0 application locale, 1 LTR, 2 RTL and 3 system UI locale (default 0). `ForceRightToLeftLayoutDirection` is a permanent boolean, default false. Both support save/load and feature overrides and are sampled independently for each SceneTree before entry. Explicit node LTR/RTL choices override forced policy; other modes are forced RTL when enabled. Global project edits affect subsequent trees, while detached nodes read current settings. [ProjectLayoutDirectionTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionTests.cs) and native [ProjectLayoutDirectionNativeTests](../../tests/Electron2D.Tests/ProjectLayoutDirectionNativeTests.cs) verify the executable profile.

`Control.TranslationContext` is a stored local string, empty by default. `Atr`/`AtrN` use it when the call's context is null or empty; a nonempty call argument overrides it. `Tr`/`TrN` retain their explicit context. The property is not inherited from a parent. Set it before assigning text: changing the context alone sends no notification and preserves already prepared captions. `Notify(Node.NotificationTranslationChanged)` refreshes that control; `PropagateNotification` can refresh its subtree. Existing automatic mode, object enablement and inherited domain rules still apply.

The fixed locale catalog exposes language/script/country names, aliases, script/regional defaults and gettext definitions through TranslationServer. Translation resources without a caller PluralSelector use these prepared integer rules; copying and archive loading restore the automatic locale policy. Unknown multi-form rules require a caller selector. Runtime Culture remains CultureInfo. Decimal CLDR category selection remains separate; integer PO/MO import executes through ResourceLoader.

## Fixed locale catalog and automatic resource plurals

The locale slice uses pinned language/script/country names, aliases, variants, ambiguous defaults and gettext definitions. All nine discovery/normalization/rule operations execute through TranslationServer; CompareLocales uses the same normalized identities with defaults. Translation resources prepare integer selectors at Locale assignment, preserve explicit caller overrides, and restore automatic selection after duplication or archive loading. Four two-form singular predicates are corrected so form zero remains singular. Catalog rule text and compiled selectors are generated together; imported definitions use a bounded prepared expression tree with allocation-free warm evaluation.

LocaleCatalogTests covers labels, aliases/casing/defaults, regional rules, copying/archives and a fresh reader process, invalid records and save rollback, overrides and a 1024-call zero-managed-byte automatic lookup. A separately compiled C++ oracle checks 2600 original-constructor normalization cases, the complete ordered metadata and 67452 plural arithmetic cases at small, million and signed-64-bit boundary counts. Actual GPU and compatibility consumers populate a language menu, select Russian and display different pixels for two/five-item forms. These checks cover Linux x64; foreign globalization/native targets remain separate gates.

## PO and MO catalog import

`ResourceLoader.Load<Translation>(path)` recognizes `po` and `mo`, shares cache modes and threaded preparation, and returns a complete contextual catalog. Header Language/X-Language selects Locale (English if absent). Plural-Forms supplies a stored definition overriding the automatic locale rule; an explicit PluralSelector wins over either. Resource copies and `.e2dres` archives preserve imported rules and independent message containers. Invalid public override text falls back to automatic rules; malformed imported headers fail before resource publication.

Input is bounded to 16 MiB, 100,000 messages, 64 forms and 4096 rule characters, with 256 expression nodes and 64 nesting levels. Expressions use unsigned 64-bit integer arithmetic, normal operator precedence, ternary and short-circuit logic; divide-by-zero or an out-of-range result fails lookup. PO supports contexts, continued quoted strings, C single-character escapes and contiguous plural indexes; fuzzy and obsolete entries are skipped. MO checks revisions, tables, terminators and aggregate decoded bytes. Non-UTF-8 encodings and system-dependent MO extensions fail explicitly. Import is read-only; save the resource as `.e2dres`.

`TranslationCatalogTests` exercises import, malformed input, cache/threaded paths, fresh-process archive restoration, override priority, copying and zero-byte warm selection. Native language-menu consumers use an imported rule with reordered forms and check rendered two/five-item pixels on GPU and compatibility renderers. These are Linux x64 checks, not foreign-platform acceptance.
