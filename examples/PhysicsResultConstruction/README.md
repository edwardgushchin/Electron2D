# Physics result construction

A separate executable consumer using only the public Electron2D API. It constructs
ray, point, shape, rest and reusable motion results for scene and raw colliders.
The library samples object associations from live RIDs; no numeric object identity
is accepted from the caller. Construction supplies geometry and does not execute
collision detection.

Run the common contract checks:

```bash
dotnet run --project examples/PhysicsResultConstruction -c Release
dotnet run --project examples/PhysicsResultConstruction -c Release -- gpu
```

The checks cover logical shape indices, scene/raw identity, rebinding, disposal,
weak collection, stale/wrong resources, nonfinite data, motion fraction ordering,
cross-space/thread rejection, synchronized callbacks, failed GPU-world rejection,
atomic replacement, ordinary BodyTestMotion and resource cleanup. Each backend
runs 128 warmed complete construction/hit/miss cycles with zero owner/all-thread
managed allocation. Numeric comparisons check exact supplied values, not solver
agreement. CPU needs no renderer; GPU requires an available compute device.

These checks establish result filling only. Registration, custom geometry and
backend/direct-state extension dispatch remain unimplemented. Native allocations,
other platforms, whole-step speed and real-window FPS are separate gates.
