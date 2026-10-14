# Public space-query extension check

This separate executable references only public Electron2D API. Its analytic circle query implementation derives from PhysicsDirectSpaceStateExtension, reads actual scene/raw/Area poses and implements all six query hooks. It changes query policy and checks inherited outputs, filters, scoped nesting, invalid results, borrowed lifetime and warmed allocation. Its limited circle geometry is fixture code, not a built-in runtime backend.

```bash
dotnet run --project tests/PhysicsSpaceExtension.Consumer/PhysicsSpaceExtension.Consumer.csproj -c Release -- cpu
dotnet run --project tests/PhysicsSpaceExtension.Consumer/PhysicsSpaceExtension.Consumer.csproj -c Release -- gpu
```

The CPU no-device profile clears DISPLAY and WAYLAND_DISPLAY and uses SDL_VIDEODRIVER=dummy plus an unavailable SDL_GPU_DRIVER. Release disables tiered compilation for stable warmed measurements. The fixture warms complete six-query cycles for at least one second after collection, in batches of 64, then measures 64 identical cycles using owner/all-thread counters. Caller-owned nonempty array tests and cold failure/thread tests stay outside that interval.

This proves caller-created views; complete numerical-extension acceptance remains separate work.


Add `--threaded` to select the separate numerical lane. ShapeLifetimeChecks covers
resource and owned-RID inputs for all four shape queries, inherited copy/reset and
geometry/policy/lifetime refusal inside hooks, shared nested inputs, failed geometry
preparation, preparation-time parameter edits, callback/validation errors and later authoring. It measures an extra
128 complete numerical frames plus four scalar/span queries after 128 warm frames,
requiring zero owner/all-thread managed bytes and actual 256-unit motion within .02.
Nonempty array overloads and user-created worker/failure checks remain cold checks.
