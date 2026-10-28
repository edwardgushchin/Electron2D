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
A separate 13-phase frozen fixture checks registration failure, empty registration,
growth/shrink, commit-only/no-op updates, actual object payloads, unregister, an
unobserved peak and disjoint/overlapping dynamic/frozen peaks. Its 64-cycle worker
keeps one segment live at the forced-GC midpoint.
A further 14-phase native fixture shares the observer's native gate with managed
and frozen publication. Native malloc/realloc/free and their ledger commit occur
inside the same critical section, with no managed callbacks. Fast object and
string/array paths acquire that gate before changing their allocation pointer;
the successful observer releases it after publishing. A native assembly shim
preserves the live volatile GPRs and stack alignment across acquisition. A
thread-local assertion rejects a fast publication that missed this gate.

Only the copied runtime is instrumented. The installed SDK, package archives and
Electron2D runtime are unchanged. The patcher checks all four source hashes and
replacement anchors before writing. Its checker also rejects a bad pin without
partial writes and rejects reapplication. The consumer project replaces both the
NativeLibrary input and its already-materialized linker argument in the original
link order; each publish retains the actual linker inputs. Replacing the item
alone leaves the original SDK archive linked and correctly fails the hook control.

The [upstream runtime](https://github.com/dotnet/runtime/tree/v10.0.1) is MIT
licensed. Its archive digest is
`4c0b3aa0fda67c304762a572d6172ca5b2d29bb62191cf7c0fcef40a15223092`.
The four original file pins remain in patch_runtime.py. Instrumentation preserves
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
c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -c \
  tools/physics/aot-memory-probe/frozen_fixture.cpp -o "$probe_root/frozen-fixture.o"
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
    -p:ProbeNativeObject="$probe_root/producer.o" -p:ProbeFrozenObject="$probe_root/frozen-fixture.o" \
    -p:ProbeRuntimeArchive="$archive" \
    -p:ServerGarbageCollection="$server_gc" -p:ConcurrentGarbageCollection="$concurrent_gc" \
    --artifacts-path "$probe_root/$artifact" -o "$probe_root/$role"
done
python3 -B tools/physics/aot-memory-probe/check.py "$probe_root" bin/aot-memory-new
rm -rf "$probe_root"
```

Workstation and two-heap server GC use the same 29 dynamic, 13 frozen and 14 native witness phases. Nine
isolated children include the two uninstrumented runtime controls, concurrent-GC
refusal, an invalid watch, an invalid snapshot, an invalid frozen resize and an invalid native alignment. Each has a 90-second process-group
deadline. The checker retains maps, actual link arguments and source/archive/binary
hashes, and requires native execution without CoreCLR. Rebuilding from a fresh
archive is required after changing the patcher; it intentionally refuses to patch
an already instrumented tree.

Frozen/image bytes now have a lifecycle ledger alongside the dynamic ledger.
The shared GC implementation publishes successful insertion, resize and removal
before releasing gc_lock. The observer takes its own native gate in that order,
never enters managed code and never takes gc_lock from a snapshot. Empty segments
retain identity while contributing zero bytes. Active bytes are allocated minus
first-object addresses; the extra header, unused committed/reserved storage and
observer records are outside this managed-object footprint. This avoids reading
partially initialized object lengths during a frozen-heap append. Each stopped-EE
census independently checks the sum of aligned frozen object sizes against the
lifecycle counter. The same gate publishes combined dynamic/frozen current and
high-water values; it does not sum separate peaks.

The frozen fixture allocates its own native mapping and constructs a 24-byte ordinary object
with the verified runtime layout. C# verifies its actual field before/after GC, then drops
that reference before shrinking or unregistering. A 100000-object region appears
and disappears before any counter read; its 2400000-byte peak must survive. A
later 2400024-byte dynamic array is first disjoint, then overlaps a second large
frozen region. The overlap must raise the combined peak and later retirement must
retain it. These are controlled object-footprint tests, not process RSS or native
backing-storage qualification.

The native fixture retains the existing owned allocator's requested-payload,
32-byte private prefix, alignment, zero-size and failure-preservation semantics.
It checks zero-filled payloads, grow/shrink content, all power-of-two alignments
from 1 through 4096, rejected and actual allocator failures, plus 64 worker cycles
overlapping forced GC. A 7000000-byte allocation disappears between reads and
must still raise the combined peak. A later 7000024-byte managed array first
occurs separately and then overlaps another native block. The aggregate peak
must differ from the sum of separate domain peaks. A sparse native allocation of
4294967313 bytes, grown to 4294967329, checks 64-bit accounting and endpoint
content across resize; only endpoint pages are touched, so this is not a physical
4-GiB working-set test. Twenty thousand prepared native allocation/resize/free/read
cycles must allocate zero managed caller bytes.

These are controlled single-image accounting domains. Common slow managed
allocations still publish after the GC allocator returns: their native overlap
window remains unqualified. Holding this gate across a GC-capable allocator would
deadlock collection and is not an accepted fix. The shipping C# NativeAllocation
and native-library callbacks have not been moved to this fixture's gate. No
complete production managed/native peak is claimed.

The full heap walk still requires non-concurrent GC. Its O(total heap) collection
cost, native observer overhead and changed runtime build must be measured before
production use. CoreCLR frozen/image integration, actual shared-module host/load
acceptance, concurrent GC, over-4-GiB sizes, real OS exhaustion, other platforms
and complete production native/managed publication remain open. This fixture does
not wire a partial counter to GetMonitor. See [the audit](../../../docs/coverage/performance-producers.md#nativeaot-success-and-retirement-producer).
