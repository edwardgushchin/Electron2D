# OptimizedTranslation

Last updated: 2026-11-14

**Inherits:** [Translation](Translation.md) → [Resource](Resource.md)

**Inherited By:** —

- **Source:** [`src/Core/String/OptimizedTranslation.cs`](../../src/Core/String/OptimizedTranslation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class OptimizedTranslation : Translation`

## Description

Stores translated values as UTF-8 bytes, optionally compressed with Brotli, under 128-bit hashes of their source strings. The source strings are discarded after `Generate`. Lookup is case-sensitive; contextual entries are skipped during generation, and plural entries keep only their first form. Plural lookup returns that singular translation. Compressed values are decoded once on first lookup and cached for subsequent calls. The catalog can be registered with `TranslationServer` and follows the inherited managed `Resource` lifetime and locale contract.

Save generated catalogs as `.e2dres` with `ResourceSaver` and load them with `ResourceLoader`. The file preserves the exact catalog type, locale, hashed lookup and translated values. Runtime loading validates a complete catalog before publication. Generation is available to tools and applications using the shared engine assembly.

## Example

```csharp
using var source = new Translation { Locale = "fr" };
source.AddMessage("Play", "Jouer");
using var compact = new OptimizedTranslation();
if (!compact.Generate(source))
    throw new InvalidOperationException("No eligible translations.");
ResourceSaver.Save(compact, "res://menu.fr.e2dres");

using var catalog = ResourceLoader.Load<OptimizedTranslation>("res://menu.fr.e2dres");
TranslationServer.AddTranslation(catalog);
try
{
    TranslationServer.Culture = System.Globalization.CultureInfo.GetCultureInfo(catalog.Locale);
    Console.WriteLine(TranslationServer.Translate("", "Play"));
}
finally
{
    TranslationServer.RemoveTranslation(catalog);
}
```

## Constructors

| Member | Description |
| --- | --- |
| `public OptimizedTranslation()` | Creates an empty English catalog. |

## Methods

| Member | Description |
| --- | --- |
| `public bool Generate(Translation? source)` | Replaces this catalog from the source's plain singular messages; returns false for null or no eligible messages. |
| `public override int GetMessageCount()` | Returns zero because source keys are discarded. |
| `public override string[] GetMessageList()` | Returns an empty array because source keys are discarded. |
| `public override string[] GetTranslatedMessageList()` | Returns an independent array of decoded translations. |
| `protected override string? OnGetMessage(string source, string context)` | Resolves a hashed singular key; context is ignored. |
| `protected override string? OnGetPluralMessage(string source, string plural, long count, string context)` | Resolves the singular key for every count. |
| `protected override Resource CreateDuplicateInstance()` | Creates the exact derived resource during duplication. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies the immutable compressed lookup snapshot after the base catalog state. |
| `protected override void Dispose(bool disposing)` | Clears lookup data during deterministic disposal. |

## Method descriptions

### Generate

Reads the source's public message list and skips keys containing the context separator. For each remaining key it copies the first translated form, hashes the source text, and stores the translated UTF-8 bytes. A value is compressed only when Brotli makes it smaller. Null, an empty source or one containing only contextual keys returns false and retains the previous catalog. On success the locale and lookup commit together before `Resource.Changed`. Use generation during asset authoring. Disposed resources and exceptions from source lookups or change observers propagate. Invalid Unicode values, key collisions and budget violations fail before publication.

### Resource files

The versioned payload stores the locale, up to 65,536 key hashes with raw or Brotli values, at most 1 MiB of UTF-8 per value and at most 32 MiB expanded overall. Empty values and empty catalogs have valid representations. All payloads must pass length, uniqueness, Brotli and UTF-8 validation before the loader can publish a resource or replace cached state. Decoded value strings are cached on demand. The inherited message mutators do not feed optimized lookup and their base message storage is omitted from this asset.

### GetMessageCount and GetMessageList

Both hide source-key enumeration, including after generation. They reject a disposed resource.

### GetTranslatedMessageList

Returns a fresh array of every decoded translated value and prepares the decoded cache for later lookup. Order is unspecified. It rejects a disposed resource.

### OnGetMessage and OnGetPluralMessage

These inherited lookup hooks return the decoded value for a matching source hash and an empty string otherwise. They do not query base `Translation` message storage. Context, source plural text, and count do not affect lookup. A registered domain therefore falls back to the original source text on a missing entry.

### CreateDuplicateInstance and CopyCustomStateTo

`Resource.Duplicate` creates another `OptimizedTranslation` and copies its locale and compressed lookup. The map is independent; stored payload bytes are shared safely because neither instance mutates them. Disposal of the source or original catalog does not affect the copy.

### Dispose

Clears the lookup map. Inherited `Translation` and `Resource` cleanup and borrowed domain unregistration still run.

## Verification and limits

The public consumer encodes two archive boundary cases independently. It removes the required payload entirely and tests the 32 MiB expanded UTF-8 ceiling with valid individual entries, including exactly 32 MiB and one byte over. Failed replacement retains the original cached identity, locale and lookup. The generation check exercises the same byte boundary from a source catalog. Calls through the base `Translation` API verify hidden source enumeration after mutation and first-form plural lookup across negative, zero and positive counts with and without context.

`VerifyTranslations` in [`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) covers in-memory lookup and registration. The public [resource locale consumer](../../tests/ResourceLocales.Consumer/OptimizedCatalogChecks.cs) exercises archive authoring and fresh-process loading with raw, compressed, Unicode and empty values, endian/compression flags, copying, cache replacement and registration. Its rendering profile switches the locale through an `OptionButton` and compares the resulting `Label` pixels with literal controls. This passed on Windows x64 with GPU/Direct3D 12 and Compatibility/Direct3D 11 on an RTX 3090 Ti. Other hosts and release packages still need qualification under ADR 0021. See [coverage](../coverage/classes/OptimizedTranslation.md) for the accepted format and authoring adaptations.

## Decisions

- [ADR 0007: Typed localization](../decisions/localization.md#adr-0007)
- [ADR 0013: Managed Resource](../decisions/resources.md#adr-0013)
- [ADR 0014: Runtime allocation budget](../decisions/resources.md#adr-0014)
