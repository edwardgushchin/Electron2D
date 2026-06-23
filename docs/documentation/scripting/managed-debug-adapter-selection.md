# Выбор managed .NET debug adapter

Статус: реализовано для `T-0163`.
Обновлено: 2026-06-23.

## Назначение

`T-0163` выбирает распространяемый .NET debug adapter до реализации полноценного managed debugger в `T-0160`. Debug adapter — это отдельный процесс, с которым `Electron2D.Editor` будет говорить через Debug Adapter Protocol, сокращённо DAP. DAP задаёт JSON-сообщения для `launch`, `attach`, breakpoints, stack frames, variables и других команд debugger UI.

Выбранный adapter: `netcoredbg`.

## Почему netcoredbg

`netcoredbg` выбран как adapter для `0.1.0 Preview`, потому что:

- реализует VS Code Debug Adapter Protocol mode через `--interpreter=vscode`;
- поддерживает launch и attach для .NET process;
- поддерживает breakpoints, stepping, threads, stackTrace, scopes, variables, expression evaluation и exception filters;
- распространяется под MIT License;
- имеет upstream binary release для Windows x64 и Linux x64;
- может быть собран из source для macOS arm64, где upstream release `3.1.3-1062` не публикует готовый `osx-arm64` artifact.

`SharpDbg` не выбран для `0.1.0`, потому что на момент проверки у него нет GitHub release/package assets. Microsoft debugger adapters не выбраны из-за несоответствия permissive redistribution policy Electron2D.

## Manifest

Tracked manifest:

```text
data/debugging/dotnet-debug-adapter-selection.json
```

Manifest фиксирует:

- selected adapter, release tag, license и primary sources;
- DAP boundary `Electron2D.Editor -> Electron2D.ManagedDebugging -> DAP stdio -> netcoredbg -> Electron2D game process`;
- command-line arguments `--interpreter=vscode`;
- candidate review;
- platform targets;
- capability matrix;
- handoff для `T-0160`, чтобы следующая задача не выбирала adapter заново.

## Platform matrix

| Platform | Source | Validation |
| --- | --- | --- |
| Windows x64 | `netcoredbg-win64.zip` из upstream release `3.1.3-1062` | Выполнен local DAP launch и attach smoke. |
| Linux x64 | `netcoredbg-linux-amd64.tar.gz` из upstream release `3.1.3-1062` | Release asset найден и захеширован; перед поставкой Linux пакета нужен тот же DAP smoke на Linux x64 host. |
| macOS arm64 | Electron2D source-build из tag `3.1.3-1062` | Upstream release не содержит `osx-arm64`; перед поставкой macOS пакета нужно собрать artifact на macOS arm64 и выполнить DAP smoke. |

Windows x64 smoke проверил:

- `--version` и `--help`;
- Debug build sample app с Portable PDB;
- DAP `initialize`;
- DAP `launch`;
- `setBreakpoints` и фактическую остановку `stopped:breakpoint`;
- `threads`;
- `stackTrace`;
- `scopes`;
- `variables`;
- `next`;
- `continue`;
- `disconnect`;
- `evaluate` для watch expression в stopped frame;
- DAP `attach` к уже запущенному .NET process;
- `pause`, managed stack frame после attach и `continue`.

Local evidence сохранён в `.temp/debug-adapter/dap-smoke/` и не входит в Git.

## Capability matrix

Для `T-0160` разрешено рассчитывать на:

- `initialize`;
- `launch`;
- `attach`;
- `setBreakpoints`;
- `configurationDone`;
- `continue`;
- `next`;
- `stepIn`;
- `stepOut`;
- `threads`;
- `stackTrace`;
- `scopes`;
- `variables`;
- exception filters и exception info;
- expression evaluation;
- conditional/function breakpoints;
- terminate support;
- Editor-managed restart, то есть controlled `disconnect` и новый `launch` через тот же adapter package.

Ограничения:

- `setBreakpoints` может сначала вернуть pending breakpoint. Electron2D должен считать breakpoint подтверждённым после события `stopped:breakpoint` и resolved stack frame.
- `netcoredbg` не объявляет native DAP restart request в `initialize` response. Restart в `T-0160` нужно реализовать в Editor как `disconnect` текущей сессии и новый `launch` на свежем `WorkspaceSnapshot`.
- macOS arm64 требует собственного source-build artifact в release pipeline.
- Remote Android, iOS и WebAssembly debugger не входят в `0.1.0`.

## Update procedure

Обновление adapter выполняется только через повтор T-0163 checklist:

1. выбрать новый pinned release tag или commit;
2. обновить `data/debugging/dotnet-debug-adapter-selection.json`;
3. скачать и захешировать platform artifacts;
4. выполнить Windows x64 DAP launch/attach smoke;
5. выполнить Linux x64 и macOS arm64 release-gate smoke перед поставкой соответствующих пакетов;
6. проверить MIT license notice рядом с bundled adapter artifact;
7. запустить `ManagedDebugAdapterSelectionTests`.

## Проверки

Focused test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ManagedDebugAdapterSelectionTests"
```

Source license check после добавления теста:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
```
