# Third-party software

Last updated: 2026-10-05

This page lists Electron2D's direct third-party integrations and backends selected by accepted decisions. **Selected** does not mean **integrated**. The project files, vendor provenance records and shader toolchain lock are the sources for exact versions; native packages may have their own transitive dependencies.

## Integrated runtime dependencies

| Software | Role and delivery | Version source |
| --- | --- | --- |
| SDL3-CS (SDL, Image and ShaderCross bindings) | Complete managed source modules compiled internally into the single `Electron2D.dll`; no public SDL types or separate managed binding assembly. | [Vendor provenance](../src/Vendor/SDL3-CS/UPSTREAM.md): `v3.4.16.1` |
| SDL3 | Native window, input and rendering foundation. The runtime project supplies RID-specific packages transitively. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}` `3.4.16.0` |
| SDL_image 3 | Native image decoding and encoding through the vendored Image bindings. The runtime project supplies RID-specific packages transitively. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Image` `3.4.6.9` |
| SDL_shadercross 3 | Native SPIR-V translation and reflection through the vendored ShaderCross bindings; the Linux package also supplies the DXC and SPIRV-Cross libraries used by the shader path. | [`Electron2D.csproj`](../Electron2D.csproj): `SDL3-CS.{platform}.Shadercross` `3.0.0.11`; [shader component](components/shader-materials.md) |
| Clipper2 | Polygon clipping and offset operations; seven C# source files compiled internally into `Electron2D.dll`. | [Vendor provenance](../src/Vendor/Clipper2/UPSTREAM.txt): `1.5.4`; [license](../licence/Clipper2-LICENSE.txt) |
| PolyPartition algorithm | Adapted convex polygon part merging; no separate package or binary. | [Geometry component](components/geometry-values.md); [notice and license](../licence/PolyPartition-LICENSE.txt) |
| PCG32 algorithm | Adapted random number generator core; no separate package or binary. | [Random generation component](components/random-generation.md); [notice and license](../licence/PCG32-LICENSE.txt) |

These are direct integrations. The three pinned native SDL packages are delivered separately from `Electron2D.dll`; the single-assembly rule applies to managed code, not native binaries. The generic desktop engine reference carries all three desktop package families so NuGet selects the native files for the application RID; Android, iOS and tvOS retain target-specific references, and Android TV uses Android packages. Only Linux x64 has a current self-contained artifact audit. Browser WASM selects no SDL package until its host is implemented. See [ADR 0012](decisions/product.md#adr-0012) and [ADR 0021](decisions/product.md#adr-0021).

## Editor content

The editor bundles IBM Plex Sans Regular 3.005 (weight 400) for its startup caption. The [font and its corresponding SIL OFL 1.1 license](components/editor-startup.md) are copied into the editor's `Assets/` directory during build/publish. They are editor content and are not included in `Electron2D.dll` or game runtime assets. The component records the pinned upstream revision and font SHA-256.

## Shader import and build tools

The separate Linux x64 `ShaderImport` tool packages **glslang 16.4.0**, **SPIRV-Tools v2026.3** (`spirv-val`) and matching **SPIRV-Headers**. Revisions and archive hashes are pinned in [toolchain.lock.json](../tools/shaders/toolchain.lock.json). HLSL import uses DXC from the already listed SDL_shadercross native package. These compilers and validators run during import/build, not inside the game runtime. See the [importer README](../tools/shaders/README.md) and [ADR 0028](decisions/rendering.md#adr-0028).

## Selected for future executable slices

| Software | Intended role | Decision and current state |
| --- | --- | --- |
| SDL_ttf 3 with HarfBuzz and FreeType; SDL3-CS TTF bindings | Font loading, shaping and glyph rasterization integrated with Electron2D's canvas. | [ADR 0046](decisions/rendering.md#adr-0046). Text backend, native package and managed bindings are not integrated yet. |
| FAudio over SDL3; FAudio# managed binding | Audio voices, bus routing and effects. | [ADR 0047](decisions/audio.md#adr-0047). FAudio 26.10 native output over SDL3 and internal managed binding execute on Linux x64; physical/other-platform gates remain separate. |
| [NVorbis](https://github.com/NVorbis/NVorbis) | Managed C# Ogg Vorbis decoder, to be compiled internally into `Electron2D.dll`. | [ADR 0047](decisions/audio.md#adr-0047). Selected, not integrated. |
| [NLayer](https://github.com/naudio/NLayer) | Managed C# MP3 decoder, to be compiled internally into `Electron2D.dll`; NAudio is not required. | [ADR 0047](decisions/audio.md#adr-0047). Selected, not integrated. |
| [qoa-fu](https://github.com/pfusik/qoa-fu) C# translation | Managed QOA encoder/decoder for imported WAV samples, to be compiled internally into `Electron2D.dll`. | [ADR 0047](decisions/audio.md#adr-0047). Selected, not integrated. |
| Box2D.NET | 2D physics backend, with managed source compiled into `Electron2D.dll`. | [ADR 0012](decisions/product.md#adr-0012). Physics source and domain are not integrated yet. |

SDL3-CS already supplies the selected WAV loading bindings; IMA ADPCM is an engine-owned C# codec, not an additional third-party library. SDL_mixer is not the selected audio mixer or decoder. Silk.NET.Shaderc.Native is not part of the runtime shader path. These choices are recorded in [ADR 0047](decisions/audio.md#adr-0047) and [ADR 0028](decisions/rendering.md#adr-0028).

## Host-provided libraries

Linux display integration directly probes the system's `libdbus-1.so.3`. GTK 3, GDK, GObject and libdecor's GTK plugin are optional host facilities used for native-looking Wayland window decorations where available. They are not engine-owned NuGet packages. See the [display component](components/display-server.md). This page is not an inventory of every operating-system library or transitive native codec dependency.

Linux TLS directly loads host-provided OpenSSL 3 (`libssl.so.3` and `libcrypto.so.3`), with the system CA paths and crypto policy. The engine does not redistribute these libraries or vendor their source. Other TLS backend/packaging targets are not integrated; see [ADR 0094](decisions/networking.md#adr-0094). Resource PEM/DER parsing uses the .NET cryptography backend.

StreamPeerGZIP directly integrates the compression PAL already shipped with .NET 10, using the private v10.0.1 PAL_ZStream ABI for incremental gzip/zlib state and strict end validation. No new native package or vendor source is shipped. Executable self-contained/foreign-host checks remain the integration gates; see [HTTP and compression](components/http.md).
