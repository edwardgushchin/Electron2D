# Settings persistence baseline

Статус: целевая спецификация для `T-0076`.
Обновлено: 2026-06-21.

## Назначение

Electron2D `0.1.0 Preview` должен иметь стабильный файловый контракт для настроек проекта и локальных пользовательских настроек. Этот контракт нужен редактору, export pipeline, шаблону проекта и будущему CLI, но не должен преждевременно расширять публичный runtime API.

`T-0076` фиксирует минимальный внутренний слой сохранения:

- project settings;
- input settings;
- window/display defaults;
- user settings;
- диагностика повреждённых файлов без частичного применения настроек.

## Project settings document

Project settings сохраняются в UTF-8 JSON без BOM и с LF line endings. Базовое имя файла для проектов `0.1.0 Preview` - `project.e2d.json`.

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
