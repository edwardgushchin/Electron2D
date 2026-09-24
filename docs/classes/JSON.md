# JSON

Last updated: 2026-09-24

**Inherits:** [Resource](Resource.md)
**Inherited By:** —
**Source:** [`src/Core/IO/JSON.cs`](../../src/Core/IO/JSON.cs)
**Namespace:** `Electron2D`
**Declaration:** `public sealed class JSON : Resource`

## Description

`JSON` owns one mutable `System.Text.Json.Nodes.JsonNode` document and optional retained source text. It parses, formats, reports syntax errors, and converts between JSON and explicitly typed C# models. JSON nodes are confined to this document API. They are not `Variant` and are not accepted as `ConfigFile` or `ProjectSettings` values. See [ADR 0048](../decisions/core-data-io.md#adr-0048).

## Example

```csharp
using var document = new Electron2D.JSON();
if (!document.Parse("{\"speed\":12}", keepText: true))
    throw new FormatException(document.GetErrorMessage());
int speed = Electron2D.JSON.ToNative<int>(document.Data!["speed"]);
string output = Electron2D.JSON.Stringify(document.Data, indent: "  ");
```

## Public members

| Member | Behavior |
| --- | --- |
| `JSON()` | Creates an empty resource. |
| `JsonNode? Data { get; set; }` | Getter returns the live owned tree. Setter deep-copies input and clears diagnostics and retained text. Null also represents JSON null or a failed/unopened parse. |
| `bool Parse(string jsonText, bool keepText = false)` | Replaces data; returns false for malformed text and records the parser diagnostic. Accepts scalar roots and trailing commas. |
| `static JsonNode? ParseString(string jsonText)` | Parses a single document without a resource; returns null for malformed text or a valid JSON null. |
| `int GetErrorLine()` | Zero-based parser error line, or zero after success. |
| `string GetErrorMessage()` | Last parser error, or empty after success. |
| `string GetParsedText()` | Verbatim last input only when `keepText` was true; empty after parsing without retention or assigning `Data`. |
| `static string Stringify(JsonNode? data, string indent = "", bool sortKeys = true, bool fullPrecision = false)` | Formats a JSON value; null emits `null`. Keys sort by Unicode scalar value by default; arbitrary indent text repeats by depth. |
| `static JsonNode? FromNative<T>(T value)` | Serializes an explicitly typed scalar, collection, or model to an independent JSON tree. |
| `static T? ToNative<T>(JsonNode? json)` | Deserializes a tree to a caller-selected concrete type. JSON null yields that type's default, including zero for `int`. |

## Ownership, errors and limits

The returned `Data` tree is live and mutable; callers must synchronize their own edits. Assigning `Data` and duplicating the resource deep-copy its tree. Disposal invalidates instance access. `Parse` leaves `Data` null on failure and stores the managed parser's error message and zero-based line. Success clears previous diagnostics. Success and failure retain the attempted source only with `keepText: true`; parsing without retention and assigning `Data` clear it. This state reset follows [ADR 0048](../decisions/core-data-io.md#adr-0048). Use `Parse` when a valid JSON null must be distinguished from an error.

Native conversion reuses the typed configuration value schemas. An `object` root or member and any engine object are rejected; arbitrary runtime type names are never constructed from JSON. Invalid model data can throw `JsonException` or a managed serialization exception. `Stringify` limits traversal to 1024 nested levels and throws beyond that limit. Parsed numbers use floating text, while explicitly typed integer nodes remain integers. Floating zero emits `0.0`; ordinary precision uses fixed decimal rounding with at most 32 fractional places, and full precision uses the managed shortest round-trip digits. Nonfinite float/double nodes format as `null` for NaN and `±1e99999` for infinity. Strings preserve Unicode and use the document escape set, including `\v` for vertical tab, which strict JSON parsers may reject. Parsing and formatting allocate and do not belong in a frame hot path.

The managed parser differs from the reference in permissive syntax, Unicode recovery and diagnostics. Full-precision decimal digits and depth-limit failure text can also differ. Engine-object conversion and the universal value protocol are absent by the accepted typed boundary. The [coverage page](../coverage/classes/JSON.md) keeps parsing, diagnostic values, formatting, and the class aggregate Partial while their applicable behavior remains open.

## Verification

[`JsonTests`](../../tests/Electron2D.Tests/JsonTests.cs) checks parsing, trailing commas, scalars, Unicode order and escaping, numeric modes, depth, diagnostics, typed conversion, duplicate ownership, excluded native types and disposal in the managed executable harness.
