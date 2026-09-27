# TextBIDIRange

Last updated: 2026-09-27

**Declaration:** `public readonly record struct TextBIDIRange(int Start, int End, TextDirection Direction)`

**Source:** [TextEnums.cs](../../src/Servers/Text/TextEnums.cs) · **Component:** [Text](../components/text.md)

## Description

One independent bidirectional context in a logical Unicode string. `Start` is the inclusive scalar index and `End` is exclusive; neither counts UTF-16 code units. `Direction` selects automatic, explicit LTR/RTL or inherited paragraph resolution. This is a text range, with no geometric third dimension or native handle.

[Label](Label.md) consumes ranges returned by the protected [Control](Control.md) structured-text parser. Contexts are resolved separately and shaped in the returned order. Empty parser output retains the ordinary paragraph context. Custom ranges may repeat or reorder text, but must remain within the parsed text, have `End >= Start` and use a supported direction. Invalid custom output throws before layout publication. Built-in list parsing preserves the complete delimiter length and never creates a final range beyond the input.

## API

| Member | Contract |
| --- | --- |
| `TextBIDIRange(int Start, int End, TextDirection Direction)` | Stores three typed values; the consuming parser validates text-relative bounds. |
| `int Start { get; init; }` | Inclusive Unicode-scalar start. |
| `int End { get; init; }` | Exclusive Unicode-scalar end. |
| `TextDirection Direction { get; init; }` | Independent context direction. |
| `Deconstruct(out int Start, out int End, out TextDirection Direction)` | Extracts the stored values. |
| Record equality, inequality, hashing and `ToString()` | Standard value semantics over the three fields. |

## Example

```csharp
protected override TextBIDIRange[] OnStructuredTextParser(
    IReadOnlyList<string> options, string text)
{
    // A custom label for a known two-scalar value.
    return [new(0, 1, TextDirection.Auto), new(1, 2, TextDirection.Auto)];
}
```

## Verification

[LabelTests](../../tests/Electron2D.Tests/LabelTests.cs) checks scalar offsets, URI/email contexts, multi-scalar list separators, real custom-parser layout and invalid-range recovery. The [Unicode text layer](../components/text.md) supplies independently verified bidi and segmentation algorithms.
