# Original shaping allocation profile

The desktop and private auto-hinter producers use the same HarfBuzz 15.0.0
source, `35f656a1a2bab548ea7f54a6dd3fba2b09680d14`, declared once in
`tools/native/harfbuzz.cmake`. No older native-assets package supplies desktop
shaping. Android/Apple/Web use the same pin through the font producer. Desktop
original malloc/calloc/realloc/free functions call the pure-C ledger; the private
auto-hinter retains its existing process-lifetime callback registration. Runtime
font preparation compares the actual version strings and rejects mixed bundles.

A required allocation can fail while a native function returns a fallback value
or reports success. The shared ledger therefore exposes a native-thread refusal
epoch. Font boundaries check it before publishing results. A generated source
copy also prevents a refused lazy table/function construction from becoming a
permanent empty cache entry. Both producers use the same checked source
preimage; upstream inputs are never edited. Normal missing-table fallbacks keep
their existing behavior. The auto-hinter uses its own thread-local producer for
this source repair, while desktop shaping reads the shared pure-C producer.

Desktop process caches remain counted until native exit. Their original native
callbacks can safely release storage after managed execution stops. The
FreeType callback profile retains its existing HB_NO_ATEXIT lifetime rule.
These different ownership rules must not be interchanged.

## Build and checks

Build a matching common core from `tools/enet-native` first, then:

```sh
cmake -S tools/shaping-native -B BUILD -G Ninja -DCMAKE_BUILD_TYPE=Release -DELECTRON2D_MEMORY_LIBRARY=/absolute/path/libElectron2DENet.so
cmake --build BUILD --target harfbuzz Electron2DShapingCheck --parallel 2
```

The test-only `ELECTRON2D_SHAPING_REFERENCE_PROFILE=ON` builds an unmodified
reference at the same pin. Never ship that reference or the refusal adapter.
Use another owned build directory with
`-DELECTRON2D_SHAPING_FAULT_PROFILE=ON` for the test-only refusal adapter.
Never ship that adapter. `FETCHCONTENT_SOURCE_DIR_SHAPING` can reuse a clean
checkout at the exact revision; a wrong revision or dirty source is rejected.

`check_memory.py CHECK OWNED FAULT REFERENCE CORE REPORT.json FONT...` compares all
16 roles per font, each native refusal, healthy retry and full native exit drain
against an unmodified build of the same source pin. It records exact input hashes. Repeat the fault
checker/library under ASan/UBSan with leak detection; Linux Clang requires
`-shared-libasan` and its runtime directory on the executable search path.
`test_prepare.py SOURCE/src` checks the shared source pin,
both error producers, unchanged inputs and rejection before output on drift.

Select the production library with `Electron2DHarfBuzzNativeLibrary`, and select
the matching core and rebuilt private FreeType with `Electron2DENetNativeLibrary`
and `Electron2DFontNativeLibrary`. Consumer targets replace the canonical native
destination for builds and publishes even when packaged files are newer. Do not
change installed SDK or package caches. Run `tests/NativeMemory.ICUHost` in
`shaping FONT`, `shaping-oracle FONT` and `shaping-fault FONT` modes with the
corresponding library in its owned publish directory, for CLR and strict full-trim
NativeAOT. These check public metrics, replacement/events, actual layout refusal,
same-font retry, original callback identity, loaded library/runtime identity and
prepared cache/ledger reads. The main test host's existing
`ELECTRON2D_TEST_FREETYPE_ALLOCATION=1` mode measures warmed actual shaping.

## Qualification boundary

See [the evidence and remaining limits](../../docs/coverage/performance-producers.md#desktop-shaping-allocation-domain-2026-10-29).
This source selection does not qualify an old package selection,
foreign targets, all C++/OS providers, or the composed managed/native monitor.
A native-thread refusal epoch is an error signal, not a managed heap estimate.
