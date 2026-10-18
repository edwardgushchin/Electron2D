# Translation component

Last updated: 2026-10-18

## Scope

This Localization component stores and resolves domain/context translations for a process-wide UI culture.

## Owned type

[`TranslationServer`](../classes/TranslationServer.md) resolves direct registrations and owns named [`TranslationDomain`](../classes/TranslationDomain.md) registrations of borrowed [`Translation`](../classes/Translation.md) resources. [`OptimizedTranslation`](../classes/OptimizedTranslation.md) is a derived catalog that stores compressed values without source keys. `ElectronObject` provides per-instance domain and enablement settings and delegates `Tr`/`TrN` calls to the server. [`Node`](../classes/Node.md) inherits domains from parents and adds automatic translation policy and `Atr`/`AtrN`.

## Current implementation status

Direct registration and contextual, mutable resource catalogs execute through the same lookup path. A registered domain can override culture and enablement and pseudolocalize singular results. Two typed project settings select the startup test culture and fallback locale; nine further settings supply pseudolocalization enablement and main-domain transforms. A twelfth setting selects the initial scene-root auto-translate mode. `TranslationServer.ReloadPseudolocalization` reapplies the eight transforms during a run without toggling enablement. Resource duplication copies independent message containers. Optimized catalogs generate hash-keyed, optionally Brotli-compressed values, skip contextual entries, keep only the first plural form, hide source keys, and duplicate their lookup maps. Typed Translation archives now execute through ResourceLoader; UTF-8 PO/MO catalogs import through ResourceLoader; operating-system locale discovery remains unimplemented.

<a id="scored-runtime-lookup"></a>
## Scored runtime lookup

Registered and independent domains now rank resources against the original request before fallback. For `sr-ME`, the `sr-Latn-ME` catalog scores 10 and wins over the `sr` parent scoring 4; the former parent-first resource pass returned the weaker result. The same rule applies to plural lookup and tool-locale selection. An invariant resource is not an implicit universal translation catalog. Direct-entry conveniences retain selected/parent candidates, ranked by the same score, plus their explicit invariant fallback; direct entries win score ties. Default-aware identity parsing is shared with CompareLocales.

Immutable parsed identities are retained with weak keys tied to the caller's string object. Warming a live identity avoids repeated parsing without retaining retired caller strings. Message results, resource locale and catalog membership are not cached. Existing prepared registration snapshots and callback-outside-lock behavior remain. Tests remove a captured catalog from its own callback, propagate its original failure, edit/replace locales and values, dispose a preferred catalog and check identity-key collection.

Two RED root/public consumers reproduce the weaker-parent selection on the previous runtime. Warm comparisons allocate 210944 bytes per 128 groups of four calls there, and zero on the corrected path. Eighteen CPU/software-Vulkan/registered root/JIT/strict NativeAOT owner/separate profiles execute 2304 measured complete moving resource-lookup physics/query intervals at strict zero owner/all-thread managed bytes. Eighteen rebuilt application-language notification regressions retain another 2304 intervals. Five related profiles cover Node/MainLoop, managed GUI context, locale metadata, gettext and actual Wayland glyphs; a separate C++ oracle rechecks the fixed normalization and plural data. Both GPU and compatibility renderers independently match four glyph states, including the selected regional resource, in eight captured PNGs. Evidence: `bin/physics-locale-lookup/2026-10-18/validation.json`.

The current resource query is FindTranslations. Both deprecated single-catalog getters have been removed with their callers and public documentation under ADR 0004. Object translation-domain access and inherited plural lookup now have executable coverage. Singular lookup keeps the exact Unicode code-point, placeholder, BIDI and expansion-rounding pseudolocalization dependency. Native memory, target-GPU throughput, window FPS and human acceptance remain unmeasured; the power cable remains unverified.

<a id="application-translation-notifications"></a>
## Application translation notifications

Changing `TranslationServer.Culture` to a different culture name commits that culture and then synchronously sends `NotificationTranslationChanged` through the current `Engine.MainLoop`. Equal-name assignment retains the caller's managed CultureInfo object without another notification. Server `PseudolocalizationEnabled` assignments notify even when equal. Explicit `ReloadPseudolocalization` prepares all eight options before notifying, preserving runtime enablement; startup prepares culture, fallback and options before one notification to an already attached loop. Direct catalog edits and domain-local policy retain their explicit refresh contract. Asset remaps remain absent.

