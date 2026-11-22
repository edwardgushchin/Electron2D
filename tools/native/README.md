# Electron2D platform packages

Install `Electron2D` for the managed C# runtime, then add `Electron2D.Windows`, `Electron2D.Linux`, `Electron2D.MacOS`, `Electron2D.Android`, `Electron2D.iOS`, `Electron2D.tvOS` or `Electron2D.Web` for your game targets. Each platform package contains its audited native files and selects the matching SDL, image, shader and font dependencies. It contains no managed backend assembly.

For example, a Linux game uses:

```bash
dotnet add package Electron2D --prerelease
dotnet add package Electron2D.Linux --prerelease
```

The platform package version is the minimum supported Electron2D version, including its prerelease suffix. `Electron2D.Linux` version `0.2.0-alpha` therefore requires `Electron2D` version `0.2.0-alpha` or later. NuGet records and enforces that minimum dependency; a version of SDL, FreeType or OpenSSL is not the version of the Electron2D platform package. Use matching product/platform release versions unless a different combination has been verified.

Publish for a specific RID to include only its native assets under `runtimes/<RID>/native`. Platform package installation does not establish target execution or complete game hosting; refer to the repository platform report for current verification limits. Native archives for Apple and Web still follow their SDK's static-link rules.

Native producers keep source receipts, binary SHA-256 values, architecture/loader/export checks and bundled notices. The maintainer source-build mode and CI prepare packages; game developers do not compile the native backends.

The source producer builds one engine module: libElectron2D.so on Linux/Android, libElectron2D.dylib on macOS, libElectron2D.dll on Windows and libElectron2D.a on static Apple/Web profiles. It contains text/regex/MSDF, mesh, rendering, networking/compression, image/video codecs with their existing private codec closures. The existing source pins, licenses and native lifetime rules remain in their owning component directories. SDL, FAudio, FreeType, HarfBuzz and OpenSSL retain their existing separate native identities.

CMakeLists.txt composes the retained static subsystem targets; exports.txt defines the exact desktop ABI. Static profiles merge the complete archive closure while preserving colliding object basenames. build.py, build_windows.py and build_cross.py select matching target toolchains and SDL. Package inspection rejects stale subsystem layouts, missing exports, wrong architectures, unexpected dependencies and build-machine search paths.

On Windows, MSVC uses the build-time import library Electron2D.lib, which refers to libElectron2D.dll. The DLL keeps its lib prefix to avoid colliding with managed Electron2D.dll; the import library is not shipped as a runtime asset.

Windows third-party payloads use FAudio.dll, freetype.dll, harfbuzz.dll and the upstream OpenSSL major-4 basenames libcrypto-4.dll/libssl-4.dll. The RID directory separates architectures. WindowsNative.targets owns the explicit retired-file list shared by the source producer and incremental build/publish cleanup; it rejects the preceding subsystem package before copy. Desktop package consumers receive Electron2D.native-manifest.json, and the Windows publish check verifies its six hashes, PE identities, dependency closure and common ABI. See [the delivery contract](../../docs/native-packaging.md#windows-native-layout) for the older public package and qualification limits.

Ninja builds probe MSVC's raw localized `/showIncludes` prefix through
`msvc-dependencies.cmake`. This avoids console decoding corrupting header
dependencies on installations without an English compiler language pack.
An environment launcher includes the prefix hash in compile commands, preserving
existing launchers and invalidating stale empty dependency records. Run
`python -B tools/native/test_msvc_dependencies.py BUILD` from a developer shell
to check cache migration and subsequent header changes. Check
`ninja -t deps OBJECT` after compiler/toolchain changes; a touched included header
must invalidate its consumer object.

Use Electron2DNativeLibrary to select a complete source-built module for a desktop consumer. Games consume the platform package and do not build native code. External font/shaping/TLS overrides and the existing .NET TLS provider contract remain unchanged. Native source integration and local tests do not qualify foreign targets or publish a package.

See [native delivery](../../docs/native-packaging.md) and the rendering, mesh, text, networking and codec producer directories for their specific behavior and checks.

## WSL build slot

The desktop native and OpenSSL source producers share a single compilation slot per Linux user in WSL. This also covers the native targets of source builds launched through `dotnet build`. The producers use one compiler job in WSL. A second compile from any worktree reports that the slot is occupied and exits. Run it again after the current compile finishes. The slot uses `flock` from util-linux and a 45-minute `timeout` from GNU coreutils with a 15-second grace period before forced termination.

Run standalone build and validation commands with the same slot. The following command wraps a direct CMake build from the repository root in WSL.

```bash
python3 -B tools/native/wsl_build.py -- cmake --build "$build_dir" --parallel 1
```

Call the standard source producer directly so it can acquire its own compilation slot. If WSL stops responding, keep the build log and ascertain the previous job's state before trying another build.
