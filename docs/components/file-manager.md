# File-manager navigation

Last updated: 2026-11-20

`OS.ShellShowInFileManager(path, openFolder = true)` accepts an ordinary filesystem path. Call `ProjectSettings.GlobalizePath` first for project/user paths. Relative paths resolve against the current process directory before native work. Null, empty and embedded-NUL paths fail before launching an external application.

On Windows, `openFolder = true` enters an existing directory. A file, or any request with `openFolder = false`, selects that exact item in its parent. Entering an already-open directory reuses native view state and does not request a different selection. Paths containing spaces, Unicode or shell metacharacters remain item identities rather than command text.

The retained static OS service starts a temporary STA worker and balances COM initialization, uninitialization and the Shell PIDL allocation. `SHParseDisplayName` resolves the item; `SHOpenFolderAndSelectItems` selects it. Directory entry uses `ShellExecuteExW` with a PIDL, synchronous handoff and native error UI suppressed. The caller apartment remains unchanged, including an existing MTA. Native failure throws `IOException`; there is no alternate command-line launch after failure. The request returns after handoff, while Explorer may finish updating its UI later. This cold, blocking operation is unsuitable for real-time callbacks and does not initialize an engine display.

On desktop Linux, system GIO constructs the target's `file://` URI and sends it to [FileManager1](https://wiki.freedesktop.org/www/Specifications/file-manager-interface/) on the session D-Bus. `ShowFolders` enters an existing directory when `openFolder` is true. `ShowItems` requests selection in the parent for a file or for a directory with `openFolder` false. Missing paths throw `IOException` before reaching the bus.

When GIO, a session bus address, or the FileManager1 destination is unavailable, the SDL launcher used by `OS.ShellOpen` requests the default application for the same directory or the item's parent. The fallback preserves literal percent sequences in directory names and cannot select an item. Service errors and transport failures after obtaining a bus address throw `IOException` and never trigger a second launch.

macOS still requires a native selection adapter. Mobile/browser behavior and foreign Windows architecture/sandbox/desktop qualification remain separate target gates under ADR 0021. No new public type or signature is introduced; ADRs 0004 and 0095 continue to govern the existing service.

## Executable verification

### Linux

From the repository root, run the public consumer against an independent GIO receiver on an isolated D-Bus session. The fixture needs .NET 10, a C compiler, `pkg-config` with the `gio-2.0` development files, `dbus-run-session` and a loadable SDL3 runtime. It records fallback calls through a private `xdg-open` fixture.

```bash
bash tests/FileManager.Consumer/verify-linux.sh
```

The receiver verifies method, argument signature and exact URI, including Unicode, spaces and literal percent escapes. The fixture checks missing paths, the folder fallback and service rejection without a second launch. A real file manager was unavailable in the WSL2 test environment, so visible item selection remains unverified.

### Windows

`FileManagerChecks`, linked by `tests/FileManager.Consumer` and the root check host (`ELECTRON2D_TEST_FILE_MANAGER`), uses only public engine API and standard .NET. `tools/file-manager-probe.cpp` is an independent Windows SDK observer: it locates the private fixture folder through `IShellWindows`/`IFolderView2` and compares canonical `IShellItem` identities and native selection counts. It checks the external folder view, rather than treating a successful native return code as acceptance.

Each Windows run covers 14 assertions: invalid input, missing-target failure, fresh directory entry, exact file and directory selection, relative paths, Unicode/metacharacters, trailing separators, retained directory selection, existing STA/MTA callers, unchanged caller apartments and permanent OS identity without a DisplayServer. Cleanup closes only newly created Explorer windows showing this run's private fixture tree; pre-existing Explorer window handles are protected. It does not inspect or log other folders' selections.

With the Windows SDK/MSVC environment and existing runtime prerequisites configured, build the independent observer and run the public consumer:

```powershell
cl /nologo /std:c++20 /EHsc /O2 /MT /utf-8 tools/file-manager-probe.cpp /Fe:<proof>/file-manager-probe.exe /Fo:<proof>/file-manager-probe.obj ole32.lib oleaut32.lib shell32.lib uuid.lib user32.lib
dotnet run --project tests/FileManager.Consumer/FileManager.Consumer.csproj -c Release -- <proof>/file-manager-probe.exe <proof>/private-output
```

The root host uses `ELECTRON2D_TEST_FILE_MANAGER=<absolute-observer-path>` and receives its private output directory as the first argument. A fresh-package consumer sets `Electron2DUsePackage=true`; strict NativeAOT also sets `PublishAot=true`, `PublishTrimmed=true`, `TrimMode=full`, `ILLinkTreatWarningsAsErrors=true` and `IlcTreatWarningsAsErrors=true`.

Qualification evidence is retained under `C:/Users/eduar/.codex/artifacts/Electron2D/file-manager-20261107`. Source, freshly packed Core and NativeAOT execution are separate profiles; the qualification receipt records the completed runs and their exact artifact identities. This does not establish native allocator measurements, a no-allocation hot-path contract, other Windows architectures, macOS/mobile/browser implementation or full product-platform acceptance.
