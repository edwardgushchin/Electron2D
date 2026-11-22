# ICU text support

This is the unchanged bounded source closure of ICU 79.1, commit `ddc6b66926bfac6036144ca9902992a0d6d50af9`, from the [official source archive](https://github.com/unicode-org/icu/releases/download/release-79.1/icu4c-79.1-sources.tgz). The archive SHA-256 is `06fa63ecb521048f8082f12ad988f6b678d90cbd88aafce24cbc94902b191d99`. [manifest.json](manifest.json) records every copied file, the runtime and host-tool source lists, the exact data entries, and all content hashes. There are 455 source/header files (9,909,788 bytes). The checked-in upstream sources and headers are unchanged. The private build generates bounded allocation-failure guards described below. It also emits a build-only copy of ubidi.cpp with UTF-16 literals for the MessageFormat bidi tags because MSVC lacks the narrow UnicodeString constructor when conversion is disabled.

The engine's [CMake project](../../Servers/Text/Native/CMakeLists.txt) compiles common code with unused conversion, collation, formatting, service, IDNA, file I/O, transliteration and regular-expression facilities disabled. Public common headers and conditional platform headers remain available for native host builds. A small separate host-tool closure builds `e2d_icupkg` from the same pinned release; the existing unchanged spoof, script-set and cleanup sources are shared with the runtime Unicode security checks and the tool's data swap routines. The tool-only `pkgitems.cpp` translation unit enables parsing of converter headers while the runtime converter implementation remains disabled.

`data/TextBreak79.dat` is an intentional versioned runtime input, not a compiler output checked into the repository. It contains 61 items (4,414,720 bytes): all break rules, break locale resources and dictionaries, normalization data, and the accompanying common support records listed in the manifest. Its SHA-256 is `bee9f22432543da67df2039ef53b4002a3cfb8d01b8d7364cefc28aadd3994b8`. The package uses the ICU `icudt79l` table prefix and little-endian data. Thai, Lao, Khmer, Burmese and CJK dictionary models are retained. The filter excludes LSTM models. The original break-data profile is extended with ulayout.icu for Indic_Syllabic_Category in contextual identifier validation. This required property must fail explicitly when missing. The package is reproduced directly from the official ICU release archive; all 455 source/header files remain unchanged.

Run `python3 tools/update-icu-text.py --check` from the repository root to verify every source, header, license and data hash offline. Run `python3 tools/update-icu-text.py` to download the pinned source archive, validate its inputs, compile the private host packaging tool with CMake/Ninja, and reproduce the byte-identical data. `--archive /path/to/icu4c-79.1-sources.tgz` avoids downloading an already available archive. The updater does not use an installed system ICU or `icupkg`. All host-build outputs are temporary. A version update requires deliberate review of the manifest, build configuration and executable conformance checks.

The native producer links ICU into the engine module with hidden symbols under the `_79_electron2d` suffix. The engine C bridge retains its entry points and signature layout. The managed runtime requires ICU 79.1 and Unicode 18 at startup. The filtered data is embedded as `Electron2D.TextBreak.dat` in the managed engine and copied to aligned process-lifetime native storage. Rebuild matching managed and platform assets together. See [native delivery](../../../docs/native-packaging.md) for the 18 target RIDs and artifact identities.

ICU and the packaged data retain the complete [upstream license](../../../licence/ICUText-LICENSE.txt), including third-party notices for the dictionaries. The independent managed bidi, grapheme, script and casing implementations keep their own provenance and tests.

## Allocation-failure adaptations

[ICUMemory.cmake](../../Servers/Text/Native/ICUMemory.cmake) generates guarded
copies of common/{rbbi_cache,locid,dictbe,uresbund,rbbi,rbbidata,resbund}.cpp and
i18n/{uspoof_impl,uspoof}.cpp in the build directory. They preserve ordinary
segmentation and text behavior while preparing fallible locale caches before
reference-only use, propagating failed dictionary progress, checking CJK storage
and retaining partial rule/resource/security ownership through cleanup. The
bridge installs immutable process-owned allocation callbacks before ICU use.
The [producer audit](../../../docs/coverage/performance-producers.md#private-icu-allocation-domain-2026-10-29)
records accounting scope and executable failure checks. The host data packager
uses the same guarded source closure; the host tool filters the pinned database without changing individual records.

## ICU 79.1 verification

The Linux x64 host tool reproduced all 61 filtered records byte for byte.
The separate Release text modules loaded on Linux x64 and Windows x64, with
Thai and CJK dictionary boundaries and explicit-length UTS 39 skeleton
checks. The Linux symbol table shows the local `u_getVersion_79_electron2d`
symbol and has no global ICU dependency. The Linux x64 allocator oracle
injected failures at 1,184 positions and reported zero owned payload after
cleanup in every process. Windows x86 and arm64 were
cross-built as separate text DLLs and inspected for their bridge exports.
These component checks leave platform package qualification to the
[native delivery workflow](../../../docs/native-packaging.md).
