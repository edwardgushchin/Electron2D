# OptimizedTranslation

Last updated: 2026-09-24

**Inherits:** [Translation](Translation.md) → [Resource](Resource.md)

**Inherited By:** —

- **Source:** [`src/Core/String/OptimizedTranslation.cs`](../../src/Core/String/OptimizedTranslation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class OptimizedTranslation : Translation`

## Description

Stores translated values as UTF-8 bytes, optionally compressed with Brotli, under 128-bit hashes of their source strings. The source strings are discarded after `Generate`. Lookup is case-sensitive; contextual entries are skipped during generation, and plural entries keep only their first form. Plural lookup returns that singular translation. Compressed values are decoded once on first lookup and cached for subsequent calls. The catalog can be registered with `TranslationServer` and follows the inherited managed `Resource` lifetime and locale contract.

This in-memory representation is not a serialized catalog format. Generation is available to tools using the shared engine assembly. Asset-file loading and the reference Smaz binary representation are not implemented.

## Example

```csharp
using var source = new Translation { Locale = "fr" };
source.AddMessage("Play", "Jouer");
using var compact = new OptimizedTranslation();
if (compact.Generate(source))
    TranslationServer.AddTranslation(compact);
// Remove the borrowed catalog before disposing it when lookup is no longer needed.
TranslationServer.RemoveTranslation(compact);
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

Reads the source's public message list and skips keys containing the context separator. For each remaining key it copies the first translated form, hashes the source text, and stores the translated UTF-8 bytes. A value is compressed only when Brotli makes it smaller. An empty source or one containing only contextual keys returns false and retains the previous catalog. On success the locale is copied and `Resource.Changed` is emitted. Generation allocates and is intended for asset authoring, not a frame callback. Disposed resources and exceptions from source lookups or change observers propagate.

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

`VerifyTranslations` in [`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) covers long and short UTF-8 values, contextual and plural behavior, no-key generation, lookup after source edits, duplication, disposal, and registration with `TranslationServer`. After 64 warm-up lookups, 1,024 direct successful lookups across both stored values allocated 0 managed bytes on Linux/.NET 8. This does not measure native or external cryptographic allocations, the entire `TranslationServer` route, other platforms, or memory saved by compression. Exact reference compression bytes, file persistence, and editor-only generation gating remain open in [coverage](../coverage/classes/OptimizedTranslation.md).

## Decisions

- [ADR 0007: Typed localization](../decisions/localization.md#adr-0007)
- [ADR 0013: Managed Resource](../decisions/resources.md#adr-0013)
- [ADR 0014: Runtime allocation budget](../decisions/resources.md#adr-0014)
