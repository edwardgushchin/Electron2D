# JSON documents component

Last updated: 2026-09-24

## Scope and owned type

This Core component owns [`JSON`](../classes/JSON.md), a mutable document resource for parsing, formatting, diagnostics and typed native conversion. Its source is [`src/Core/IO/JSON.cs`](../../src/Core/IO/JSON.cs).

## Runtime flow and dependencies

`Parse` and `ParseString` share a document token parser that creates a `JsonNode` tree. It accepts trailing commas, raw string line breaks, permissive numeric tokens and 1024 nested levels, and reports token or structural errors with zero-based lines. Numeric tokens use 18-digit mantissa conversion before entering the tree; excessive exponent text is safely capped. Parsing without retention and assigning `Data` clear source text; a successful parse clears diagnostics. `Stringify` traverses a supplied tree with optional Unicode scalar key sorting and indentation; parsed numbers format as floats, while typed integer nodes remain integers. Full-precision floating text uses the internal MIT-licensed Grisu2 path. Generic native conversion uses the existing typed `ConfigFile` JSON schemas, returns the selected type's default for a null root, and rejects untyped or engine-object members. This component depends on Core resource lifetime and `System.Text.Json`; it adds no package or separate assembly.

## Invariants and limits

The JSON tree exists only at the document API boundary. It cannot be used as a universal engine value or stored in configuration and project settings. Setting `Data` and duplicating the resource copy their trees, but getter-returned nodes are live and caller-synchronized. Parser and formatter work is allocating and not frame-safe. A valid JSON null shares the `ParseString` null result with malformed text; use `Parse` for diagnostics. Formatting past 1024 levels emits `...` and a managed trace diagnostic; the truncated text is not valid JSON. The document escape set includes `\v`, which strict JSON parsers may reject. All applicable own rows are [Implemented](../coverage/classes/JSON.md) under ADR 0048.

## Verification and decision

[`JsonTests`](../../tests/Electron2D.Tests/JsonTests.cs) checks document state, common permissive syntax, Unicode pairs, diagnostics, depth, formatting modes and truncation, typed conversion, rejected types and failures in the managed executable harness. Pinned-source numeric probes matched 100,000 doubles and 10,000 widened floats. The architectural boundary is [ADR 0048](../decisions/core-data-io.md#adr-0048).
