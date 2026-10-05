# Third-party software

Last updated: 2026-10-05

This page lists Electron2D's direct third-party integrations and backends selected by accepted decisions. **Selected** does not mean **integrated**. The project files, vendor provenance records and shader toolchain lock are the sources for exact versions; native packages may have their own transitive dependencies.

## Integrated runtime dependencies

| Software | Role and delivery | Version source |
| --- | --- | --- |
| SDL3-CS (SDL, Image and ShaderCross bindings) | Complete managed source modules compiled internally into the single `Electron2D.dll`; no public SDL types or separate managed binding assembly. | [Vendor provenance](../src/Vendor/SDL3-CS/UPSTREAM.md): `v3.4.18.0` |
| SDL3 | Native window, input and rendering foundation. The runtime project supplies RID-specific packages transitively. | [Native pins](../tools/native-package.props): `SDL3-CS.{platform}` `3.4.18.0` (NuGet normalizes it to `3.4.18`) |
| SDL_image 3 | Native image decoding and encoding through the vendored Image bindings. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Image` `3.4.6.12`, upstream SDL_image `3.4.6` |
| SDL_shadercross 3 | Native SPIR-V translation and reflection; the Linux package also supplies DXC and SPIRV-Cross. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Shadercross` `3.0.0.13`; [shader component](components/shader-materials.md) |
| Clipper2 | Polygon clipping and offsets; nine C# source files compiled internally. | [Vendor provenance](../src/Vendor/Clipper2/UPSTREAM.txt): `2.0.1`; [license](../licence/Clipper2-LICENSE.txt) |
| FastNoiseLite | Internal managed noise algorithm; the public resource exposes 1D/2D sampling. | [Vendor provenance](../src/Vendor/FastNoiseLite/UPSTREAM.txt): `1.1.1` |
| Box2D.NET | Internal managed physics backend, including the recorded allocation adaptations. | [Vendor provenance](../src/Vendor/Box2D.NET/VENDOR.md): `3.1.654` |
| FAudio and FAudio# | Native SDL3 mix graph and internal managed binding. | [Manifest](../src/Vendor/FAudio/manifest.json): `26.10`; [audio component](components/audio-playback.md) |
| NLayer / NVorbis / qoa-fu | Internal MP3, Vorbis and QOA codecs. | Manifests: [NLayer `3.0.0`](../src/Vendor/NLayer/manifest.json), [NVorbis `0.10.5`](../src/Vendor/NVorbis/manifest.json), [qoa-fu pinned commit](../src/Vendor/QOA/manifest.json) |
| Avalonia Unicode / ICU | Internal Unicode 17 algorithms and private dictionary segmentation. | [Avalonia `12.1.3`](../src/Vendor/UnicodeText/manifest.json), [ICU `78.3`](../src/Vendor/ICU/manifest.json); no Avalonia UI dependency |
| FreeType / HarfBuzzSharp | Private C ABI font loading, rasterization and shaping. | [`Electron2D.csproj`](../Electron2D.csproj): MonoGame.Library.FreeType `2.13.2.5`, HarfBuzzSharp.NativeAssets `14.2.1.301`; private FreeType producer below |
| ENet / FastLZ | Private native network transport and compression. | [ENet current pinned commit](../src/Vendor/ENet/manifest.json), [FastLZ `0.5.0` source closure](../src/Vendor/FastLZ/manifest.json) |
| PolyPartition algorithm | Adapted convex polygon part merging; no separate package or binary. | [Geometry component](components/geometry-values.md); [notice and license](../licence/PolyPartition-LICENSE.txt) |
| PCG32 algorithm | Adapted random number generator core; no separate package or binary. | [Random generation component](components/random-generation.md); [notice and license](../licence/PCG32-LICENSE.txt) |

