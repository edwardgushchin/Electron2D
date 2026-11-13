# Process consumer

This public consumer shares OSProcessTests with the root runner. Run from the repository root:

```powershell
dotnet run --project tests/OSProcesses.Consumer/OSProcesses.Consumer.csproj -c Release
```

Use the matching native cohort, including libElectron2D.dll, under the consumer's `runtimes/win-x64/native` directory. A legacy platform package without that library cannot run this source profile. The consumer does not use engine internals. Child launch resolves a dotnet entry DLL by name in AppContext.BaseDirectory; executable and NativeAOT hosts relaunch directly. JSON metadata is generated for all fixtures.

For a fresh candidate package, pack Electron2D into an isolated feed, restore this consumer with `Electron2DUsePackage=true` and an isolated `RestorePackagesPath`, then rerun the same executable. Verify its managed DLL equals the candidate package DLL. Keep the native cohort identity alongside the package hash; manually supplied native overrides do not qualify a delivered platform package.

For strict NativeAOT, publish with `-r win-x64 -p:RuntimeFrameworkVersion=10.0.14 -p:IlcPackageVersion=10.0.14 -p:PublishAot=true -p:PublishTrimmed=true -p:TrimMode=full -p:ILLinkTreatWarningsAsErrors=true -p:IlcTreatWarningsAsErrors=true`. Use the installed Windows native toolchain. The explicit servicing version avoids the installed SDK's older vulnerable ILC; it does not change the engine package dependency contract.

The Windows scenarios independently observe hidden, new and inherited console identity/membership and hide fixture windows. They exercise literal/empty/Unicode/quoted argv, native output order, capture/discard, full exit bits, independent lifetime, failures, concurrent capture, blocking/nonblocking duplex, partial retries and finalization. Root selectors `ELECTRON2D_TEST_OS_PROCESS=1`, `ELECTRON2D_TEST_OS_STDIO=1`, `ELECTRON2D_TEST_OPEN_WITH_PROGRAM=1` and `ELECTRON2D_TEST_FILE_ACCESS=1` retain connected regressions. This is bounded Windows x64 evidence; the [component](../../docs/components/processes.md) records remaining platform and qualification limits.

The separate [post-create failure fixture](../ProcessAdoption.Tests/README.md) instruments an isolated engine build, independently observes raw handles/child retirement and checks successful retry. Run its CLR and strict NativeAOT profiles before rebuilding the ordinary public consumer and package; test instrumentation is excluded from those artifacts.
