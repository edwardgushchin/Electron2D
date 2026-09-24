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
| `bool Parse(string jsonText, bool keepText = false)` | Replaces data; returns false for malformed text and records the document parser's diagnostic. Accepts scalar roots, trailing commas, raw line breaks in strings and permissive number tokens with 18-digit conversion. |
| `static JsonNode? ParseString(string jsonText)` | Uses the same document syntax without a resource; returns null for malformed text or a valid JSON null. |
| `int GetErrorLine()` | Zero-based parser error line, or zero after success. |
| `string GetErrorMessage()` | Last parser error, or empty after success. |
| `string GetParsedText()` | Verbatim last input only when `keepText` was true; empty after parsing without retention or assigning `Data`. |
| `static string Stringify(JsonNode? data, string indent = "", bool sortKeys = true, bool fullPrecision = false)` | Formats a document value; null emits `null`. Keys sort by Unicode scalar value by default; arbitrary indent text repeats by depth. Values past depth 1024 emit `...` and report a trace diagnostic. |
| `static JsonNode? FromNative<T>(T value)` | Serializes an explicitly typed scalar, collection, or model to an independent JSON tree. |
| `static T? ToNative<T>(JsonNode? json)` | Deserializes a tree to a caller-selected concrete type. JSON null yields that type's default, including zero for `int`. |

## Ownership, errors and limits

The returned `Data` tree is live and mutable; callers must synchronize their own edits. Assigning `Data` and duplicating the resource deep-copy its tree. Disposal invalidates instance access. `Parse` leaves `Data` null on failure and stores the document parser's error message and zero-based line. Literal line feeds, including those inside strings, increment the line; escaped `\n` does not. Success clears previous diagnostics. Success and failure retain the attempted source only with `keepText: true`; parsing without retention and assigning `Data` clear it. This state reset follows [ADR 0048](../decisions/core-data-io.md#adr-0048). Use `Parse` when a valid JSON null must be distinguished from an error.

Native conversion reuses the typed configuration value schemas. An `object` root or member and any engine object are rejected; arbitrary runtime type names are never constructed from JSON. Invalid model data can throw `JsonException` or a managed serialization exception. `Stringify` emits `...` beyond 1024 levels and reports through `System.Diagnostics.Trace`; this truncated text is not valid JSON. Parsed numbers use floating text, while explicitly typed integer nodes remain integers. Floating zero emits `0.0`; ordinary precision uses fixed decimal rounding with at most 32 fractional places, and full precision uses the bundled MIT-licensed Grisu2 digit path. Nonfinite float/double nodes format as `null` for NaN and `±1e99999` for infinity. Strings preserve Unicode and use the document escape set, including `\v` for vertical tab, which strict JSON parsers may reject. Parsing and formatting allocate and do not belong in a frame hot path.

The parser's permissive syntax, escaped surrogate pairs, structural diagnostics, 18-digit numeric conversion and depth boundary are tested. A comparison of 5008 numeric texts with the pinned conversion found no differing `double` bits. Exponent text beyond the native integer range is safely capped under ADR 0048. Formatting matched 100,000 sampled binary doubles and 10,000 widened floats in both precision modes; tests also cover sorting, escapes, indentation and depth truncation. Engine-object conversion and the universal value protocol are absent by the accepted typed boundary. The [coverage page](../coverage/classes/JSON.md) records all applicable own members as Implemented.

## Verification

[`JsonTests`](../../tests/Electron2D.Tests/JsonTests.cs) checks common permissive and malformed syntax, escaped Unicode pairs, number normalization, line diagnostics, both parse entry points, 1024-level depth, formatted digits, truncation and trace, typed conversion, duplicate ownership, excluded native types and disposal in the managed executable harness. Separate pinned-source differential probes compared 100,000 double values and 10,000 widened float values without text differences in either precision mode.
