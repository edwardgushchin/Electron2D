# Third-party software

Last updated: 2026-10-05

This page lists Electron2D's direct third-party integrations and backends selected by accepted decisions. **Selected** does not mean **integrated**. The project files, vendor provenance records and shader toolchain lock are the sources for exact versions; native packages may have their own transitive dependencies.

## Integrated runtime dependencies

| Software | Role and delivery | Version source |
| --- | --- | --- |
| SDL3-CS (SDL, Image and ShaderCross bindings) | Complete managed source modules compiled internally into the single `Electron2D.dll`; no public SDL types or separate managed binding assembly. | [Vendor provenance](../src/Vendor/SDL3-CS/UPSTREAM.md): `v3.4.20.0` |
| SDL3 | Native window, input and rendering foundation. The runtime project supplies RID-specific packages transitively. | [Native pins](../tools/native-package.props): `SDL3-CS.{platform}` `3.4.20.0` (NuGet normalizes it to `3.4.20`) |
| SDL_image 3 | Native image decoding and encoding through the vendored Image bindings. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Image` `3.4.6.12`, upstream SDL_image `3.4.6` |
| SDL_shadercross 3 | Native SPIR-V translation and reflection; the Linux package also supplies DXC and SPIRV-Cross. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Shadercross` `3.0.0.13`; [shader component](components/shader-materials.md) |
| Clipper2 | Polygon clipping and offsets; nine C# source files compiled internally. | [Vendor provenance](../src/Vendor/Clipper2/UPSTREAM.txt): `2.0.1`; [license](../licence/Clipper2-LICENSE.txt) |
| FastNoiseLite | Internal managed noise algorithm; the public resource exposes 1D/2D sampling. | [Vendor provenance](../src/Vendor/FastNoiseLite/UPSTREAM.txt): `1.1.1` |
| Box2D.NET | Internal managed physics backend, including the recorded allocation adaptations. | [Vendor provenance](../src/Vendor/Box2D.NET/VENDOR.md): `3.1.654` |
| FAudio and FAudio# | Native SDL3 mix graph and internal managed binding. | [Manifest](../src/Vendor/FAudio/manifest.json): `26.11`; [audio component](components/audio-playback.md) |
| NLayer / NVorbis / qoa-fu | Internal MP3, Vorbis and QOA codecs. | Manifests: [NLayer `3.0.1`](../src/Vendor/NLayer/manifest.json), [NVorbis `0.10.5`](../src/Vendor/NVorbis/manifest.json), [qoa-fu pinned commit](../src/Vendor/QOA/manifest.json) |
| Avalonia Unicode / ICU | Managed Unicode 17 algorithms and private Unicode 18 dictionary segmentation, normalization and security. | [Avalonia `12.1.6`](../src/Vendor/UnicodeText/manifest.json), [ICU `79.1`](../src/Vendor/ICU/manifest.json); no Avalonia UI dependency |
| msdfgen | Private native core for multichannel font distance generation. | [Manifest](../src/Vendor/MSDFGen/manifest.json): [v1.13.1](https://github.com/Chlumsky/msdfgen/releases/tag/v1.13.1); [MIT license](../licence/MSDFGen-LICENSE.txt); [native delivery](native-packaging.md#msdf-font-generation) |
| FreeType / HarfBuzzSharp | Private C ABI font loading, rasterization and shaping. | [`Electron2D.csproj`](../Electron2D.csproj): MonoGame.Library.FreeType `2.13.2.5`, HarfBuzzSharp.NativeAssets `14.2.1.301`; private FreeType producer below |
| libdatachannel, libjuice, plog and usrsctp | Private desktop WebRTC data-channel, ICE, logging and SCTP sources. | [Manifest](../src/Vendor/libdatachannel/manifest.json): libdatachannel `0.24.8`, exact dependency commits and SHA-256 of the four GitHub source archives; [WebRTC component](components/webrtc.md) |
| ENet / FastLZ | Private native network transport and compression. | [ENet current pinned commit](../src/Vendor/ENet/manifest.json), [FastLZ `0.5.0` source closure](../src/Vendor/FastLZ/manifest.json) |
| meshoptimizer | Private structural simplification for ImporterMesh LODs. The common native module embeds the upstream header, allocator and simplifier. The engine bridge owns the allocator callbacks. | [Manifest](../src/Vendor/meshoptimizer/manifest.json): [v1.3](https://github.com/zeux/meshoptimizer/releases/tag/v1.3), source revision and SHA-256; [MIT license](../licence/Meshoptimizer-LICENSE.txt); [verification](native-packaging.md#importer-mesh-dependency-checkpoint) |
| PolyPartition algorithm | Adapted convex polygon part merging; no separate package or binary. | [Geometry component](components/geometry-values.md); [notice and license](../licence/PolyPartition-LICENSE.txt) |
| PCG32 algorithm | Adapted random number generator core; no separate package or binary. | [Random generation component](components/random-generation.md); [notice and license](../licence/PCG32-LICENSE.txt) |

These are direct integrations. The three pinned native SDL packages are delivered separately from `Electron2D.dll`; the single-assembly rule applies to managed code, not native binaries. The generic desktop engine reference carries all three desktop package families so NuGet selects the native files for the application RID; Android, iOS and tvOS retain target-specific references, and Android TV uses Android packages. Only Linux x64 has a current self-contained artifact audit. Browser WASM selects no SDL package until its host is implemented. See [ADR 0012](decisions/product.md#adr-0012) and [ADR 0021](decisions/product.md#adr-0021).

## Editor content

The editor bundles IBM Plex Sans Regular 3.005 (weight 400) for its startup caption. The [font and its corresponding SIL OFL 1.1 license](components/editor-startup.md) are copied into the editor's `Assets/` directory during build/publish. They are editor content and are not included in `Electron2D.dll` or game runtime assets. The component records the pinned upstream revision and font SHA-256.

## Shader import and build tools

The separate Linux x64 `ShaderImport` tool packages **glslang 16.6.0** and its upstream `known_good.json` revisions of **SPIRV-Tools** (`spirv-val`) and **SPIRV-Headers**. Revisions and archive hashes are pinned in [toolchain.lock.json](../tools/shaders/toolchain.lock.json). HLSL import uses DXC from the SDL_shadercross native package. These compilers and validators run during import/build. See the [importer README](../tools/shaders/README.md) and [ADR 0028](decisions/rendering.md#adr-0028).

## Private native producers and update audit

The [FreeType producer](../tools/font-native/CMakeLists.txt) pins FreeType `2.15.0`, zlib `1.4`, libpng `1.6.59`, Brotli `1.2.1` and HarfBuzz `15.2.0`. The [ENet producer](../tools/enet-native/CMakeLists.txt) uses the same zlib revision for Windows, Android and Web and pins Zstandard `1.5.7` where it builds Zstandard from source.

FreeType builds its WOFF2 decoder from the [Brotli 1.2.1 release commit](https://github.com/google/brotli/commit/db24a98aed38a16de78cc05ca15cdd8dc2a0d92d) using a verified Git checkout. The producer consumes no Brotli release archives. FreeType routes WOFF2 allocations through its own memory callbacks, and the Emscripten build explicitly links the decoder to Brotli Common. See the [native package layout](native-packaging.md#freetype-woff2-delivery) for the delivered libraries.

The native producers pin [zlib 1.4 to Git commit `99623ef7097352da7993ba614161fd6a311e046e`](https://github.com/madler/zlib/commit/99623ef7097352da7993ba614161fd6a311e046e). The [upstream release tarball](https://zlib.net/fossils/zlib-1.4.tar.gz) has a published SHA-256 of `02dd159783677a749c04adbdd02b55b8a2bcc7e054f51b76b549199a916ad584`. The downloaded tarball matched this checksum. Producer builds fetch the identified Git revision. The separate .NET compression producer uses its own zlib-ng input.

The [TLS producer](../tools/native/build_tls.py) pins OpenSSL `4.1.0` and verifies its source archive by SHA-256 before extraction. Windows packages use the ABI 4 pair `libcrypto-4.dll` and `libssl-4.dll`. Linux packages use `libcrypto.so.4` and `libssl.so.4`. The [migration record](openssl-migration.md#upstream-sources) gives the release revisions, archive hashes and compatibility with the former `3.6.5` baseline. The original notices accompany the [native delivery](native-packaging.md).

Desktop shaping and FreeType auto-hinting use the same [HarfBuzz Git revision](https://github.com/harfbuzz/harfbuzz/commit/ed51d202cfd0c29372d3ee056eb0dc715688a410) `ed51d202cfd0c29372d3ee056eb0dc715688a410`. Both producers verify the clean checkout before applying the [local allocation and lazy-cache adaptation](../tools/shaping-native/README.md). The upstream [15.2.0 source release archive](https://github.com/harfbuzz/harfbuzz/releases/expanded_assets/15.2.0) `harfbuzz-15.2.0.tar.xz` has SHA-256 `e2ec49090744383cdbe7df808c1cedaafba06a97be8dc4211fe546ba585f12cb`. It is a provenance cross-check. The producers fetch Git.

The 2026-10-05 upstream release/tag and NuGet audit refreshed SDL3-CS and its native core/Image/Shadercross packages, Clipper2, FastNoiseLite, HarfBuzzSharp, private font/compression/TLS source pins and the shader import toolchain. Box2D.NET, FAudio, NLayer, stable NVorbis, Avalonia Unicode, ICU and Zstandard already match their latest stable releases; ENet and qoa-fu match upstream HEAD, and FastLZ has no newer stable release. NVorbis 1.0 prereleases remain outside this stable update. MonoGame.Library.FreeType has no newer stable package. Original comparison-derived algorithms, fonts and icons retain their independent provenance.

Changed source fingerprints and SDL dependencies require rebuilt private packages, versioned `0.1.0-preview.4`. Local Linux source builds and package checks establish only their exercised host; publication and foreign target execution remain separate gates under ADR 0021.

SDL3-CS already supplies the selected WAV loading bindings; IMA ADPCM is an engine-owned C# codec, not an additional third-party library. SDL_mixer is not the selected audio mixer or decoder. Silk.NET.Shaderc.Native is not part of the runtime shader path. These choices are recorded in [ADR 0047](decisions/audio.md#adr-0047) and [ADR 0028](decisions/rendering.md#adr-0028).

## Host-provided libraries

Linux display integration directly probes the system's `libdbus-1.so.3`. GTK 3, GDK, GObject and libdecor's GTK plugin are optional host facilities used for native-looking Wayland window decorations where available. They are not engine-owned NuGet packages. See the [display component](components/display-server.md). This page is not an inventory of every operating-system library or transitive native codec dependency.

Linux TLS uses the packaged OpenSSL 4 pair and the host's system CA paths for system trust. Resource PEM/DER parsing uses .NET cryptography, so startup must select the same private pair for the .NET PAL. A process whose PAL has already selected OpenSSL 3 fails the engine TLS version check. The [.NET provider selection and target library identities](components/tls.md#unified-openssl-source-profile) follow [ADR 0094](decisions/networking.md#adr-0094).

StreamPeerGZIP directly integrates the compression PAL already shipped with .NET 10, using the private v10.0.1 PAL_ZStream ABI for incremental gzip/zlib state and strict end validation. No new native package or vendor source is shipped. Executable self-contained/foreign-host checks remain the integration gates; see [HTTP and compression](components/http.md).
