# Unicode text algorithm source

This directory contains the bounded Unicode runtime dependency closure from
[Avalonia 12.1.3](https://github.com/AvaloniaUI/Avalonia/tree/8eeda4f6f546165b3f72e63c9f42247abb306905),
commit `8eeda4f6f546165b3f72e63c9f42247abb306905`, with Unicode Character Database
**17.0.0** tables. It compiles internally into `Electron2D.dll`. No Avalonia
assembly, UI, renderer, dependency package or native library is included.

`manifest.json` records every upstream URL and SHA-256, every adapted destination
SHA-256, and the official Unicode conformance-data provenance. The source closure
contains the bidi algorithm/data, Unicode properties and trie lookup, grapheme, word and
line-break enumerators, required enums/data, and six small buffer/collection helpers.
The generator, trie builder implementation and UI text layout
are excluded. `UnicodeTrieBuilder.Constants.cs` is retained because trie lookup uses
those layout constants.

## Mechanical adaptations

- `Avalonia.Media.TextFormatting` becomes `Electron2D.TextFormatting`.
- `Avalonia.Utilities` becomes `Electron2D.TextFormatting.Utilities`.
- Public top-level dependency types become internal. Members and algorithms are unchanged.
- UTF-8 BOMs and CRLF line endings are normalized; other source formatting is retained.

Run `python3 tools/update-unicode-text.py --check` for an offline hash check.
Run `python3 tools/update-unicode-text.py` to download the pinned originals,
validate them, repeat only these adaptations and restore the recorded outputs.
All downloaded hashes and transformed hashes are checked before any replacement.
`--full-fixture /tmp/UnicodeTextConformance.full.json` also writes the complete
official test corpus for update-time verification.

## Engine entry points and verified integration

Use the engine-owned `TextBIDI.Resolve` integration in `src/Servers/Text/TextBIDI.cs`
for bidi levels. Do not call the upstream `BidiAlgorithm.Process(BidiData)` shortcut
from layout code. The imported revision has two upstream issues:

1. `BidiData.Reset` leaves false embedding/isolate hints, while `Append` only sets
   previously unknown hints. The engine calls the full overload with unknown hints,
   so each paragraph is correctly scanned even when buffers are reused.
2. `SetPairedBracketDirection` records the correct N0 direction for following NSMs
   in the original-class buffer instead of the working-class buffer. Untouched core
   results fail Unicode `BidiCharacterTest.txt` lines 85–90, 104, 110 and 115.
   The engine recognizes exactly these N0 annotations by comparing the original
   Unicode NSM class with the core's resulting L/R annotation. These marked NSMs
   receive the level of their immediately preceding paired bracket. They share its
   explicit embedding state and resolved type, so I1/I2 require the same level.
   Other classes and unannotated NSMs remain unchanged. Bracket matching, isolates,
   overrides and X9 removal are still performed by the imported core.

This integration does not modify or optimize vendored algorithms. The rule is
[UAX #9 N0's final clause](https://www.unicode.org/reports/tr9/#N0), rather than a
list of special-cased test strings. The nine original counterexamples are permanently
present in the committed conformance fixture. The test also checks that the adapter
does not change unrelated classes across embedding/isolate boundaries.

`UnicodeTextTests` verifies all 766 grapheme, 1,944 word-break and 19,338 line-break cases, plus a
deterministic bounded sample of 2,044 bidi-character cases and 3,262 bidi-class/direction
cases. Its optional fixture-path argument runs the full official corpus. Full update-time
verification passed all 91,707 bidi-character cases and 770,241 bidi-class/direction cases
with unchanged expected levels and visual order. Warm reusable bidi/grapheme/word/line-break
processing passed a 64+64-cycle zero-managed-allocation check. This is algorithm evidence,
not native glyph shaping, font, Label, renderer or platform acceptance.

Grapheme and line-break positions are UTF-16 code-unit offsets. Word segments expose both UTF-16 and scalar offsets/lengths. Bidi levels use logical
Unicode-scalar indices. Layout code must map these units explicitly. Shaping, bidi line
reordering/mirroring, font fallback and ellipsis remain engine-owned integrations.

## Licenses and attribution

Original source notices remain in their files. `LICENCES/` and matching files in
`licence/UnicodeText-*.txt` retain the complete applicable notices:

- Avalonia: MIT, copyright AvaloniaUI OÜ.
- Bidi algorithm/data and array helpers: Six Labors, Apache-2.0.
- Trie and line-break support: Topten Software/RichTextKit, Apache-2.0;
  original foliojs `unicode-trie` and `linebreak` MIT notices are also retained.
- Grapheme code adapted from .NET: .NET Foundation MIT notice.
- Unicode tables and official test data: Unicode License V3.

Official test files are pinned to the Unicode Consortium's `unicode-org/unicodetools`
mirror commit `0509b4b256ff75c65300c8aaecb9e6ec816d9520`, under `ucd/17.0.0`.
The committed JSON removes comments and records original line numbers; it never changes
expected results. The manifest retains hashes of the complete original files.
