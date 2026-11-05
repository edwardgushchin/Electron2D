# Public physics timing

This project references the runtime as an ordinary consumer. It has no friend
access, backend dependencies, private reflection, internal profiling controls or
private timing counters. Performance intervals call the public Electron2D API.

## Complete scene frames

The `scene` mode measures `SceneTree.PhysicsFrame` on an explicitly selected CPU
or GPU world, with four walls, awake rigid bodies, contact events, eight-contact
caps and interpolation. Each fresh process warms 1600 and measures 256 frames
at fixed 1/144 second, with sixteen solver iterations. It checks completed ticks,
scene/direct-state agreement, finite and awake bodies, containment, penetration,
energy, contacts and zero warmed owner/process managed allocations. Physical
validation and cleanup are outside the timed interval.

```powershell
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe scene cpu 16384 bin/scene-cpu-16384.json
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe scene gpu 16384 bin/scene-gpu-16384.json
```

The scene mode also rejects accidental runtime friend access at startup. It does
not produce private solver-stage timings or window FPS. Native device/driver and
host-load observations are separate environment evidence, outside physics timing.

## Direct-space queries

This public-only consumer measures every public query overload declared by
`PhysicsDirectSpaceState`: ray, point (array/span), shape (array/span), motion
cast, collision (array/span), and rest info. Startup compares the registered cases
against the compiled class and rejects missing or duplicate overloads.

Publish once in Release, then use fresh processes for each backend and body count:

```powershell
dotnet publish tests/PhysicsQueryPerformance.Consumer -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=10.0.14 -o bin/physics-query-timing
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe cpu 256 bin/query-cpu-256.json
bin/physics-query-timing/PhysicsQueryPerformance.Consumer.exe gpu 256 bin/query-gpu-256.json
```

Keep the runtime version, compiled runtime, native dependencies, host, fixture and
settings identical within each comparison. Record the actual selected GPU adapter
and driver separately. A software device does not measure hardware GPU speed.
The ordinary native asset prerequisites still apply to the GPU profile.

The fixture uses an unchanged static circle grid and executes both a one-collider
hit and a distant miss. Each overload warms 64 batches and measures 128 batches of
64 calls. Positive/negative results and collider identity are checked outside the
clock; every timed result contributes to the checked checksum. Array overloads
retain their allocation contract; span storage is caller-owned and reused.

The JSON reports unadjusted wall nanoseconds per call, including loop/delegate
overhead, and a separate empty-loop baseline. Percentiles describe batch means,
not individual calls. Synchronous query results include necessary transfers and
completion. Managed bytes cover the measured owner/process interval; they do not
describe retained, native or device memory. Setup, validation, reporting and
cleanup are outside the clock. No collection is forced in a timed interval.

These repeated warmed queries do not measure cold/edited-world queries, every
physical API method, solver stages, networking or window FPS. Full steps belong
to the separate scene profile above. Full per-method CPU/GPU comparison remains
open until the other physical method families have executable cases and matched
hardware pairs. See the [current measurements](../../docs/components/physics-current-performance.md).
