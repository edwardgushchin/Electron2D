# Формат проекта и шаблон `electron2d-empty`

Статус: реализованный AI-ready шаблон.
Задачи: `T-0006`, `T-0148`.
Обновлено: 2026-06-23.

## Где находится шаблон

```text
data/templates/electron2d-empty/
```

Шаблон содержит .NET template metadata, минимальный `.csproj`, `Program.cs`, `Scripts/MainScene.cs`, `project.e2d.json`, `global.json`, `electron2d.lock.json`, пустую сцену `scenes/main.scene.json`, `AGENTS.md`, `.gitignore`, стартовые project-local skills и начальные task documents в `.electron2d/tasks/`.

`project.e2d.json` уже использует текущий project settings формат: `Electron2D.ProjectSettings`, `formatVersion: 1`, имя проекта, версию проекта, engine version, main scene, renderer profile, physics tick rate, пустой `input.actions` и display/window defaults.

`global.json` фиксирует .NET SDK `10.0.101` с `rollForward: latestFeature`. `electron2d.lock.json` фиксирует Electron2D package version, target framework, package metadata, renderer profile, physics backend marker, serialization schema version, export template version и signing policy `referencesOnly`.

Проверка этих файлов описана в [Reproducibility lock и e2d doctor](../project-system/reproducibility-lock-and-doctor.md).

## AI-ready файлы

`AGENTS.md` в пользовательском проекте описывает версию Electron2D, .NET SDK, выбранный renderer profile, команды `e2d validate`, `dotnet build`, `dotnet test`, `e2d run`, `e2d export`, `e2d api compare-godot <type>`, структуру проекта, правила stable UID и запрет ручной правки generated/local-only каталогов.

Шаблон не создаёт `TASKS.md`, `completed-tasks/` или `dev-diary/`. Для пользовательских задач используется `ProjectTaskManager`: начальная доска хранится в `.electron2d/tasks/board.e2tasks`, стартовая задача — в `.electron2d/tasks/welcome.e2task`.

Project-local skills находятся в `.codex/skills/`:

- `electron2d-scene`;
- `electron2d-gameplay-code`;
- `electron2d-resource-import`;
- `electron2d-run-test`;
- `electron2d-export`.

`.gitignore` игнорирует generated/local-only каталоги `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/context/`, `.electron2d/session/` и `.electron2d/user/`, но не игнорирует `.electron2d/tasks/`.

## Создание проекта

Project Manager и `e2d project create` используют общий `ProjectTemplateCreator` из `Electron2D.ProjectSystem`. Он копирует template, удаляет `.template.config/` из созданного проекта, переименовывает `.csproj`, обновляет `project.e2d.json` и `electron2d.lock.json`, переписывает namespace sample-кода, создаёт AI-ready файлы и пытается выполнить `git init`.

Если `git init` успешен, в проекте появляется `.git/`. Если `git` недоступен или завершается ошибкой, проектные файлы остаются созданными, а результат содержит warning diagnostic `E2D-PROJECT-0003`.

CLI форма:

```powershell
e2d project create MyGame --output .\projects --renderer-profile Compatibility --format json
```

JSON-результат содержит `projectName`, `projectPath`, `projectSettingsPath`, `mainScenePath`, `rendererProfile`, `gitInitialized`, `taskBoardPath`, `starterSkillCount` и `agentInstructionsPath`.

## Как проверять

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ProjectTemplate.ps1
```

Команда собирает локальный package `Electron2D.0.1.0-preview`, копирует шаблон во временную директорию, восстанавливает проект из fresh local package source и NuGet.org в изолированный packages folder, собирает и запускает его.

Verifier проверяет output:

```text
Electron2D empty scene loaded: scenes/main.scene.json
Electron2D C# script lifecycle: _EnterTree,_Ready
Electron2D C# script services: tree=True,text=True
```

Verifier также проверяет `AGENTS.md`, `.gitignore`, пять starter skills, `.electron2d/tasks/board.e2tasks`, `.electron2d/tasks/welcome.e2task`, отсутствие `TASKS.md`, `completed-tasks/` и `dev-diary/`, а также то, что `.gitignore` не скрывает `.electron2d/tasks/`.

При запуске из редактора template также понимает переменную процесса `ELECTRON2D_CURRENT_SCENE`. Это относительный project path для запуска выбранной scene без изменения `project.e2d.json`.

## Script sample

`Scripts/MainScene.cs` наследуется от `Electron2D.Node`, переопределяет `_EnterTree()` и `_Ready()`, а в `_Ready()` обращается к `GetTree()` и `RenderingServer`. Это минимальный baseline C# scripting без runtime compilation и без dynamic assembly load.
