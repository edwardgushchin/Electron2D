# TextCase

Last updated: 2026-09-27

- Declaration: `internal static partial class TextCase`
- Sources: [TextCase.cs](../../src/Servers/Text/TextCase.cs), [TextCase.Data.cs](../../src/Servers/Text/TextCase.Data.cs)
- Component: [canvas rendering](../components/canvas-rendering.md)
- Visibility: internal; used by text controls when uppercase text is invalidated.

## Contract

`internal static string ToUpper(string text, string language = "")` returns full Unicode uppercase. Null arguments fail. Empty text is returned directly. An empty language uses TranslationServer.GetToolLocale; controls normally supply their resolved language before calling. Locale matching accepts the relevant two/three-letter codes with ordinal case-insensitive matching and hyphen/underscore suffixes.

The generated Unicode 17.0.0 data contains 1,505 simple uppercase mappings, 102 full expansion mappings, combining-class boundaries, Soft_Dotted, Cased and Case_Ignorable properties. This handles expansions such as `ß → SS` and `ﬃ → FFI` without relying on the installed operating system's Unicode version. Turkic `tr`/`tur`/`az`/`aze` preserves dotted-I casing. Lithuanian `lt`/`lit` removes a combining dot above after a soft-dotted character only when the required combining context holds.

Greek `el`/`ell` tailoring removes the appropriate accents, preserves disjunctive standalone eta's tonos, preserves/adds diaeresis after accented vowels, and expands subscript iotas using the ICU 78.3 algorithm and tables. Armenian `hy`/`hye` expands the ech-yiwn ligature to ech-vew; other locales, including Western Armenian `hyw`, use the default Unicode expansion. Other locale codes use the default uppercase rules.

The complete managed string length is honored, including embedded NULs, and unmatched UTF-16 code units are retained around transformed text. This avoids native NUL-termination truncating unrelated input. Results that do not change the text retain the original string identity. A StringBuilder is created only on the first actual change, so unchanged warmed calls with a resolved language allocate no managed bytes. Text controls cache changed results and rerun this transform when text or language changes.

## Data ownership and regeneration

This is engine-owned casing code and generated data; it does not load native ICU or modify the vendored Unicode algorithm closure. [tools/text-case.json](../../tools/text-case.json) records immutable Unicode/ICU source revisions and every input/output SHA-256. The [update tool](../../tools/update-text-case.py) validates pinned inputs before regenerating tables and the scoped license notice:

```bash
python3 tools/update-text-case.py
python3 tools/update-text-case.py --check
```

`--source-dir /path/to/verified/input/files` permits offline regeneration from the named pinned files. `--check` checks recorded generated hashes without network access. The Unicode and ICU copyright/permission text is shipped in [TextCase-LICENSE.txt](../../licence/TextCase-LICENSE.txt).

## Verification and limits

[TextCaseTests](../../tests/Electron2D.Tests/TextCaseTests.cs) contains 348 ICU 78.3 golden profiles covering full expansions, supplementary characters, Unicode 17 additions, locale aliases, combining contexts, Greek/Armenian tailoring and embedded NULs. It checks malformed UTF-16 preservation, string identity, guards and sixty-four measured unchanged calls after sixty-four warmup calls with zero managed bytes in each tested locale.

An update-time differential passed all 1,112,064 valid Unicode scalars in six locale profiles: 6,672,384 comparisons. Greek context combinations and 50,000 seeded multilingual strings added 332,080 comparisons, for 7,004,464 exact matches against ICU 78.3 on Linux x64. ICU is an independent verification oracle only; it is not a runtime dependency. These checks establish the transform, not text-control rendering, native allocator behavior or owner acceptance. See [ADR 0046](../decisions/rendering.md#adr-0046).
