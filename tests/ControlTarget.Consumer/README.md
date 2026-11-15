# Control GUI target consumer

Uses only public API in managed and native SubViewport scenes. Checks overlap with reversed Z,
sibling moves, independent TopLevel roots, CanvasLayer priority, viewport isolation,
custom local HasPoint clipping and stationary geometry/visibility changes. Also checks
hover callback failure recovery, reentrant hiding, replacement after disposal, wheel
propagation, touch capture and prepared active/idle frame allocations.

```powershell
dotnet run --project tests/ControlTarget.Consumer/ControlTarget.Consumer.csproj -c Release
```

Native profiles warp the actual Windows pointer into a scaled embedded control and
check local delivery, rendered pixels, cursor fallback/refresh and 128 complete idle
and active frame allocation brackets. They use the qualified Windows native package.

```powershell
dotnet run --project tests/ControlTarget.Consumer/ControlTarget.Consumer.csproj -c Release -- gpu
```

```powershell
dotnet run --project tests/ControlTarget.Consumer/ControlTarget.Consumer.csproj -c Release -- compatibility
```

The root `ELECTRON2D_TEST_CONTROL_TARGET` profiles reuse these contracts and add
adjacent managed regressions or native cursor-factory failure/handle-lifetime checks.
Foreign hosts, native heap accounting and physical-device/human acceptance remain separate.
