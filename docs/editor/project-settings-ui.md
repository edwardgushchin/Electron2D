# Project Settings UI редактора

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0085`.
Дата: 2026-06-23.

## Цель

`Electron2D.Editor` должен дать разработчику видимый экран настроек проекта, который редактирует тот же файловый формат, что используют runtime, Project Manager, export pipeline, CLI и будущие reference games. UI не должен хранить отдельную копию настроек и не должен подменять проектные JSON-файлы локальными workflow-файлами.

Экран относится к visible Editor UI. Поэтому готовность задачи подтверждается только real-window screenshot: редактор создаёт desktop window, показывает Project Settings frame, проверяет pointer/keyboard interaction, сохраняет PNG и JSON analysis, а затем тесты подтверждают отсутствие text overflow и запрещённых UI-разделов.

## Область настроек

Минимальный экран `Project Settings` для `0.1-preview` содержит:

- `Project Settings` как центральный workspace или modal-like panel внутри общего shell layout;
- поле `Main Scene`, которое записывает `mainScene` в `project.e2d.json`;
- блок `Display` с `windowWidth`, `windowHeight`, `fullscreen`, `stretchMode`, `stretchAspect`, `stretchScaleMode`, `stretchScale` и `dpiScale`;
- выбор `Renderer Profile`: `Automatic`, `Compatibility` или `Standard`;
- поле `Physics Tick Rate`, которое записывает `physicsTicksPerSecond`;
- `Input Map` с action list, deadzone и persistable keyboard/mouse/gamepad bindings;
- `Export Presets` со списком presets для Windows, Linux, macOS, Android, iOS и WebAssembly browser, которые сохраняются в `export_presets.e2export.json`.

## Файловый контракт

Project Settings UI сохраняет настройки через существующие внутренние stores:

- `project.e2d.json` формата `Electron2D.ProjectSettings`;
- `export_presets.e2export.json` формата `Electron2D.ExportPresets`.

После сохранения UI обязан заново загрузить оба файла через store-слои и показать round-trip state. Если файл не загружается, smoke завершается ошибкой и не объявляет UI готовым.

UI не сохраняет secrets. Android и iOS presets могут содержать только non-secret `identity` и `credentialReference`, например ссылку на environment variable. Реальные passwords, tokens, private keys, certificate bodies и keystore содержимое в project files запрещены.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--project-settings-smoke <work-root>
```

Smoke-режим должен:

- создать валидный Editor-проект из canonical template;
- открыть проект через тот же Project Manager path, что и ручной workflow;
- изменить `mainScene`, display settings, renderer profile, physics tick rate и Input Map;
- создать `export_presets.e2export.json` с presets для `WindowsX64`, `LinuxX64`, `MacOSArm64`, `AndroidArm64`, `IosArm64` и `WebAssemblyBrowser`;
- сохранить project settings и export presets;
- заново загрузить оба файла и подтвердить round-trip;
- показать Project Settings frame в реальном окне `Electron2D.Editor`;
- проверить pointer selection по одной настройке и keyboard save command;
- сохранить screenshot и JSON analysis;
- вывести machine-readable строки `ProjectSettingsWritten=True`, `InputMapRoundTrip=True`, `ExportPresetsRoundTrip=True`, `WindowCreated=True`, `WindowShown=True`, `FramePresented=True`, `PointerInteractionObserved=True`, `KeyboardInteractionObserved=True`, `TextOverflowCount=0`, `ForbiddenUiMatches=0`, `ScreenshotReviewed=True`, `ScreenshotPath=...` и `AnalysisPath=...`.

## Visual acceptance

Screenshot должен показывать:

- верхний shell с workspace switcher без `3D`, `AssetLib` и GDScript UI;
- центральный Project Settings panel;
- секции `Main Scene`, `Display`, `Renderer`, `Physics`, `Input Map` и `Export Presets`;
- видимые значения сохранённых настроек;
- click targets для save/apply, main scene chooser, input action row и export preset row.

JSON analysis должен содержать bounds секций, количество кликабельных controls, список labels, text overflow count, forbidden UI matches, result pointer/keyboard interaction и absolute paths сохранённых файлов.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --project-settings-smoke <work-root>` и подтверждает успешный exit code.
- Test проверяет, что `project.e2d.json` содержит изменённые `mainScene`, display settings, renderer profile, physics tick rate и input actions.
- Test проверяет, что `export_presets.e2export.json` содержит presets для Windows, Linux, macOS, Android, iOS и WebAssembly browser.
- Test проверяет real-window screenshot и JSON analysis: окно создано, frame представлен, pointer/keyboard interaction наблюдались, text overflow отсутствует, forbidden UI отсутствует.
- Implementation documentation описывает smoke-команду, файлы, write-through behavior и ограничения по секретам.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-LocalDocumentation.ps1` проходит после обновления индекса документации.

