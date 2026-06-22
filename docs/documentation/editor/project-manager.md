# Project Manager редактора

Статус: документация реализации для `T-0079` и `T-0148`.
Дата: 2026-06-23.

## Назначение

Project Manager в `Electron2D.Editor` отвечает за первый рабочий путь проекта: создать AI-ready проект из стандартного шаблона, открыть существующий проект, запомнить последние открытые проекты, выбрать renderer profile и заранее проверить доступность .NET SDK.

Это внутренняя часть editor executable. Она не добавляет новые публичные типы в runtime assembly `Electron2D`.

## Создание проекта

Project Manager создаёт проект через общий `ProjectTemplateCreator` из `Electron2D.ProjectSystem`, используя `data/templates/electron2d-empty/`:

- копирует `Electron2D.Empty.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json`, `scenes/main.scene.json`, `README.md`, `AGENTS.md`, `.gitignore`, `.codex/skills/` и `.electron2d/tasks/`;
- пропускает `.template.config/`, потому что это metadata для `dotnet new`, а не часть созданного проекта;
- переименовывает `.csproj` под имя проекта;
- обновляет `project.e2d.json`: `name` и `rendererProfile`;
- обновляет `electron2d.lock.json`: `project.rendererProfile`;
- нормализует namespace sample-кода, чтобы проект с пользовательским именем продолжал собираться.
- создаёт или переписывает project-local `AGENTS.md`, starter skills и начальную доску `ProjectTaskManager`;
- пытается выполнить `git init` в корне нового проекта.

Создание проекта не перезаписывает существующую непустую папку.

Если `git` недоступен, файлы проекта остаются созданными, а create result получает warning diagnostic `E2D-PROJECT-0003`. На машине с доступным `git` после создания существует каталог `.git/`.

Project Manager не создаёт `TASKS.md`, `completed-tasks/` или `dev-diary/` в пользовательском проекте. Canonical task storage находится в `.electron2d/tasks/`.

## Открытие проекта

Открыть можно папку проекта или конкретный `project.e2d.json`. Project Manager читает JSON через тот же внутренний формат настроек, который использует runtime для project settings. Если файл повреждён, содержит неизвестный формат или ссылается на отсутствующую main scene, открытие завершается диагностикой и не меняет список последних проектов.

После успешного открытия путь проекта сохраняется первым в recent projects, а `lastProjectPath` получает тот же путь.

## Clean-machine smoke workflow

Локальная проверка:

```powershell
dotnet run --project src\Electron2D.Editor\Electron2D.Editor.csproj -- --project-manager-smoke .temp\editor-project-manager --user-data-dir .temp\editor-project-manager-user
```

Ожидаемый результат:

```text
Electron2D.Editor project manager smoke passed
ProjectName=ProjectManagerSmoke
RendererProfile=Compatibility
SdkAvailable=True
RecentProjects=1
```

Smoke-режим также выводит абсолютные пути `ProjectPath`, `ProjectSettingsPath`, `MainScenePath` и `UserSettingsPath`, чтобы тесты и CI могли проверить файлы без ручной подготовки окружения.

## Ограничения

- В этой задаче Project Manager реализован как внутренняя логика редактора и smoke-команда. Полноценный экран выбора проектов в интерактивном окне добавляется следующими editor-задачами.
- Project Manager не собирает пользовательский проект; он только проверяет доступность .NET SDK до будущих build/run workflow.

## Проверки

Фокусная проверка:

```powershell
dotnet test tests\Electron2D.Tests.Integration\Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~EditorProjectManagerTests"
```

Полные проверки:

```powershell
powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1
powershell -ExecutionPolicy Bypass -File tools\Run-Tests.ps1
dotnet build src\Electron2D.sln -c Release
```
