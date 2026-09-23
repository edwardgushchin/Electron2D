# Translation component

Last updated: 2026-09-23

## Scope

This Localization component stores and resolves domain/context translations for a process-wide UI culture.

## Owned type

[`TranslationServer`](../classes/TranslationServer.md) resolves direct registrations and owns named [`TranslationDomain`](../classes/TranslationDomain.md) registrations of borrowed [`Translation`](../classes/Translation.md) resources. `ElectronObject` provides per-instance domain and enablement settings and delegates `Tr`/`TrN` calls to the server.

## Current implementation status

Direct registration and contextual, mutable resource catalogs execute through the same lookup path. A registered domain can override culture and enablement and pseudolocalize singular results. Resource duplication copies independent message containers. No catalog asset loader or automatic locale selection exists.

## Dependencies

The component depends only on .NET globalization, collections, and threading primitives. Core calls into it, but it has no dependency back on Core and no SDL3-CS dependency.

## Lookup

Singular keys consist of culture, domain, context, and source message. Plural keys additionally contain source singular and plural forms. Resolution tries the exact culture, then each parent, then invariant culture.

Direct plural registrations take `Func<long, string>`. Resource catalogs store plural-form lists and select an index through `Func<long, int>`; English has its source fallback, while other locales with multiple forms require a selector.

## Threading

Direct registration, domain registry changes, clearing, and culture changes use the server lock. Domain and resource state have their own locks; lookup snapshots registered resources and invokes their override hooks and selectors outside the server lock. Direct selector delegates still execute under the server lock and should be short. The global enabled flag uses volatile access.

## Exclusions

The component does not load files, format parameters, infer plural rules, or select culture from the operating system after startup.

## Verification

Tests cover parent-culture lookup, source fallback, domain selection, direct and resource plural selectors, contextual edits, independent duplication, removal, disposal, per-object disabling, domain locale override and singular pseudolocalization. Exact Unicode and locale-score parity remain unaudited.
