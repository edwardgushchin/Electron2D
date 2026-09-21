# TranslationServer

Last updated: 2026-09-21

## Declaration

- Source: [`TranslationServer.cs`](../../src/Core/String/TranslationServer.cs)
- Namespace: `Electron2D`
- Declaration: `public static class TranslationServer`
- Domain: [Localization](../domains/localization.md)
- Component: [Translation](../components/localization.md)

## Responsibility

`TranslationServer` is the process-wide in-memory translation catalog. It resolves ordinal domain/context keys for the selected UI culture and its parent cultures without a `Variant` payload or external localization dependency.

## Public API

| Member | Current behavior |
| --- | --- |
| `bool Enabled { get; set; }` | Global volatile switch; defaults to `true` |
| `CultureInfo Culture { get; set; }` | Lock-protected lookup culture; initialized once from `CultureInfo.CurrentUICulture` |
| `AddTranslation(CultureInfo culture, string domain, string message, string translation, string? context = null)` | Adds or replaces one singular translation |
| `AddPluralTranslation(CultureInfo culture, string domain, string singular, string plural, Func<long, string> selector, string? context = null)` | Adds or replaces one caller-defined plural selector |
| `Translate(string domain, string message, string? context = null)` | Resolves exact culture, parents, then invariant; otherwise returns `message` |
| `TranslatePlural(string domain, string singular, string plural, long count, string? context = null)` | Runs the first matching selector; otherwise returns singular only for count 1 |
| `Clear()` | Removes every singular and plural registration without changing culture or enabled state |

## State and key rules

Singular keys contain culture name, domain, normalized context, and source message. Plural keys also contain both source forms. A null context is normalized to the empty string. All string matching uses the tuple/string default ordinal, case-sensitive equality.

The culture chain includes the exact culture, each parent, and invariant culture. Registrations overwrite an identical key. No catalog version, priority, or removal of a single entry exists.

## Invariants and errors

- Required reference arguments are non-null; invalid inputs throw the standard argument exceptions.
- A null result from a plural selector throws `InvalidOperationException`.
- Exceptions thrown by a plural selector propagate to the caller.
- When globally disabled, lookup returns source text without taking the catalog lock.

## Threading

One process-wide lock protects catalogs and `Culture`. `Enabled` uses volatile reads/writes. Lookup and registration are thread-safe, but plural selectors currently execute while the lock is held and should therefore be short; recursive selector logic remains the caller's responsibility.

## Dependencies and interactions

`ElectronObject.Tr()` and `TrN()` use the object's `TranslationDomain` and `CanTranslateMessages` before calling this server. The server depends only on .NET globalization, collections, and threading primitives.

## Verification and limitations

Tests verify parent-culture fallback, missing-message fallback, domain selection, caller-defined plural behavior, and per-object disabling.

No catalog file loader, CLDR plural rules, message formatting, locale negotiation, pseudo-localization, bidirectional text support, or per-thread culture override is implemented.
