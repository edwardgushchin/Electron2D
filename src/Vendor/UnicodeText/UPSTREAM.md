# Unicode text algorithm source

This directory contains the bounded Unicode runtime dependency closure from
[Avalonia 12.1.6](https://github.com/AvaloniaUI/Avalonia/tree/cf8e564674700348947e93ab19105f27afe859a9),
commit `cf8e564674700348947e93ab19105f27afe859a9`, with Unicode Character Database
**17.0.0** tables. It compiles internally into `Electron2D.dll`. No Avalonia
assembly, UI, renderer, dependency package or native library is included.

`manifest.json` records every upstream URL and SHA-256, every adapted destination
SHA-256, and the official Unicode conformance-data provenance. The importer reads
individual files at pinned revisions. Git preserves the bytes of the vendored
files and keeps the conformance fixture in LF form on Windows. The source closure
contains the bidi algorithm/data, Unicode properties and trie lookup, grapheme, word and
line-break enumerators, required enums/data, and six small buffer/collection helpers.
The generator, trie builder implementation and UI text layout
are excluded. `UnicodeTrieBuilder.Constants.cs` is retained because trie lookup uses
those layout constants.

The SHA-256 values below cover the compressed GitHub source archives for the
two exact revisions. All 36 selected archive members per revision matched the
individual files served by GitHub, including the license.

| Release and source archive | SHA-256 |
| --- | --- |
| [12.1.3 tar.gz](https://codeload.github.com/AvaloniaUI/Avalonia/tar.gz/8eeda4f6f546165b3f72e63c9f42247abb306905) | `9109d00f8cfb7d38962d66ff1eb55fce8b144162111239f8f4f5d57dd91d6fcb` |
| [12.1.6 tar.gz](https://codeload.github.com/AvaloniaUI/Avalonia/tar.gz/cf8e564674700348947e93ab19105f27afe859a9) | `7df88dd44b1abdcb825e1b044ed28ca021f0379ad468dda07b6cba7b259ebdfc` |

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

Run the bounded fixture from the repository root.

```sh
dotnet run --project tests/Electron2D.Tests -c Release -- --unicode-text
```

Pass the generated full fixture as the next argument to execute the full corpus.

## Engine entry points and verified integration

The upgrade changes three imported files: `BiDiAlgorithm.cs` tracks the
preceding strong class in one pass for W2, `BiDiData.cs` re-detects embedding
and isolate flags after reset, and `LineBreakEnumerator.cs` adds an optional
scan limit. Electron2D uses the original unbounded constructor. The Unicode 17
tables, script data, grapheme code and imported licenses have identical upstream
bytes in both releases.

Use the engine-owned `TextBIDI.Resolve` integration for bidi levels. It calls
the full overload with unknown hints. The upstream `SetPairedBracketDirection`
still writes the N0 direction for following NSMs into the original-class buffer.
The engine identifies those annotations by comparing the original Unicode NSM
class with the core's L/R annotation and assigns those marks the level of the
preceding bracket. Other classes and unannotated NSMs retain the core result.

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
