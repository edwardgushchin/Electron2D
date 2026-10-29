# Owned compression runtime component

This Linux x64 source-runtime profile builds the .NET **10.0.1** compression PAL
and its pinned static zlib-ng/Brotli inputs against the engine's existing native
allocation ledger. It does not modify the installed SDK, runtime or engine public
API. Stock framework-dependent applications keep their existing runtime codecs.
Shipping runtime selection and composed managed/native monitors remain open.

The input archive is
[dotnet/runtime v10.0.1](https://github.com/dotnet/runtime/tree/v10.0.1), SHA-256
`4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092`.
`prepare.py` verifies that pin before extracting only the native component and
its source closure into a new owned directory. It checks replacement anchors,
records the original/prepared hashes and patch, and rejects changed cached files,
extra source files and symlinks. A failed preparation stays unusable; use a fresh
build directory after diagnosing it. The upstream license stays in the prepared
tree; codec notices remain under their source directories.

The outer zlib record and every internal allocation use explicit callbacks.
Failed initialization retires both partially initialized codec state and its
outer record. Brotli's default callbacks, encoder bootstrap, decoder bootstrap
and shared dictionary bootstrap use the same core. Explicit caller allocators
retain their ownership. Encoder OOM cleanup is enabled instead of process exit.
There is no global malloc override. Internal symbol binding prevents another
loaded Brotli library from mixing allocation and release domains. The production
PAL exports the twenty upstream functions plus one private memory ABI witness.
It retains a relocatable dependency on `libElectron2DENet.so`; loading the common
ledger opens no networking or device.

Use an existing qualified production memory bundle, never the test fault core:

```bash
component_root=$(mktemp -d /tmp/e2d-compression-XXXXXX)
curl -fsSL https://codeload.github.com/dotnet/runtime/tar.gz/refs/tags/v10.0.1 \
  -o "$component_root/runtime.tar.gz"
cmake -S tools/runtime-native/compression -B "$component_root/native" \
  -DCMAKE_BUILD_TYPE=Release \
  -DE2D_RUNTIME_ARCHIVE="$component_root/runtime.tar.gz" \
  -DE2D_MEMORY_LIBRARY="$Electron2DENetNativeLibrary" -DE2D_COMPRESSION_TESTS=ON
cmake --build "$component_root/native" -j 4
python3 -O -B tools/runtime-native/compression/test_prepare.py "$component_root/runtime.tar.gz"
python3 -O -B tools/runtime-native/compression/test_memory.py \
  "$component_root/native" "$component_root/memory.json"
```

The native matrix needs independent host zlib and Brotli libraries. It checks raw
deflate/zlib/gzip, reset, Brotli streaming/one-shot, quality 0/5/11, repeated and
incompressible data, invalid initialization, explicit allocator ownership, every
observed allocation failure, healthy retry and full native drain. Wire bytes are
decoded independently; independent fixtures exercise the tracked decoders.
Repeat in a separate CMake directory with
`-DCMAKE_C_FLAGS='-fsanitize=address,undefined -fno-omit-frame-pointer'`,
`-DCMAKE_EXE_LINKER_FLAGS=-no-pie` and `RelWithDebInfo`. The runner enables leak
detection and stops on sanitizer failures. The fault core replaces malloc,
calloc and realloc only inside its test copy of the production ledger. It is
used solely by the native children through their explicit library search path.

For public consumers, supply the same qualified native overrides as the engine
cohort, including `Electron2DENetNativeLibrary`. Publish into fresh owned folders:

```bash
dotnet publish tests/NativeMemory.CompressionHost/NativeMemory.CompressionHost.csproj \
  -c Release -r linux-x64 --self-contained true -p:PublishAot=false \
  -p:E2dCompressionNative="$component_root/native/libSystem.IO.Compression.Native.so" \
  -o "$component_root/clr"
dotnet publish tests/NativeMemory.CompressionHost/NativeMemory.CompressionHost.csproj \
  -c Release -r linux-x64 --self-contained true -p:PublishAot=true -p:TrimMode=full \
  -p:E2dCompressionNative="$component_root/native/libSystem.IO.Compression.Native.so" \
  -o "$component_root/aot"
for host in clr aot; do
  for codec in deflate gzip zlib brotli; do
    "$component_root/$host/NativeMemory.CompressionHost" "$codec"
  done
done
```

The test project removes NativeAOT's direct compression P/Invoke choice and uses
the copied shared PAL. Its stream, public ByteBuffer and ZIP routes require a
positive native contribution, complete disposal/finalizer drain, retained peaks,
20,000 prepared snapshot reads at zero managed bytes, one actual PAL and one
ledger mapping, and the expected CLR/NativeAOT runtime identity. Preserve a RED
control published without the override: stock compression bypasses the ledger.
Record the PAL/core/host/PDB/source identities with the receipts before removing
owned builds. Do not remove shared SDK or other consumers' caches.
