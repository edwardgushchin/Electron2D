# RegExMatch

Last updated: 2026-09-23

- **Inherits:** [ElectronObject](ElectronObject.md)
- **Inherited By:** —
- **Source:** [`src/Core/String/RegExMatch.cs`](../../src/Core/String/RegExMatch.cs)
- **Namespace:** `Electron2D`
**Declaration:** `public sealed class RegExMatch : ElectronObject`

## Description

Holds one immutable match from [RegEx.Search](RegEx.md) or `SearchAll`. It snapshots each group's text and bounds plus matched named groups. Group zero is the whole match. Results do not retain their `RegEx` owner. Public empty construction is supported for the reference's empty-result behavior.

## Example

```csharp
using var expression = RegEx.CreateFromString(@"(?<word>\w+)");
using var match = expression.Search("ready");
if (match is not null)
    Console.WriteLine($"{match.GetString("word")} at {match.GetStart()}");
```

## Constructor

| Signature | Behavior |
| --- | --- |
| `public RegExMatch()` | Creates an empty result with no groups or subject. |

## Properties

| Signature | Behavior |
| --- | --- |
| [`public string Subject { get; }`](#subject) | Complete original search text. |
| [`public int GroupCount { get; }`](#groupcount) | Capturing groups, excluding group zero. |
| [`public IReadOnlyDictionary<string, int> Names { get; }`](#names) | Read-only snapshot of successfully matched name-to-number entries. |
| [`public string[] Strings { get; }`](#strings) | Fresh array of whole match and capture texts; absent captures are empty. |

## Methods

| Signature | Behavior |
| --- | --- |
| [`public int GetStart(int group = 0)`](#getstart-int) | Inclusive start of numeric group, or -1. |
| [`public int GetStart(string name)`](#getstart-string) | Inclusive start of matched named group, or -1. |
| [`public int GetEnd(int group = 0)`](#getend-int) | Exclusive end of numeric group, or -1. |
| [`public int GetEnd(string name)`](#getend-string) | Exclusive end of matched named group, or -1. |
| [`public string GetString(int group = 0)`](#getstring-int) | Captured numeric group, or empty string. |
| [`public string GetString(string name)`](#getstring-string) | Captured named group, or empty string. |

## Property descriptions

### `Subject`

The full original subject, including the suffix excluded by a bounded search. An empty result has an empty subject.

### `GroupCount`

The number of captures other than group zero; an explicitly empty result returns zero.

### `Names`

A read-only snapshot of successfully matched names and their numeric indices. Unmatched names are absent; .NET mixed-group numbering and duplicate-name behavior may differ from PCRE2.

### `Strings`

A fresh array containing the whole match followed by each capture. Unmatched captures become empty strings; changing the array cannot mutate the result.

## Method descriptions

<a id="getstart-int"></a>
### `GetStart(int)`

Returns the inclusive start in `Subject` for a numeric group, including group zero. Negative, missing and unmatched groups return -1.

<a id="getstart-string"></a>
### `GetStart(string)`

Uses a matched named group; unknown or unmatched names return -1. Null throws `ArgumentNullException`.

<a id="getend-int"></a>
### `GetEnd(int)`

Returns the exclusive end for a numeric group. Negative, missing and unmatched groups return -1.

<a id="getend-string"></a>
### `GetEnd(string)`

Uses a matched named group; unknown or unmatched names return -1. Null throws `ArgumentNullException`.

<a id="getstring-int"></a>
### `GetString(int)`

Returns the captured text of a numeric group; group zero is the whole match. Negative, missing and unmatched groups return an empty string.

<a id="getstring-string"></a>
### `GetString(string)`

Uses a matched named group; unknown or unmatched names return an empty string. Null throws `ArgumentNullException`.

All bounds are positions in `Subject`. Calls after disposal throw `ObjectDisposedException`.

The managed backend uses UTF-16 positions and its own numeric capture ordering. Mixed named and unnamed captures and duplicate names may differ from the pinned PCRE2 reference. See [RegEx](RegEx.md) and [coverage](../coverage/classes/RegExMatch.md).

## Lifecycle and verification

The result is immutable after construction. Disposal makes later property and method calls throw `ObjectDisposedException`. [RegExTests](../../tests/Electron2D.Tests/RegExTests.cs) check bounds, names, absent captures, copied data, empty construction and disposal. No native backend is involved; full PCRE2 equivalence is pending.
