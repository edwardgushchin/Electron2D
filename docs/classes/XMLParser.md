# XMLParser

Last updated: 2026-09-24

**Inherits:** [ElectronObject](ElectronObject.md)

**Inherited By:** —

**Source:** [`src/Core/IO/XMLParser.cs`](../../src/Core/IO/XMLParser.cs)

**Namespace:** `Electron2D`
**Declaration:** `public sealed class XMLParser : ElectronObject`

## Description

`XMLParser` reads tokens from a copied UTF-8 byte buffer. It keeps the current token, attribute list, byte offset and count of consumed newline bytes. It accepts incomplete markup and does not validate the nesting or expand external entities. `Open` composes with `FileAccess` path resolution, including directory-backed `res://` and `user://`. Reading, seeking and inspection are serialized per instance. The entire input is held in memory; it is unsuitable for a frame callback or unbounded external input.

## Example

```csharp
using var parser = new XMLParser();
parser.OpenBuffer("<items><item name='a'/></items>"u8.ToArray());
while (parser.Read())
{
    if (parser.GetNodeType() == XMLParser.NodeType.Element && parser.GetNodeName() == "item")
        Console.WriteLine(parser.GetNamedAttributeValue("name"));
}
```

## Constructors and enumeration

| Signature | Behavior |
| --- | --- |
| `public XMLParser()` | Creates an unopened parser. |
| `public enum NodeType` | Current token identity. |

`NodeType`: `None = 0`, `Element = 1`, `ElementEnd = 2`, `Text = 3`, `Comment = 4`, `CDATA = 5`, `Unknown = 6`. `Unknown` represents question-mark declarations; `Comment` also covers other exclamation-mark declarations.

## Methods

| Signature | Behavior |
| --- | --- |
| [`public void OpenBuffer(byte[] buffer)`](#openbuffer) | Copies and opens UTF-8 bytes. |
| [`public void Open(string path)`](#open) | Opens an ordinary or directory-backed virtual path. |
| [`public bool Read()`](#read) | Advances one token; false at EOF. |
| [`public bool Seek(long position)`](#seek) | Reads from a byte offset. |
| [`public void SkipSection()`](#skipsection) | Reads to the matching closing token. |
| [`public NodeType GetNodeType()`](#getnodetype) | Current token kind. |
| [`public string GetNodeName()`](#getnodename) | Element name or markup content; text tokens return empty with a trace diagnostic. |
| [`public string GetNodeData()`](#getnodedata) | Current text content; other token types return empty with a trace diagnostic. |
| [`public long GetNodeOffset()`](#getnodeoffset) | Start offset of the current read. |
| [`public int GetCurrentLine()`](#getcurrentline) | Count of consumed newlines. |
| [`public bool IsEmpty()`](#isempty) | Whether the current element is self-closing. |
| [`public int GetAttributeCount()`](#getattributecount) | Current or last element's attribute count. |
| [`public string GetAttributeName(int index)`](#getattributename) | Ordered name, or empty with a trace diagnostic for an invalid index. |
| [`public string GetAttributeValue(int index)`](#getattributevalue) | Ordered value, or empty with a trace diagnostic for an invalid index. |
| [`public bool HasAttribute(string name)`](#hasattribute) | Exact name presence. |
| [`public string GetNamedAttributeValue(string name)`](#getnamedattributevalue) | First named value, or empty with a trace diagnostic when absent. |
| [`public string GetNamedAttributeValueSafe(string name)`](#getnamedattributevaluesafe) | Named value or empty text. |
| [`protected override void Dispose(bool disposing)`](#dispose) | Releases the owned input buffer. |

## Method descriptions

### OpenBuffer

Copies nonempty input and resets the byte cursor and line counter. The previous token and attribute state remain visible immediately after opening, before the next `Read`; caller edits cannot change the copied input. Null throws `ArgumentNullException`; empty input throws `ArgumentException` without replacing the old source.

### Open

Reads the complete file through `FileAccess.GetFileAsBytes`, then performs the same cursor/line reset as `OpenBuffer`. Ordinary and directory-backed `res://` paths are checked in the managed harness. Empty files throw `IOException`; path and access errors use the existing file-access exception contract. A failed read preserves the prior source and token.

### Read

Reads one token, including comments, CDATA and declarations. `false` means EOF. A short whitespace run before markup is skipped; longer whitespace is returned as text. Text and attribute values decode five XML predefined entities and valid numeric character references. An unopened parser throws `InvalidOperationException`.

### Seek

Positions at a byte offset within the copied input, then reads once. An offset outside the input throws `ArgumentOutOfRangeException`. Line count reflects consumed bytes since the last open; seeking does not recompute line numbers from the beginning.

### SkipSection

For a nonempty opening element, reads nested opening and closing tokens until the matching closing level. On a self-closing element or another token kind, it does nothing. At EOF it stops without fabricating a closing token.

### GetNodeType

Returns `None` on a new parser before its first read; reopening retains the previous token until the next read updates it. EOF does not erase the last token.

### GetNodeName

Returns the current element name, closing name, comment content, CDATA content or declaration content. On text it reports a trace diagnostic and returns empty.

### GetNodeData

Returns decoded content only for `Text`; other token kinds report a trace diagnostic and return empty.

### GetNodeOffset

Returns the byte position at which the current read began, including whitespace skipped immediately before a markup token. UTF-8 multibyte characters count by their byte width.

### GetCurrentLine

Returns the zero-based number of newline bytes consumed since the last open. It is cursor state, so calling `Seek` does not reconstruct a source line number.

### IsEmpty

True only for a self-closing current opening element.

### GetAttributeCount

Returns the number of attributes in the current or last element. Text, comment, CDATA and unknown tokens retain the previous element's attribute list; the next opening or closing element replaces or clears it.

### GetAttributeName

Returns an attribute name by source order. Invalid indices report a trace diagnostic and return empty.

### GetAttributeValue

Returns the decoded value at a source-order index. Invalid indices report a trace diagnostic and return empty.

### HasAttribute

Uses exact, case-sensitive comparison. Null names throw `ArgumentNullException`.

### GetNamedAttributeValue

Returns the first matching value; a missing name reports a trace diagnostic and returns empty. Null throws `ArgumentNullException`.

### GetNamedAttributeValueSafe

Returns the first matching value or empty text when absent. Null throws `ArgumentNullException`.

### Dispose

Deterministic disposal releases the copied input and rejects later operations through the inherited object lifecycle.

## Verification and limitations

[`XMLParserTests`](../../tests/Electron2D.Tests/XMLParserTests.cs) covers all seven token identities, getter values and invalid-call diagnostics, ordered/duplicate attributes, retained attributes across non-element tokens, byte offsets and lines, seeking, section skipping, ordinary/virtual file reads, input copying, failed/open reopening and disposal. Getter, enum and open rows are Implemented in [coverage](../coverage/classes/XMLParser.md). Read, Seek, SkipSection and the class aggregate remain Partial until malformed byte, declaration, entity and cursor edges are compared. This type does not provide a DTD, external-entity resolver, document tree or streaming input.

## Decisions

- [ADR 0003: Managed object lifetime](../decisions/core-object-runtime.md#adr-0003)
- [ADR 0020: File access and exceptions](../decisions/core-data-io.md#adr-0020)
