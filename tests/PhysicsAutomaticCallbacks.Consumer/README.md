# Public physics callback consumer

The default profiles exercise the physical Node callback contract, full frames,
PackedScene reconstruction, lifecycle/weak retirement and warmed owner/all-thread
managed allocations. Select `cpu` or `gpu`, optionally followed by `threaded`.
Other existing named physics profiles are listed in `Program.cs`.

```powershell
dotnet run --project tests/PhysicsAutomaticCallbacks.Consumer -c Release -- cpu
dotnet run --project tests/PhysicsAutomaticCallbacks.Consumer -c Release -- cpu threaded
dotnet run --project tests/PhysicsAutomaticCallbacks.Consumer -c Release -- gpu
dotnet run --project tests/PhysicsAutomaticCallbacks.Consumer -c Release -- gpu threaded
```

The shared GUI/array/catalog AOT fixture has its own explicit mode:

```powershell
dotnet run --project tests/PhysicsAutomaticCallbacks.Consumer -c Release -- --aot-prerequisites
```

That mode retains every `PhysicsAOTPrerequisiteTests` check and requires its
matching general native/font dependencies. It exits after that fixture. It does
not run before the physical profiles. Run it separately for the owning shared
runtime qualification; its unrelated GUI dependency must not prevent a headless
CPU callback check from starting.

Apply the same selectors to a strict NativeAOT publish when qualifying that host.
Passing one backend/mode does not establish the other backend, renderer, platform,
hardware or full-frame performance gates. The [physics task boundary](../../docs/components/physics-contract-audit.md#physics-task-boundary-2026-11-05)
defines acceptance separately from the inherited declaration census.
