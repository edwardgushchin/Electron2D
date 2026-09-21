# TranslationServer

Last updated: 2026-09-21

**Inherits:** —

**Inherited By:** —

- **Source:** [`src/Core/String/TranslationServer.cs`](../../src/Core/String/TranslationServer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public static class TranslationServer`

> Stores and resolves process-wide in-memory translations for the selected UI culture.

## Description

Stores and resolves process-wide in-memory translations for the selected UI culture.

`TranslationServer` is the process-wide in-memory translation catalog. It resolves ordinal domain/context keys for the selected UI culture and its parent cultures without a `Variant` payload or external localization dependency.

Lookups use exact, case-sensitive domain, context, and source-message keys, then walk from the selected culture
through its parents to the invariant culture. This intentionally small typed service does not load catalogs or
implement CLDR plural rules.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
TranslationServer.Culture = new CultureInfo("ru");
string text = TranslationServer.Translate("ui", "menu.play");
```

## Properties

| Member | Description |
| --- | --- |
| [`public static bool Enabled { get; set; }`](#p-electron2d-translationserver-enabled) | Gets or sets whether translation lookup is enabled globally. |
| [`public static CultureInfo Culture { get; set; }`](#p-electron2d-translationserver-culture) | Gets or sets the culture used for subsequent translation lookups. |

## Methods

| Member | Description |
| --- | --- |
| [`public static void AddTranslation(CultureInfo culture, string domain, string message, string translation, string context = null)`](#m-electron2d-translationserver-addtranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-string) | Adds or replaces one singular translation. |
| [`public static void AddPluralTranslation(CultureInfo culture, string domain, string singular, string plural, Func<long, string> selector, string context = null)`](#m-electron2d-translationserver-addpluraltranslation-system-globalization-cultureinfo-system-string-system-string-system-string-system-func-system-int64-system-string-system-string) | Adds or replaces one plural translation selector. |
| [`public static string Translate(string domain, string message, string context = null)`](#m-electron2d-translationserver-translate-system-string-system-string-system-string) | Resolves a singular message for the current culture and its parent cultures. |
| [`public static string TranslatePlural(string domain, string singular, string plural, long count, string context = null)`](#m-electron2d-translationserver-translateplural-system-string-system-string-system-string-system-int64-system-string) | Resolves a plural message for the current culture and its parent cultures. |
| [`public static void Clear()`](#m-electron2d-translationserver-clear) | Removes all singular and plural translation registrations. |

## Property Descriptions

<a id="p-electron2d-translationserver-enabled"></a>
### `public static bool Enabled { get; set; }`

Gets or sets whether translation lookup is enabled globally.

**Value:** `true` by default. When false, lookup returns source text without consulting the catalogs.

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
- `Exception`: A matching plural selector throws.

**Remarks:** The selector executes synchronously while the catalog lock is held and should complete quickly.

<a id="m-electron2d-translationserver-clear"></a>
### `public static void Clear()`

Removes all singular and plural translation registrations.

**Remarks:** The selected [`TranslationServer.Culture`](TranslationServer.md#p-electron2d-translationserver-culture) and [`TranslationServer.Enabled`](TranslationServer.md#p-electron2d-translationserver-enabled) state are not changed.

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
