# Managed C# debugger в редакторе

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт, состояние и проверки

Статус: реализовано для `T-0160`.
Обновлено: 2026-06-23.

## Назначение

Managed C# debugger — это внутренняя подсистема `Electron2D.Editor` для локальной отладки desktop-запусков C# games. Она отвечает за source breakpoints, current execution line, threads, call stack, locals, arguments, watches, exception info и Debug Output. Runtime debug bridge остаётся отдельной подсистемой для scene tree, runtime properties, screenshots, frame stepping и metrics.

## Debugger core

Debugger core живёт в отдельной сборке:

```text
src/Electron2D.ManagedDebugging/
```

Сборка не входит в игровой runtime и не зависит от `Electron2D.Tooling` или `Electron2D.Mcp`. `Electron2D.Editor` подключает её как внутренний сервис `Script` workspace.

Core читает tracked manifest `data/debugging/dotnet-debug-adapter-selection.json`, созданный в `T-0163`, и использует выбранный adapter без повторного выбора:

```text
Electron2D.Editor -> Electron2D.ManagedDebugging -> DAP stdio -> netcoredbg -> Electron2D game process
```

Editor-managed restart реализуется как controlled `disconnect` текущей session и новый `launch` на свежем `WorkspaceSnapshot`, потому что выбранный adapter не объявляет native DAP restart request.

## Breakpoints

Локальные breakpoint metadata хранятся в:

```text
.electron2d/user/breakpoints.e2debug
```

Файл является пользовательскими metadata редактора: он не входит в игру, runtime snapshot, asset pack или production export. Breakpoint хранит:

- `BreakpointId`;
- `DocumentId`;
- `SourceAnchor`;
- `Enabled`;
- `Verified`;
- `ResolvedLine`;
- `ResolvedColumn`;
- `LastBoundSnapshotId`;
- `AdapterMessage`.

Breakpoint следует за rename через `DocumentId`, переносит `SourceAnchor` после text edits и становится `Verified = false`, если перенос неоднозначен.

## Debug session state

Debug session создаётся на immutable `WorkspaceSnapshot`. Debug build помечается как build с Portable PDB и source mapping от snapshot file к canonical `CodeDocument`. Если C# document меняется после старта debugger session, session получает `stale` marker и Editor предлагает rebuild/restart без автоматического сохранения dirty buffer.

State содержит:

- выбранный adapter id, release tag, arguments и DAP boundary;
- DAP transcript команд и событий для launch/attach/breakpoints/step/continue/pause/disconnect;
- `SnapshotId`;
- attached game process id из `RunSession.ProcessId`;
- текущую исполняемую строку;
- threads;
- call stack;
- locals и arguments выбранного frame;
- watch definitions и evaluated values;
- exception type, source location и stack trace;
- Debug Output.

## Smoke и visual acceptance

Команда:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --managed-debugger-smoke .temp\managed-debugger
```

Создаёт:

- `.temp/managed-debugger/managed-debugger.state.json`;
- `.temp/managed-debugger/visual/managed-debugger.png`;
- `.temp/managed-debugger/visual/managed-debugger.analysis.json`.

Smoke использует deterministic DAP transcript поверх выбранного adapter manifest, чтобы обычные integration tests не скачивали и не запускали внешний debugger binary. Реальный Windows DAP launch/attach/evaluate smoke выбранного adapter уже выполнен в `T-0163` и описан в [выборе managed .NET debug adapter](managed-debug-adapter-selection.md). Release packaging для Linux x64 и macOS arm64 должен выполнить такой же live DAP smoke перед поставкой platform packages.

Visual artifact показывает `Script` workspace с debugger controls, breakpoint gutter, current line highlight, right `Debug Session`/`Stale Rebuild`, bottom Debugger panel с threads, call stack, locals, arguments, watches и exception. JSON analysis фиксирует bounds, clickability, overflow, selected workspace и отсутствие `3D`, `AssetLib`, GDScript UI и `.gd`.

## Проверки

Focused проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorManagedDebuggerTests"
```

UI smoke:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --managed-debugger-smoke .temp\managed-debugger
```

Общие проверки задачи дополнительно запускают source license headers, local documentation verifier, UI public API gate, source domain layout, solution build и полный `tools\Run-Tests.ps1`.
