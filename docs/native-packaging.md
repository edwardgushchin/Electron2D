# Private native delivery

Last updated: 2026-10-05

Ordinary desktop source builds restore `Electron2D.Native.Linux` and `Electron2D.Native.MacOS` alongside the existing native dependencies. They do not compile the private text, audio, ENet or macOS OpenSSL/FreeType libraries and do not require CMake, Ninja, C/C++ compilers or an installed SDL development package. The managed bindings and ICU data remain inside `Electron2D.dll`. [ADR 0012](decisions/product.md#adr-0012) owns this boundary.

The current development package version is `0.1.0-preview.4`, defined with the SDL version in `tools/native-package.props`; it refreshes SDL3 to 3.4.18 and the private font/compression/TLS producer pins, with matching source receipts and notices. Its public publication remains a separate gate. Until publication, use the `private-native-package` CI artifact as a local NuGet feed, or prepare a local package with the commands below. Set `RestoreAdditionalProjectSources` to the feed's absolute directory when running `dotnet restore`, `build`, `pack` or `publish`; the normal nuget.org source remains available. A clean checkout cannot restore an unpublished package from nuget.org.

The preceding [Linux `0.1.0-preview.1`](https://www.nuget.org/packages/Electron2D.Native.Linux/0.1.0-preview.1) is public. [Publication run 37281011805](https://github.com/edwardgushchin/Electron2D/actions/runs/37281011805), at commit `150c03b9401c8f83e0440c6bc34f8b96e8f50677`, built and verified both Linux architectures and published through Trusted Publishing. A fresh source snapshot of that runtime restored only from nuget.org with an empty package cache and native tools blocked, built without warnings, executed public text/audio/ENet calls and ran its self-contained publish with `LD_LIBRARY_PATH` unset. Both RID manifests, all six native payloads, transitive targets and notices matched the publication artifact. The signed public package SHA-256 is `2d38b086f1bd1ef9bf72ce73b8a92825ce9512146627fec1e44320e2b3f08b14`. This verifies that released runtime and package; it does not make the newer development version or other platform backends publicly available.

## CI and package contents

The reusable [native workflow](../.github/workflows/native.yml) builds Linux x64 and ARM64 on their corresponding runners, using Ubuntu 22.04 build containers. It fetches the pinned SDL 3.4.18 archive, verifies SHA-256 `9c75cf16330322c217dedd2e0609f1124f1b54b8633e763467b4684d0f4334a3`, and uses only its public headers. FAudio links against the selected RID's library from SDL3-CS.Linux; SDL is not rebuilt. Runtime compatibility also depends on the selected SDL and other native packages, not just the private build container.

Each RID contributes exactly these files:

- `runtimes/<RID>/native/libElectron2DTextBreak.so`: private ICU boundary ABI.
- `runtimes/<RID>/native/libFAudio.so.0`: FAudio with the engine-owned output bridge.
- `runtimes/<RID>/native/libElectron2DENet.so`: private ENet/socket/codec bridge.

Staging records the source fingerprint and binary SHA-256 values. Packing rejects missing/extra RIDs, stale source receipts, changed binaries, wrong ELF architectures or SONAMEs, missing bridge exports, and build-machine RPATH/RUNPATH entries. The final package includes that manifest, required notices and transitive build targets; it includes no managed backend DLL. Native files retain their runtime directory in both project and package consumers. Notices flow to `licence/` during publication.

The single [CI workflow](../.github/workflows/ci.yml) prepares the native packages once. Its separate Build and Tests matrices then restore those same artifacts from the current run's local feed. The native workflow uploads the `.nupkg`; compilation runs at most once per RID, and package audits and fresh consumer checks execute once per CI run. The separate manual [publishing workflow](../.github/workflows/publish-native.yml) rebuilds and verifies the complete bundle before publishing it to nuget.org. It runs only from `main` and obtains a short-lived credential through [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). Version changes and rebuilt payloads require the artifact/license checks in [Maintaining](maintaining.md).

CI caches only each RID's staged native libraries and source receipts, keyed by RID, runner OS/architecture and the existing source fingerprint, including the native build workflow. A cache hit must pass the same current-source receipt, checksum, architecture, loader and export checks before packing; it cannot replace those checks or fresh consumer execution. Tests-only and unrelated documentation changes can reuse the native compilation result. Native source/build-recipe changes invalidate it. Standalone native dispatch defaults to `rebuild=true`, and publication explicitly forces a full native rebuild; both bypass the CI cache.

## macOS native producer

The same package project accepts `NativePackagePlatform=MacOS` and produces `Electron2D.Native.MacOS` with both `osx-x64` and `osx-arm64`. Matching macOS runners build private ICU, FAudio over the restored SDL core, ENet with statically linked pinned Zstandard, and private OpenSSL 3.6.5. Native assets retain the RID directory. Mach-O checks require the exact CPU, relocatable library identities, expected bridge exports, shared SDL identity, private crypto dependency and no build-machine RPATH. Source archive hashes and original licenses are pinned; OpenSSL loader identities are changed and the dylibs ad-hoc signed after relocation.

The package now also supplies `libElectron2DFreeType.dylib`, built from pinned FreeType 2.14.3 with static zlib 1.3.2, libpng 1.6.59, Brotli 1.2.0 and HarfBuzz 14.5.1. The build requires every codec/auto-hinting dependency, includes libpng's generated headers explicitly, reads the actual bundled WOFF2 font before staging, and rejects runtime imports of global codec/HarfBuzz libraries. Existing licenses cover these source versions; [the notices](../licence/THIRD_PARTY_NOTICES.md) record their exact commits. The ordinary runtime resolves this private library rather than the upstream macOS FreeType binary whose build disables Brotli and HarfBuzz.

[Tests run 37281227270](https://github.com/edwardgushchin/Electron2D/actions/runs/37281227270), at `6e464e97e5db208dc4d181f6faf01da5ee238e94`, passed both matching macOS native producers and combined package audits. The first connected [integration run 37284017058](https://github.com/edwardgushchin/Electron2D/actions/runs/37284017058) failed in both public macOS consumers with FreeType error 2 while decoding the bundled WOFF2; downstream full suites did not run. The replacement passed local decoder/raster checks; its first macOS compilation then exposed the missing generated-libpng include path, fixed before the corrected run below. The native workflow exercises fresh ProjectReference/NuGet consumers and publishes on both architectures with native tools blocked; successful production alone does not establish execution.

[CI run 37291533093](https://github.com/edwardgushchin/Electron2D/actions/runs/37291533093), at `2ab0d081`, passed all four native producers, both combined packages and fresh public consumers on Linux/macOS x64/macOS ARM64. Each macOS producer decoded the bundled WOFF2; each macOS consumer executed public font/audio/ENet calls through both ProjectReference and NuGet builds/publishes with native tools blocked. Both subsequent full macOS suites stopped at LineEdit's test-generated Ctrl+Z event, before the remainder of the suite; that helper and the analogous ItemList mouse helper now use the existing platform-aware command modifier. Full-suite acceptance still requires a corrected target rerun. Linux/macOS test oracles are installed only by Tests jobs, not build-only jobs.

Private macOS OpenSSL uses OS certificate-chain validation through .NET/Keychain for system-trust clients, with certificate downloads and automatic revocation fetch disabled. OpenSSL then validates its TLS certificate policy and expected name/IP. Custom CA and unsafe-client contracts are unchanged. Linux continues to use its system OpenSSL and CA paths. [TLS](components/tls.md) records the owner-thread and cold-handshake boundary.

The ENet bridge normalizes socket buffers into its engine-owned pointer/length ABI because upstream Windows and Unix structs use different field order. Clock/RNG primitives are selected by platform, and every managed ENet import explicitly uses Cdecl. The Linux wire/codec/fragmentation and warmed allocation regression passed after this change.

## Windows native producer

The native workflow now configures three Windows producers (`win-x86`, `win-x64`, `win-arm64`) and a combined `Electron2D.Native.Windows` package. The selected MSVC environment builds private ICU, FAudio, ENet, WOFF2-capable FreeType and OpenSSL. FAudio uses an import library generated from the restored, CPU-checked SDL DLL instead of compiling another SDL core. FreeType codecs, ENet compression dependencies and the C/C++ runtime are linked statically; OpenSSL has private DLL identities. The PE audit checks the target machine, native DLL/export identity, engine bridge exports and dependency closure, rejecting managed assemblies and unbundled compiler/codec libraries. Source receipts and the existing cache/packing gates apply unchanged.

The first Windows candidate compiled ICU and FAudio on the three selected architectures, then stopped while configuring the stock ENet test oracle because its Windows source file was not retained. That unchanged file now comes from the same pinned ENet commit and is covered by its source manifest. The corrected producer still requires a target run. It does not yet add the package to ordinary runtime restoration, remove Windows backend guards or establish full Windows runtime acceptance.

## Android, Apple and Web producers

The shared native workflow also selects all eleven Android/iOS/tvOS/Web rows from the authoritative CI RID registry and packs one complete package per platform. A contract check rejects any difference between the package RID registry and the declared eighteen targets. Each producer contributes actual target-compiled files and current source receipts; missing or foreign binaries fail packing. These producer candidates are not runtime acceptance or public publication.

Android uses NDK `28.2.13676358`, API21, static C++ runtime and the existing pinned SDL binary. Its private ELF closure contains ICU, FAudio, ENet, OpenSSL, FreeType and HarfBuzz. Audits verify ELF32/ELF64 CPU, SONAME, bridge exports, closed dynamic dependencies and 16 KB load-segment alignment. OpenSSL's private loader names preserve that alignment.

Apple uses Xcode with explicit CPU, SDK, minimum version and device/simulator target. Its static archives include the private bridges, OpenSSL, FreeType/HarfBuzz and their codec dependencies. Every object must carry the selected Mach-O CPU and device/simulator platform; an ARM64 device archive cannot substitute for an ARM64 simulator archive. The FAudio build retains the selected mobile deployment target instead of imposing its desktop default. No physical Apple execution or signing is claimed.

Web uses the Emscripten toolchain supplied by the selected .NET WebAssembly SDK. It builds real wasm32 archives, including SDL3 from its pinned 3.4.18 source archive, and rejects archives containing foreign objects. A linked native FreeType executable decodes and rasterizes the bundled WOFF2 and rejects corrupt data under Node. Browser engine hosting, presentation, audio lifecycle and application static-link integration remain separate unverified work.

## Public publication

The active NuGet trusted publishing policy uses repository owner `edwardgushchin`, repository `Electron2D`, workflow file `publish-native.yml`, and package scope `Electron2D.Native*`. It permits publishing new native packages and versions; the optional environment is empty. Manually run **Publish private native package** on `main`, supplying the owner's NuGet profile name. The workflow collects all `private-native-package*` artifacts from the native producer and publishes their audited platform packages without a stored API key. It rejects duplicate-version publication instead of silently accepting a different payload under an existing version. A publishing policy does not supply missing target binaries or establish their runtime acceptance.

After publication and NuGet indexing, verify a fresh source build and public text/audio/ENet consumer with an empty package cache, the default nuget.org feed and native build tools blocked. Compare the downloaded package's manifest and payload hashes with the CI artifact before removing the initial-publication caveat above.

## Full native rebuild

Only this explicit mode needs CMake 3.20 or later, Ninja, C/C++17 compilers, Python 3, GNU binutils, and zlib/Zstandard development libraries. Restore requires NuGet access, and the initial header fetch requires HTTPS access to libsdl.org. The CMake build directory caches the fetched archive and headers.

From the repository root, on the matching Linux host:

```bash
dotnet restore Electron2D.csproj -r linux-x64 -p:Electron2DBuildNativeFromSource=true
dotnet msbuild Electron2D.csproj -t:BuildElectron2DNativeAssets -p:RuntimeIdentifier=linux-x64 -p:Configuration=Release -p:Electron2DBuildNativeFromSource=true
dotnet pack tools/native/Native.csproj -c Release -o bin/native-feed -p:NativePackageRIDs=linux-x64
```

The current desktop consumer also needs the audited macOS package in the feed. Download both `private-native-package*` artifacts from one matching CI run before running `python3 -B tools/native/test_consumer.py bin/native-feed`; a Linux-only feed cannot restore the whole desktop dependency graph.

The explicit one-RID pack is a local Linux x64 verification artifact. CI merges independently built x64 and ARM64 artifacts and packs both; the default pack rejects an incomplete two-RID bundle. Cross-compiling private source still requires target toolchains or audited prebuilt libraries under the existing per-component properties. `dotnet build -p:Electron2DBuildNativeFromSource=true` also retains the full runtime-plus-native source build.

## Verification boundaries

The 2026-10-05 dependency refresh changes the private package source fingerprint and SDL dependency to 3.4.18, so all producers must rebuild version 0.1.0-preview.4. Earlier CI evidence below covers its previous source pins. Local Linux source tests, Wayland rendering and package audits do not establish foreign producer execution or public NuGet availability for this version. The source profile passed the full headless suite, native source/receipt and package checks, and the self-contained Linux inventory audit (68 ELF files, 65 notices). Wayland GPU/compatibility font, material/boolean and noise-texture pixel scenarios passed. The broad rendering runner still reaches the existing explicit Wayland Vulkan hide/show rejection, so that lifecycle is not accepted by these focused results.

Linux x64 local verification exercises a fresh source checkout, ProjectReference and NuGet consumers, public font layout/audio output/ENet creation, and self-contained application publications with native build tools blocked and `LD_LIBRARY_PATH` unset. [Tests run 37260737192](https://github.com/edwardgushchin/Electron2D/actions/runs/37260737192) built both Linux architectures, audited and packed their combined NuGet artifact, and passed the fresh consumer check. Its Linux x64 and ARM64 jobs restored that artifact and passed the full headless runtime suites plus trimmed/NativeAOT contracts. The complete 18-RID contract matrix passed, with unsigned build-only physical Apple profiles; this does not establish renderer or hardware audio acceptance. Package wiring does not enable native backends on other platforms or remove their runtime guards; full Windows/macOS suites and the remaining native dependency builds are still required. See [Platform verification](platform-verification.md).
