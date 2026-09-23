# XML parsing component

Last updated: 2026-09-23

## Scope and owned type

This Core component owns [`XMLParser`](../classes/XMLParser.md), a permissive UTF-8 token reader with byte-offset seeking and source-order attributes. The source is [`src/Core/IO/XMLParser.cs`](../../src/Core/IO/XMLParser.cs).

## Runtime flow and dependencies

`OpenBuffer` copies a nonempty byte array; `Open` reads an ordinary or directory-backed virtual path through `FileAccess`. `Read` consumes tokens from the owned bytes and updates current type, content, attributes, offset and line count. `Seek` resumes from a supplied byte offset; `SkipSection` traverses nested element tokens. The component depends only on Core object lifetime, file access, and .NET text/collection primitives.

## Invariants and limits

Calls are serialized per parser. File and buffer input allocate proportional to input size. The tokenizer accepts incomplete markup, leaves document validation to callers and never resolves external entities. The C# API uses exceptions for bad input and `bool` for EOF under the accepted Core I/O contract. No streaming or document tree is claimed.

## Verification

[`XMLParserTests`](../../tests/Electron2D.Tests/XMLParserTests.cs) runs in the executable harness and checks token order, attributes/entities, byte offsets, seek, skip, file input and lifecycle. The [coverage page](../coverage/classes/XMLParser.md) retains unverified malformed-input and exact-reference edge cases as Partial.

## Decisions

- [ADR 0003](../decisions/core-object-runtime.md#adr-0003)
- [ADR 0020](../decisions/core-data-io.md#adr-0020)
