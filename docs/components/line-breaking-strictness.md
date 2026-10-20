# Project line-breaking strictness

Last updated: 2026-10-21

## Scope and selection

This text/configuration slice connects one stored project definition to executable native line boundaries shared by Font layouts, TextServer buffers, TextLine/TextParagraph and scene text controls. It uses the existing private ICU 78.3 bridge and bounded process cache; no native ABI, vendored algorithm or new renderer is added. MIDI remains dependent on the separate user-approved domain/native-host decision explicitly required by ADR 0038. Raw viewport anisotropy would add a smaller server-authoring entry point over the scene sampler policy that already executes.

## Public workflow

```csharp
// Configure or load the project before first nonempty native line analysis.
ProjectSettings.Set(ProjectSettings.LineBreakingStrictness, 3);
using var paragraph = new TextParagraph { Width = 180 };
paragraph.AddString("あぁい", font, 16, "ja");
// A supplied option-bearing language preserves its own native locale rules.
paragraph.Clear();
paragraph.AddString("あぁい", font, 16, "ja@lb=loose");
```

`public static ProjectSetting<int> LineBreakingStrictness { get; }` defines internationalization/locale/line_breaking_strictness, initially 0. Values are Auto=0, Loose=1, Normal=2 and Strict=3; other values reject before base or feature storage changes. Every independent ProjectSettingsRegistry registers this advanced definition and its restart-required metadata. Ordinary persistence, initial values, reset and active feature overrides apply. Only the process registry supplies the runtime policy.

## Runtime flow and invariants

[NativeTextBreak](../classes/NativeTextBreak.md) captures the effective feature value at the first nonempty line-boundary request, under its existing native cursor lock. Creating/discovering TextServer, calling unrelated Unicode helpers and analyzing empty text do not capture it. Subsequent project edits remain authorable/persistable but require a new process for native analysis to use them, including on later cache misses. This is the lazy native initialization adaptation of the retained text implementation's startup configuration; it is not a live layout-cache invalidation switch.

The supplied language, or TranslationServer.GetToolLocale when empty, selects the requested locale. If it contains no `@`, Loose/Normal/Strict append the matching `@lb=...` option for native iterator creation. Auto forwards the locale unchanged and delegates to native locale rules; no independent width heuristic is invented. Any explicit locale options suppress default injection, including an option set that does not itself contain `lb`, preserving the pinned factory contract. Word iterators remain root-locale dictionary analysis and do not receive line-strictness options.

The existing 64-entry cache stays keyed by requested locale, so effective options are constructed only on a cache miss. Requested aliases can occupy independent entries with identical native rules; this private ownership choice preserves bounded lifetime and allocation-free warm calls. Eviction closes its own native cursor. Native handles and borrowed pinned text remain internal, and both cursors reset to empty input before unpinning. Out-of-range project data never reaches this branch because the typed definition rejects it.

The same Fill path supplies base text and each retained style-language segment. Font, public shaped buffers, paragraph wrapping, Label and other existing controls therefore consume the policy without separate implementations or public flags. Explicit per-language options override the process default. Strictness changes legal line opportunities, rather than glyph shaping, direction, word segmentation or font fallback ownership. Existing Word versus adaptive/grapheme fallback policies retain their own meanings.

## Verification and limits

[LineBreakingStrictnessTests](../../tests/Electron2D.Tests/LineBreakingStrictnessTests.cs) selects fresh processes through ELECTRON2D_TEST_LINE_STRICTNESS=managed, gpu or compatibility and ELECTRON2D_STRICTNESS_MODE=0..3. ELECTRON2D_STRICTNESS_FEATURE=1 selects an effective feature override over a different base value. Tests cover defaults/metadata/bounds, independent save/load/reset/features, unrelated and empty native initialization, capture and later edits/cache misses, explicit option precedence, Japanese small kana/prolonged marks/middle dot, hard breaks, Thai dictionary output, native scalar projection, concurrent calls, public TextServer/TextParagraph/scene Label consumers and 1024 prepared boundary calls at zero owner/all-thread managed bytes.

The independent native C ABI oracle under bin/line-break-strictness/2026-10-21/native-oracle.json uses the same pinned ICU data directly, outside the C# policy resolver. Its Japanese vectors distinguish Loose from Normal and Strict; original upstream metadata/factory input is retained in reference/. Auto preserves native default tailoring. New programmatic font/paragraph consumers use the existing pinned CJK font fixture.

Local software Wayland/X11 GPU and Compatibility profiles compare actual project-default paragraph pixels against an explicit-language reference paragraph, assert independent expected line counts and visible CJK glyphs, and execute 64 prepared retained native text frames without owner-thread managed allocation. Existing default text/native-break/font/layout/control profiles remain separate regressions. Test artifacts are retained under bin/line-break-strictness/2026-10-21.

Physical GPU output, native allocation totals, foreign-platform execution/package acceptance and human/editor acceptance are unverified. Existing native package/compiler prerequisites remain unchanged. See [ADR 0046](../decisions/rendering.md#adr-0046), [typed project settings](../decisions/core-data-io.md#adr-0019) and [platform verification](../platform-verification.md).
