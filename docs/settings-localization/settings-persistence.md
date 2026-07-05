# Settings persistence baseline

Обновлено: 2026-06-23.

Этот файл является единым доменным документом. Он заменяет прежнее разделение на отдельную спецификацию и отдельную документацию реализации: требования, фактическое состояние, ограничения и проверки ведутся здесь вместе.

## Ведение документа

- Перед изменением домена обновите раздел с контрактом и ожидаемым поведением.
- Затем добавьте красные тесты, реализуйте изменение, проверьте зеленые тесты и обновите этот документ, если фактическое поведение или ограничения изменились.
- Если требования и реализация расходятся, не создавайте второй документ; зафиксируйте решение и актуальное состояние в этом файле.

## Контракт и ожидаемое поведение

Статус: целевая спецификация для `T-0076`.
Обновлено: 2026-06-21.

## Назначение

Electron2D `0.1-preview` должен иметь стабильный файловый контракт для настроек проекта и локальных пользовательских настроек. Этот контракт нужен редактору, export pipeline, шаблону проекта и будущему CLI, но не должен преждевременно расширять публичный runtime API.

`T-0076` фиксирует минимальный внутренний слой сохранения:

- project settings;
- input settings;
- window/display defaults;
- user settings;
- диагностика повреждённых файлов без частичного применения настроек.

## Project settings document

Project settings сохраняются в UTF-8 JSON без BOM и с LF line endings. Базовое имя файла для проектов `0.1-preview` - `project.e2d.json`.

Root object содержит:

- `format`: строка `Electron2D.ProjectSettings`;
- `formatVersion`: число `1`;
- `name`: имя проекта;
- `version`: версия пользовательского проекта;
- `engineVersion`: версия Electron2D, для которой создан файл;
- `mainScene`: путь к главной сцене относительно корня проекта;
- `rendererProfile`: `Compatibility`, `Standard` или `Automatic`;
- `physicsTicksPerSecond`: положительное целое число;
- `input`: блок action settings;
- `display`: блок window/display defaults.

`input.actions` использует тот же action snapshot, который уже поддерживает `InputMap`: action name, deadzone и persistable input events. Поддерживаются keyboard, mouse button, gamepad button и gamepad axis bindings.

`display` содержит:

- `windowWidth` и `windowHeight`: положительные размеры logical window;
- `fullscreen`: boolean;
- `dpiScale`: положительное конечное число;
- `stretchMode`: `Disabled`, `CanvasItems` или `Viewport`;
- `stretchAspect`: `Ignore`, `Keep`, `KeepWidth`, `KeepHeight` или `Expand`;
- `stretchScaleMode`: `Fractional` или `Integer`;
- `stretchScale`: положительное конечное число;
- `orientation`: значение `DisplayServer.ScreenOrientation`;
- `safeArea`: объект `x`, `y`, `width`, `height` для последнего известного display safe area.

## User settings document

User settings сохраняются отдельно от project settings. Файл выбирает вызывающий слой: редактор может хранить его рядом с project workspace или в пользовательской директории приложения.

Root object содержит:

- `format`: строка `Electron2D.UserSettings`;
- `formatVersion`: число `1`;
- `locale`: нормализованная locale string для `TranslationServer`;
- `lastProjectPath`: последний открытый проект или пустая строка;
- `recentProjects`: стабильный список путей без пустых значений и дублей;
- `window`: локальное состояние окна редактора: `x`, `y`, `width`, `height`, `maximized`.

## Fail-closed загрузка

Загрузка настроек не должна мутировать runtime state до полной валидации документа.

Если JSON повреждён, версия формата не поддерживается, обязательное поле отсутствует или значение недопустимо, loader возвращает failure result с диагностикой:

- `Code`: стабильный код ошибки;
- `Message`: человекочитаемое объяснение;
- `Path`: путь файла, если он известен.

В failure case:

- `InputMap` не заменяется;
- текущая locale не меняется;
- текущая display state не меняется;
- предыдущие настройки пользователя не перезаписываются;
- вызывающий слой может продолжить работу с defaults или уже загруженным состоянием.

## Runtime application

После успешной загрузки project settings внутренний слой может применить проверенный snapshot:

- заменить actions в `InputMap`;
- передать orientation и safe area в `DisplayServer`;
- построить `ViewportPresentationSettings` из display defaults;
- передать renderer profile, main scene и physics tick rate будущему editor/export runtime.

`T-0076` не добавляет публичный `Window` API и не создаёт UI Project Settings. Эти части остаются задачами редактора.

