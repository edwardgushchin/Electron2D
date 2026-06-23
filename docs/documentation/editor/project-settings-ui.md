# Project Settings UI редактора

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
