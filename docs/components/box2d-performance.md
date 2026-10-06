# Box2D backend performance

Last updated: 2026-10-06

## Fixed workload and comparison

[The managed benchmark](../../tests/Box2D.Performance/Program.cs) and [native oracle](../../tools/physics/benchmark_native.c) create the same enclosed tank, 1,536 always-awake circles, material, density, damping, initial velocities, gravity, contact thresholds and fixed 1/144-second interval with four substeps. Initial rows fit inside the tank; final positions are checked. Each fresh process warms 1,600 ticks and samples 1,024 ticks. The final topology has 14,520 potential contacts and 3,944 colored/overflow solving contacts. This is a comparable dense workload, not a reconstruction of the user's earlier 5.71/6.53 ms run whose exact command was unavailable.

Linux x64, Ryzen 7 5700X, .NET 10.0.1, Release with `DOTNET_TieredCompilation=0`. Three sequential trials per configuration; the table reports medians of the trial means and trial p99 values, not a percentile of pooled samples. Desktop load is uncontrolled; raw trials are retained in ignored `bin/physics-performance/results.json`. The baseline is `408edc4e`, which already contains partial four-lane SIMD; earlier scalar and preliminary sparse/tower runs are excluded from the final comparison.

The native source is pinned at [`c05c48738fbe5c27625e36c5f0cfbdaddfc8359a`](https://github.com/erincatto/box2d/tree/c05c48738fbe5c27625e36c5f0cfbdaddfc8359a), the native main snapshot before the managed source pin's date. Build: GCC/CMake Release, `BOX2D_AVX2=ON`, samples/tests/benchmarks disabled; the native comparator uses one worker. This does not assert whole-port equivalence or native multithread throughput.

| Backend | Workers | Mean ms | p99 ms | Managed bytes, all threads |
| --- | ---: | ---: | ---: | ---: |
| Previous managed backend | 1 | 5.431 | 6.437 | 0 |
| Optimized managed backend | 1 | 1.910 | 2.393 | 0 |
| Optimized managed backend, runtime policy | 4 | 0.933 | 1.403 | 0 |
| Optimized managed backend, experiment | 8 | 0.799 | 1.397 | 0 |
| Native Box2D, AVX2 | 1 | 1.192 | 1.619 | Not measured |

Four workers improve the mean by about 5.8 times. Their p99 leaves about 5.54 ms of the 6.94 ms budget for work outside this raw kernel. Eight workers have a lower mean but nearly the same median p99 and a worse maximum trial p99 (1.852 versus 1.534 ms), so the runtime retains four. This is a measured policy for this host, not a universal optimum. One managed worker remains slower than the one-worker native oracle.

Every managed configuration and the native oracle produce the same complete final position, rotation and velocity bytes for all 1,536 bodies: SHA-256 `19F4255E0256541FA813CFF94F8738FAD5602021960BE461AE7283BAC0B819CD`. Managed allocation brackets exclude construction, lane checks and report serialization, and include all worker threads via `GC.GetTotalAllocatedBytes(true)`. Native memory allocation is not measured.

## Runtime change and rejected candidates

The contact solver now stores eight-lane intrinsic vectors, vectorizes arithmetic/comparisons/selection and avoids absent second-point normal/friction work. Separate multiply/add, scalar NaN/signed-zero selection and the actual contact algorithm remain intact. Eight-body gather/scatter, contact indices and layout assertions change together. The serial stage avoids work-stealing synchronization. A retained per-world task adapter splits collision/finalization ranges and runs the existing colored solver tasks. Worlds with fewer than 256 awake bodies and browser hosts use one worker; other worlds use up to four available processors. Workers start on first parallel use, park between intervals, and join on disposal. Game callbacks stay on the owner; one-way pair state is synchronized internally.

`Task.Run` per backend job regressed the first experimental workload to 1.80 ms mean/9.02 ms p99 with managed allocation, and was removed. Retained workers and job storage avoid that cost. A Debug population-transition assertion initially assumed active worker count equaled allocated context capacity; it now checks the active prefix against retained capacity. Debug lane/parallel tests exercise this transition. Fatal constraint-job errors signal peer cancellation; resuming a partly solved world is not promised.

## Reproduce

```bash
DOTNET_TieredCompilation=0 dotnet run --project tests/Box2D.Performance -c Release -- 4
```

Arguments 1, 2, 4 and 8 select test-only worker counts. The benchmark compiles the vendored source and owning task adapter directly without native game packages. Engine integration is checked separately. To build the recorded serial baseline, extract `src/Vendor/Box2D.NET` from `408edc4e` into a temporary directory, then build the same benchmark with `-p:SerialBaseline=true -p:VendorRoot=/absolute/path/to/extracted/src/Vendor/Box2D.NET -p:EnableNETAnalyzers=false`. The last flag accommodates upstream-only analyzer diagnostics outside the repository's vendor configuration; it does not change emitted physics code.

```bash
git clone https://github.com/erincatto/box2d.git /tmp/box2d-native
git -C /tmp/box2d-native checkout c05c48738fbe5c27625e36c5f0cfbdaddfc8359a
cmake -S /tmp/box2d-native -B /tmp/box2d-native/build -DCMAKE_BUILD_TYPE=Release -DBOX2D_AVX2=ON -DBOX2D_SAMPLES=OFF -DBOX2D_UNIT_TESTS=OFF -DBOX2D_BENCHMARKS=OFF
cmake --build /tmp/box2d-native/build -j 4
cc -O3 -ffp-contract=off -DNDEBUG -I /tmp/box2d-native/include tools/physics/benchmark_native.c /tmp/box2d-native/build/src/libbox2d.a -lm -o /tmp/box2d-native-benchmark
/tmp/box2d-native-benchmark /tmp/box2d-native-state.bin
sha256sum /tmp/box2d-native-state.bin
```

## Integration and limits

The full Release runtime tests and focused optimized Debug lane/parallel tests pass. PhysicsParallelTests covers uneven ranges, task reuse/error/cancellation, actual 288-body one-way contacts, owner integration/events, freeze/unfreeze and serial/parallel transitions. The benchmark with `DOTNET_EnableAVX=0` also preserves the final state and zero all-thread managed allocation; this is a local fallback check, not execution on a foreign CPU.

The actual PhysicsSandbox 1,024-body scene was measured separately at 60 Hz through SceneTree and native Wayland GPU presentation, with normal/debug trials and zero sampled owner-thread managed frame bytes. The final isolated headless run measured 5.610 ms fixed mean/6.593 ms p95, of which 0.913 ms was the backend step (`bin/physics-sandbox/profile-Release-box2d-final-isolated.json`). Synchronization, contact reports, queries and scene/UI callbacks remain a material cost. The native normal/debug trial recorded 57.1/45.6 FPS under desktop load, with exactly zero maximum sampled owner frame bytes (`profile-Release-box2d-final.json`); its fixed sample overlapped a deliberately slow no-intrinsics check and is not used as a steady throughput result. These raw kernel gains do not establish 144 rendered FPS. Foreign platforms, native heaps, physical interaction and owner visual acceptance remain unverified.
