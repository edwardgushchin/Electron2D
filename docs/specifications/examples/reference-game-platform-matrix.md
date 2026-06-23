# Reference game platform matrix 0.1.0 Preview

Статус: целевая спецификация для `T-0096`.

## Цель

`examples/reference-platformer/` и `examples/ui-heavy-reference/` должны быть одной парой полноценных приёмочных проектов `Electron2D.Editor`, которые экспортируются из обычной структуры проекта под все runtime-платформы `0.1.0 Preview`: Windows, Linux, macOS, Android, iOS и WebAssembly browser. Задача не создаёт отдельные platform forks и не подменяет проекты standalone fixture-ами.

Проверяемый результат `T-0096` - общий verifier `tools\Verify-ReferenceGamePlatformMatrix.ps1` и артефакт `data/quality/reference-game-platform-matrix.json`. Они должны доказать, что обе игры:

- имеют одинаковый набор export targets: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64`, `WebAssemblyBrowser`;
- используют `project.e2d.json`, `.csproj`, main scene, `export_presets.e2export.json`, локальные resources, C# scripts и `.electron2d/tasks/**` как обычный формат проекта;
- проходят свои проектные verifiers `tools\Verify-ReferencePlatformer.ps1` и `tools\Verify-UiHeavyReference.ps1`;
- не содержат platform-specific игровой fork в `Scripts/`, `scenes/`, `resources/` или `assets/`;
- не зависят от локальных repository workflow-файлов `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Разрешённые platform-specific отличия

Внутри reference games разрешены только отличия, которые относятся к упаковке или платформенной конфигурации:

- export target, configuration, runtime identifier и output directory в `export_presets.e2export.json`;
- renderer profile, если он является частью проверяемого контракта игры, например `Compatibility` у UI-heavy reference game;
- иконки приложения и branding metadata;
- signing references без секретов;
- storefront metadata;
- browser hosting metadata для WebAssembly package.

Gameplay code, scenes, resources, imported asset manifests, task metadata и project settings не должны иметь отдельные версии для конкретной платформы. Если будущий runtime действительно потребует platform-specific gameplay behavior, это должно стать отдельной спецификацией и не может быть скрытым исключением в `T-0096`.

## Артефакт матрицы

`data/quality/reference-game-platform-matrix.json` должен быть deterministic JSON-файлом со следующими данными:

- `format = "Electron2D.ReferenceGamePlatformMatrix"`;
- `version = 1`;
- `release = "0.1.0-preview"`;
- `targetSet` с шестью target identifiers;
- `allowedDifferences` с перечислением разрешённых типов отличий;
- `projects` для `reference-platformer` и `ui-heavy-reference`;
- для каждого проекта: `projectPath`, `projectFile`, `settingsFile`, `exportPresetFile`, `mainScene`, `scriptRoots`, `sceneRoots`, `resourceRoots`, `editorMetadataRoots`, `verifier`, `expectedTargets`, `forbiddenPlatformSpecificRoots` и `evidence`.

Пути в артефакте должны быть относительными к корню репозитория и использовать `/`.

## Verification

`tools\Verify-ReferenceGamePlatformMatrix.ps1` должен:

- запускать `tools\Verify-ReferencePlatformer.ps1` и `tools\Verify-UiHeavyReference.ps1`;
- читать `data/quality/reference-game-platform-matrix.json` и проверять его `format`, `version`, `release`, `targetSet`, `allowedDifferences` и список проектов;
- проверять обязательные project files каждого reference project;
- читать `project.e2d.json` и проверять `format`, `mainScene` и отсутствие ссылок на `TASKS.md`, `dev-diary/`, `completed-tasks`;
- читать `export_presets.e2export.json` и проверять точное совпадение target set с релизной матрицей;
- проверять, что каждый preset не содержит секреты signing values, а `credentialReference` указывает только на безопасную ссылку, например `env:...`;
- проверять, что `.csproj` не содержит conditional compile или platform-specific source includes для gameplay code;
- проверять отсутствие platform-specific directories и files в `Scripts/`, `scenes/`, `resources/` и `assets/`;
- проверять, что `.electron2d/tasks/**` существует как Editor metadata, но не используется как runtime resource path;
- сохранять machine-readable summary в `.temp/reference-game-platform-matrix/summary.json`;
- завершаться ненулевым кодом при любом нарушении.

Focused automated test должен сначала падать на отсутствующих verifier/artifact, затем проходить после реализации. Реальный device/simulator smoke, 30-минутный soak и iOS/macOS hardware verification остаются задачами `T-0092`, `T-0093` и release candidate gate; `T-0096` закрывает shared-codebase/export-preset gate для двух reference projects.
