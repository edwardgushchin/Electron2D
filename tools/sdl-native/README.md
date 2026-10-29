# SDL original allocation source profile

This builds the pinned SDL 3.4.18 core with the existing pure-C allocation ledger
as its original allocator. Loading or first using SDL therefore cannot create
untracked SDL_malloc pointers that a later allocator replacement would free
through another owner. Both callbacks and late frees remain native. This is a
selected source-build profile; the ordinary native workflow still restores SDL
from SDL3-CS. It does not establish a complete process memory monitor.

The CMake recipe verifies the upstream archive SHA-256 and generates four
translation units from exact source preimages. It never modifies those inputs.
The core reports the explicit `Electron2D memory 1` vendor identity. The matching
renderer accepts that exact revision and the existing SDK revision through one
shared guard; GPU layout generation retains its original three preimage hashes.
Besides original allocator routing, it repairs rejected environment-string
insertion and dynamic IO buffer publication on allocation failure. IO pointer
metadata is reserved before the buffer and updated under its existing lock;
failed resize retains bytes and ownership. Clearing the public IO memory-pointer
property still transfers ownership to the caller before close.

## Build and native checks

Use an owned build directory, outside installed SDK and package caches:

```sh
cmake -S tools/sdl-native -B BUILD -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build BUILD --target Electron2DSDLMemoryCheck Electron2DMemoryCheck Electron2DSDLFaultCore Electron2DSDLLifetime --parallel 2
LD_LIBRARY_PATH=BUILD/sdl:BUILD BUILD/Electron2DSDLMemoryCheck
BUILD/ledger/Electron2DMemoryCheck
python3 -B tools/sdl-native/check_memory.py BUILD REPORT.json
```

Run `test_prepare_memory.py` against each downloaded input:
`src/stdlib/SDL_malloc.c`, `src/stdlib/SDL_getenv.c`, `src/SDL_properties.c` and
`src/io/SDL_iostream.c`. It checks generation, unchanged inputs and rejected
corruption. CMake also rejects a changed source list or preimage.

The failure matrix uses a fixed synthetic environment, actual production C
allocation code and a test-only backing-allocator fault library. Every calibrated
request runs in a fresh process, checks full shutdown drain, then retries healthy.
The ordinary checker covers zero-size normalization, calloc overflow, failed
resize contents and native-thread TLS cleanup. Dynamic IO failures preserve the
preceding byte count/content; successful transfer survives stream close.

Repeat under ASan/UBSan with leak detection. For Clang on Linux use
`-fsanitize=address,undefined -fno-omit-frame-pointer` C flags,
`-fsanitize=address,undefined -shared-libasan` shared/executable linker flags,
and an executable runtime search path from `clang --print-runtime-dir`.
The shared core retains its no-undefined link check. The matrix sets
`detect_leaks=1:halt_on_error=1` and `halt_on_error=1:print_stacktrace=1` itself.

## Managed consumer and lifetime

Select the resulting `sdl/libSDL3.so.0` through
`Electron2DSDLNativeLibrary` and its matching `libElectron2DENet.so` through
`Electron2DENetNativeLibrary`. The consumer targets replace only their canonical
destination, even if the packaged SDL file is newer. Do not overwrite installed
SDK or shared package files. Preserve the other matching native dependencies of
the consumer; the font, text, mesh, render and image producers have independent
qualification requirements.

Publish `tests/NativeMemory.CompressionHost` for linux-x64, self-contained CLR
and full-trim NativeAOT, with those properties and
`E2dSDLLifetime=BUILD/libElectron2DSDLLifetime.so`. Run its `sdl` mode with
`ELECTRON2D_SDL_BACKEND=compatibility` and `gpu`, using a real display. It checks
original/current callback identity, public window/render/pixel retirement,
20,000 allocation-free snapshots, one actual SDL/core mapping, runtime identity
and the native atexit callback. The exit fixture is test-only and must not ship.
Replacing just SDL with a stock binary is the negative control: original-owner
validation must reject it before opening a window.

## Qualification boundary

See [the recorded producer evidence](../../docs/coverage/performance-producers.md#sdl-original-allocation-source-domain-2026-10-29).
Only the selected source-build domain is covered. Stock package delivery,
foreign native targets, direct libc requests and independent OS/graphics/audio
provider allocations remain open. Native dependencies may use their own
allocators even when they load this SDL core. Retained process caches are not
asserted to vanish after a public window closes; native SDL shutdown drain is a
separate check. No managed heap composition or public API row is promoted.