## Критерии приёмки

- Integration tests подтверждают round-trip project settings: project identity, input actions, renderer profile, physics tick, display/window defaults.
- Integration tests подтверждают round-trip user settings: locale, last project, recent projects и window state.
- Integration tests подтверждают, что corrupted project settings возвращают diagnostics и не заменяют текущий `InputMap`.
- Integration tests подтверждают, что corrupted user settings возвращают diagnostics и не меняют текущую locale.
- Implementation documentation описывает фактические JSON fields, ограничения и команды проверки.

## Фактическое состояние, ограничения и проверки

Текущая реализация добавляет внутренний слой сохранения настроек. Внутренний слой означает код движка и будущего редактора, доступный тестам и toolchain через assembly internals, но не являющийся public runtime API для игры.

## Project settings

Project settings сохраняются в `project.e2d.json`. Файл является UTF-8 JSON документом:

- `format`: `Electron2D.ProjectSettings`;
- `formatVersion`: `1`;
- `name`: имя проекта;
- `version`: версия пользовательского проекта;
- `engineVersion`: версия Electron2D;
- `mainScene`: путь к главной сцене;
- `rendererProfile`: `Automatic`, `Compatibility` или `Standard`;
- `physicsTicksPerSecond`: положительное число fixed physics ticks в секунду;
- `input.actions`: сохранённые `InputMap` actions;
- `display`: window/display defaults.

`data/templates/electron2d-empty/project.e2d.json` уже использует этот формат. `Program.cs` в шаблоне по-прежнему читает `mainScene` из root object, поэтому template verifier сохраняет прежний запуск пустой сцены.

## Input settings

Project settings переиспользует текущий `InputMapProjectSettings` serializer. Это сохраняет один формат action bindings:

- action name;
- deadzone;
- keyboard bindings;
- mouse button bindings;
- gamepad button bindings;
- gamepad axis bindings.

`Electron2DProjectSettings.Capture(...)` снимает snapshot текущего `InputMap`. `Electron2DSettingsStore.SaveProject(...)` записывает его в `input.actions`. После успешного `LoadProject(...)` вызывающий код может применить snapshot через `ApplyToRuntime()` или сразу вызвать `LoadProjectAndApply(...)`.

## Display/window settings

`display` сохраняет:

- `windowWidth` и `windowHeight`;
- `fullscreen`;
- `dpiScale`;
- `stretchMode`;
- `stretchAspect`;
- `stretchScaleMode`;
- `stretchScale`;
- `orientation`;
- `safeArea`.

При применении runtime получает только то, что уже имеет существующую внутреннюю точку применения: orientation и safe area передаются в `DisplayServer`. Window creation, real fullscreen switch и UI Project Settings остаются задачами редактора/export pipeline.

## User settings

User settings сохраняются отдельно от project settings через `Electron2DSettingsStore.SaveUser(...)`. Путь выбирает вызывающий слой.

Файл содержит:

- `format`: `Electron2D.UserSettings`;
- `formatVersion`: `1`;
- `locale`: нормализованная locale string;
- `lastProjectPath`;
- `recentProjects`: список без пустых строк и дублей;
- `window`: `x`, `y`, `width`, `height`, `maximized`.

`LoadUserAndApply(...)` после успешной полной загрузки применяет locale через `TranslationServer.SetLocale(...)`.

## Fail-closed diagnostics

Load methods возвращают `Electron2DSettingsLoadResult<T>`.

Успешная загрузка:

- `Succeeded == true`;
- `Settings` содержит проверенный snapshot;
- `Diagnostics` пустой.

Ошибка загрузки:

- `Succeeded == false`;
- `Settings == null`;
- `Diagnostics` содержит stable code, message и path;
- runtime state не мутируется.

Коды диагностики текущего baseline:

- `settings.malformed_json` - JSON невозможно разобрать;
- `settings.invalid_value` - формат, версия, обязательное поле или значение не проходят проверку;
- `settings.io_error` - файл нельзя прочитать из-за I/O или прав доступа.

## Ограничения

- Public `ProjectSettings` API не добавлен.
- Public `Window` API не добавлен.
- Project Settings UI, Input Map UI и user settings path policy остаются задачами редактора.
- `LoadProject(...)` только валидирует и возвращает snapshot; runtime state меняет `ApplyToRuntime()` или `LoadProjectAndApply(...)`.

## Проверки

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~SettingsPersistenceTests" --no-restore -m:1
powershell -ExecutionPolicy Bypass -File tools\Verify-ProjectTemplate.ps1
```
