# NativeAOT allocation and GC producer probe

This Linux x64 / NativeAOT 10.0.1 correctness diagnostic builds a separate pinned
runtime copy. It observes successful object/string/array fast allocations and the
common allocator, including pinned/direct and finalizable allocations. At GC end
it enumerates actual surviving dynamic-heap objects while the EE is stopped.
Reads copy cached native counters; they neither collect nor walk the heap.

The consumer checks generation 0/1/2 retirement, retained payloads, finalization,
an unseen temporary peak, two retained workers and two allocation workers
interleaved with induced collections. A midpoint barrier requires a collection
before either worker finishes its remaining 16 batches. It also checks an over-limit array and a
one-shot injected failure before a pinned allocation: no publication on failure,
a successful retry and complete later retirement. The injection exercises failure
propagation without exhausting host memory; it does not qualify real OS exhaustion.
Twenty thousand prepared reads must allocate zero managed bytes on the caller.

Only the copied runtime is instrumented. The installed SDK, package archives and
Electron2D runtime are unchanged. The patcher checks all three source hashes and
replacement anchors before writing. Its checker also rejects a bad pin without
partial writes and rejects reapplication. The consumer project replaces both the
NativeLibrary input and its already-materialized linker argument in the original
link order; each publish retains the actual linker inputs. Replacing the item
alone leaves the original SDK archive linked and correctly fails the hook control.

The [upstream runtime](https://github.com/dotnet/runtime/tree/v10.0.1) is MIT
licensed. Its archive digest is
`4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092`.
The three original file pins remain in patch_runtime.py. Instrumentation preserves
the allocator's successful return value, callee-saved registers and slow/OOM frame
paths. The [pinned helper kill set](https://github.com/dotnet/runtime/blob/v10.0.1/src/coreclr/jit/codegencommon.cpp#L705-L762)
uses the standard caller-trash ABI for these allocation helpers. Observer calls
never enter managed code or trigger GC.

Run from the repository root with .NET 10.0.1 NativeAOT prerequisites, a C++
compiler, CMake and Ninja. The native runtime build also needs its platform build
prerequisites. This configuration retains EventPipe/performance tracing and turns
off the optional LTTng provider so missing LTTng development headers do not prevent
this diagnostic. It does not qualify a stock runtime's tracing configuration.
Allow about 2 GiB of temporary disk space; remove only your own temporary folder
after inspecting the new output:

```bash
set -e
probe_root=$(mktemp -d /tmp/e2d-aot-memory-XXXXXX)
curl -fsSL https://codeload.github.com/dotnet/runtime/tar.gz/refs/tags/v10.0.1 -o "$probe_root/runtime.tar.gz"
printf '%s  %s\n' 4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092 "$probe_root/runtime.tar.gz" | sha256sum --check
# Python's data filter rejects archive entries escaping the owned extraction root.
python3 -c 'import sys,tarfile; tarfile.open(sys.argv[1]).extractall(sys.argv[2], filter="data")' "$probe_root/runtime.tar.gz" "$probe_root"
runtime_root="$probe_root/runtime-10.0.1"
bash "$runtime_root/src/coreclr/build-runtime.sh" -release -arch x64 -component nativeaot \
  -numproc 4 -skipgenerateversion -ninja -cmakeargs '-DFEATURE_EVENTSOURCE_XPLAT=0 -DFEATURE_PERFTRACING=1'
runtime_libs="$runtime_root/artifacts/obj/coreclr/linux.x64.Release/nativeaot/Runtime/Full"
cp "$runtime_libs/libRuntime.WorkstationGC.a" "$probe_root/unmodified.WorkstationGC.a"
python3 -B tools/physics/aot-memory-probe/patch_runtime.py "$runtime_root" "$probe_root/runtime.patch"
cmake --build "$runtime_root/artifacts/obj/coreclr/linux.x64.Release" \
  --target Runtime.WorkstationGC Runtime.ServerGC -j 4
c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -pthread -c \
  tools/physics/aot-memory-probe/producer.cpp -o "$probe_root/producer.o"
for role in probe server source-baseline sdk-baseline concurrent; do
  server_gc=false; concurrent_gc=false; archive="$runtime_libs/libRuntime.WorkstationGC.a"
  artifact="managed-$role"
  case "$role" in
    probe) artifact=managed ;;
    server) server_gc=true; archive="$runtime_libs/libRuntime.ServerGC.a" ;;
    source-baseline) archive="$probe_root/unmodified.WorkstationGC.a" ;;
    sdk-baseline) archive= ;;
    concurrent) concurrent_gc=true ;;
  esac
  dotnet publish tools/physics/aot-memory-probe/AOTMemoryProbe.csproj -c Release -r linux-x64 \
    -p:ProbeNativeObject="$probe_root/producer.o" -p:ProbeRuntimeArchive="$archive" \
    -p:ServerGarbageCollection="$server_gc" -p:ConcurrentGarbageCollection="$concurrent_gc" \
    --artifacts-path "$probe_root/$artifact" -o "$probe_root/$role"
done
python3 -B tools/physics/aot-memory-probe/check.py "$probe_root" bin/aot-memory-new
rm -rf "$probe_root"
```

Workstation and two-heap server GC use the same 29 numeric witness checks. Seven
isolated children include the two uninstrumented runtime controls, concurrent-GC
refusal, an invalid watch and an invalid snapshot. Each has a 90-second process-group
deadline. The checker retains maps, actual link arguments and source/archive/binary
hashes, and requires native execution without CoreCLR. Rebuilding from a fresh
archive is required after changing the patcher; it intentionally refuses to patch
an already instrumented tree.

The dynamic ledger explicitly excludes frozen/image objects. DiagWalkHeap also
visits frozen segments, so each visit is classified with IsInFrozenSegment and
reported separately. Those GC-time image censuses do not qualify allocation/load
lifetime or a complete process peak. Reads retain a dynamic-heap high-water mark
from successful allocation events rather than a maximum of sampled GC snapshots.

The full heap walk runs at every collection and requires non-concurrent GC. Its
O(total heap) collection cost, native observer overhead and changed runtime build
must be measured before production use. NativeAOT image/frozen ownership,
concurrent GC, over-4-GiB allocation sizes, real OS exhaustion, other platforms and
coherent production native/managed publication remain open. This fixture does not
wire a partial counter to GetMonitor. See [the audit](../../../docs/coverage/performance-producers.md#nativeaot-success-and-retirement-producer).
