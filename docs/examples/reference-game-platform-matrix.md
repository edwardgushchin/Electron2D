# Reference game platform matrix 0.1.0 Preview

Обновлено: 2026-06-24.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0096` и `T-0224`.

## Цель

`examples/reference-platformer/` сейчас является активным проектом матрицы и кандидатом на приёмочный проект `Electron2D.Editor`. Он объявляет полный набор целевых платформ и пресетов экспорта из обычной структуры проекта: Windows, Linux, macOS, Android, iOS и WebAssembly browser. Полноценным приёмочным проектом он станет только после принятия `T-0222`, `T-0223` и `T-0225`. Матрица может расширяться на другие reference games позже; текущая задача не создаёт отдельные platform forks и не подменяет проект standalone fixture-ом.

Матрица разделяет три набора, потому что они отвечают на разные вопросы:

- `runtimeTargets` - платформы, на которых экспортированная игра должна запускаться: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64`, `WebAssemblyBrowser`.
- `editorTargets` - платформы, на которых должен запускаться `Electron2D.Editor`: `Windows`, `Linux`, `macOS`. Android, iOS и WebAssembly browser не являются editor targets без отдельного продуктового решения.
- `releaseVerificationTargets` - платформы, где конкретный релиз `0.1.0 Preview` требует проверяемого результата быстрой и длительной проверки. Для текущего продукта этот набор не сокращает матрицу запуска и экспорта: все шесть `runtimeTargets` остаются в релизной проверке. Источник продуктового решения - раздел «Матрицы платформ» в `docs/releases/0.1.0-preview.md`; агент не должен удалять iOS или WebAssembly browser из этого набора ради упрощения локальной проверки.

Машинно-читаемый отчёт о заблокированном окружении (`blocked-environment artifact`) допускается как честное состояние незавершённой проверки. Такой отчёт не засчитывает релизную проверку: финальный релизный проход всё равно требует реального результата быстрой и длительной проверки или отдельного изменения release-документа.

Проверяемый результат `T-0096` - общий verifier `tools\Verify-ReferenceGamePlatformMatrix.ps1` и артефакт `data/quality/reference-game-platform-matrix.json`. Они должны доказать, что активный reference project:

- имеет набор `runtimeTargets`: `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64`, `WebAssemblyBrowser`;
- имеет набор `editorTargets`: `Windows`, `Linux`, `macOS`;
- имеет отдельный `releaseVerificationTargets` с правилами smoke/soak, blocked-environment artifact и release blocker для каждой платформы;
- использует named `.e2d` project file, `.csproj`, main scene, embedded export presets, локальные resources, C# scripts и `.electron2d/tasks/**` как обычный формат проекта;
- проходит проектный verifier `tools\Verify-ReferencePlatformer.ps1`;
- не содержит platform-specific игровой fork в `scripts/`, `scenes/`, `resources/` или `assets/`;
- не зависит от локальных repository workflow-файлов `TASKS.md`, `dev-diary/` или `completed-tasks/`.

## Разрешённые platform-specific отличия

Внутри reference games разрешены только отличия, которые относятся к упаковке или платформенной конфигурации:

- export target, configuration, runtime identifier и output directory в `export_presets.e2export.json`;
- renderer profile, если он является частью проверяемого контракта игры;
- иконки приложения и branding metadata;
- signing references без секретов;
- storefront metadata;
- browser hosting metadata для WebAssembly package.

Gameplay code, scenes, resources, imported asset manifests, task metadata и project settings не должны иметь отдельные версии для конкретной платформы. Если будущий runtime действительно потребует platform-specific gameplay behavior, это должно стать отдельной спецификацией и не может быть скрытым исключением в `T-0096`.

## Артефакт матрицы

`data/quality/reference-game-platform-matrix.json` должен быть deterministic JSON-файлом со следующими данными:

- `format = "Electron2D.ReferenceGamePlatformMatrix"`;
- `version = 2`;
- `release = "0.1.0-preview"`;
- `runtimeTargets` с шестью game runtime/export target identifiers;
- `editorTargets` с тремя desktop editor platform identifiers;
- `releaseVerificationTargets` с записью для каждого runtime target: `target`, `realSmokeSoakRequired`, `blockedEnvironmentArtifactAllowed` и `releaseGateBlocker`;
- `releaseVerificationDecision` с явной ссылкой на продуктовое решение, по которому `0.1.0 Preview` пока требует релизную проверку для всех `runtimeTargets`;
- `allowedDifferences` с перечислением разрешённых типов отличий;
- `projects` для активного `reference-platformer`;
- для каждого проекта: `projectPath`, `projectFile`, `settingsFile`, `exportPresetFile`, `mainScene`, `scriptRoots`, `sceneRoots`, `resourceRoots`, `editorMetadataRoots`, `verifier`, `expectedRuntimeTargets`, `forbiddenPlatformSpecificRoots` и `evidence`.

Пути в артефакте должны быть относительными к корню репозитория и использовать `/`.

## Verification

`tools\Verify-ReferenceGamePlatformMatrix.ps1` должен:

