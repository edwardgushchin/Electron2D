# Direct-space query timing

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
physical API method, solver stages, full steps, networking or window FPS.
Full CPU/GPU comparison remains open until those required cases and real hardware
pairs exist. See the [current measurements](../../docs/components/physics-current-performance.md).