SceneTree uses its existing depth-first hierarchy snapshot and revalidates live membership before every callback, including paused/disabled nodes. Notification errors are aggregated after later nodes run, without rolling back the committed language. Independent library trees are refreshed explicitly. Scene delivery requires its owner thread: a foreign Culture setter can commit global state before delivery rejects, so callers use the ordinary scene scheduler for owner notification. The setter releases its own catalog lock before callbacks; the separate direct-plural-selector locked contract remains unchanged.

Reentrant delivery uses independent prepared buffers at each nesting depth. Capture and delivery release node references on success or failure. Successful delivery at prepared hierarchy/depth capacity avoids the former recursive iterator/array allocations. This uses the existing MainLoop notification path and creates no observer registry or background dispatcher.

The public `BackendChecks.RunLocaleChanges` consumer executes immediate translation, paused nodes, numerical identity, nested changes, removed snapshot candidates, callback failures, cross-thread rejection and explicit owner scheduling, pseudolocalization reload and a subsequent generic MainLoop startup. Eighteen root/JIT/strict NativeAOT CPU/software-Vulkan/registered owner/separate profiles measure 2304 complete moving physics/query intervals with actual language changes and strict zero owner/all-thread managed bytes. Five related regressions cover Node/MainLoop, GUI contexts, locale catalogs, gettext import and native glyph output. Linux Wayland GPU and compatibility renderers independently match all three glyph states, including the automatic global culture change, in six captured PNGs. Evidence: `bin/physics-locale-notifications/2026-10-18/validation.json`. The legacy MainLoop allocation test now warms both measured frame lanes; its former cold physics call reproduced the same 120 bytes on the preceding commit. Native memory, hardware GPU speed/window FPS and human acceptance are unmeasured; the hardware power cable remains unverified.

## Dependencies

The component depends on typed Core project settings, .NET globalization, collections, cryptography and Brotli compression. It has no SDL3-CS dependency.

## Lookup

Singular keys consist of culture, domain, context and source message; plural keys add both source forms. Resource candidates compete against the selected locale using fixed default-aware language/script/country/variant scores. Only a miss tries the configured fallback, followed by source text. Selected/parent direct registrations compete by that same score and win ties; explicit invariant direct entries supply the lowest-priority universal fallback. A later resource wins an equal resource score. Empty resource results let another eligible candidate resolve the message; an empty direct result remains an explicit override.

Direct plural registrations take `Func<long, string>`. Resource catalogs store plural-form lists and select an index through `Func<long, int>`; English has its source fallback, while other locales with multiple forms require a selector.

Scene [`Control`](../classes/Control.md) can use a domain's selected culture and registered catalog when resolving application-locale layout direction. Explicit LTR/RTL remains independent of translation. Managed culture direction and catalog presence drive the current geometric mirror; root/forced project policies now execute in both Window and Control. Global culture notifications now refresh active scene consumers. Exact runtime locale/catalog negotiation remains incomplete.

## Threading

Direct registration, domain registry changes, clearing, and culture changes use the server lock. Domain and resource state have their own locks; lookup snapshots registered resources and invokes their override hooks and selectors outside the server lock. Direct selector delegates still execute under the server lock and should be short. The global enabled flag uses volatile access.

## Exclusions

The component imports bounded UTF-8 PO/MO catalogs and supplies prepared integer plural rules. It does not format parameters, select culture from the operating system after startup or reload asset remaps. Local node domain/mode changes and application culture/pseudolocalization operations notify their affected consumers as described above. Optimized catalogs do not implement the source engine's serialized Smaz format or restrict `Generate` to an editor build in the shared assembly.

## Verification

Tests cover parent-culture lookup, source fallback, scored regional selection, project fallback, domain selection, direct and resource plural selectors, contextual edits, optimized compressed lookup and key hiding, independent duplication, removal, disposal, per-object disabling, domain locale override, singular pseudolocalization, typed project-setting persistence, startup sampling and transform reload. SDL dummy `WindowRuntimeTests` verifies startup sampling before scene ready. A warmed 1,024-call direct optimized lookup check measured 0 managed allocated bytes on Linux/.NET 8. Native or external allocations, the complete server lookup route, total catalog memory use and automatic operating-system locale acquisition remain separate.

Generated numeric input now uses TranslationServer.FormatNumber/ParseNumber/GetPercentSign with exact rune/decimal/exponent data for nine systems and 92 locale identities. The SpinBox first consumer respects Control.LocalizeNumeralSystem; ordinary text is unchanged. Independent locale vectors include supplementary-plane digits, unknown-locale identity and hyphen spellings. Global locale notifications execute through the current application loop; broader CLDR/alias negotiation remains separate.

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