- запускать `tools\Verify-ReferencePlatformer.ps1`;
- читать `data/quality/reference-game-platform-matrix.json` и проверять его `format`, `version`, `release`, `runtimeTargets`, `editorTargets`, `releaseVerificationTargets`, `releaseVerificationDecision`, `allowedDifferences` и список проектов;
- проверять обязательные project files каждого reference project;
- читать named `.e2d` project file и проверять `format`, `mainScene` и отсутствие ссылок на `TASKS.md`, `dev-diary/`, `completed-tasks`;
- читать embedded export presets из `.e2d` project file или loose `export_presets.e2export.json` и проверять точное совпадение export presets с `runtimeTargets`;
- проверять, что `releaseVerificationTargets` не подменяет `runtimeTargets` и не удаляет iOS/WebAssembly browser без явного изменения release-документа;
- проверять, что каждый preset не содержит секреты signing values, а `credentialReference` указывает только на безопасную ссылку, например `env:...`;
- проверять, что `.csproj` не содержит conditional compile или platform-specific source includes для gameplay code;
- проверять отсутствие platform-specific directories и files в `Scripts/`, `scenes/`, `resources/` и `assets/`;
- проверять, что `.electron2d/tasks/**` существует как Editor metadata, но не используется как runtime resource path;
- сохранять machine-readable summary в `.temp/reference-game-platform-matrix/summary.json`;
- завершаться ненулевым кодом при любом нарушении.

Focused automated test должен сначала падать на отсутствующих verifier/artifact, затем проходить после реализации. Реальная проверка на устройстве или симуляторе, 30-минутный длительный прогон и аппаратная проверка iOS/macOS остаются задачами `T-0092`, `T-0093` и release candidate gate; `T-0096` закрывает shared-codebase/export-preset gate для активного проекта матрицы.

## Фактическое состояние, ограничения и проверки

Статус: реализованный gate для `T-0096`.
Обновлено: 2026-06-24.

## Что проверяет gate

`tools\Verify-ReferenceGamePlatformMatrix.ps1` проверяет `examples/reference-platformer/` как активный проект матрицы `Electron2D.Editor`, который объявляет одну кодовую базу и пресеты экспорта под `runtimeTargets`:

- `WindowsX64`;
- `LinuxX64`;
- `MacOSArm64`;
- `AndroidArm64`;
- `IosArm64`;
- `WebAssemblyBrowser`.

`editorTargets` ограничены desktop-платформами: `Windows`, `Linux`, `macOS`.

`releaseVerificationTargets` для `0.1.0 Preview` сейчас содержит те же шесть `runtimeTargets`, но это отдельный релизный набор, а не доказательство готовности каждой платформы. Для каждой платформы указано, требуется ли реальная быстрая и длительная проверка, можно ли временно сохранить отчёт о заблокированном окружении и что именно блокирует релизную проверку. Такой отчёт не считается успешной проверкой.

Gate не выполняет реальную проверку на устройстве и не заменяет 30-минутный длительный прогон. Он проверяет подготовленность активного проекта матрицы к обычному export workflow: полный набор presets, отсутствие platform-specific игрового fork-а и отсутствие зависимости от локальных workflow-файлов репозитория.

## Артефакт

Матрица описана в `data/quality/reference-game-platform-matrix.json`.

Артефакт содержит:

- `format = "Electron2D.ReferenceGamePlatformMatrix"`;
- `release = "0.1.0-preview"`;
- общий `runtimeTargets`;
- отдельный `editorTargets`;
- отдельный `releaseVerificationTargets`;
- `releaseVerificationDecision`;
- разрешённые platform-specific отличия;
- текущую активную запись проекта: `reference-platformer`; будущие reference games добавляются отдельными записями, а не platform-specific fork-ами этого проекта;
- пути к named `.e2d` project file, `.csproj`, main scene, script/scene/resource roots, `.electron2d/tasks/**` metadata и project verifier-у.

Разрешённые отличия ограничены export/package metadata: target/configuration/runtime identifier/output directory, renderer profile, иконки/branding, signing references без секретов, storefront metadata и browser hosting metadata.

## Проверка

Основная команда:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-ReferenceGamePlatformMatrix.ps1
```

Verifier выполняет:

- запуск `tools\Verify-ReferencePlatformer.ps1`;
- чтение и проверку `data/quality/reference-game-platform-matrix.json`;
- проверку, что embedded export presets активного проекта содержат точный набор `runtimeTargets`;
- проверку, что `editorTargets` не включает mobile/web platforms;
- проверку, что `releaseVerificationTargets` содержит отдельную запись для каждого runtime target и не используется как замена runtime matrix;
- проверку safe signing references: обязательный signing должен ссылаться на `env:...`, а не хранить секрет в preset;
- проверку `.csproj` на отсутствие conditional compile для platform-specific gameplay code;
- проверку отсутствия platform-specific folders/files в `Scripts/`, `scenes/`, `resources/` и `assets/`;
- проверку, что `.electron2d/tasks/**` существует как Editor metadata, но не попадает в runtime resource roots;
- запись summary artifact в `.temp/reference-game-platform-matrix/summary.json` с теми же `runtimeTargets`, `editorTargets`, `releaseVerificationTargets` и `releaseVerificationDecision`.

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
