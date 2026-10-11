# Independent physics backend consumer

This executable references only the public Electron2D API. It registers a bounded circle/spring test solver through PhysicsServerManager and owns numerical bodies, contact/query geometry, constraints and private history. It does not wrap a built-in solver and is not a production physics option.

```sh
dotnet run --project tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj -c Release -r linux-x64 -p:Electron2DBuildNativeFromSource=true
```

Checks cover strict default priorities, lazy captured World aliases, explicit selections, reserved/duplicate names, factory failures and recursion, outside-lock registration, scene/raw full steps, real queries/exclusions, Area/contact events, spring response, local/portable correction, terminal solve failure and cleanup aggregation. After 64 warmup frames, 64 complete frames with raw callbacks require zero owner/all-thread managed bytes. A headless no-GPU run uses SDL_VIDEODRIVER=dummy and an unavailable Vulkan ICD.

The circle/spring model intentionally exercises a bounded fixture. Arbitrary custom geometry, other shapes/constraints, unexercised extension operations, physical platforms, native allocator totals, real rendering and final GPU performance acceptance remain separate requirements.

Initialization/Finish/disposal failure injection retains all three errors after retirement. Scene diagnostic capture executes the selected ReadDebugContacts hook after synchronization; invalid count or nonfinite points reject before diagnostic publication and fail the started interval.

Custom geometry checks use a canonical little-endian analytic annulus, exact unit-density mass and an empty hole. Use --draw for actual GPU/compatibility diagnostic pixels, and ELECTRON2D_CUSTOM_GPU=1 to include built-in GPU rejection. The custom boundary is not an approximation to a built-in primitive. Custom file codecs/editor registration and generic production solver acceptance remain open.

Portable episode checks validate bounded output after growth/shrink, invalid identities/shapes/pieces and duplicate reversal, preservation of a previous packet, different local creation order, real pass-through/contact impulse after restore and 64 allocation-free capture/apply cycles. The episode fixture uses explicit retained decisions, not a generic one-way detection algorithm.


Identity checks publish object/canvas associations and collision priority authored
before attachment, including real point-query membership, scene canvas entry,
fresh-world transfer, weak sampled targets, failure cleanup/retry and owner/borrow
guards. After initialized allocation counters and 64 warmups, 128 repeated
body/Area canvas edits, priority writes and point queries allocate zero managed
bytes on the owner and across all process threads. Numerical priority response in
an arbitrary solver is outside this bounded fixture.

Shape mutation checks reject nested body/Area slot/filter edits and shared server-shape data/release during initial identity and live geometry/filter hooks. They compare scene/raw borrowing, owner access, delivered transform/one-way values and real query membership after ordinary edits. After 64 warmups, 128 layer-edit/point-query cycles require zero owner/all-thread managed bytes. The bounded fixture does not certify arbitrary transform or one-way numerical response.
