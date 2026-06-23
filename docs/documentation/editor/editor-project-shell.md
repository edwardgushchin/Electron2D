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

## Ограничения

- В этой задаче нет постоянного desktop window event loop.
- Нет Project Manager и файловых операций editor UI.
- Общий shell layout уже содержит зоны docks, но содержательное scene editing, Inspector UI, 2D viewport tools, code editor и Agent Workspace content реализуются отдельными задачами.
- Editor project не должен добавлять WPF, WinForms, Avalonia или другой внешний UI framework.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests"
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorShellLayoutTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReleaseMetadata.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