These are direct integrations. The three pinned native SDL packages are delivered separately from `Electron2D.dll`; the single-assembly rule applies to managed code, not native binaries. The generic desktop engine reference carries all three desktop package families so NuGet selects the native files for the application RID; Android, iOS and tvOS retain target-specific references, and Android TV uses Android packages. Only Linux x64 has a current self-contained artifact audit. Browser WASM selects no SDL package until its host is implemented. See [ADR 0012](decisions/product.md#adr-0012) and [ADR 0021](decisions/product.md#adr-0021).

## Editor content

The editor bundles IBM Plex Sans Regular 3.005 (weight 400) for its startup caption. The [font and its corresponding SIL OFL 1.1 license](components/editor-startup.md) are copied into the editor's `Assets/` directory during build/publish. They are editor content and are not included in `Electron2D.dll` or game runtime assets. The component records the pinned upstream revision and font SHA-256.

## Shader import and build tools

The separate Linux x64 `ShaderImport` tool packages **glslang 16.6.0** and its upstream `known_good.json` revisions of **SPIRV-Tools** (`spirv-val`) and **SPIRV-Headers**. Revisions and archive hashes are pinned in [toolchain.lock.json](../tools/shaders/toolchain.lock.json). HLSL import uses DXC from the SDL_shadercross native package. These compilers and validators run during import/build. See the [importer README](../tools/shaders/README.md) and [ADR 0028](decisions/rendering.md#adr-0028).

## Private native producers and update audit

The [FreeType producer](../tools/font-native/CMakeLists.txt) pins FreeType `2.14.3`, zlib `1.3.2`, libpng `1.6.59`, Brotli `1.2.0` and HarfBuzz `14.5.1`. The [ENet producer](../tools/enet-native/CMakeLists.txt) uses pinned zlib `1.3.2` and Zstandard `1.5.7` where static compression is required. The [TLS producer](../tools/native/build_tls.py) pins OpenSSL `3.6.5`, retaining the accepted OpenSSL 3 ABI; OpenSSL 4 requires a separate backend migration. Their original notices accompany the [native delivery](native-packaging.md).

The 2026-10-05 upstream release/tag and NuGet audit refreshed SDL3-CS and its native core/Image/Shadercross packages, Clipper2, FastNoiseLite, HarfBuzzSharp, private font/compression/TLS source pins and the shader import toolchain. Box2D.NET, FAudio, NLayer, stable NVorbis, Avalonia Unicode, ICU and Zstandard already match their latest stable releases; ENet and qoa-fu match upstream HEAD, and FastLZ has no newer stable release. NVorbis 1.0 prereleases remain outside this stable update. MonoGame.Library.FreeType has no newer stable package. Original comparison-derived algorithms, fonts and icons retain their independent provenance.

Changed source fingerprints and SDL dependencies require rebuilt private packages, versioned `0.1.0-preview.4`. Local Linux source builds and package checks establish only their exercised host; publication and foreign target execution remain separate gates under ADR 0021.

SDL3-CS already supplies the selected WAV loading bindings; IMA ADPCM is an engine-owned C# codec, not an additional third-party library. SDL_mixer is not the selected audio mixer or decoder. Silk.NET.Shaderc.Native is not part of the runtime shader path. These choices are recorded in [ADR 0047](decisions/audio.md#adr-0047) and [ADR 0028](decisions/rendering.md#adr-0028).

## Host-provided libraries

Linux display integration directly probes the system's `libdbus-1.so.3`. GTK 3, GDK, GObject and libdecor's GTK plugin are optional host facilities used for native-looking Wayland window decorations where available. They are not engine-owned NuGet packages. See the [display component](components/display-server.md). This page is not an inventory of every operating-system library or transitive native codec dependency.

Linux TLS directly loads host-provided OpenSSL 3 (`libssl.so.3` and `libcrypto.so.3`), with the system CA paths and crypto policy. Private target producers build the pinned OpenSSL source with engine-specific library identities; target acceptance is recorded separately in [native delivery](native-packaging.md) and [ADR 0094](decisions/networking.md#adr-0094). Resource PEM/DER parsing uses the .NET cryptography backend.

StreamPeerGZIP directly integrates the compression PAL already shipped with .NET 10, using the private v10.0.1 PAL_ZStream ABI for incremental gzip/zlib state and strict end validation. No new native package or vendor source is shipped. Executable self-contained/foreign-host checks remain the integration gates; see [HTTP and compression](components/http.md).
