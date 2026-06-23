# Electron2D.Editor project shell

Статус: документация реализации для `T-0078`.
Дата: 2026-06-22.

## Назначение

`Electron2D.Editor` является отдельным executable project для desktop-редактора. Базовый shell проверяет, что editor build path существует, использует runtime `Electron2D`, может стартовать без внешнего desktop UI framework и строит стартовый UI root через общий shell layout model.

Это ещё не полноценный редактор с постоянным desktop event loop. Project Manager, docks, viewport interactions, Inspector, run/stop workflow, встроенный редактор кода и Agent Workspace panel реализуются отдельными задачами поверх этого проекта. Общий layout shell, persistence и visual harness описаны отдельно: [Editor shell layout и visual harness](editor-shell-layout.md).

## Текущее поведение

Editor executable поддерживает smoke-режим:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --smoke
```

Smoke-режим создаёт `SceneTree`, настраивает root `Viewport`, строит первый UI root через runtime controls и выводит machine-readable строки с результатом. Проверка используется тестами и CI, чтобы подтвердить, что editor shell запускается на Electron2D runtime.

`Electron2D.Editor` подключает `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon`, поэтому собираемый desktop executable получает брендовую иконку из поставляемого asset pack.

Дополнительная smoke-команда shell layout:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --shell-layout-smoke .temp\editor-shell-visual
```

Она сохраняет layout state, PNG screenshot и JSON analysis artifact для проверки default layout, workspace switcher, docks, bottom panel, persistence и отсутствия 3D/GDScript/AssetLib UI.

Smoke-команда Agent Workspace panel:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --agent-workspace-panel-smoke .temp\agent-workspace-panel
```

Она сохраняет dock state, PNG screenshot и JSON analysis artifact для проверки правого размещения Agent Workspace, session/task/job/actions model, полного diagnostics payload, artifacts с snapshot identity, grouped Undo и отсутствия AI action для human acceptance.

Smoke-команда Project Tasks board:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --tasks-board-smoke .temp\project-tasks-board
```

Она сохраняет board state, PNG screenshot и JSON analysis artifact для проверки центрального `Tasks` workspace, колонок `ProjectTaskManager`, правого `Task Details`, filters, drag-and-drop intent, trusted human actions и отсутствия запрещённых 3D/GDScript/AssetLib UI.

## Ограничения

- В этой задаче нет постоянного desktop window event loop.
- Нет Project Manager и файловых операций editor UI.
- Общий shell layout уже содержит зоны docks. Project Manager, scene editing, Inspector UI, 2D viewport tools, run workflow, Script workspace, model-first Agent Workspace content и model-first `Tasks` workspace реализованы отдельными задачами, а постоянный desktop event loop остаётся следующим слоем.
- Editor project не должен добавлять WPF, WinForms, Avalonia или другой внешний UI framework.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorShellLayoutTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorAgentWorkspacePanelTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectTasksBoardTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReleaseMetadata.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
