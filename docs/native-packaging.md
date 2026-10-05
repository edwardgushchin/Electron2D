# Private native delivery

Last updated: 2026-10-05

Ordinary Electron2D source builds restore `Electron2D.Native.Linux` alongside the existing native dependencies. They do not compile the private text, audio or ENet libraries and do not require CMake, Ninja, C/C++ compilers or an installed SDL development package. The managed bindings and ICU data remain inside `Electron2D.dll`. [ADR 0012](decisions/product.md#adr-0012) owns this boundary.

The initial package version is `0.1.0-preview.1`, defined with the SDL version in `tools/native-package.props`. Its first public publication is pending. Until that publication, use the `private-native-package` CI artifact as a local NuGet feed, or prepare a local package with the commands below. Set `RestoreAdditionalProjectSources` to the feed's absolute directory when running `dotnet restore`, `build`, `pack` or `publish`; the normal nuget.org source remains available. A clean checkout cannot restore this new package from nuget.org before publication.

## CI and package contents

The reusable [native workflow](../.github/workflows/native.yml) builds Linux x64 and ARM64 on their corresponding runners, using Ubuntu 22.04 build containers. It fetches the pinned SDL 3.4.16 archive, verifies SHA-256 `7322236cd12090c3eb40b9728be4d49c76f66ad17d04369584d4ecad5cf77c68`, and uses only its public headers. FAudio links against the selected RID's library from SDL3-CS.Linux; SDL is not rebuilt. Runtime compatibility also depends on the selected SDL and other native packages, not just the private build container.

Each RID contributes exactly these files:

- `runtimes/<RID>/native/libElectron2DTextBreak.so`: private ICU boundary ABI.
- `runtimes/<RID>/native/libFAudio.so.0`: FAudio with the engine-owned output bridge.
- `runtimes/<RID>/native/libElectron2DENet.so`: private ENet/socket/codec bridge.

Staging records the source fingerprint and binary SHA-256 values. Packing rejects missing/extra RIDs, stale source receipts, changed binaries, wrong ELF architectures or SONAMEs, missing bridge exports, and build-machine RPATH/RUNPATH entries. The final package includes that manifest, required notices and transitive build targets; it includes no managed backend DLL. Native files retain their runtime directory in both project and package consumers. Notices flow to `licence/` during publication.

Build and Tests first prepare the package, then restore it from the workflow's local feed for the existing RID matrix. The native workflow uploads the `.nupkg`; it does not publish to nuget.org. Public publishing requires a separately authorized release and credentials. Version changes and rebuilt payloads require the artifact/license checks in [Maintaining](maintaining.md).

## Full native rebuild

Only this explicit mode needs CMake 3.20 or later, Ninja, C/C++17 compilers, Python 3, GNU binutils, and zlib/Zstandard development libraries. Restore requires NuGet access, and the initial header fetch requires HTTPS access to libsdl.org. The CMake build directory caches the fetched archive and headers.

From the repository root, on the matching Linux host:

```bash
dotnet restore Electron2D.csproj -r linux-x64 -p:Electron2DBuildNativeFromSource=true
dotnet msbuild Electron2D.csproj -t:BuildElectron2DNativeAssets -p:RuntimeIdentifier=linux-x64 -p:Configuration=Release -p:Electron2DBuildNativeFromSource=true
dotnet pack tools/native/Native.csproj -c Release -o bin/native-feed -p:NativePackageRIDs=linux-x64
RestoreAdditionalProjectSources="$PWD/bin/native-feed" dotnet build Electron2D.csproj -c Release
python3 -B tools/native/test_consumer.py bin/native-feed
```

The explicit one-RID pack is a local Linux x64 verification artifact. CI merges independently built x64 and ARM64 artifacts and packs both; the default pack rejects an incomplete two-RID bundle. Cross-compiling private source still requires target toolchains or audited prebuilt libraries under the existing per-component properties. `dotnet build -p:Electron2DBuildNativeFromSource=true` also retains the full runtime-plus-native source build.

## Verification boundaries

Linux x64 local verification exercises a fresh source checkout, ProjectReference and NuGet consumers, public font layout/audio output/ENet creation, and self-contained application publications with native build tools blocked and `LD_LIBRARY_PATH` unset. [Tests run 37252083794](https://github.com/edwardgushchin/Electron2D/actions/runs/37252083794) built both Linux architectures, audited and packed their combined NuGet artifact, and passed the fresh consumer check. Its Linux x64 and ARM64 jobs restored that artifact and passed the full headless runtime suites plus trimmed/NativeAOT contracts. The overall run still requires the Apple app correction; these Linux outcomes do not establish renderer or hardware audio acceptance. Package wiring does not enable other platforms or remove their runtime guards; see [Platform verification](platform-verification.md).
