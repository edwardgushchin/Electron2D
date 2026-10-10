# Public space-query extension check

This separate executable references only public Electron2D API. Its analytic circle query implementation derives from PhysicsDirectSpaceStateExtension, reads actual scene/raw/Area poses and implements all six query hooks. It changes query policy and checks inherited outputs, filters, scoped nesting, invalid results, borrowed lifetime and warmed allocation. Its limited circle geometry is fixture code, not a built-in runtime backend.

```bash
dotnet run --project tests/PhysicsSpaceExtension.Consumer/PhysicsSpaceExtension.Consumer.csproj -c Release -- cpu
dotnet run --project tests/PhysicsSpaceExtension.Consumer/PhysicsSpaceExtension.Consumer.csproj -c Release -- gpu
```

The CPU no-device profile clears DISPLAY and WAYLAND_DISPLAY and uses SDL_VIDEODRIVER=dummy plus an unavailable SDL_GPU_DRIVER. Release disables tiered compilation for stable warmed measurements. The fixture warms complete six-query cycles for at least one second after collection, in batches of 64, then measures 64 identical cycles using owner/all-thread counters. Caller-owned nonempty array tests and cold failure/thread tests stay outside that interval.

This proves caller-created views; registration of a complete custom physics backend remains separate work.
