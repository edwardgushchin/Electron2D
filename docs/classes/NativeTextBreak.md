# NativeTextBreak

Last updated: 2026-10-05

**Declaration:** `internal static unsafe partial class NativeTextBreak` · **Source:** [NativeTextBreak.cs](../../src/Servers/Text/NativeTextBreak.cs) · **Component:** [Text](../components/text.md)

## Responsibilities and lifetime

Loads the private ICU 78.3 / Unicode 17 boundary library and its embedded, pinned data. It owns a process-lifetime, 16-byte-aligned immutable data block, one root-locale word iterator and a bounded cache of 64 line iterators keyed by locale. Evicting a line iterator closes its native handle. A shared lock serializes mutable cursors; no iterator or native pointer escapes into the public API.

`Fill` accepts a complete UTF-16 string, a language, the scalar-to-UTF-16 offset table and reusable output spans. Line breaking uses the supplied locale or the tool-locale fallback. Default line strictness is automatic; an explicit locale keyword such as `ja@lb=strict` is retained. Word segmentation uses the root locale and includes script dictionaries. Native positions must coincide with scalar boundaries before they are written into the caller's arrays. Word status distinguishes lexical ends from punctuation/space-only segments.

Each call pins the string only while setting and reading the iterators. Both iterators are reset to empty text in `finally` before unpinning, so cached cursors cannot retain a moving managed string. Warm cache hits and caller-supplied spans allocate no managed buffers. Initial data loading, iterator construction and cache misses can allocate. This is a private process service, not a renderer, font RID service or public resource.

`IsLocaleRTL` supplies the neutral-paragraph fallback policy for `ar`, `dv`, `he`, `fa`, `ff`, `ku` and `ur`; it does not replace Unicode strong-character resolution. The managed culture-tag separator `-` is accepted alongside `_`. ICU's general locale-orientation policy is deliberately not substituted for that specified language policy.

`IsNonprinting` classifies valid Unicode scalars using the same pinned ICU data: a scalar is nonprinting when it is neither graphic nor blank. Controls and unassigned scalars therefore remain distinct from spaces, tabulation and graphic private-use characters. TextLayout caches this result once per decoded scalar before shaping. The private library exposes nine engine-owned C entrypoints, including this classification query.

## Packaging and verification

iOS/tvOS use the selected target's static text bridge through `__Internal`; ICU data remains embedded in the managed runtime and its private native symbols remain separate from OS globalization. Apple test applications execute dictionary wrapping as part of repeated engine lifecycles. Their native simulator result is still required; device bundles are unsigned build-only checks.

The private library has its own identity and hides ICU symbols. It must not replace .NET globalization. Desktop packages use `runtimes/<RID>/native`, with a Linux `.so`, macOS `.dylib` or Windows `Electron2DTextBreak.dll`. [NativeLibraries](NativeLibraries.md) resolves that location; pinned data remains embedded in `Electron2D.dll`. [Build targets](../../tools/text-native.targets), [upstream manifest](../../src/Vendor/ICU/UPSTREAM.md) and [ADR 0046](../decisions/rendering.md#adr-0046) define source/data gates. Linux executes in CI. Windows x86/x64/ARM64 loading/full-suite checks are connected but pending; macOS fresh consumers passed while full-suite acceptance remains pending. Mobile/Web retain separate integration requirements.

[NativeTextBreakTests](../../tests/Electron2D.Tests/NativeTextBreakTests.cs) checks dictionary boundaries, locale tailoring, scalar positions, bounded cache eviction, concurrent use and warmed allocations. Text-layout tests additionally verify that those boundaries affect actual wrapping and justification. A Unicode default-rule conformance result alone does not prove dictionary segmentation.

Android's private `.so` and embedded ICU data are now connected to the SDK application bundle and resolver. The Android native host checks actual dictionary-driven wrapping; local x64/ARM64 runs passed. The remaining 32-bit execution and Apple/Web static-link gates are recorded in the platform matrix.
