# ResourceLocaleRemap

Last updated: 2026-10-21

Source: [`ResourceLocaleRemap.cs`](../../src/Core/IO/ResourceLocaleRemap.cs)
Declaration: `public sealed class ResourceLocaleRemap`
Inherits: `System.Object`

One immutable locale-specific file candidate for a logical resource. [`ProjectSettings.LocaleResourceRemaps`](ProjectSettings.md#localeresourceremaps) copies the containing array; immutable record objects may be shared. No native ownership or disposal is involved.

## Description

Sources match exact logical file paths after caller UID resolution. A selected file supplies data without replacing logical ResourcePath/cache identity. Candidates do not chain. The highest positive CompareLocales score wins; first exact or last tied nonexact wins. A missing selected file uses the original. Catalog fallback and per-domain locale overrides do not select files. See [localized resources](../components/localized-resources.md) for live refresh, errors and threading.

## Example

```csharp
ProjectSettings.Set(ProjectSettings.LocaleResourceRemaps,
    new ResourceLocaleRemap[] {
        new("res://title.png", "fr", "res://locales/fr/title.png")
    });
using var title = ResourceLoader.Load<ImageTexture>("res://title.png");
TranslationServer.Culture = System.Globalization.CultureInfo.GetCultureInfo("fr");
// title keeps its identity and logical path while its data changes.
```

The original and localized files must exist for this partial application snippet. Execute culture changes on the active SceneTree owner.

## Constructors

| Signature | Purpose |
| --- | --- |
| `public ResourceLocaleRemap(string sourcePath, string locale, string localizedPath)` | Validate and retain one candidate. |

## Properties

| Signature | Value |
| --- | --- |
| `public string SourcePath { get; }` | Exact original native or virtual file path. |
| `public string Locale { get; }` | Trimmed nonempty locale identifier. |
| `public string LocalizedPath { get; }` | Selected native, virtual or registered UID file path. |

## Constructor Descriptions

### ResourceLocaleRemap

`sourcePath` and `localizedPath` must contain 1–4096 characters, valid UTF-16, no control characters and no `::` subresource suffix. A source cannot use `uid://`; configure its resolved logical path. Targets may use a registered UID resolved at selection. `locale` must be nonblank, contain at most 128 characters, valid UTF-16 and no controls; surrounding whitespace is trimmed. Null throws ArgumentNullException; other invalid values throw ArgumentException. Scoring uses TranslationServer.CompareLocales rather than requiring a managed CultureInfo entry for every candidate.

## Property Descriptions

### SourcePath

Exact ordinal source/cache path after UID resolution. No implicit path normalization or recursive mapping is added. Configure source spelling consistently with ResourceLoader calls.

### Locale

Immutable trimmed scoring input. Only a positive score is eligible. A record does not assign application culture or catalog fallback.

### LocalizedPath

Physical input selected for decoding. Relative archive dependencies resolve beside this file. Missing selected input falls back to source; a present malformed file fails instead of silently hiding the error.

## Lifecycle, verification and decisions

Records are immutable and usable across threads. Project setters validate nonnull records and copy arrays before publication; settings edits do not perform I/O. ResourceLocales.Consumer verifies configuration isolation, persistence, selection/order, invalid strings and real format/scene consumers. NativeAOT uses compiled JSON metadata. Actual file refresh is cold allocation-capable work; foreign-platform/hardware acceptance remains separate. [ADR 0007](../decisions/localization.md#adr-0007), [ADR 0013](../decisions/resources.md#adr-0013), [ADR 0019](../decisions/core-data-io.md#adr-0019).
