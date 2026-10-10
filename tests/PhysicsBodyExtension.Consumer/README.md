# Public body-state extension check

This separate executable references only public Electron2D API, with no friend assembly or backend packages. ControlledState implements every required state/force/contact/space hook. A bit mask verifies the entire family is reached through inherited public operations. Its impulse hook doubles the physical impulse on a two-kilogram raw body; its contact hook captures an actual scene floor contact in PhysicsBodyContact.

```bash
dotnet run --project tests/PhysicsBodyExtension.Consumer/PhysicsBodyExtension.Consumer.csproj -c Release -- cpu
dotnet run --project tests/PhysicsBodyExtension.Consumer/PhysicsBodyExtension.Consumer.csproj -c Release -- gpu
```

The CPU no-device profile removes DISPLAY/WAYLAND_DISPLAY and uses SDL_VIDEODRIVER=dummy and an unavailable SDL_GPU_DRIVER. Release disables tiered compilation. After collection, complete unchanged operation cycles warm for at least one second in batches of 64; 64 further cycles must allocate 0 owner/all-thread managed bytes. Cold failure/thread/structural work remains outside that interval.

Caller-created extensions are exercised here. Registered custom server factories and factory-returned callback views remain separate unfinished work.
