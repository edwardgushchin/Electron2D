# Control sizing consumer

Uses only the public runtime API. It checks independent horizontal/vertical growth, two nested margin containers, wrapped-window limits, native client geometry and rendered pixels. Requires the qualified Windows native package; the task used the T-0035/review-11 package in an isolated NuGet cache.

```powershell
dotnet run --project tests/ControlSize.Consumer/ControlSize.Consumer.csproj -c Release -- gpu
```

```powershell
dotnet run --project tests/ControlSize.Consumer/ControlSize.Consumer.csproj -c Release -- compatibility
```

Verified on Windows Direct3D 12 and Direct3D 11. Foreign targets, full-trim NativeAOT and native allocator qualification remain separate.
