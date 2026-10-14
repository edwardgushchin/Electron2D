# WebP image saving

Last updated: 2026-10-14

`Image.SaveWebPToBuffer(bool lossy = false, float quality = 0.75f)` returns owned bytes. `SaveWebP(string path, bool lossy = false, float quality = 0.75f)` atomically replaces a file after encoding succeeds. Both use a stable image snapshot, never emit `Changed`, and preserve the source format, compressed blocks and complete mip chain.

Lossless encoding preserves every RGBA8 byte, including RGB beneath zero alpha. Lossy encoding uses quality in `[0, 1]`; quality one still selects a lossy VP8 bitstream and preserves alpha exactly. Nonfinite or out-of-range quality throws `ArgumentOutOfRangeException` in either mode. Lossless mode ignores valid quality values and instead uses the typed project setting below. This exception/range validation is the existing managed image convention.

Every uncompressed format converts through the existing RGBA8 conversion semantics. Supported BC formats decompress through the portable decoder first; other compressed families reject. Only the base level is encoded. The format maximum is 16383 in either dimension. Base RGBA8 expansion and emitted output are each bounded to 64 MiB; the writer checks remaining capacity before copying native output. Pixel import, writer callbacks and failure cleanup are cold authoring/I/O operations, independent of render replay.

| Setting | Definition | Default | Range |
| --- | --- | --- | --- |
| `rendering/textures/webp_compression/compression_method` | `ProjectSettings.WebPCompressionMethod` (`int`) | 2 | 0–6 |
| `rendering/textures/webp_compression/lossless_compression_factor` | `ProjectSettings.WebPLosslessCompressionFactor` (`float`) | 25 | finite 0–100 |

These registered definitions use the normal typed persistence and feature overrides. Values are sampled for each save, so changes affect subsequent encodes. Lossless effort controls cost/size, never pixel fidelity.

## Native boundary and delivery

`tools/webp-native` supplies the private C ABI `e2d_webp_encode`; the managed binding stays inside `Electron2D.dll`. Its native image owns all temporary codec memory and frees it on success, pixel-import failure and writer rejection. Callback failures return through a managed output context and never unwind across native frames. Each concurrent call owns its own picture, stream and handle.

Desktop/Android producers statically link libwebp into `Electron2DWebP`; Apple device and browser producers deliver the bridge and codec archives. The source producer pins libwebp 1.6.0 (`https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0.tar.gz`, SHA-256 `e4ab7009bf0629fd11982d4c2aa83964cf244cffba7347ecd39019a9e38c4564`). Browser uses the existing pinned SDL_image codec target when present to avoid a duplicate codec. Existing `licence/libwebp-COPYING.txt` supplies its unchanged BSD notice. No upstream source is patched.

The SDL_image save wrapper does not select ARGB before importing its pixels, and its quality-100 path also conflates lossy quality one with lossless. The private encoder explicitly selects ARGB, exact transparent RGB, mode and compression effort. The advanced mature codec performs compression; Electron2D does not implement the bitstream.

Source builds use `Electron2DBuildNativeFromSource=true`; prebuilt consumers require a newly produced platform bundle containing the bridge. Older bundles lack this asset. Native receipts include its source recipe and required export. Local Linux verification does not establish package publication or Windows/macOS/mobile/browser execution under ADR 0021.

## Verification

`ImageWebPTests` covers nine lossless method/effort combinations, three lossy quality endpoints, all 25 uncompressed formats, BC conversion, mip/source/event preservation, exact maximum width, expanded byte budget, invalid/empty/disposed/unsupported inputs, twelve concurrent saves, virtual/absolute paths, atomic failure and a fresh-process texture archive. GPU and Compatibility profiles render a saved texture and round-trip an actual captured frame exactly.

`Electron2DWebPCheck` independently decodes bridge output through the official libwebp decoder at all seven methods, repeats 128 writer failures and rejects invalid input layout. AddressSanitizer with leak detection verifies failure cleanup on Linux. A newly packed native bundle passes separate public game consumers with native build tools blocked, for both explicit RID and default-host builds/publishes. Native package producer checks remain separate from target execution and acceptance.

KTX loading remains unimplemented and requires a mature KTX2/Basis-aware container/transcoder integration, with complete mip/format/orientation semantics and actual texture consumers. A KTX1-only reader would not satisfy that loader. ETC/EAC/ASTC still require their selected portable codec families; further image source-channel/color-metadata semantics remain in coverage.

The selected Linux publish audit now accounts for 70 ELF files, including the existing mesh bridge and this codec, and 76 exact notice files. Runtime adaptation notice links point to the shipped `CanvasStyleGeometry-LICENSE.txt`; comparison-only data keeps its source-only notice.
