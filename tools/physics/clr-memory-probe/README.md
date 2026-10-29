# CoreCLR reservation and frozen-memory probe

This Linux x64 correctness profile builds a separate v10.0.1 CoreCLR, JIT and
CoreLib. It reuses the NativeAOT producer, GC reservation patch and numeric
witnesses, together with the production native allocation ledger. The profiler
enables allocation notifications and nonconcurrent GC; ordinary allocations then
use the VM allocator. Successful SOH/UOH reservations publish before initialization
and typed callbacks. GC completion independently walks surviving objects, while
frozen registry transitions publish under the GC registry lock.

The VM's typed callback outlives its Alloc stack frame. A native scope stays alive
until typed completion or allocation failure; neither a retry nor a GC holds the
accounting gate. Its observer storage and the callback profiler's object map are
outside the measured domains. Their cost remains a production qualification task.

Use several GiB of owned temporary space and retain receipts before cleanup.
The installed SDK and shared runtime are read-only inputs. Run from the repository
root with a .NET 10.0.1 SDK selected:

```bash
set -e
clr_probe=$(mktemp -d /tmp/e2d-clr-memory-XXXXXX)
curl -fsSL https://codeload.github.com/dotnet/runtime/tar.gz/refs/tags/v10.0.1 -o "$clr_probe/runtime.tar.gz"
printf '%s  %s\n' 4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092 "$clr_probe/runtime.tar.gz" | sha256sum --check
python3 -c 'import sys,tarfile; tarfile.open(sys.argv[1]).extractall(sys.argv[2], filter="data")' "$clr_probe/runtime.tar.gz" "$clr_probe"
runtime_root="$clr_probe/runtime-10.0.1"
cmake -S tools/enet-native -B "$clr_probe/native" -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build "$clr_probe/native" --target Electron2DENet -j 4
cp "$clr_probe/native/libElectron2DENet.so" "$clr_probe/libElectron2DENet.so"
for role in normal deferred concurrent; do
  producer_dir="$clr_probe/$role"; flags=
  case "$role" in
    normal) producer_dir="$clr_probe" ;;
    deferred) flags=-DE2D_TEST_DEFER_COMMON_COMMIT ;;
    concurrent) flags=-DE2D_TEST_CONCURRENT_GC ;;
  esac
  mkdir -p "$producer_dir"
  c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -shared -pthread $flags \
    tools/physics/managed-live-probe/profiler.cpp tools/physics/clr-memory-probe/producer.cpp \
    tools/physics/clr-memory-probe/frozen_fixture.cpp "$clr_probe/libElectron2DENet.so" \
    -Wl,-soname,libElectron2DAOTMemory.so -o "$producer_dir/libElectron2DAOTMemory.so"
done
bash "$runtime_root/src/coreclr/build-runtime.sh" -release -arch x64 -component coreclr \
  -numproc 4 -skipgenerateversion -ninja \
  -cmakeargs "-DFEATURE_EVENTSOURCE_XPLAT=0 -DFEATURE_PERFTRACING=1 -DE2D_MEMORY_PRODUCER=$clr_probe/libElectron2DAOTMemory.so"
native_build="$runtime_root/artifacts/obj/coreclr/linux.x64.Release"
cp "$native_build/dlls/mscoree/coreclr/libcoreclr.so" "$clr_probe/unmodified-coreclr.so"
cmake --build "$native_build" --target clrjit -j 4
python3 -B tools/physics/clr-memory-probe/patch_runtime.py "$runtime_root" "$clr_probe/runtime.patch"
LD_LIBRARY_PATH="$clr_probe" cmake --build "$native_build" --target coreclr -j 4
use_installed_dotnet_cli=false NUGET_PACKAGES="$clr_probe/nuget" \
  bash "$runtime_root/build.sh" -subset clr.corelib -c Release -rc Release -m:4 \
    /p:FeatureXplatEventSource=false
dotnet build tools/physics/clr-memory-probe/CLRMemoryProbe.csproj -c Release \
  --artifacts-path "$clr_probe/managed"
python3 - "$clr_probe" <<'PY'
from pathlib import Path
import shutil, subprocess, sys
w = Path(sys.argv[1]); root = w / 'dotnet'
lines = subprocess.check_output(['dotnet', '--list-runtimes'], text=True).splitlines()
installed = {Path(line.split('[', 1)[1].removesuffix(']')).parents[1]
             for line in lines if line.startswith('Microsoft.NETCore.App 10.0.1 [')}
assert len(installed) == 1
installed = installed.pop(); target = root / 'shared/Microsoft.NETCore.App/10.0.1'
target.mkdir(parents=True)
shutil.copy2(installed / 'dotnet', root / 'dotnet')
(root / 'host').symlink_to(installed / 'host', target_is_directory=True)
for file in (installed / 'shared/Microsoft.NETCore.App/10.0.1').iterdir():
    if file.name not in ['libcoreclr.so', 'libclrjit.so', 'System.Private.CoreLib.dll']:
        (target / file.name).symlink_to(file)
native = w / 'runtime-10.0.1/artifacts/obj/coreclr/linux.x64.Release'
shutil.copy2(native / 'dlls/mscoree/coreclr/libcoreclr.so', target / 'libcoreclr.so')
shutil.copy2(native / 'jit/libclrjit.so', target / 'libclrjit.so')
shutil.copy2(w / 'runtime-10.0.1/artifacts/bin/coreclr/linux.x64.Release/IL/System.Private.CoreLib.dll', target / 'System.Private.CoreLib.dll')
PY
python3 -B tools/physics/clr-memory-probe/check.py "$clr_probe" bin/clr-memory-new
```

The matching managed `FeatureXplatEventSource=false` is required when native
Xplat tracing is disabled. Otherwise XplatEventLogger can call a missing QCall
before Main, even in an uninstrumented source build. PerfTracing stays enabled.
The source-built CoreLib reports `.NET 10.0.1-dev`; receipts identify its exact
binary alongside the CLR/JIT, source archive, patch and producer identities.

Thirteen bounded children cover workstation/server GC, source/SDK baselines,
disabled profiling, an enabled concurrent collector, invalid inputs, a deliberately
late producer and bounded-heap OOM. The shared 29 dynamic, 13 frozen, 14 native and
seven common phases preserve the same byte oracles as NativeAOT. Frozen snapshots
include their dynamic contribution under one native gate, avoiding allocations
between separate observations during CLR interop/JIT preparation. Maps must show
the copied CLR and exactly one production native ledger.

`tests/NativeMemory.SharedHost` also runs with `PublishAot=false` against this
runtime and profiler. Its prepared-read measurement disables tiered compilation.
Put its copied memory bundle first in LD_LIBRARY_PATH so
the loader and engine resolver select the same file. It checks public codec
startup, full native drain and prepared zero-byte reads. The existing NativeAOT
profile uses the same host with `PublishAot=true`.

This profile qualifies its reservation, GC and registry accounting boundaries.
Concurrent collection, complete module/unload and native-library ownership,
observer cost, foreign platforms and shipping runtime delivery remain open.
It does not expose the public memory selectors. See the
[producer coverage](../../../docs/coverage/performance-producers.md#coreclr-reservation-and-frozen-lifetime-producer).
