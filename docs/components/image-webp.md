# WebP image I/O

Last updated: 2026-10-29

`Image.LoadWebPFromBuffer` and ordinary `.webp` file/resource loading use the private libwebp bridge. Input is copied and bounded to 64 MiB; the existing image dimension/pixel limits apply before allocating the RGBA8 result. Static images retain straight RGBA bytes, including transparent RGB. Animated input supplies its first fully reconstructed canvas, including frame offsets and blending. Decoding and native cleanup complete before atomic image replacement and one `Changed` event; malformed data preserves the previous pixels and events.

`Image.SaveWebPToBuffer(bool lossy = false, float quality = 0.75f)` returns owned bytes. `SaveWebP(string path, bool lossy = false, float quality = 0.75f)` atomically replaces a file after encoding succeeds. Both use a stable image snapshot, never emit `Changed`, and preserve the source format, compressed blocks and complete mip chain.

Lossless encoding preserves every RGBA8 byte, including RGB beneath zero alpha. Lossy encoding uses quality in `[0, 1]`; quality one still selects a lossy VP8 bitstream and preserves alpha exactly. Nonfinite or out-of-range quality throws `ArgumentOutOfRangeException` in either mode. Lossless mode ignores valid quality values and instead uses the typed project setting below. This exception/range validation is the existing managed image convention.

Every uncompressed format converts through the existing RGBA8 conversion semantics. Supported BC formats decompress through the portable decoder first; other compressed families reject. Only the base level is encoded. The format maximum is 16383 in either dimension. Base RGBA8 expansion and emitted output are each bounded to 64 MiB; the writer checks remaining capacity before copying native output. Pixel import, writer callbacks and failure cleanup are cold authoring/I/O operations, independent of render replay.

| Setting | Definition | Default | Range |
| --- | --- | --- | --- |
| `rendering/textures/webp_compression/compression_method` | `ProjectSettings.WebPCompressionMethod` (`int`) | 2 | 0–6 |
| `rendering/textures/webp_compression/lossless_compression_factor` | `ProjectSettings.WebPLosslessCompressionFactor` (`float`) | 25 | finite 0–100 |

These registered definitions use the normal typed persistence and feature overrides. Values are sampled for each save, so changes affect subsequent encodes. Lossless effort controls cost/size, never pixel fidelity.

## Native boundary and delivery

`tools/webp-native` supplies the private C ABI `e2d_webp_encode`, `e2d_webp_decode` and allocator registration `e2d_webp_memory`; the managed bindings stay inside `Electron2D.dll`. Static decoding writes into a size-checked borrowed result buffer. Animation decoding copies the first reconstructed canvas before deleting its native decoder on every path. Encoding owns all temporary codec memory and frees it on success, pixel-import failure and writer rejection. Callback failures return through a managed output context and never unwind across native frames. Each concurrent call owns its own codec state.

Desktop/Android producers statically link libwebp and its demux/animation decoder into `Electron2DWebP`; Apple device and browser producers deliver the bridge, codec and demux archives. The source producer pins libwebp 1.6.0 (`https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0.tar.gz`, SHA-256 `e4ab7009bf0629fd11982d4c2aa83964cf244cffba7347ecd39019a9e38c4564`). Browser uses the existing pinned SDL_image codec target when present to avoid a duplicate codec. Existing `licence/libwebp-COPYING.txt` supplies its unchanged BSD notice. The build generates two allocator-only source overrides from hash-checked libwebp utility and SharpYUV inputs; downloaded sources remain pristine.

Before codec use, the engine prepares its existing native ledger and registers one immutable allocation/free callback pair. Repeated identical registration is concurrent-safe; incomplete or different ownership rejects. The complete utility allocator and SharpYUV scratch domain publish requested payload current/peak at actual native ownership boundaries, including allocation failure and transient storage between reads. Native OOM returns null; no managed exception crosses the allocator ABI. Codec completion releases its scratch storage. This accounts CPU native payload, excluding managed pixel/output arrays, wrapper/header bookkeeping and GPU resources. It does not complete either whole-memory monitor. See [the allocation qualification](../coverage/performance-producers.md#webp-owned-allocation-domain-2026-10-29).

The shipped macOS SDL_image cannot load WebP, as confirmed by the connected package consumer. The private decoder therefore serves the complete loading family on every target. The SDL_image save wrapper does not select ARGB before importing its pixels, and its quality-100 path also conflates lossy quality one with lossless. The private encoder explicitly selects ARGB, exact transparent RGB, mode and compression effort. The mature codec performs decoding, animation reconstruction and compression; Electron2D does not implement the bitstream.

Source builds use `Electron2DBuildNativeFromSource=true`; prebuilt consumers require a newly produced platform bundle containing all three bridge entry points. An older bundle rejects before entering the codec, preserving the existing image. Native receipts include the source recipe, required exports and static demux closure. Local Linux verification does not establish package publication or Windows/macOS/mobile/browser execution under ADR 0021.

## Verification

`ImageWebPTests` covers nine lossless method/effort combinations, three lossy quality endpoints, all 25 uncompressed formats, BC conversion, mip/source/event preservation, exact maximum width, expanded byte budget, invalid/empty/disposed/unsupported inputs, twelve concurrent saves, virtual/absolute paths, atomic failure and a fresh-process texture archive. GPU and Compatibility profiles render a saved texture and round-trip an actual captured frame exactly.

`ImageCodecTests` includes a libwebpmux-generated animation whose first red 2x2 frame is offset to (2, 2) inside a transparent 4x4 canvas. It verifies complete canvas pixels, one event, and state preservation after truncation or corrupt frame data.

`Electron2DWebPCheck` independently decodes bridge output through the official libwebp decoder at all seven methods, checks the private decoder against those pixels with output canaries, repeats 128 writer failures and 128 animation/decode failures, and rejects invalid dimensions/capacity/input lengths. AddressSanitizer with leak detection checks native cleanup on Linux. Fresh package consumers with native build tools blocked must pass again for the changed ABI, for both explicit RID and default-host builds/publishes. Native production remains separate from target execution and acceptance.

KTX/Basis and ETC/EAC/ASTC use their separate [codec contract](image-ktx.md). Further image source-channel/color-metadata semantics remain in coverage.

The selected Linux publish audit accounts for 73 ELF files and 101 source, license and notice files. Runtime adaptation notice links point to the shipped `CanvasStyleGeometry-LICENSE.txt`; comparison-only data keeps its source-only notice.
