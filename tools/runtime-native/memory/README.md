# Linux x64 managed memory profile

The opt-in package consumer profile uses the pinned .NET 10.0.14 allocation
hooks and the same native ledger as the packaged Electron2D engine. The host
uses workstation GC with concurrent collection disabled. CoreCLR requires the
source-built runtime and startup profiler. NativeAOT requires the source-built
runtime archive at publish time. Stock .NET continues to support the native-only
snapshot.

## Build the runtime inputs

Use a Linux x64 host with the .NET SDK containing Microsoft.NETCore.App
10.0.14, Python 3, Clang/LLVM, CMake, Ninja, patchelf and the .NET runtime's
native prerequisites. Use absolute paths and an owned temporary location with
several GiB available. Download the [v10.0.14 runtime source archive](https://codeload.github.com/dotnet/runtime/tar.gz/refs/tags/v10.0.14).
The build checks the archive and original source hashes.

Build, stage and pack the complete engine and Linux platform packages using
the [native packaging procedure](../../../docs/native-packaging.md#full-native-rebuild).
Set `feed` to a local package directory and choose a distinct candidate
`version` so restore can select the exact pair.
Use the same `-p:Electron2DVersion="$version"` on the native build and both pack
commands. Run the managed pack from the same checkout.

```bash
dotnet pack Electron2D.csproj -c Release -o "$feed" -p:Electron2DVersion="$version"
```

Set `engine` to the staged Linux x64 `libElectron2D.so` from the native package.
Set `runtime_archive` to the downloaded archive, `sdk` to the selected SDK
root and `profile` to an empty output directory.

```bash
bash tools/runtime-native/memory/build.sh aot "$runtime_archive" "$sdk" "$profile"
E2D_MEMORY_NATIVE="$engine" bash tools/runtime-native/memory/build.sh clr "$runtime_archive" "$sdk" "$profile"
```

The pinned archive declares `PatchVersion=13` in `eng/Versions.props`.
Its source-built CoreLib reports `.NET 10.0.13-dev`. The CoreCLR launcher
selects the instrumented framework in the `10.0.14` directory.

## Publish and run

Use an isolated NuGet package cache. Select an SDK with .NET 10.0.14 packs,
such as SDK 10.0.114. Set `ConcurrentGarbageCollection=false` and use
workstation GC. Place the outputs in separate directories. On WSL, run
NativeAOT publish through the [shared build slot](../../native/README.md#wsl-build-slot).

```bash
project=tests/NativeMemory.SharedHost/NativeMemory.SharedHost.csproj
for mode in clr aot; do
  if [[ "$mode" == aot ]]; then
    publish_aot=true
    self_contained=true
  else
    publish_aot=false
    self_contained=false
  fi
  dotnet publish "$project" -c Release -r linux-x64 --disable-build-servers \
    --source "$feed" --source https://api.nuget.org/v3/index.json \
    -p:MemoryCheckPackageVersion="$version" -p:MemoryCheckNativePackageVersion="$version" \
    -p:Electron2DMemoryProfile="$profile" -p:RuntimeFrameworkVersion=10.0.14 \
    -p:PublishAot="$publish_aot" --self-contained "$self_contained" \
    --artifacts-path "$profile/obj-$mode" -o "$profile/app-$mode"
done

bash "$profile/app-clr/Electron2D.MemoryProfile.sh" clr "$profile" "$profile/app-clr/NativeMemory.SharedHost.dll"
bash "$profile/app-aot/Electron2D.MemoryProfile.sh" aot "$profile" "$profile/app-aot/NativeMemory.SharedHost"
LD_LIBRARY_PATH="$profile/app-clr/runtimes/linux-x64/native" \
  dotnet "$profile/app-clr/NativeMemory.SharedHost.dll" --native-only
```

The launcher puts the application package's native directory first during
process startup so the runtime hooks and engine imports use the same
`libElectron2D.so`. For the stock control, use an uninstrumented .NET 10
host and the same native library search directory.

The shared host checks live managed and native ownership, collection and free,
the peak between two reads, disjoint and overlapping ownership, the existing
native-only snapshot and 20,000 allocation-free prepared reads. Record the
engine revision or source hashes, SDK and OS versions, the package, deployed
module and runtime hashes, process maps and the output of both hosts.
