# RegEx

Last updated: 2026-09-23

- **Inherits:** [ElectronObject](ElectronObject.md)
- **Inherited By:** —
- **Source:** [`src/Core/String/RegEx.cs`](../../src/Core/String/RegEx.cs)
- **Namespace:** `Electron2D`
**Declaration:** `public sealed class RegEx : ElectronObject`

## Description

Compiles one pattern and searches or replaces strings. An instance is reusable: `Compile` replaces the previous program, and `Clear` removes it. Searches snapshot the compiled program under the instance lock. Returned [RegExMatch](RegExMatch.md) objects own immutable result data and remain valid after a later compile or clear. Dispose each returned object when finished.

This implementation uses `System.Text.RegularExpressions` with culture-invariant matching and a five-second match timeout. It supports the .NET pattern and replacement dialect. The pinned reference uses PCRE2 over Unicode code points; .NET uses UTF-16 indices and has different syntax, group numbering for mixed named/unnamed captures, duplicate-name behavior, and replacement tokens. [Coverage](../coverage/classes/RegEx.md) records these as semantic gaps. Text utilities allocate and are unsuitable for a real-time frame hot path.

## Example

```csharp
using var pattern = RegEx.CreateFromString(@"(?<name>[a-z]+)-\d+");
using var match = pattern.Search("item-42");
if (match is not null)
    Console.WriteLine(match.GetString("name")); // item
```

## Constructors

| Signature | Behavior |
| --- | --- |
| `public RegEx()` | Creates an uncompiled expression. |
| `public RegEx(string pattern)` | Compiles immediately; invalid patterns throw. |

## Properties

| Signature | Behavior |
| --- | --- |
| [`public string Pattern { get; }`](#pattern) | Last attempted pattern, including a failed compile; empty after `Clear`. |
| [`public bool IsValid { get; }`](#isvalid) | Whether a compiled program exists. |
| [`public int GroupCount { get; }`](#groupcount) | Capturing group count, excluding group zero; zero when uncompiled. |

## Methods

| Signature | Behavior |
| --- | --- |
| [`public void Compile(string pattern)`](#compile) | Replaces the program; throws `ArgumentException` with diagnostics on invalid syntax. |
| [`public void Clear()`](#clear) | Removes the program and pattern text. |
| [`public static RegEx CreateFromString(string pattern)`](#createfromstring) | Constructs and compiles a caller-owned expression. |
| [`public string[] GetNames()`](#getnames) | Fresh array of named capture groups; empty when uncompiled. |
| [`public RegExMatch? Search(string subject, int offset = 0, int end = -1)`](#search) | First match or null in the region. |
| [`public RegExMatch[] SearchAll(string subject, int offset = 0, int end = -1)`](#searchall) | All nonoverlapping matches in source order. |
| [`public string Sub(string subject, string replacement, bool all = false, int offset = 0, int end = -1)`](#sub) | Replaces the first or all matches in the region. |

## Property descriptions

### `Pattern`

The last attempted pattern. It remains available after a compile error and becomes empty on `Clear`. Access is synchronized; disposal throws `ObjectDisposedException`.

### `IsValid`

True only while a compiled program exists. Invalid compilation and `Clear` make it false. Access is synchronized; disposal throws `ObjectDisposedException`.

### `GroupCount`

The number of captures excluding group zero, or zero when uncompiled. Named and unnamed captures are counted. The result depends on the .NET dialect. Access is synchronized; disposal throws `ObjectDisposedException`.

## Method descriptions

### `Compile`

Accepts a non-null .NET pattern. It clears the old program before compiling, retains the attempted `Pattern` on failure, and throws `ArgumentException` with diagnostics instead of returning a numeric error or printing it. Null throws `ArgumentNullException`; disposal throws `ObjectDisposedException`. Compilation is serialized per instance.

### `Clear`

Resets both pattern and compiled program under the instance lock. Existing match results stay valid. Disposal throws `ObjectDisposedException`.

### `CreateFromString`

Creates and compiles a caller-owned expression, with the same pattern validation and diagnostics as `Compile`. The caller disposes the returned expression.

### `GetNames`

Returns a fresh array of distinct named capture groups, or an empty array before compilation. .NET's named-group order and duplicate handling are not promised to match the pinned reference. Disposal throws `ObjectDisposedException`.

### `Search`

Returns the first match or null. It accepts a non-null subject, nonnegative inclusive `offset`, and exclusive `end`; negative `end` means the full string and values above its length are capped. The expression sees the prefix up to `end`, so `^` is relative to the original start rather than `offset`. If `offset` exceeds the effective end, it returns null. Null subject, negative offset, uncompiled state, disposal and match timeout raise the respective .NET exceptions. The caller owns the result.

### `SearchAll`

Uses the same region and validation rules as `Search`. Returns all nonoverlapping caller-owned matches in source order. Empty matches advance, and partial results are disposed if a later match throws. It returns an empty array when no match exists.

### `Sub`

Uses the same region and validation rules. It preserves the suffix after `end`. It accepts .NET replacement syntax, including `$1`, `${name}` and `$$`. The default replaces one match; `all: true` replaces all. Null replacement throws `ArgumentNullException`. A match timeout throws `RegexMatchTimeoutException` without changing the compiled program.

## Lifecycle, threading, and limits

Compilation, clearing and access to program metadata are serialized per instance. Concurrent searches operate on their captured immutable program. Disposal makes later calls fail with `ObjectDisposedException` but does not invalidate existing result objects. There is no native regex dependency. The managed [RegExTests](../../tests/Electron2D.Tests/RegExTests.cs) cover matching, group snapshots, anchors, range bounds, replacement, zero-width progress, compile failure, and disposal. PCRE2 parity and other platform runtimes remain unverified.
