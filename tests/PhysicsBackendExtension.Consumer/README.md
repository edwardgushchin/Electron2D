# Independent physics backend consumer

This executable references only the public Electron2D API. It registers a bounded circle/spring test solver through PhysicsServerManager and owns numerical bodies, contact/query geometry, constraints and private history. It does not wrap a built-in solver and is not a production physics option.

```sh
dotnet run --project tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj -c Release -r linux-x64 -p:Electron2DBuildNativeFromSource=true
```

Checks cover strict default priorities, lazy captured World aliases, explicit selections, reserved/duplicate names, factory failures and recursion, outside-lock registration, scene/raw full steps, real queries/exclusions, Area/contact events, spring response, local/portable correction, terminal solve failure and cleanup aggregation. After 64 warmup frames, 64 complete frames with raw callbacks require zero owner/all-thread managed bytes. A headless no-GPU run uses SDL_VIDEODRIVER=dummy and an unavailable Vulkan ICD.

The circle/spring model intentionally exercises a bounded fixture. Arbitrary custom geometry, other shapes/constraints, unexercised extension operations, physical platforms, native allocator totals, real rendering and final GPU performance acceptance remain separate requirements.

Initialization/Finish/disposal failure injection retains all three errors after retirement. Scene diagnostic capture executes the selected ReadDebugContacts hook after synchronization; invalid count or nonfinite points reject before diagnostic publication and fail the started interval.
