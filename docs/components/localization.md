# Translation component

Last updated: 2026-09-24

## Scope

This Localization component stores and resolves domain/context translations for a process-wide UI culture.

## Owned type

[`TranslationServer`](../classes/TranslationServer.md) resolves direct registrations and owns named [`TranslationDomain`](../classes/TranslationDomain.md) registrations of borrowed [`Translation`](../classes/Translation.md) resources. [`OptimizedTranslation`](../classes/OptimizedTranslation.md) is a derived catalog that stores compressed values without source keys. `ElectronObject` provides per-instance domain and enablement settings and delegates `Tr`/`TrN` calls to the server. [`Node`](../classes/Node.md) inherits domains from parents and adds automatic translation policy and `Atr`/`AtrN`.

## Current implementation status

Direct registration and contextual, mutable resource catalogs execute through the same lookup path. A registered domain can override culture and enablement and pseudolocalize singular results. Two typed project settings select the startup test culture and fallback locale; nine further settings supply pseudolocalization enablement and main-domain transforms. A twelfth setting selects the initial scene-root auto-translate mode. `TranslationServer.ReloadPseudolocalization` reapplies the eight transforms during a run without toggling enablement. Resource duplication copies independent message containers. Optimized catalogs generate hash-keyed, optionally Brotli-compressed values, skip contextual entries, keep only the first plural form, hide source keys, and duplicate their lookup maps. No catalog asset loader or operating-system locale discovery exists.

## Dependencies

The component depends on typed Core project settings, .NET globalization, collections, cryptography and Brotli compression. It has no SDL3-CS dependency.

## Lookup

Singular keys consist of culture, domain, context, and source message. Plural keys additionally contain source singular and plural forms. Resolution tries the selected culture, each parent and invariant culture, then positive-score resource locales, then the configured fallback. Direct registrations precede resources at each exact culture. A later resource wins an equal score.

Direct plural registrations take `Func<long, string>`. Resource catalogs store plural-form lists and select an index through `Func<long, int>`; English has its source fallback, while other locales with multiple forms require a selector.

Scene [`Control`](../classes/Control.md) can use a domain's selected culture and registered catalog when resolving application-locale layout direction. Explicit LTR/RTL remains independent of translation. Managed culture direction and catalog presence drive the current geometric mirror; native locale alias tables, root/forced policies and automatic scene reflow after a global culture change are not yet equivalent.

## Threading

Direct registration, domain registry changes, clearing, and culture changes use the server lock. Domain and resource state have their own locks; lookup snapshots registered resources and invokes their override hooks and selectors outside the server lock. Direct selector delegates still execute under the server lock and should be short. The global enabled flag uses volatile access.

## Exclusions

The component does not load catalog files, format parameters, infer plural rules, or select culture from the operating system after startup. Reloading pseudolocalization does not reload asset remaps or notify an active scene of a translation change; local node domain and mode changes do notify affected nodes. Optimized catalogs do not implement the source engine's serialized Smaz format or restrict `Generate` to an editor build in the shared assembly.

## Verification

Tests cover parent-culture lookup, source fallback, scored regional selection, project fallback, domain selection, direct and resource plural selectors, contextual edits, optimized compressed lookup and key hiding, independent duplication, removal, disposal, per-object disabling, domain locale override, singular pseudolocalization, typed project-setting persistence, startup sampling and transform reload. SDL dummy `WindowRuntimeTests` verifies startup sampling before scene ready. A warmed 1,024-call direct optimized lookup check measured 0 managed allocated bytes on Linux/.NET 8. Native or external allocations, the complete server lookup route, total catalog memory use, and reference locale-alias/default-script parity remain unmeasured or unaudited.
