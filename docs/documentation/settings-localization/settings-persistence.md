# Settings persistence baseline

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

`templates/electron2d-empty/project.e2d.json` уже использует этот формат. `Program.cs` в шаблоне по-прежнему читает `mainScene` из root object, поэтому template verifier сохраняет прежний запуск пустой сцены.

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
