# Managed allocation boundary probe

This Linux x64/.NET 10 diagnostic compares the existing calibrated CLR profiler
with selected NativeAOT allocation helpers. It uses 512 retained objects, 64
retained strings and one pinned byte array. Helper-entry counts include allocation
attempts; a complete memory producer also needs successful allocation publication,
GC/free lifetime and coherent managed/native current/peak state.

The probe adds two GNU linker wrappers to its own NativeAOT executable. The object
wrapper preserves the allocation helper's tail-call ABI, and the slow wrapper
forwards the original transition frame. The installed SDK and engine runtime are
unchanged. EventSourceSupport is enabled for the published diagnostic. The CLR profiler is loaded from the exact configured path so the host
and probe share its initialized instance. The checker selects the .NET 10 runtime
root reported by the invoked CLI and requires matching CLR/NativeAOT versions.

Run from the repository root with the selected .NET 10 SDK, C/C++ compiler and
NativeAOT prerequisites. Use a new output directory:

```bash
probe_dir=$(mktemp -d)
cc -std=c11 -O2 -fPIC -shared tools/physics/allocation-boundary-probe/route.c -o "$probe_dir/libElectron2DMemoryRoute.so"
cc -std=c11 -O2 -fPIC -DE2D_NATIVEAOT -c tools/physics/allocation-boundary-probe/route.c -o "$probe_dir/route.o"
cc -c tools/physics/allocation-boundary-probe/route.S -o "$probe_dir/route-asm.o"
c++ -std=c++17 -O2 -fPIC -shared tools/ci/managed_allocation_trace.cpp -o "$probe_dir/libElectron2DManagedAllocationTrace.so" -pthread
dotnet build tools/ci/managed-allocation/Calibration.csproj -c Release --artifacts-path "$probe_dir/calibration"
dotnet build tools/physics/allocation-boundary-probe/AllocationBoundaryProbe.csproj -c Release --artifacts-path "$probe_dir/managed"
dotnet publish tools/physics/allocation-boundary-probe/AllocationBoundaryProbe.csproj -c Release -r linux-x64 \
  -p:PublishAot=true -p:PublishTrimmed=true -p:TrimMode=full \
  -p:IlcTreatWarningsAsErrors=true -p:ILLinkTreatWarningsAsErrors=true -p:TrimmerSingleWarn=false \
  -p:ProbeNativeObject="$probe_dir/route.o" -p:ProbeFastObject="$probe_dir/route-asm.o" \
  --artifacts-path "$probe_dir/native-managed" -o "$probe_dir/aot"
python3 -B tools/physics/allocation-boundary-probe/check.py \
  "$probe_dir/libElectron2DManagedAllocationTrace.so" \
  "$probe_dir/calibration/bin/Calibration/release/Calibration" \
  "$probe_dir/managed/bin/AllocationBoundaryProbe/release/AllocationBoundaryProbe" \
  "$probe_dir/aot/AllocationBoundaryProbe" bin/allocation-boundary-new
rm -rf "$probe_dir"
```

The checker first runs the existing one/two/worker/empty/stack/error calibration
on the same profiler library. Plain/traced CLR runs retain exact object counts,
raw sizes and aligned owner-byte totals. A disabled CLR profiler and a configured
NativeAOT profiler must reject the uninitialized trace bridge. NativeAOT route
counts expose fast allocation and direct pinned-array gaps; process maps and
source/binary digests stay in the output. Each owned subprocess has a bounded
process group so timeout cleanup cannot leave a calibration child running.

This fixture qualifies the selected allocation routes. It does not track GC
retirement, startup coverage, failed-allocation rollback, large-object sizing,
live heap ownership or full concurrent native/managed high-water marks. Runtime and architectural
results are in [the memory producer audit](../../../docs/coverage/performance-producers.md#runtime-allocation-hook-boundary).
