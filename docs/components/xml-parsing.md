# XML parsing component

Last updated: 2026-09-24

## Scope and owned type

This Core component owns [`XMLParser`](../classes/XMLParser.md), a permissive UTF-8 token reader with byte-offset seeking and source-order attributes. The source is [`src/Core/IO/XMLParser.cs`](../../src/Core/IO/XMLParser.cs).

## Runtime flow and dependencies

`OpenBuffer` copies a nonempty byte array; `Open` reads an ordinary or directory-backed virtual path through `FileAccess`. Success resets position and line while the previous token remains visible immediately after opening; failure preserves the previous input. `Read` consumes tokens from the owned bytes and updates current type, content, offset and line count. Opening and closing elements replace or clear attributes; text, comment, CDATA and unknown tokens retain the last element's list. `Seek` resumes from a supplied byte offset; `SkipSection` traverses nested element tokens. The component depends only on Core object lifetime, file access, and .NET text/collection primitives.

## Invariants and limits

Calls are serialized per parser. File and buffer input allocate proportional to input size. The tokenizer accepts incomplete markup, leaves document validation to callers and never resolves external entities. Open and Seek use C# exceptions for invalid input, while invalid getter calls return empty text with trace diagnostics; Read uses `bool` for EOF. No streaming or document tree is claimed.

## Verification

[`XMLParserTests`](../../tests/Electron2D.Tests/XMLParserTests.cs) checks token identities, getter results, retained attributes, source order, invalid getter diagnostics, byte offsets, seek, skip, ordinary/virtual file input, reopen state, failed-open preservation and lifecycle. Getter, enum and open rows are Implemented; the [coverage page](../coverage/classes/XMLParser.md) retains Read/Seek/SkipSection and the class row as Partial for malformed-input and exact-cursor behavior.

## Decisions

- [ADR 0003](../decisions/core-object-runtime.md#adr-0003)
- [ADR 0020](../decisions/core-data-io.md#adr-0020)
