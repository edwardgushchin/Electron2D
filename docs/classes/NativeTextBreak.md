# NativeTextBreak

Last updated: 2026-10-21

**Declaration:** `internal static unsafe partial class NativeTextBreak` · **Source:** [NativeTextBreak.cs](../../src/Servers/Text/NativeTextBreak.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and lifetime

Loads the private ICU 78.3 / Unicode 17 boundary library and its embedded, pinned data. It owns a process-lifetime, 16-byte-aligned immutable data block, one root-locale word iterator and a bounded cache of 64 line iterators keyed by locale. Evicting a line iterator closes its native handle. A shared lock serializes mutable cursors; no iterator or native pointer escapes into the public API.

`Fill` accepts a complete UTF-16 string, a language, the scalar-to-UTF-16 offset table and reusable output spans. Line breaking uses the supplied locale or the tool-locale fallback. The effective ProjectSettings.LineBreakingStrictness is captured at the first nonempty line analysis. Auto forwards the native locale; Loose/Normal/Strict add their option only when the language has no explicit `@` options. Later edits require process restart. Options are constructed only on cache misses; empty analysis and unrelated Unicode helpers do not capture policy. See [the complete startup and option contract](../components/line-breaking-strictness.md). Word segmentation uses the root locale and includes script dictionaries. Native positions must coincide with scalar boundaries before they are written into the caller's arrays. Word status distinguishes lexical ends from punctuation/space-only segments.

Each call pins the string only while setting and reading the iterators. Both iterators are reset to empty text in `finally` before unpinning, so cached cursors cannot retain a moving managed string. Warm cache hits and caller-supplied spans allocate no managed buffers. Initial data loading, iterator construction and cache misses can allocate. This is a private process service, not a renderer, font RID service or public resource.

`IsLocaleRTL` supplies the neutral-paragraph fallback policy for `ar`, `dv`, `he`, `fa`, `ff`, `ku` and `ur`; it does not replace Unicode strong-character resolution. The managed culture-tag separator `-` is accepted alongside `_`. ICU's general locale-orientation policy is deliberately not substituted for that specified language policy.

`IsNonprinting` classifies valid Unicode scalars using the same pinned ICU data: a scalar is nonprinting when it is neither graphic nor blank. Controls and unassigned scalars therefore remain distinct from spaces, tabulation and graphic private-use characters. TextLayout caches this result once per decoded scalar before shaping. The combined private text library exposes fifteen engine-owned C entrypoints, including Unicode transforms/properties and MSDF generation/release; packaging checks use the canonical TEXT_EXPORTS set.

## Packaging and verification

The Web package links the private text archive and uses its generated Mono module table. Chromium checks the independent ICU dictionary/locale/scalar/cache vectors and warmed allocation scope on one browser thread; concurrent-use checks remain on threaded runtimes. Public font wrapping also executes across repeated engine lifecycles.

iOS/tvOS use the selected target's static text bridge through `__Internal`; ICU data remains embedded in the managed runtime and its private native symbols remain separate from OS globalization. Apple test applications execute dictionary wrapping as part of repeated engine lifecycles. Their native simulator result is still required; device bundles are unsigned build-only checks.

The private library has its own identity and hides ICU symbols. It must not replace .NET globalization. Desktop packages use `runtimes/<RID>/native`, with a Linux `.so`, macOS `.dylib` or Windows `Electron2DTextBreak.dll`. [NativeLibraries](NativeLibraries.md) resolves that location; pinned data remains embedded in `Electron2D.dll`. [Build targets](../../tools/text-native.targets), [upstream manifest](../../src/Vendor/ICU/UPSTREAM.md) and [ADR 0046](../decisions/rendering.md#adr-0046) define source/data gates. Linux executes in CI. Windows x86/x64/ARM64 loading/full-suite checks are connected but pending; macOS fresh consumers passed while full-suite acceptance remains pending. Mobile/Web retain separate integration requirements.

[NativeTextBreakTests](../../tests/Electron2D.Tests/NativeTextBreakTests.cs) checks dictionary boundaries, locale tailoring, scalar positions, bounded cache eviction, concurrent use and warmed allocations. Text-layout tests additionally verify that those boundaries affect actual wrapping and justification. A Unicode default-rule conformance result alone does not prove dictionary segmentation.

Android's private `.so` and embedded ICU data are now connected to the SDK application bundle and resolver. The Android native host checks actual dictionary-driven wrapping; local x64/ARM64 runs passed. The remaining 32-bit execution and Apple/Web static-link gates are recorded in the platform matrix.

## MSDF integration

The existing private text library also contains the immutable MSDF core and its bounded generation/release exports. Break iterator initialization/data/lifetime remains independent of cold font generation. Native publish checks require the complete combined export closure and forbid global ICU imports.

## Standalone string operations

[NativeTextBreak.Unicode.cs](../../src/Servers/Text/NativeTextBreak.Unicode.cs) and [TextUnicode.cpp](../../src/Servers/Text/Native/TextUnicode.cpp) reuse the same process data owner and lock for public TextServer utilities. Per-call character/word iterators are closed before unpinning; no source cursor escapes. Native lowercase/titlecase and diacritic removal preflight explicit-length output under the 64 MiB UTF-16 budget. Per-scalar NFKD decomposition uses fixed native stack storage because removing nonzero combining classes makes cross-scalar reordering irrelevant. Identifier checks require NFC, XID/recommended scripts and complete contextual join controls; required Indic properties are checked before use. The filtered data adds ulayout.icu, with no upstream algorithm changes. Threaded tests, whole-string differential cases, missing-data failure and public rendered consumers are recorded in [the Unicode verification contract](../components/text-server.md#unicode-verification-2026-10-20). Foreign execution claims above precede these four new exports and do not establish their acceptance.
