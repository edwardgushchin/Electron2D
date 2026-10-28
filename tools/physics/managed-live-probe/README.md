# Managed live-memory producer probe

This Linux x64 / CoreCLR 10.0.1 diagnostic tracks successful dynamic GC-heap
allocation callbacks through generation 0/1/2 collection, compaction, pinned and
large arrays, finalization and worker-thread lifetime. It retains a callback-ledger
high-water mark even when no snapshot observes the temporary objects. It is a
separate test host; it does not implement the public memory monitor.

Run from the repository root with the selected .NET 10.0.1 runtime/SDK and C++
compiler. Use a fresh output directory and remove only the temporary build:

```bash
live_probe_dir=$(mktemp -d)
curl -fsSL https://raw.githubusercontent.com/dotnet/runtime/v10.0.1/src/coreclr/inc/corprof.idl \
  -o "$live_probe_dir/corprof.idl"
c++ -std=c++17 -O2 -Wall -Wextra -Werror -fPIC -shared -pthread \
  tools/physics/managed-live-probe/profiler.cpp -o "$live_probe_dir/libElectron2DLiveMemory.so"
dotnet build tools/physics/managed-live-probe/ManagedLiveProbe.csproj -c Release \
  --artifacts-path "$live_probe_dir/managed"
python3 -B tools/physics/managed-live-probe/check.py \
  "$live_probe_dir/libElectron2DLiveMemory.so" \
  "$live_probe_dir/managed/bin/ManagedLiveProbe/release/ManagedLiveProbe" \
  "$live_probe_dir/corprof.idl" bin/managed-live-new
rm -rf "$live_probe_dir"
```

The checker verifies the pinned IDL digest and used COM slots, pins the apphost's
runtime root, and retains process maps, binary/source hashes and raw logs.
Workstation GC and two-heap server GC execute the same 25 numeric witness checks,
including two retained workers and two allocation workers overlapping forced
collections and reads. The three rejection children cover profiling disabled,
an invalid tag and an invalid snapshot. Each child has a 90-second deadline with
cleanup of its own process group. Twenty thousand prepared snapshots must allocate
zero managed bytes on their calling thread. The byte oracles use eight-byte
Linux x64 object alignment; 65 strings occupy 18070 raw / 18200 aligned bytes.

The native producer uses ICorProfilerInfo4::GetObjectSize2 and
ICorProfilerCallback4::MovedReferences2/SurvivingReferences2, retaining pointer-sized
sizes and range lengths. Old object addresses remain keys until every range has
been applied, so overlapping relocations cannot overwrite another object. Only
objects in collected generations can retire. Finalization-queued objects remain
live until a later collection actually reclaims them. The mutex protects only
native bookkeeping and non-GC-triggering profiler queries; no managed callback or
collection occurs under it. Diagnostic map storage belongs to the profiler and
is outside the observed managed-byte domain.

This profiling mode enables MONITOR_GC, which disables concurrent GC. Forced
collections are separated by a 1 ms mutator interval: a continuous induced-GC
loop starved allocation workers and hit its deadline. The workload still verifies
that each worker commits and retires all 32768 allocations. This is a correctness
diagnostic, not a performance claim.

Frozen/non-GC objects are explicitly counted as excluded from this experiment;
ordinary GC callbacks cannot qualify their unload lifetime. A frozen tagged
witness is refused. The initial cold string/type setup also allocated an extra
32-byte object, so witness preparation is untagged and drained before the numeric
intervals. These exclusions must not be copied into the full monitor contract.

The probe does not qualify NativeAOT, frozen/image-heap accounting, allocations
above 4 GiB, failed-allocation rollback, runtime/observer native overhead or the
coherent production native/managed peak at actual allocator commit boundaries.
Its global snapshot is the observed dynamic-heap callback ledger, not complete
process memory. See the [producer audit](../../../docs/coverage/performance-producers.md#clr-dynamic-heap-lifetime-producer)
for the current result and remaining implementation boundary.
