# JSON documents component

Last updated: 2026-09-23

## Scope and owned type

This Core component owns [`JSON`](../classes/JSON.md), a mutable document resource for parsing, formatting, diagnostics and typed native conversion. Its source is [`src/Core/IO/JSON.cs`](../../src/Core/IO/JSON.cs).

## Runtime flow and dependencies

`Parse` uses the .NET JSON parser to create a `JsonNode` tree and updates the resource's root, optional original text and diagnostic state together. `Stringify` traverses a supplied tree with optional ordinal key sorting and indentation. Generic native conversion uses the existing typed `ConfigFile` JSON schemas and rejects untyped or engine-object members. This component depends on Core resource lifetime and `System.Text.Json`, already part of the runtime; it adds no package.

## Invariants and limits

The JSON tree exists only at the document API boundary. It cannot be used as a universal engine value or stored in configuration and project settings. Setting `Data` and duplicating the resource copy its tree, but getter-returned nodes are live and caller-synchronized. Parser and formatter work is allocating and not frame-safe. A valid JSON null shares the `ParseString` null result with malformed text; use `Parse` for diagnostics. Reference parser permissiveness, exact text/number output, and native object conversion are Partial in [coverage](../coverage/classes/JSON.md).

## Verification and decision

[`JsonTests`](../../tests/Electron2D.Tests/JsonTests.cs) runs in the managed executable harness. The architectural boundary is [ADR 0048](../decisions/core-data-io.md#adr-0048).
