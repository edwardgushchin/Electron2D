# Translation

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md)

**Inherited By:** Custom translation resources

- **Source:** [`src/Core/String/Translation.cs`](../../src/Core/String/Translation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class Translation : Resource`

## Description

`Translation` is a mutable in-memory catalog for one locale. Each `(context, source)` key stores one or more translated strings. The catalog owns copies of plural-form lists and keeps no external resource references. Register it with [TranslationServer](TranslationServer.md) to serve `ElectronObject.Tr` and `TrN`; registration borrows the catalog, observes later edits, and ends on removal, clearing, or disposal.

Reads and writes are serialized per resource. `Resource.Changed` is raised after a mutation commits, outside the resource lock; a throwing handler does not roll it back. The managed `Resource` duplication hooks copy message containers independently and retain the plural-selector delegate reference. All public operations except construction reject a disposed resource.

## Example

```csharp
using var catalog = new Translation { Locale = "fr" };
catalog.AddMessage("Play", "Jouer", "menu");
catalog.AddPluralMessage("pear", ["poire", "poires"]);
catalog.PluralSelector = count => count > 1 ? 1 : 0;
TranslationServer.AddTranslation(catalog, "ui");
TranslationServer.Culture = CultureInfo.GetCultureInfo("fr-FR");
string label = TranslationServer.Translate("ui", "Play", "menu");
TranslationServer.RemoveTranslation(catalog, "ui");
```

The snippet assumes `using System.Globalization;` and leaves any prior process-wide culture restoration to its caller.

## Constructors

| Member | Description |
| --- | --- |
| [`public Translation()`](#constructor) | Creates an empty catalog for `en`. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Locale { get; set; }`](#locale) | Normalized culture name, initially `en`. |
| [`public Func<long, int>? PluralSelector { get; set; }`](#pluralselector) | Optional typed plural-form index selector. |

## Methods

| Member | Description |
| --- | --- |
| [`public void AddMessage(string source, string translation, string context = "")`](#addmessage) | Replaces one contextual entry with a singular form. |
| [`public void AddPluralMessage(string source, IReadOnlyList<string> translations, string context = "")`](#addpluralmessage) | Copies and replaces all translated forms of one entry. |
| [`public void EraseMessage(string source, string context = "")`](#erasemessage) | Removes a contextual entry when present. |
| [`public string GetMessage(string source, string context = "")`](#getmessage) | Resolves the first form or returns empty text. |
| [`public string GetPluralMessage(string source, string plural, long count, string context = "")`](#getpluralmessage) | Resolves one plural form or returns empty text. |
| [`public int GetMessageCount()`](#getmessagecount) | Counts contextual entries. |
| [`public string[] GetMessageList()`](#getmessagelist) | Returns copied source keys, with contexts encoded by EOT. |
| [`public string[] GetTranslatedMessageList()`](#gettranslatedmessagelist) | Returns all stored translated forms. |
| [`protected virtual string? OnGetMessage(string source, string context)`](#ongetmessage) | Derived singular lookup before local storage. |
| [`protected virtual string? OnGetPluralMessage(string source, string plural, long count, string context)`](#ongetpluralmessage) | Derived plural lookup before local storage. |
| [`protected override Resource CreateDuplicateInstance()`](#createduplicateinstance) | Creates an exact-type duplication target. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#copycustomstateto) | Copies catalog state for `Resource.Duplicate` and `CopyFromResource`. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#getpropertydescriptors) | Adds the stored `Locale` descriptor. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Clears catalog storage during deterministic disposal. |

## Property Descriptions

### Locale

`Locale` starts at `en`. Assignment normalizes underscores to hyphens and resolves the name through `CultureInfo`; a null or unsupported locale throws before mutation. The committed change raises `Resource.Changed`. The locale determines which server culture tier may use this catalog and whether English fallback plural selection applies.

### PluralSelector

`PluralSelector` returns a zero-based index into an entry's translated forms. Assigning null restores the default: English locales choose form zero for one and form one otherwise; a one-form entry always uses zero. Non-English multi-form entries require an explicit selector and throw `InvalidOperationException` without one. The selector runs outside the catalog and server locks and can propagate its own exception. A returned index outside the entry throws `InvalidOperationException`.

## Method Descriptions

### Constructor

`Translation()` creates no entries and sets `Locale` to `en` without notifying observers.

### AddMessage

Stores one translated string under the exact, case-sensitive source and context. A previous plural entry at that key is replaced. Null arguments throw; a successful assignment emits `Changed` after commitment.

### AddPluralMessage

Requires at least one non-null translated form, copies the supplied list, replaces the exact contextual key, then emits `Changed`. A null source, list, or context throws. Invalid lists are rejected before changing the resource.

### EraseMessage

Removes an exact contextual key. Missing keys do nothing; removing an existing key emits one `Changed` notification. Null arguments throw.

### GetMessage

Calls `OnGetMessage` first. A non-null result is returned even when it is empty; otherwise the first stored form is returned. Missing entries return an empty string. Null arguments throw.

### GetPluralMessage

Calls `OnGetPluralMessage` first. Without a custom result, negative counts and missing entries return an empty string. For present entries, `PluralSelector` or the English fallback chooses a form; unsupported or invalid selection throws. The source plural text is passed to the override but does not change the storage key. Null arguments throw.

### GetMessageCount

Returns the number of contextual source keys. Multiple translated plural forms count as one entry.

### GetMessageList

Returns a fresh array of source keys. A nonempty context is prefixed with `context + '\u0004'`; enumeration order is unspecified.

### GetTranslatedMessageList

Returns a fresh flattened array containing every stored translated form. Enumeration order between entries is unspecified.

### OnGetMessage

Derived catalogs may return a translation or null to continue into local storage. The callback runs outside the resource lock and may throw to the caller.

### OnGetPluralMessage

Derived catalogs may return a translation or null to continue into local plural selection. It runs before the default negative-count check and outside the resource lock.

### CreateDuplicateInstance

The base implementation constructs `Translation` and rejects a derived runtime type unless that type supplies its own exact-type factory.

### CopyCustomStateTo

Copies locale, selector reference, and independently cloned message/form containers. The catalog has no nested resources to duplicate.

### GetPropertyDescriptors

Adds `Locale` as a stored typed descriptor to the inherited `Resource` properties.

### Dispose

Clears stored entries. Registered catalogs are detached by the server's disposal observer; the server does not own or dispose them.

## Verification and limitations

`VerifyTranslations` in [`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) covers context keys, plural forms, edits after registration, parent-culture lookup, duplication, removal, disposal, invalid forms, and missing selectors. Built-in non-English plural rules, textual plural-rule expressions, asset-file loading, and full locale-identifier parity remain unimplemented; [coverage](../coverage/classes/Translation.md) retains those gaps.

## Decisions

- [ADR 0007: Typed localization](../decisions/localization.md#adr-0007)
- [ADR 0013: Managed Resource](../decisions/resources.md#adr-0013)
