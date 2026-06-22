# Формат проекта и шаблон `electron2d-empty`

Статус: целевая спецификация.
Задачи: `T-0006`, `T-0148`.
Обновлено: 2026-06-23.

## Цель

Новый проект Electron2D должен начинаться из минимального шаблона без legacy API. После `T-0044` шаблон также фиксирует минимальную C# script model: обычный class file наследуется от `Node`, компилируется обычной .NET toolchain и получает lifecycle callbacks.

## Шаблон

Canonical template:

```text
data/templates/electron2d-empty/
```

Шаблон должен содержать:

- `.template.config/template.json`
- `Electron2D.Empty.csproj`
- `global.json`
- `electron2d.lock.json`
- `Program.cs`
- `Scripts/MainScene.cs`
- `project.e2d.json`
- `scenes/main.scene.json`
- `README.md`
- `.gitignore`
- `AGENTS.md`
- `.codex/skills/electron2d-scene/SKILL.md`
- `.codex/skills/electron2d-gameplay-code/SKILL.md`
- `.codex/skills/electron2d-resource-import/SKILL.md`
- `.codex/skills/electron2d-run-test/SKILL.md`
- `.codex/skills/electron2d-export/SKILL.md`

`TASKS.md`, `completed-tasks/` и `dev-diary/` не входят в пользовательский шаблон. Эти файлы относятся только к рабочему процессу репозитория Electron2D.

## Минимальный формат проекта

`project.e2d.json` должен быть валидным project settings документом `Electron2D.ProjectSettings` и указывать:

- `format`
- `formatVersion`
- `name`
- `version`
- `engineVersion`
- `mainScene`
- `rendererProfile`
- `physicsTicksPerSecond`
- `input.actions`
- `display`

`global.json` и `electron2d.lock.json` должны соответствовать [Reproducibility lock и `e2d doctor`](../project-system/reproducibility-lock-and-doctor.md). Шаблон фиксирует .NET SDK, версию Electron2D, target framework, package metadata, renderer profile, physics backend marker, serialization schema version, export template version и signing policy без секретных значений.

`scenes/main.scene.json` должен описывать пустую сцену без legacy component-полей.

## AI-ready project surface

Каждый новый проект, созданный через Project Manager или `e2d project create`, получает предсказуемую рабочую поверхность для coding agents без копирования приватных пользовательских правил.

### Project-local `AGENTS.md`

`AGENTS.md` создаётся в корне проекта и должен описывать:

- текущую версию Electron2D и требуемую версию .NET;
- выбранный renderer profile;
- команды `e2d validate`, `dotnet build`, `dotnet test`, `e2d run`, `e2d export` и `e2d api compare-godot <type>`;
- структуру проекта: `project.e2d.json`, `scenes/`, `Scripts/`, `.electron2d/tasks/`, `.electron2d/import-cache/`, `.electron2d/workspaces/`, `.electron2d/user/`;
- запрет редактировать `.electron2d/import-cache/` и другие generated/local-only каталоги вручную;
- правило сохранять stable UID и проверять проект через `e2d validate`;
- предупреждение не использовать внешний API вне утверждённого Electron2D 2D-профиля;
- правило подключаться к активной Editor-сессии, если она открыта;
- правило использовать `ProjectTaskManager` через Editor, Tooling или MCP, а не прямую правку task storage files;
- правило отправлять завершённую агентом работу на человеческую приёмку через `task_submit_for_acceptance`.

`AGENTS.md` не должен упоминать repository-local `TASKS.md`, `completed-tasks/` или `dev-diary/` как источник задач пользовательского проекта.

### Starter skills

Шаблон поставляет project-local skills под `.codex/skills/`. Минимальный набор:

- `electron2d-scene` — создание и изменение сцен;
- `electron2d-gameplay-code` — написание C# gameplay-кода;
- `electron2d-resource-import` — импорт ресурсов и работа с UID;
- `electron2d-run-test` — проверка через validate, build, run и scene tests;
- `electron2d-export` — подготовка export presets и проверка package contents.

Каждый `SKILL.md` должен иметь YAML front matter с `name` и `description`, быть самодостаточным и не содержать абсолютных путей, секретов или ссылок на локальный workflow репозитория Electron2D.

### Project tasks storage

Новый проект получает начальную доску `.electron2d/tasks/board.e2tasks` и стартовую задачу `.electron2d/tasks/welcome.e2task`. Эти документы являются `EditorMetadata`: они доступны Editor, Tooling, CLI и MCP, но не являются игровыми ресурсами и не попадают в runtime snapshot или production package contents.

Стартовая задача должна быть в статусе `Backlog`, содержать acceptance criteria для первого запуска проекта и использовать canonical formatter `ProjectTaskSerializer`.

### `.gitignore` и git init

Template `.gitignore` не должен игнорировать `.electron2d/` целиком. Обязательные ignored paths:

```gitignore
.electron2d/import-cache/
.electron2d/workspaces/
.electron2d/context/
.electron2d/session/
.electron2d/user/
```

`.electron2d/tasks/` должен быть явно отслеживаемым по умолчанию: в `.gitignore` не должно быть правила, которое скрывает `.electron2d/tasks/`.

Создание проекта пытается выполнить `git init` в корне нового проекта. Если `git` недоступен или команда завершается ошибкой, создание проекта не откатывает файлы шаблона, но возвращает structured warning с понятным сообщением. Если `git` доступен, после создания должен существовать каталог `.git`.

### CLI `project create`

`e2d project create <name> --output <projects-root> [--renderer-profile Standard|Compatibility|Automatic] --format json` создаёт тот же AI-ready project surface, что и Project Manager.

JSON-результат должен содержать:

- `projectName`;
- `projectPath`;
- `projectSettingsPath`;
- `mainScenePath`;
- `rendererProfile`;
- `gitInitialized`;
- `taskBoardPath`;
- `starterSkillCount`;
- `agentInstructionsPath`.

Команда не должна создавать `TASKS.md`, `completed-tasks/` или `dev-diary/`.

## Верификация

```powershell
powershell -ExecutionPolicy Bypass -File tools/Verify-ProjectTemplate.ps1
```

Verifier должен:

1. собрать локальный package `Electron2D.0.1.0-preview`;
2. создать временный проект из шаблона;
3. восстановить зависимости из локального package source;
4. собрать проект;
5. запустить проект и подтвердить, что пустая сцена найдена;
6. подтвердить, что C# script sample получил `_EnterTree()`/`_Ready()`;
7. подтвердить, что script sample увидел `GetTree()` и `RenderingServer`.
8. подтвердить наличие `AGENTS.md`, `.gitignore`, стартовых skills и `.electron2d/tasks/`.
9. подтвердить отсутствие `TASKS.md`, `completed-tasks/` и `dev-diary/`.
10. подтвердить, что `.gitignore` не исключает `.electron2d/tasks/`.
11. focused integration tests должны проверять Project Manager и CLI `project create`.

## Editor run override

Шаблон должен понимать переменную процесса `ELECTRON2D_CURRENT_SCENE`. Если она задана, проект запускает указанный scene file как относительный путь внутри проекта и не меняет `project.e2d.json`.
