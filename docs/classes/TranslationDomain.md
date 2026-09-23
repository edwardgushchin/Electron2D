# TranslationDomain

Last updated: 2026-09-23

**Inherits:** [`ElectronObject`](ElectronObject.md)
**Inherited By:** —

- **Source:** [`src/Core/String/TranslationDomain.cs`](../../src/Core/String/TranslationDomain.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class TranslationDomain : ElectronObject`

## Description

A domain owns the registration order of borrowed [`Translation`](Translation.md) resources. It does not dispose them. A live catalog's edits and locale changes affect subsequent lookups; its disposal unregisters it. Registered domains also participate in [`TranslationServer`](TranslationServer.md) direct entries. An independent domain resolves only its own catalogs. The most recently registered matching resource wins within each locale; lookups visit the selected culture, its parents, then invariant culture.

`Enabled`, `LocaleOverride` and pseudolocalization options belong to each domain. The process-wide server enablement still applies. The server's main domain uses the empty name. Removing a custom domain detaches it without disposing it; disposing a registered domain removes its registration and direct entries. Managed lifetime follows `ElectronObject`.

## Example

```csharp
using var catalog = new Translation { Locale = "fr" };
catalog.AddMessage("Play", "Jouer");
var domain = TranslationServer.GetOrAddDomain("ui");
domain.AddTranslation(catalog);
domain.LocaleOverride = "fr";
string label = domain.Translate("Play"); // Jouer
TranslationServer.RemoveDomain("ui");
domain.Dispose();
```

## Constructor

| Member | Description |
| --- | --- |
| `public TranslationDomain()` | Creates an independent, empty domain. |

## Properties

| Member | Description |
| --- | --- |
| `public bool Enabled { get; set; }` | Enables lookup; defaults to `true`. |
| `public string LocaleOverride { get; set; }` | Culture override; empty selects `TranslationServer.Culture`. |
| `public bool PseudolocalizationEnabled { get; set; }` | Enables singular-result transformation; defaults to `false`. |
| `public bool PseudolocalizationAccentsEnabled { get; set; }` | Accented Latin substitutions; defaults to `true`. |
| `public bool PseudolocalizationDoubleVowelsEnabled { get; set; }` | Doubles ASCII vowels; defaults to `false`. |
| `public bool PseudolocalizationFakeBIDIEnabled { get; set; }` | Adds bidirectional override controls; defaults to `false`. |
| `public bool PseudolocalizationOverrideEnabled { get; set; }` | Replaces unprotected characters with `*`; defaults to `false`. |
| `public bool PseudolocalizationSkipPlaceholdersEnabled { get; set; }` | Preserves common percent placeholders; defaults to `true`. |
| `public float PseudolocalizationExpansionRatio { get; set; }` | Underscore padding ratio; defaults to `0`. |
| `public string PseudolocalizationPrefix { get; set; }` | Prefix; defaults to `[`. |
| `public string PseudolocalizationSuffix { get; set; }` | Suffix; defaults to `]`. |

## Methods

| Member | Description |
| --- | --- |
| `public void AddTranslation(Translation translation)` | Registers a borrowed live catalog by identity; repeated addition has no effect. |
| `public void RemoveTranslation(Translation translation)` | Unregisters without disposing. |
| `public void Clear()` | Unregisters all catalogs without disposing them. |
| `public Translation[] GetTranslations()` | Returns an independent array in registration order. |
| `public Translation[] FindTranslations(string locale, bool exact)` | Returns exact or same-language matches. |
| `public bool HasTranslation(Translation translation)` | Tests registration by identity. |
| `public bool HasTranslationForLocale(string locale, bool exact)` | Tests locale availability. |
| `public Translation? GetTranslationObject(string locale)` | Returns the latest exact match, then the latest same-language match. |
| `public string Translate(string message, string context = "")` | Resolves singular text, including server direct entries when registered. |
| `public string TranslatePlural(string singular, string plural, long count, string context = "")` | Resolves plural text; singular source fallback is used only for count `1`. |
| `public string Pseudolocalize(string message)` | Applies configured transforms regardless of `PseudolocalizationEnabled`. |
| `protected override void Dispose(bool disposing)` | Detaches borrowed catalogs on managed disposal. |

## Member behavior

- `LocaleOverride` accepts .NET culture names and underscore separators, normalizes them to `CultureInfo.Name`, and rejects unknown names. Empty removes the override. A null value throws `ArgumentNullException`.
- `FindTranslations` and `HasTranslationForLocale` normalize the requested locale. With `exact = false`, they accept catalogs with the same two-letter language. `GetTranslationObject` tries exact culture and then any same-language catalog, preferring the latest registration at each tier. Returned arrays are snapshots, but their resource objects remain live and caller-owned.
- `Translate` and `TranslatePlural` use a case-sensitive context. Direct server entries take priority at each culture tier for a registered domain. Disabled lookups return source text. Resource plural selectors may throw their own exceptions. Singular translations, including source fallback, are pseudolocalized when enabled; plural translations are not.
- `Pseudolocalize` applies override, vowel doubling, accents, and fake BIDI in that order, followed by symmetric expansion padding and prefix/suffix. Common `%s`, `%c`, `%d`, `%o`, `%x`, `%X`, and `%f` placeholders are protected when enabled. Empty input remains empty. The expansion ratio must be finite and nonnegative; a null prefix, suffix, or message throws `ArgumentNullException`.
- A disposed domain rejects its public operations. A catalog can be disposed concurrently with lookup; lookup skips it. User resource selectors and override callbacks execute outside the server lock; direct plural selectors execute under the server lock.

## Verification and limits

`TranslationDomainTests` checks registry identity, direct-entry priority, locale override, enablement, catalog lifecycle, main-domain wrappers, and pseudolocalization. The lookup uses .NET culture ancestry and same-language matching; exact reference locale scoring, full Unicode code-point transforms, formatting and built-in non-English plural rules remain coverage gaps. This managed component has no native platform validation requirement.

## Decision

[ADR 0007](../decisions/localization.md#adr-0007) defines ownership, lookup order and typed plural policy.
