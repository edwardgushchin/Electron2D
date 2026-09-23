# TranslationServer

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/String/TranslationServer.cs`](../../src/Core/String/TranslationServer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class TranslationServer`

> Stores and resolves process-wide in-memory translations for the selected UI culture.

## Description

Stores and resolves process-wide in-memory translations for the selected UI culture.

`TranslationServer` is the process-wide in-memory translation registry. It resolves direct entries and borrowed [`Translation`](Translation.md) resources by ordinal domain/context keys for the selected UI culture and its parent cultures.

Lookups use exact, case-sensitive domain, context, and source-message keys, then walk from the selected culture
through its parents to the invariant culture. Within one locale, direct registrations precede resource catalogs, and the most recently registered resource is queried first. This typed service does not load catalog files or implement CLDR plural rules.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
TranslationServer.Culture = new CultureInfo("ru");
string text = TranslationServer.Translate("ui", "menu.play");
```

## Properties

| Member | Description |
| --- | --- |
| [`public static CultureInfo Culture { get; set; }`](#p-electron2d-translationserver-culture) | Gets or sets the culture used for subsequent translation lookups. |

## Methods

| Member | Description |
| --- | --- |
| [`public static void AddTranslation(CultureInfo culture, string domain, string message, string translation, string context = null)`](#m-electron2d-translationserver-addtranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-string) | Adds or replaces one singular translation. |
| [`public static void AddTranslation(Translation translation, string domain = "")`](#add-resource-translation) | Registers a borrowed live resource catalog. |
| [`public static void AddPluralTranslation(CultureInfo culture, string domain, string singular, string plural, Func<long, string> selector, string context = null)`](#m-electron2d-translationserver-addpluraltranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-func-system-int64-system-string-system-string) | Adds or replaces one plural translation selector. |
| [`public static void RemoveTranslation(Translation translation, string domain = "")`](#remove-resource-translation) | Removes a resource registration without disposing it. |
| [`public static string Translate(string domain, string message, string context = null)`](#m-electron2d-translationserver-translate-system-string-system-string-system-string) | Resolves a singular message for the current culture and its parent cultures. |
| [`public static string TranslatePlural(string domain, string singular, string plural, long count, string context = null)`](#m-electron2d-translationserver-translateplural-system-string-system-string-system-string-system-int64-system-string) | Resolves a plural message for the current culture and its parent cultures. |
| [`public static void Clear()`](#m-electron2d-translationserver-clear) | Removes all singular and plural translation registrations. |

## Property Descriptions

<a id="p-electron2d-translationserver-culture"></a>
### `public static CultureInfo Culture { get; set; }`

Gets or sets the culture used for subsequent translation lookups.

**Value:** The process-wide lookup culture, initialized from `Globalization.CultureInfo.CurrentUICulture`.

**Exceptions**

- `ArgumentNullException`: The assigned value is `null`.

## Method Descriptions

<a id="m-electron2d-translationserver-addtranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-string"></a>
### `public static void AddTranslation(CultureInfo culture, string domain, string message, string translation, string context = null)`

Adds or replaces one singular translation.

**Parameters**

- `culture`: The culture whose name forms part of the lookup key.
- `domain`: The case-sensitive translation domain.
- `message`: The source message.
- `translation`: The translated message.
- `context`: An optional disambiguation context. Null is normalized to an empty string.

**Exceptions**

- `ArgumentNullException`: `culture`, `domain`, `message`, or
`translation` is `null`.

<a id="add-resource-translation"></a>
### `public static void AddTranslation(Translation translation, string domain = "")`

Registers a live resource in a case-sensitive domain. Repeating the same resource/domain pair does nothing. Later resource edits are visible without re-registration; disposing it unregisters it. The server borrows the resource. Null arguments and a disposed resource throw.

<a id="m-electron2d-translationserver-addpluraltranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-func-system-int64-system-string-system-string"></a>
### `public static void AddPluralTranslation(CultureInfo culture, string domain, string singular, string plural, Func<long, string> selector, string context = null)`

Adds or replaces one plural translation selector.

**Parameters**

- `culture`: The culture whose name forms part of the lookup key.
- `domain`: The case-sensitive translation domain.
- `singular`: The source singular form.
- `plural`: The source plural form.
- `selector`: A function that maps a quantity to translated text.
- `context`: An optional disambiguation context. Null is normalized to an empty string.

**Exceptions**

- `ArgumentNullException`: `culture`, `domain`, `singular`,
`plural`, or `selector` is `null`.

<a id="remove-resource-translation"></a>
### `public static void RemoveTranslation(Translation translation, string domain = "")`

Removes the exact resource/domain registration if present. The resource remains live and caller-owned. Null arguments throw.

<a id="m-electron2d-translationserver-translate-system-string-system-string-system-string"></a>
### `public static string Translate(string domain, string message, string context = null)`

Resolves a singular message for the current culture and its parent cultures.

**Parameters**

- `domain`: The case-sensitive translation domain.
- `message`: The source message.
- `context`: An optional disambiguation context. Null is equivalent to an empty string.

**Returns:** The first matching translation, or `message` when none exists or translation is disabled.

**Exceptions**

- `ArgumentNullException`: `domain` or `message` is `null`.

**Remarks:** Direct entries win within each culture tier. Resource lookups use a snapshot and run custom hooks outside the server lock; a concurrently disposed resource is skipped.

<a id="m-electron2d-translationserver-translateplural-system-string-system-string-system-string-system-int64-system-string"></a>
### `public static string TranslatePlural(string domain, string singular, string plural, long count, string context = null)`

Resolves a plural message for the current culture and its parent cultures.

**Parameters**

- `domain`: The case-sensitive translation domain.
- `singular`: The source singular form.
- `plural`: The source plural form.
- `count`: The quantity passed to the matching selector.
- `context`: An optional disambiguation context. Null is equivalent to an empty string.

**Returns:** The selected translation. Without a matching entry, `singular` is returned only when
`count` equals `1`; otherwise `plural` is returned.

**Exceptions**

- `ArgumentNullException`: `domain`, `singular`, or `plural` is `null`.
- `InvalidOperationException`: A matching plural selector returns `null`.
- `InvalidOperationException`: A resource catalog cannot select a plural form for its locale or returns an invalid index.
- `Exception`: A matching plural selector throws.

**Remarks:** Direct selectors execute synchronously under the server lock; resource selectors execute outside it.

<a id="m-electron2d-translationserver-clear"></a>
### `public static void Clear()`

Removes all direct singular/plural registrations and resource registrations without disposing resources.

**Remarks:** The selected [`TranslationServer.Culture`](TranslationServer.md#p-electron2d-translationserver-culture) is not changed.

## State and key rules

Singular keys contain culture name, domain, normalized context, and source message. Plural keys also contain both source forms. A null context is normalized to the empty string. All string matching uses the tuple/string default ordinal, case-sensitive equality.

The culture chain includes the exact culture, each parent, and invariant culture. Direct registrations overwrite an identical key. Resources are queried in reverse registration order within their domain and locale. Direct entries have priority; `RemoveTranslation` removes one resource registration.

## Invariants and errors

- Required reference arguments are non-null; invalid inputs throw the standard argument exceptions.
- A null result from a plural selector throws `InvalidOperationException`.
- Exceptions thrown by a plural selector propagate to the caller.
- When globally disabled, lookup returns source text without taking the catalog lock.

## Threading

One process-wide lock protects direct catalogs, resource registrations, and `Culture`. Resource state has its own lock. The internal `Enabled` flag uses volatile reads/writes. Direct plural selectors execute while the server lock is held and should therefore be short. Resource hooks and selectors run after a snapshot outside the server lock.

## Dependencies and interactions

`ElectronObject.Tr()` and `TrN()` use the object's `TranslationDomain` and `CanTranslateMessages` before calling this server. The server depends only on .NET globalization, collections, and threading primitives.

## Verification and limitations

Tests verify parent-culture fallback, missing-message fallback, domain selection, caller-defined plural behavior, resource edits, duplication, removal, disposal, and per-object disabling.

No catalog file loader, CLDR plural rules, message formatting, locale negotiation, pseudo-localization, bidirectional text support, or per-thread culture override is implemented.
