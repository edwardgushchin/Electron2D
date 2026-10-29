# RegEx

Last updated: 2026-10-29

- **Inherits:** [ElectronObject](ElectronObject.md)
- **Source:** [RegEx.cs](../../src/Core/String/RegEx.cs)
- **Declaration:** `public sealed class RegEx : ElectronObject`

Compiles a PCRE2 pattern and searches or replaces ordinary C# strings. The pinned private interpreter supports recursive/conditional/branch-reset patterns, native group numbering and Unicode code-point regions. Programs and temporary interpreter storage use the [common native allocator](../coverage/performance-producers.md#pcre2-owned-allocation-domain-2026-10-29). Programs use deterministic native ownership and one instance lock; independent instances can run concurrently. Results retain immutable captured data after program retirement. Matching allocates and may block; see [the component](../components/text-patterns.md) for exact budgets, errors, native provenance and verification boundaries.

## Example

```csharp
using var pattern = RegEx.CreateFromString(@"(?<name>[a-z]+)-\d+");
using var match = pattern.Search("item-42");
if (match is not null)
    Console.WriteLine(match.GetString("name"));
```

## Properties

| Signature | Contract |
| --- | --- |
| `public string Pattern { get; }` | [Pattern](#pattern): last attempted pattern, retained after Clear. |
| `public bool IsValid { get; }` | [IsValid](#isvalid): a compiled program currently exists. |
| `public int GroupCount { get; }` | [GroupCount](#groupcount): captures excluding group zero. |

## Methods

| Signature | Contract |
| --- | --- |
| `public RegEx()` | Creates an uncompiled owner. |
| `public RegEx(string pattern)` | Compiles immediately or throws. |
| `public void Compile(string pattern)` | [Compile](#compile) |
| `public void Clear()` | [Clear](#clear) |
| `public static RegEx CreateFromString(string pattern)` | [CreateFromString](#createfromstring) |
| `public string[] GetNames()` | [GetNames](#getnames) |
| `public RegExMatch? Search(string subject, int offset = 0, int end = -1)` | [Search](#search) |
| `public RegExMatch[] SearchAll(string subject, int offset = 0, int end = -1)` | [SearchAll](#searchall) |
| `public string Sub(string subject, string replacement, bool all = false, int offset = 0, int end = -1)` | [Sub](#sub) |
| `protected override void Dispose(bool disposing)` | Releases the native program before managed retirement. |

### Pattern

Last attempted pattern, including failed compilation. Initially empty; Clear retains it. Disposed access throws.

### IsValid

True while a compiled program exists. Failed compilation, Clear and disposal release that program.

### GroupCount

The fixed capture count excluding group zero, or zero before compilation. Named and unnamed groups follow source order; branch-reset groups retain native numbering.

### Compile

Validates non-null input before changing state, releases the previous program, stores the attempted pattern and compiles PCRE2. ArgumentException carries native diagnostics and a code-point location. Native allocation failure is distinct. Explicit UTF/UCP directives select their native character policy.

### Clear

Releases the compiled program and retains Pattern. Existing RegExMatch objects remain independent.

### CreateFromString

Constructs and compiles a caller-owned expression. Invalid patterns throw rather than return an invalid factory result or emit a separate logging switch.

### GetNames

Returns a fresh array of distinct named groups in native table order, or an empty array when uncompiled. Duplicate names remain valid; individual matches select their first participating capture.

### Search

Finds the first match, or returns null. Offset/end count code points; negative end uses the complete subject and a larger end clamps to its length. A negative offset throws; an offset beyond the region has no match. Start anchors still address the original beginning. The result retains the full original Subject. Native work/deadline failure throws, preserving the compiled program.

### SearchAll

Uses the same region contract and returns caller-owned results in source order. An empty match advances one code point; an empty final match is retained. Failure releases already produced results before rethrowing.

### Sub

Replaces the first or all native matches. Supports PCRE2 capture tokens such as $0, $&, $1 and ${name}; $$ yields a literal dollar and unset/unknown groups become empty. It preserves prefix and the suffix outside end. An offset beyond the region or invalid replacement throws. Native global empty substitution uses its own nonempty-alternative progression.

## Lifecycle and limits

Dispose every returned result and expression when finished; a private SafeHandle also releases a forgotten native program. Operations on one expression serialize, including disposal. Pattern/subject/replacement/output are bounded to 16,777,216 code points. Native match heap is 32 MiB; normal work/depth limits and a five-second callout deadline apply. All are cold preparation operations. Linux source-native, independent oracle, sanitizer, public consumer and software rendering evidence do not establish old bundle support, foreign hosts or release readiness.
