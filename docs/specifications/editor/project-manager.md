# Project Manager редактора

Статус: целевая спецификация для `T-0079`.
Дата: 2026-06-22.

## Цель

`Electron2D.Editor` должен получить первый Project Manager: безопасный путь создания и открытия проектов из чистой рабочей машины. Эта задача закрывает не весь визуальный интерфейс редактора, а проверяемую основу, на которую будут опираться следующие editor-docks и workflow запуска игры.

## Контракт создания проекта

- Project Manager использует canonical template `data/templates/electron2d-empty/`.
- Создание проекта принимает имя проекта, целевую папку и renderer profile.
- Имя проекта используется как имя папки, project display name в `project.e2d.json` и имя `.csproj`.
- Значение namespace в template-коде нормализуется до валидного C# namespace token, если имя проекта содержит пробелы или недопустимые символы.
- `.template.config/` не копируется в созданный проект.
- `project.e2d.json` сохраняет формат `Electron2D.ProjectSettings`, `formatVersion: 1`, `mainScene`, display defaults, input actions и выбранный `rendererProfile`.
- Создание не перезаписывает существующую непустую папку проекта.

## Контракт открытия проекта

- Project Manager принимает путь к папке проекта или путь к `project.e2d.json`.
- Открытие валидирует project settings через тот же внутренний JSON-контракт runtime, который используется шаблоном и проверками настроек.
- Открытие проверяет, что `mainScene` непустой и существует относительно корня проекта.
- Успешно открытый проект добавляется в список последних проектов и становится `lastProjectPath`.
- Неуспешное открытие возвращает диагностический текст и не добавляет путь в recent projects.

## Recent projects

- Список последних проектов хранится в user settings file формата `Electron2D.UserSettings`.
- Project Manager нормализует пути до абсолютного вида, удаляет дубликаты и держит последний открытый проект первым.
- Для `0.1.0 Preview` достаточно хранить до 10 последних проектов.

## SDK check

- Project Manager должен проверять, что команда `dotnet --version` доступна и возвращает версию SDK.
- Результат проверки содержит булево состояние, найденную версию или диагностическое сообщение.
- Smoke-проверка должна явно выводить результат SDK check, чтобы clean-machine workflow видел проблему до попытки собрать пользовательский проект.

## Smoke-режим

Editor executable должен поддерживать аргумент:

```text
--project-manager-smoke <work-root> --user-data-dir <user-data-dir>
```

Smoke-режим должен:

- создать новый проект из template в `<work-root>`;
- открыть созданный проект;
- проверить выбранный renderer profile;
- сохранить recent projects в `<user-data-dir>`;
- выполнить SDK check;
- вернуть exit code `0`;
- вывести machine-readable строки с путём проекта, именем проекта, renderer profile, состоянием SDK и количеством recent projects.

## Приемочные критерии

- Integration test запускает `dotnet run --project src/Electron2D.Editor/Electron2D.Editor.csproj -- --project-manager-smoke ...` и подтверждает создание, открытие, recent projects, renderer profile и SDK check.
- Документация clean-machine workflow описывает команду smoke-проверки и ожидаемый результат.
- `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` проходит.
- `powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1` проходит.
- `dotnet build src\Electron2D.sln -c Release` проходит.
