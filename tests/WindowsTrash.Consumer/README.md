# Windows recycle consumer

Build the independent oracle from a Visual Studio developer shell:

```powershell
cl /EHsc /O2 /MT /std:c++17 /utf-8 tools/windows-trash/oracle.cpp /Fo"ARTIFACTS/oracle.obj" /Fe"ARTIFACTS/oracle.exe" ole32.lib shell32.lib uuid.lib user32.lib /link /MANIFEST:EMBED /MANIFESTINPUT:tools/windows-trash/oracle.manifest
dotnet run --project tests/WindowsTrash.Consumer -c Release -- ARTIFACTS/oracle.exe ARTIFACTS/fixtures
```

Supply an absolute native oracle path and controlled writable workspace. Each run creates a fresh child directory, recycles only its fixtures, verifies/restores exact native records and removes the child on success. A failed run preserves its recovered controlled files for diagnosis. Restoration uses the Shell undelete verb, not a filesystem move out of the recycle directory. The oracle's longPathAware manifest matches managed/AOT hosts. `inspect`, `restore`, `guard` and `junction` are test-only commands.

The root runner exposes ELECTRON2D_TEST_WINDOWS_TRASH=ABSOLUTE_ORACLE and optional ELECTRON2D_WINDOWS_TRASH_WORKSPACE. Use a matching full native cohort for the font-backed FileDialog scenario. Run the complete profile with a fresh managed package using Electron2DUsePackage=true and strict PublishAot/PublishTrimmed/TrimMode=full, with ILC/linker warnings as errors. ELECTRON2D_WINDOWS_TRASH_PRECEDING=1 is the preceding-managed-engine negative control; it expects a refused Windows operation and unchanged data.

See [the component](../../docs/components/windows-trash.md) for exact scope, native policy refusal and the distinction between real filesystem/Shell behavior and managed browser input versus rendered UI acceptance. No external application is launched by the browser scenario.
