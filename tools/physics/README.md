# Physics measurement helpers

## Managed heap observations

The standalone [managed-memory probe](managed-memory-probe/ManagedMemoryProbe.csproj)
compares warmed snapshot-query allocations and a temporary 64 MiB managed peak,
then observes native-runtime GC event delivery. It uses no Electron2D runtime,
window or GPU. Its forced collections belong only to the counterexample and
must not be copied into a monitor implementation.

```bash
dotnet run --project tools/physics/managed-memory-probe/ManagedMemoryProbe.csproj -c Release
dotnet publish tools/physics/managed-memory-probe/ManagedMemoryProbe.csproj -c Release -r linux-x64 -p:PublishAot=true -p:TrimMode=full -o bin/managed-memory-probe
bin/managed-memory-probe/ManagedMemoryProbe
```

Both hosts explicitly enable EventSource and use nonconcurrent workstation GC.
The output is one JSON observation record; a missing transient-peak witness
fails the process. Event callbacks and allocation ticks are observations, not
an exact aggregate allocation/free producer. See the [source qualification](../../docs/coverage/performance-producers.md#managed-heap-source-qualification)
for current evidence and its limits.

## Native GPU device retirement

The standalone [SDL lifetime probe](../../tests/Electron2D.Tests/Native/GPUDeviceLifetime.c)
uses two Vulkan GPU devices and one claimed window. It loads the exact SDL library
supplied by the caller; no managed runtime, physics, shaders or frame submission
is involved. Run each profile in a fresh process with a chosen ICD and video
driver, a deadline and core dumps disabled:

```bash
mkdir -p bin/physics-device-lifetime/local
cc -std=c11 -O0 -g -Wall -Wextra -Werror tests/Electron2D.Tests/Native/GPUDeviceLifetime.c -ldl -o bin/physics-device-lifetime/local/probe
ulimit -c 0
SDL_VIDEODRIVER=wayland GDK_BACKEND=wayland timeout -k 5s 90s bin/physics-device-lifetime/local/probe /absolute/path/to/libSDL3.so.0 0 0 0 1
```

The four trailing flags are `compute-first`, `render-retires-first`,
`extra-loader-reference` and `extra-video-reference`, each exactly `0` or `1`.
The probe prints the loaded SDL version/revision and each lifecycle stage. Its
1.2-second delay lets an external sampler observe driver mappings; it is not a
performance measurement. Zero means complete normal retirement; `2` reports an
SDL operation failure and `3` rejects arguments or library/symbol loading.
A signal termination remains a failed lifecycle profile. Compare both creation
and retirement orders, then repeat with `SDL_VIDEODRIVER=x11` when available.
The [native isolation evidence](../../docs/components/gpu-device-lifetime.md#direct-native-lifetime-control)
records the current failing Wayland and passing X11 controls without qualifying
the independent-device experiment.

The public-only consumer selects `--device-window-lifetime`. Add `--render-first`
to start its explicit GPU space after native renderer startup, and
`--render-retire-first` to retire the renderer before freeing that space;
`--threaded` selects the separate numerical thread. The fixture uses SpaceCreate,
SpaceStep and FreeRID with Engine.Run, verifies one-unit motion at 60 units/s
over 1/60 second, and checks continued GPU execution after renderer retirement.
Use the same fresh-process, ICD, video-driver, deadline and core-dump policy.
Add `--churn` to keep a local RenderingDevice buffer alive across three native
window lifetimes. The current shared-device policy passes all 16 public CLR
profiles. The historical two-device C controls still reproduce the native defect;
see [the ownership contract](../../docs/components/gpu-device-lifetime.md).
`ELECTRON2D_TEST_GPU_DEVICE_LIFETIME=1` selects the internal native window/RID,
lease-finalizer and cross-owner checks.

`native_allocations.c` is an optional test-only Linux/glibc allocator interposer.
It does not enter the runtime or native packages. Build and calibrate it before
running the complete scene benchmark:

```bash
mkdir -p bin/physics-native-profile/local
gcc -std=c11 -O2 -fPIC -fno-builtin -shared -Wall -Wextra -Werror tools/physics/native_allocations.c -ldl -o bin/physics-native-profile/local/libElectron2DAllocationProbe.so
gcc -std=c11 -O2 -fno-builtin -Wall -Wextra -Werror tools/physics/test_native_allocations.c -ldl -pthread -o bin/physics-native-profile/local/test_native_allocations
LD_PRELOAD="$PWD/bin/physics-native-profile/local/libElectron2DAllocationProbe.so" bin/physics-native-profile/local/test_native_allocations
```

For an instrumented benchmark, set `LD_PRELOAD` to that absolute library path,
`LD_LIBRARY_PATH` to its directory, and
`ELECTRON2D_PHYSICS_PERFORMANCE_NATIVE_ALLOCATIONS=1`. The performance selector
is `ELECTRON2D_TEST_PHYSICS_PERFORMANCE=1`; backend, count, warmup, samples and
output use the existing `ELECTRON2D_PHYSICS_PERFORMANCE_` settings.
`ELECTRON2D_PHYSICS_PERFORMANCE_THREADED=0` or `1` explicitly selects the numerical
lane; absence retains the project setting. Use a fresh process per profile.

The managed probe verifies actual allocator attachment, the five-counter ABI and
an exact 4-allocation/131-byte/3-free/1-reallocation calibration before scene setup.
The native check additionally verifies a worker thread, nested/inactive windows,
failed reallocation and glibc's zero-size reallocation release. Missing preload
must fail rather than report a misleading zero.

Schema 5 records optional `nativeLibcTraffic` counters across **all process
threads while each complete PhysicsFrame is bracketed**. Successful malloc,
calloc, realloc and aligned requests increment Allocations and RequestedBytes;
successful realloc counts its entire requested size, even if the pointer stays
unchanged. Reallocations counts resize calls, including failures and zero-size
release. Frees counts explicit non-null free calls and realloc-to-zero releases;
internal realloc replacement frees are not observable. Failures counts failed
requests except the successful glibc realloc-to-zero release. Calls are accounted
when their underlying allocation completes, or when free enters the wrapper.
The short shared counter lock also covers driver/CLR workers and adds overhead.

These are requested allocator traffic, **not live, retained or peak memory**.
Direct mapping, device memory and private/non-interposed allocators are outside
the probe. Window pumping, rendering, setup and teardown are outside the physics
window. Worker activity completing after the window is outside its count.
Do not subtract frees from Allocations to infer leaks or attribute the whole
process total to engine-owned buffers.

Measure ordinary timing in a separate fresh process with the flag unset and
both preload variables removed. Keep identical scenes, full physical/publication
checks, source and executable fingerprints; label instrumented timings separately.
Hardware capability, real-window FPS and target mass-scene advantage require
their own profiles. The current bounded software-driver evidence and its limits
are in [complete scene measurements](../../docs/components/physics-scene-performance.md#scoped-native-allocation-profiling-2026-10-14).

### Grouped world references

Run `--group-worlds --cpu|--gpu` (omit either for the registered provider), with
optional `--threaded` and `--pool-owner`, in PhysicsBackendExtension.Consumer.
The root selector is `ELECTRON2D_TEST_GROUP_WORLDS=cpu|gpu|registered`, optionally
suffixed with `-threaded` and `-pool-owner`. It verifies cold concurrent logical
world publication, lifetime/startup guards and 128 complete warmed physics/query
frames at zero owner/all-thread managed bytes. See [worlds](../../docs/components/worlds.md#world-observations-from-processing-groups).

### Grouped canvas modulation

Run public `--group-modulation --cpu|--gpu` (omit backend for the registered
provider), optionally `--threaded` and `--pool-owner`. The root selector is
`ELECTRON2D_TEST_GROUP_MODULATION=cpu|gpu|registered` with matching suffixes.
Native pixels use `--native-group-modulation --cpu|--gpu`, optional `--threaded`,
and `ELECTRON2D_GROUP_MODULATION_RENDERER=gpu|compatibility`; use the corresponding
`ELECTRON2D_TEST_NATIVE_GROUP_MODULATION` root selector. See
[grouped canvas diagnostics](../../docs/components/canvas-rendering.md#grouped-canvas-modulation-and-diagnostics).

### Grouped coordinate observations

`--group-coordinates [--cpu|--gpu] [--threaded] [--pool-owner]` checks logical
canvas/viewport/window coordinate families with actual selected physical frames and
queries. Omitting the backend selects the registered provider. The root selector is
`ELECTRON2D_TEST_GROUP_COORDINATES=cpu|gpu|registered[-threaded][-pool-owner]`.
`--native-group-coordinates --cpu|--gpu [--threaded]` runs actual window pixels;
`ELECTRON2D_GROUP_COORDINATES_RENDERER=gpu|compatibility` selects rendering independently.
The root native selector is `ELECTRON2D_TEST_NATIVE_GROUP_COORDINATES=cpu|gpu[-threaded]`.
See [the coordinate contract](../../docs/components/canvas-rendering.md#coordinate-observation-from-processing-groups).