## Фактическое состояние, ограничения и проверки

Статус: документация реализации для `T-0085`.
Дата: 2026-06-23.

## Назначение

Project Settings UI в `Electron2D.Editor` связывает видимый экран настроек с уже существующими проектными файлами. Экран не добавляет public runtime API: он использует внутренние store-слои, которыми уже пользуются шаблон проекта, Project Manager, export pipeline и тесты.

## Файлы

Smoke-команда создаёт валидный Editor-проект из `data/templates/electron2d-empty/`, открывает его через Project Manager и сохраняет:

- `project.e2d.json` формата `Electron2D.ProjectSettings`;
- `export_presets.e2export.json` формата `Electron2D.ExportPresets`.

`project.e2d.json` получает изменённые значения:

- `mainScene`: `scenes/settings-smoke.scene.json`;
- `rendererProfile`: `Standard`;
- `physicsTicksPerSecond`: `120`;
- `display.windowWidth`: `960`;
- `display.windowHeight`: `540`;
- `display.fullscreen`: `false`;
- `input.actions`: `jump` и `dash`.

`export_presets.e2export.json` содержит presets:

- `android-release`;
- `browser-debug`;
- `ios-release`;
- `linux-debug`;
- `macos-release`;
- `windows-debug`.

Android и iOS presets используют только non-secret signing references, например `env:E2D_ANDROID_KEYSTORE` и `env:E2D_IOS_CERTIFICATE`. Файл не хранит passwords, tokens, private keys, certificate bodies или keystore содержимое.

## Real-window smoke

Проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --project-settings-smoke .temp\project-settings-ui
```

Команда:

- создаёт проект из canonical template;
- открывает его через Project Manager;
- меняет main scene, display settings, renderer profile, physics tick rate, Input Map и export presets;
- сохраняет оба JSON-файла;
- заново загружает оба файла и подтверждает round-trip;
- рисует Project Settings frame;
- показывает frame в настоящем окне `Electron2D.Editor`;
- проверяет pointer hit-test по строке Input Map и keyboard save command;
- сохраняет PNG и JSON visual analysis.

Ожидаемые machine-readable строки:

```text
Electron2D.Editor project settings smoke passed
ProjectSettingsWritten=True
InputMapRoundTrip=True
ExportPresetsRoundTrip=True
WindowCreated=True
WindowShown=True
FramePresented=True
PointerInteractionObserved=True
KeyboardInteractionObserved=True
TextOverflowCount=0
ForbiddenUiMatches=0
ScreenshotReviewed=True
```

Основные artifacts:

- `.temp/project-settings-ui/visual/project-settings-ui.png`;
- `.temp/project-settings-ui/visual/project-settings-ui.analysis.json`.

Screenshot показывает общий Editor shell, центральный `Project Settings` panel, секции `Main Scene`, `Display`, `Renderer`, `Physics`, `Input Map`, `Export Presets`, а также действия `Save Apply` и `Revert`.

JSON analysis содержит paths сохранённых файлов, bounds секций, section labels, clickable control count, text overflow count, forbidden UI matches и факт показа frame в real window.

## Ограничения

- В текущей задаче реализован проверяемый Editor UI smoke и внутренняя write-through модель. Полноценное ручное редактирование всех полей через интерактивные widgets будет расширяться следующими editor-задачами.
- UI не выполняет export build, signing, deploy или публикацию. Он только создаёт и сохраняет presets.
- Smoke-команда использует временный проект внутри переданного `<work-root>` и не меняет текущий репозиторный `project.e2d.json`.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectSettingsUiTests" --no-restore -m:1
```

Визуальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --project-settings-smoke .temp\project-settings-ui
```

После запуска агент должен открыть `.temp/project-settings-ui/visual/project-settings-ui.png` и убедиться, что sections находятся в центральной области, текст читаем и не выходит за bounds, `Save Apply`/`Revert` находятся внизу panel, запрещённые `3D`, `AssetLib` и GDScript элементы отсутствуют.
