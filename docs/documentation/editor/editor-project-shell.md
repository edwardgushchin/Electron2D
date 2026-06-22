# Electron2D.Editor project shell

Статус: документация реализации для `T-0078`.
Дата: 2026-06-22.

## Назначение

`Electron2D.Editor` является отдельным executable project для будущего desktop-редактора. Текущий shell проверяет, что editor build path существует, использует runtime `Electron2D` и может стартовать без внешнего desktop UI framework.

Это не полноценный редактор. Project Manager, docks, viewport interactions, Inspector, run/stop workflow, встроенный редактор кода и Agent Workspace panel реализуются отдельными задачами поверх этого проекта.

## Текущее поведение

Editor executable поддерживает smoke-режим:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --smoke
```

Smoke-режим создаёт `SceneTree`, настраивает root `Viewport`, строит первый UI root через runtime controls и выводит machine-readable строки с результатом. Проверка используется тестами и CI, чтобы подтвердить, что editor shell запускается на Electron2D runtime.

`Electron2D.Editor` подключает `data/assets/branding/icon/electron2d.ico` как `ApplicationIcon`, поэтому собираемый desktop executable получает брендовую иконку из поставляемого asset pack.

## Ограничения

- В этой задаче нет постоянного desktop window event loop.
- Нет Project Manager и файловых операций editor UI.
- Нет scene editing, docks, Inspector, 2D viewport tools или code editor.
- Editor project не должен добавлять WPF, WinForms, Avalonia или другой внешний UI framework.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectShellTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Verify-ReleaseMetadata.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
