# Reference game platform matrix

Статус: реализованный gate для `T-0096`.
Обновлено: 2026-06-23.

## Что проверяет gate

`tools\Verify-ReferenceGamePlatformMatrix.ps1` проверяет `examples/reference-platformer/` и `examples/ui-heavy-reference/` как пару валидных проектов `Electron2D.Editor`, которые экспортируются из одной кодовой базы под релизную матрицу:

- `WindowsX64`;
- `LinuxX64`;
- `MacOSArm64`;
- `AndroidArm64`;
- `IosArm64`;
- `WebAssemblyBrowser`.

Gate не выполняет реальный device smoke и не заменяет 30-минутный soak. Он проверяет подготовленность обоих проектов к обычному export workflow: одинаковый набор presets, отсутствие platform-specific игрового fork-а и отсутствие зависимости от локальных workflow-файлов репозитория.

## Артефакт

Матрица описана в `data/quality/reference-game-platform-matrix.json`.

Артефакт содержит:

- `format = "Electron2D.ReferenceGamePlatformMatrix"`;
- `release = "0.1.0-preview"`;
- общий `targetSet`;
- разрешённые platform-specific отличия;
- две записи проектов: `reference-platformer` и `ui-heavy-reference`;
- пути к `project.e2d.json`, `.csproj`, `export_presets.e2export.json`, main scene, script/scene/resource roots, `.electron2d/tasks/**` metadata и project verifier-ам.

Разрешённые отличия ограничены export/package metadata: target/configuration/runtime identifier/output directory, renderer profile, иконки/branding, signing references без секретов, storefront metadata и browser hosting metadata.

## Проверка

Основная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferenceGamePlatformMatrix.ps1
```

Verifier выполняет:

- запуск `tools\Verify-ReferencePlatformer.ps1`;
- запуск `tools\Verify-UiHeavyReference.ps1`;
- чтение и проверку `data/quality/reference-game-platform-matrix.json`;
- проверку, что оба `export_presets.e2export.json` содержат точный target set релиза;
- проверку safe signing references: обязательный signing должен ссылаться на `env:...`, а не хранить секрет в preset;
- проверку `.csproj` на отсутствие conditional compile для platform-specific gameplay code;
- проверку отсутствия platform-specific folders/files в `Scripts/`, `scenes/`, `resources/` и `assets/`;
- проверку, что `.electron2d/tasks/**` существует как Editor metadata, но не попадает в runtime resource roots;
- запись summary artifact в `.temp/reference-game-platform-matrix/summary.json`.

Focused automated test:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~ReferenceGamePlatformMatrixTests"
```

## Текущие границы

Этот gate закрывает shared-codebase/export-preset часть `T-0096`. Он не доказывает:

- запуск reference games на реальной машине или устройстве для каждой платформы;
- iOS device/simulator smoke на macOS;
- 30-минутный platform soak;
- отсутствие утечек во время долгого platform run;
- финальный release candidate путь install -> project -> scene -> C# code -> run -> debug -> export -> play.

Эти проверки остаются в `T-0092`, `T-0093`, `T-0103` и `T-0104`.
