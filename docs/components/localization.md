# Translation component

Last updated: 2026-09-20

## Scope

This Localization component stores and resolves domain/context translations for a process-wide UI culture.

## Owned type

[`TranslationServer`](../classes/TranslationServer.md) is the only owned production type. `ElectronObject` provides per-instance domain and enablement settings and delegates `Tr`/`TrN` calls to the server.

## Current implementation status

Implemented as an in-memory runtime catalog and covered by executable checks. No catalog asset loader or automatic locale selection exists.

## Dependencies

The component depends only on .NET globalization, collections, and threading primitives. Core calls into it, but it has no dependency back on Core and no SDL3-CS dependency.

## Lookup

Singular keys consist of culture, domain, context, and source message. Plural keys additionally contain source singular and plural forms. Resolution tries the exact culture, then each parent, then invariant culture.

Plural registrations take `Func<long, string>`, allowing the catalog owner to implement language-specific rules without a universal dynamic value or a built-in incomplete plural heuristic.

## Threading

Catalog registration, clearing, culture changes, and lookup use one internal lock. The global enabled flag uses volatile access. Translation selector delegates execute while the catalog lock is held and therefore should be short; recursive selector logic remains the caller's responsibility.

## Exclusions

The component does not load files, format parameters, infer plural rules, or select culture from the operating system after startup.

## Verification

Tests cover parent-culture lookup, source fallback, domain selection, custom plural rules, and per-object disabling.
